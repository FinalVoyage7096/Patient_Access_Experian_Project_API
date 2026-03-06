using Microsoft.AspNetCore.Mvc;
using Patient_Access_Experian_Project_API.Data_Transfer_Objects;
using Patient_Access_Experian_Project_API.Services;

namespace Patient_Access_Experian_Project_API.Controllers
{
    [ApiController]
    [Route("api/coverage")]
    public class CoverageController : ControllerBase
    {
        private readonly CoverageService _service;
        public CoverageController(CoverageService service) => _service = service;

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
                if (error is null) return BadRequest("Unknown error.");

                if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    return NotFound(error);

                return BadRequest(error);
            }

            return Ok(response);
        }
    }
}
