using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patient_Access_Experian_Project_API.Data;
using Patient_Access_Experian_Project_API.Data_Transfer_Objects;
using Patient_Access_Experian_Project_API.Services;

namespace Patient_Access_Experian_Project_API.Controllers
{
    [ApiController]
    [Route("api/providers")]

    public class ProvidersController : ControllerBase
    {
        private readonly PatientAccessDbContext _db;
        private readonly SlotService _slots;
        public ProvidersController(PatientAccessDbContext db, SlotService slots)
        {
            _db = db;
            _slots = slots;
        }

        /// <summary>
        /// Returns a list of all healthcare providers with their ID, name, and specialty.
        /// </summary>
        /// <returns>Returns a list of all healthcare providers with their ID, name, and specialty.</returns>
        [HttpGet]
        public async Task <ActionResult<List<ProviderSummaryDto>>> GetProviders()
        {
            var providers = await _db.Providers
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => new ProviderSummaryDto(p.Id, p.Name, p.Specialty))
                .ToListAsync();
            return Ok(providers);
        }

        /// <summary>
        /// Returns all info of the requested healthcare provider, including their availability windows.
        /// </summary>
        /// <param name="providerId">The healthcare provider's unique ID (GUID).</param>
        /// <returns>Returns all info of the requested provider.</returns>
        /// <response code="200">Returns the requested healthcare provider details.</response>
        /// <response code="404">If the healthcare provider is not found.</response>
        [HttpGet("{providerId:guid}")]
        public async Task<ActionResult<ProviderDetailDto>> GetProvider(Guid providerId)
        {
            var provider = await _db.Providers
                .AsNoTracking()
                .Where(p => p.Id == providerId)
                .Select(p => new ProviderDetailDto(
                    p.Id,
                    p.Name,
                    p.Specialty,
                    p.AvailabilityWindows
                        .OrderBy(w => w.DayOfWeek)
                        .ThenBy(w => w.StartMinuteOfDay)
                        .Select(w => new AvailabilityWindowDto(w.DayOfWeek, w.StartMinuteOfDay, w.EndMinuteOfDay))
                        .ToList()
                ))
                .FirstOrDefaultAsync();

            return provider is null ? NotFound() : Ok(provider);
        }

        /// <summary>
        /// Returns available appointment slots for a provider in a UTC time range.
        /// Slots are computed from provider availability windows minus existing appointments.
        /// </summary>
        /// <param name="providerId">Provider ID.</param>
        /// <param name="clinicId">Optional clinic filter (limits conflict checks to that clinic).</param>
        /// <param name="fromUtc">Range start (UTC).</param>
        /// <param name="toUtc">Range end (UTC).</param>
        /// <param name="slotMinutes">Slot size in minutes (5..120). Default 30.</param>
        [HttpGet("{providerId:guid}/slots")]
        [ProducesResponseType(typeof(List<AvailableSlotDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSlots(
            Guid providerId,
            [FromQuery] Guid? clinicId,
            [FromQuery] DateTime fromUtc,
            [FromQuery] DateTime toUtc,
            [FromQuery] int slotMinutes = 30,
            CancellationToken ct = default)
        {
            var (success, error, slots) = await _slots.GetProviderSlotsAsync(
                providerId, clinicId, fromUtc, toUtc, slotMinutes, ct);

            if (!success)
            {
                if (string.IsNullOrWhiteSpace(error))
                    return BadRequest("Invalid request.");

                if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    return NotFound(error);

                return BadRequest(error);
            }

            return Ok(slots);
        }
    }
}
