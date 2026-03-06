using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patient_Access_Experian_Project_API.Data;
using Patient_Access_Experian_Project_API.Data_Transfer_Objects;


namespace Patient_Access_Experian_Project_API.Controllers
{
    [ApiController]
    [Route("api/providers")]

    public class ProvidersController : ControllerBase
    {
        private readonly PatientAccessDbContext _db;
        public ProvidersController(PatientAccessDbContext db) => _db = db;


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
       
    }
}
