using Microsoft.AspNetCore.Mvc;
using Patient_Access_Experian_Project_API.Data_Transfer_Objects;
using Patient_Access_Experian_Project_API.Services;
using Microsoft.EntityFrameworkCore;
using Patient_Access_Experian_Project_API.Data;

namespace Patient_Access_Experian_Project_API.Controllers
{
    [ApiController]
    [Route("api/coverage")]
    public class CoverageController : ControllerBase
    {
        private readonly CoverageService _service;
        private readonly PatientAccessDbContext _db;
        public CoverageController(CoverageService service, PatientAccessDbContext db)
        {
            _service = service;
            _db = db;
        }
            

        /// <summary>
        /// Checks the eligibility of a coverage request and returns the result as an HTTP response.
        /// </summary>
        /// <remarks>This method handles various error scenarios and returns appropriate HTTP status codes
        /// based on the outcome of the eligibility check. Use this endpoint to determine whether a coverage request
        /// meets eligibility criteria.</remarks>
        /// <param name="request">The coverage eligibility request containing the necessary information to evaluate eligibility.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the eligibility check operation.</param>
        /// <returns>An IActionResult containing the eligibility response. Returns a 200 OK result with coverage details if
        /// successful, a 400 Bad Request for invalid requests or errors, or a 404 Not Found if the requested coverage
        /// cannot be located.</returns>
        [HttpPost("eligibility")]
        [ProducesResponseType(typeof(CoverageEligibilityResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Eligibility([FromBody] CoverageEligibilityRequest request, CancellationToken ct)
        {
            var (success, error, response) = await _service.CheckEligibilityAsync(request, ct);

            if (!success)
            {
                if (string.IsNullOrWhiteSpace(error))
                    return Problem(title: "Eligibility check failed.", statusCode: StatusCodes.Status400BadRequest);

                if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    return Problem(title: "Not Found", detail: error, statusCode: StatusCodes.Status404NotFound);

                return Problem(title: "Bad Request", detail: error, statusCode: StatusCodes.Status400BadRequest);
            }

            return Ok(response);
        }

        /// <summary>
        /// Retrieves a list of coverage log items filtered by optional patient ID and date range.
        /// </summary>
        /// <remarks>The logs are returned in descending order by creation date. Filtering parameters are
        /// optional; omitting them returns all logs up to the specified limit.</remarks>
        /// <param name="patientId">The optional patient ID to filter the logs. If provided, only logs associated with this patient will be
        /// returned.</param>
        /// <param name="fromUtc">The optional start date and time in UTC to filter the logs. Only logs created on or after this date will be
        /// included.</param>
        /// <param name="toUtc">The optional end date and time in UTC to filter the logs. Only logs created before this date will be
        /// included.</param>
        /// <param name="take">The maximum number of log items to return. Must be between 1 and 200, inclusive. Defaults to 50 if not
        /// specified.</param>
        /// <param name="ct">A cancellation token to observe while waiting for the asynchronous operation to complete.</param>
        /// <returns>An IActionResult containing a list of coverage log items that match the specified filters. Returns an empty
        /// list if no logs are found.</returns>
        [HttpGet("logs")]
        [ProducesResponseType(typeof(List<CoverageLogItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetLogs(
            [FromQuery] int take = 50,
            [FromQuery] int skip = 0,
            [FromQuery] Guid? patientId = null,
            [FromQuery] Guid? providerId = null,
            [FromQuery] Guid? clinicId = null,
            [FromQuery] DateTime? fromUtc = null,
            [FromQuery] DateTime? toUtc = null,
            CancellationToken ct = default)
        {
            // Basic validation for date range + UTC
            if (fromUtc.HasValue && fromUtc.Value.Kind != DateTimeKind.Utc)
                return BadRequest("fromUtc must be a UTC DateTime (DateTimeKind.Utc).");

            if (toUtc.HasValue && toUtc.Value.Kind != DateTimeKind.Utc)
                return BadRequest("toUtc must be a UTC DateTime (DateTimeKind.Utc).");

            if (fromUtc.HasValue && toUtc.HasValue && fromUtc.Value >= toUtc.Value)
                return BadRequest("fromUtc must be earlier than toUtc.");

            IQueryable<Models.CoverageCheckLog> query = _db.CoverageCheckLogs.AsNoTracking();

            if (patientId.HasValue)
                query = query.Where(x => x.PatientId == patientId.Value);

            if (providerId.HasValue)
                query = query.Where(x => x.ProviderId == providerId.Value);

            if (clinicId.HasValue)
                query = query.Where(x => x.ClinicId == clinicId.Value);

            if (fromUtc.HasValue)
                query = query.Where(x => x.CreatedUtc >= fromUtc.Value);

            if (toUtc.HasValue)
                query = query.Where(x => x.CreatedUtc < toUtc.Value);

            skip = Math.Max(skip, 0);
            take = Math.Clamp(take, 1, 200);

            var items = await query
                .OrderByDescending(x => x.CreatedUtc) // ORDER FIRST
                .Skip(skip)                           // THEN SKIP
                .Take(take)                           // THEN TAKE
                .Select(x => new CoverageLogItemDto(
                    x.Id, // referenceId
                    x.PatientId,
                    x.ClinicId,
                    x.ProviderId,
                    x.ServiceCode,
                    x.ScheduledStartUtc,
                    x.Eligible,
                    x.CoverageStatus,
                    x.Copay,
                    x.DeductibleRemaining,
                    x.EstimatedPatientResponsibility,
                    x.CreatedUtc
                ))
                .ToListAsync(ct);

            return Ok(items);
        }
    }
}
