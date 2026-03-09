using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patient_Access_Experian_Project_API.Data;
using Patient_Access_Experian_Project_API.Data_Transfer_Objects;

namespace Patient_Access_Experian_Project_API.Controllers
{
    [ApiController]
    [Route("api/clinics")]
    public class ClinicsController : ControllerBase
    {
        private readonly PatientAccessDbContext _db;

        public ClinicsController(PatientAccessDbContext db) => _db = db;

        /// <summary>
        /// Returns a list of all clinics with basic details.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<ClinicSummaryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ClinicSummaryDto>>> GetClinics(CancellationToken ct)
        {
            var clinics = await _db.Clinics
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new ClinicSummaryDto(c.Id, c.Name, c.TimeZone))
                .ToListAsync(ct);

            return Ok(clinics);
        }
    }
}
