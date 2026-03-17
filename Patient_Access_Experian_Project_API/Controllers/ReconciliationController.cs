using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patient_Access_Experian_Project_API.Data;
using Patient_Access_Experian_Project_API.Data_Transfer_Objects;
using Patient_Access_Experian_Project_API.Data_Transfer_Objects.Reconciliation;
using Patient_Access_Experian_Project_API.Models;

namespace Patient_Access_Experian_Project_API.Controllers
{
    [ApiController]
    [Route("api/reconciliation")]
    public class ReconciliationController : ControllerBase
    {
        private readonly PatientAccessDbContext _db;
        public ReconciliationController(PatientAccessDbContext db) => _db = db;

        /// <summary>
        /// Returns claim-level reconciliation KPIs for a time range and optional clinic/payer filters.
        /// </summary>
        [HttpGet("claims-summary")]
        [ProducesResponseType(typeof(ClaimsSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ClaimsSummary(
            [FromQuery] Guid? clinicId,
            [FromQuery] Guid? payerId,
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc,
            CancellationToken ct = default)
        {
            if (fromUtc.HasValue && fromUtc.Value.Kind != DateTimeKind.Utc)
                return BadRequest("fromUtc must be a UTC DateTime (DateTimeKind.Utc).");

            if (toUtc.HasValue && toUtc.Value.Kind != DateTimeKind.Utc)
                return BadRequest("toUtc must be a UTC DateTime (DateTimeKind.Utc).");

            if (fromUtc.HasValue && toUtc.HasValue && fromUtc.Value >= toUtc.Value)
                return BadRequest("fromUtc must be earlier than toUtc.");

            var q = _db.Claims.AsNoTracking().AsQueryable();

            if (clinicId.HasValue) q = q.Where(c => c.ClinicId == clinicId.Value);
            if (payerId.HasValue) q = q.Where(c => c.PayerId == payerId.Value);

            if (fromUtc.HasValue) q = q.Where(c => c.CreatedUtc >= fromUtc.Value);
            if (toUtc.HasValue) q = q.Where(c => c.CreatedUtc < toUtc.Value);

            // Aggregations
            var agg = await q
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    ClaimsCreated = g.Count(),
                    ClaimsSubmitted = g.Sum(x => x.Status == ClaimStatus.Submitted ? 1 : 0),
                    ClaimsPaid = g.Sum(x => x.Status == ClaimStatus.Paid ? 1 : 0),
                    ClaimsDenied = g.Sum(x => x.Status == ClaimStatus.Denied ? 1 : 0),

                    TotalCharge = g.Sum(x => x.TotalCharge),
                    TotalAllowed = g.Sum(x => x.AllowedAmount ?? 0m),
                    TotalPayerPaid = g.Sum(x => x.PayerPaid ?? 0m),
                    TotalPatientResp = g.Sum(x => x.PatientResponsibility ?? 0m),
                })
                .FirstOrDefaultAsync(ct);

            // When there are no rows, aggregations is null; return zeros
            var created = agg?.ClaimsCreated ?? 0;
            var denied = agg?.ClaimsDenied ?? 0;
            var denialRate = created == 0 ? 0m : Math.Round((decimal)denied / created, 4);

            double? avgDaysToPay = null;

            var paidDates = await q
                .Where(x => x.Status == ClaimStatus.Paid)
                .Select(x => new { x.CreatedUtc, x.UpdatedUtc })
                .ToListAsync(ct);

            if (paidDates.Count > 0)
            {
                avgDaysToPay = paidDates.Average(x => (x.UpdatedUtc - x.CreatedUtc).TotalDays);
            }

            var dto = new ClaimsSummaryDto(
                clinicId, payerId, fromUtc, toUtc,
                ClaimsCreated: created,
                ClaimsSubmitted: agg?.ClaimsSubmitted ?? 0,
                ClaimsPaid: agg?.ClaimsPaid ?? 0,
                ClaimsDenied: denied,
                DenialRate: denialRate,
                TotalCharge: agg?.TotalCharge ?? 0m,
                TotalAllowed: agg?.TotalAllowed ?? 0m,
                TotalPayerPaid: agg?.TotalPayerPaid ?? 0m,
                TotalPatientResponsibility: agg?.TotalPatientResp ?? 0m,
                AvgDaysToPay: avgDaysToPay
            );

            return Ok(dto);
        }
    }
}