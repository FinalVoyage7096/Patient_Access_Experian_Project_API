using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patient_Access_Experian_Project_API.Data;
using Patient_Access_Experian_Project_API.Data_Transfer_Objects;

namespace Patient_Access_Experian_Project_API.Controllers
{
    [ApiController]
    [Route("api/patients")]
    public class PatientsController : ControllerBase
    {
        private readonly PatientAccessDbContext _db;

        public PatientsController(PatientAccessDbContext db) => _db = db;

        /// <summary>
        /// Returns a list of patients with their ID and name.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<PatientSummaryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PatientSummaryDto>>> GetPatients(CancellationToken ct)
        {
            var patients = await _db.Patients
                .AsNoTracking()
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .Select(p => new PatientSummaryDto(p.Id, p.FirstName, p.LastName))
                .ToListAsync(ct);

            return Ok(patients);
        }
    }
}