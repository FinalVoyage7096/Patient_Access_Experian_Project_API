using Microsoft.EntityFrameworkCore;
using Patient_Access_Experian_Project_API.Data;
using Patient_Access_Experian_Project_API.Data_Transfer_Objects;
using Patient_Access_Experian_Project_API.Models;

namespace Patient_Access_Experian_Project_API.Services
{
    public class SlotService
    {
        private readonly PatientAccessDbContext _db;
        private readonly ILogger<SlotService> _logger;

        public SlotService(PatientAccessDbContext db, ILogger<SlotService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<(bool Success, string? Error, List<AvailableSlotDto>? Slots)> GetProviderSlotsAsync(
            Guid providerId,
            Guid? clinicId,
            DateTime fromUtc,
            DateTime toUtc,
            int slotMinutes = 30,
            CancellationToken ct = default)
        {
            if (fromUtc.Kind != DateTimeKind.Utc || toUtc.Kind != DateTimeKind.Utc)
                return (false, "fromUtc and toUtc must be UTC DateTimes (DateTimeKind.Utc).", null);

            if (toUtc <= fromUtc)
                return (false, "toUtc must be later than from Utc.", null);

            slotMinutes = Math.Clamp(slotMinutes, 5, 120);

            // Provider exists and load availability windows
            var provider = await _db.Providers
                .AsNoTracking()
                .Include(p => p.AvailabilityWindows)
                .FirstOrDefaultAsync(p => p.Id == providerId, ct);

            if (provider is null)
                return (false, "Provider not found.", null);

            if (clinicId.HasValue)
            {
                var clinicExists = await _db.Clinics.AsNoTracking().AnyAsync(c => c.Id == clinicId.Value, ct);
                if (!clinicExists)
                    return (false, "Clinic not found.", null);
            }

            // Pull existing appointments in range for provider (exclude Cancelled)
            var apptsQuery = _db.Appointments
               .AsNoTracking()
               .Where(a => a.ProviderId == providerId)
               .Where(a => a.Status != AppointmentStatus.Cancelled)
               // overlap range: a.Start < AND from < a.End
               .Where(a => a.StartUtc < toUtc && fromUtc < a.EndUtc);

            if (clinicId.HasValue)
                apptsQuery = apptsQuery.Where(a => a.ClinicId == clinicId.Value);

            var appts = await apptsQuery
                .Select(a => new { a.StartUtc, a.EndUtc })
                .ToListAsync(ct);

            // Build slots day by day in [fromUtc, toUtc)
            var slots = new List<AvailableSlotDto>();

            var dayStart = fromUtc.Date; //midnight UTC of starting day
            var dayEnd = toUtc.Date;

            // iterate days from dayStart to dayEnd inclusive (need to cover partial end day)
            for (var d = dayStart; d <= dayEnd; d = d.AddDays(1))
            {
                var dow = (int)d.DayOfWeek;

                var windows = provider.AvailabilityWindows
                    .Where(w => w.DayOfWeek == dow)
                    .Select(w => new
                    {
                        Start = d.AddMinutes(w.StartMinuteOfDay),
                        End = d.AddMinutes(w.EndMinuteOfDay)
                    })
                    .ToList();

                foreach(var w in windows)
                {
                    // clip window to requested range
                    var windowStart = Max(w.Start, fromUtc);
                    var windowEnd = Min(w.End, toUtc);

                    if (windowEnd <= windowStart) continue;

                    // generate discrete slots
                    for (var s = windowStart; s.AddMinutes(slotMinutes) <= windowEnd; s = s.AddMinutes(slotMinutes))
                    {
                        var e = s.AddMinutes(slotMinutes);

                        // overlap check with existing appointments
                        var overlaps = appts.Any(a => s < a.EndUtc && a.StartUtc < e);
                        if (!overlaps)
                            slots.Add(new AvailableSlotDto(s, e));
                    }
                }



            }

            slots = slots
                .OrderBy(x => x.StartUtc)
                .ToList();

            _logger.LogInformation(
                "Slots computed. ProviderId={ProviderId} ClinicId={ClinicId} FromUtc={FromUtc} ToUtc={ToUtc} SlotMinutes={SlotMinutes} Slots={SlotCount}",
                providerId, clinicId, fromUtc, toUtc, slotMinutes, slots.Count
            );

            return (true, null, slots);
        }

        private static DateTime Max(DateTime a, DateTime b) => a >= b ? a : b;
        private static DateTime Min(DateTime a, DateTime b) => a <= b ? a : b;
    }
}
