using Microsoft.EntityFrameworkCore;
using Patient_Access_Experian_Project_API.Data;
using Patient_Access_Experian_Project_API.Models;

namespace Patient_Access_Experian_Project_API.Services
{
    public class AppointmentService
    {
        private readonly PatientAccessDbContext _db;

        public AppointmentService(PatientAccessDbContext db) => _db = db;

        public async Task<(bool Success, string? Error, Appointment? Appointment)> CreateAsync(
            Guid clinicId,
            Guid providerId,
            Guid patientId,
            DateTime startUtc,
            int durationMinutes,
            CancellationToken ct = default)
        {
            // Basic validation
            if (durationMinutes <= 0 || durationMinutes > 8 * 60)
                return (false, "DurationMinutes must be between 1 and 480.", null);

            if (startUtc.Kind != DateTimeKind.Utc)
                return (false, "StartUtc must be a UTC DateTime (DateTimeKind.Utc).", null);

            var endUtc = startUtc.AddMinutes(durationMinutes);

            // Existence Checks

            var clinicExists = await _db.Clinics.AnyAsync(c => c.Id == clinicId, ct);
            if (!clinicExists) return (false, "Clinic not found.", null);

            var provider = await _db.Providers
                .Include(p => p.AvailabilityWindows)
                .FirstOrDefaultAsync(p => p.Id == providerId, ct);
            if (provider is null) return (false, "Provider not found.", null);

            var patientExists = await _db.Patients.AnyAsync(p => p.Id == patientId, ct);
            if (!patientExists) return (false, "Patient not found.", null);

            // Validate provider availability window (based on UTC day/time
            // For mvp interpret availability windows in UTC and convert  later with Clinic.TimeZone

            var dayOfWeek = (int)startUtc.DayOfWeek; // sunday =0, monday = 1, ..., saturday = 6
            var startMinute = startUtc.Hour * 60 + startUtc.Minute;
            var endMinute = endUtc.Hour * 60 + endUtc.Minute;

            var hasWindow = provider.AvailabilityWindows.Any(w => 
               w.DayOfWeek == dayOfWeek &&
               startMinute >= w.StartMinuteOfDay &&
               endMinute <= w.EndMinuteOfDay);

            if (!hasWindow)
                return (false, "Requested time is outside provider availability.", null);

            // Double-booking prevention (overlap check)
            var hasConflict = await _db.Appointments
                .AnyAsync(a =>
                    a.ProviderId == providerId &&
                    a.Status == AppointmentStatus.Scheduled &&
                    startUtc < a.EndUtc && 
                    endUtc > a.StartUtc, ct);

            if (hasConflict)
                return (false, "Scheduling conflict: provider is already booked for that time.", null);

            // Create an appointment
            var appt = new Appointment
            {
                ClinicId = clinicId,
                ProviderId = providerId,
                PatientId = patientId,
                StartUtc = startUtc,
                EndUtc = endUtc,
                Status = AppointmentStatus.Scheduled,
                CreatedUtc = DateTime.UtcNow
            };

            _db.Appointments.Add(appt);
            await _db.SaveChangesAsync(ct);

            return (true, null, appt);
        }
    }
}
