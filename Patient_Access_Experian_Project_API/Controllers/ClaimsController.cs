using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patient_Access_Experian_Project_API.Data;
using Patient_Access_Experian_Project_API.Data_Transfer_Objects;
using Patient_Access_Experian_Project_API.Data_Transfer_Objects.Reconciliation;
using Patient_Access_Experian_Project_API.Models;

namespace Patient_Access_Experian_Project_API.Controllers
{
    [ApiController]
    [Route("api/claims")]
    public class ClaimsController : ControllerBase
    {
        private readonly PatientAccessDbContext _db;
        public ClaimsController(PatientAccessDbContext db) => _db = db;

        /// <summary>
        /// Creates a new claim with the specified details and returns a summary of the created claim.
        /// </summary>
        /// <remarks>The method validates the existence of referenced entities and the integrity of
        /// service line data before creating the claim. Returns appropriate error responses for invalid input or
        /// missing entities.</remarks>
        /// <param name="request">The request object containing the details of the claim to create, including clinic, patient, provider,
        /// payer, and service line information. Must not be null, and must include at least one valid service line.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A 201 Created response containing a summary of the newly created claim if successful; otherwise, a 400 Bad
        /// Request or 404 Not Found response if validation fails or related entities are not found.</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateClaimRequest request, CancellationToken ct)
        {
            if (!await _db.Clinics.AnyAsync(x => x.Id == request.ClinicId, ct)) return NotFound("Clinic not found.");
            if (!await _db.Patients.AnyAsync(x => x.Id == request.PatientId, ct)) return NotFound("Patient not found.");
            if (!await _db.Providers.AnyAsync(x => x.Id == request.ProviderId, ct)) return NotFound("Provider not found.");
            if (!await _db.Payers.AnyAsync(x => x.Id == request.PayerId, ct)) return NotFound("Payer not found.");

            if (request.ServiceLines is null || request.ServiceLines.Count == 0)
                return BadRequest("At least one service line is required.");

            var claim = new Claim
            {
                Id = Guid.NewGuid(),
                ClinicId = request.ClinicId,
                PatientId = request.PatientId,
                ProviderId = request.ProviderId,
                PayerId = request.PayerId,
                AppointmentId = request.AppointmentId,
                Status = ClaimStatus.Draft,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            };

            foreach (var line in request.ServiceLines)
            {
                if (string.IsNullOrWhiteSpace(line.ServiceCode)) return BadRequest("ServiceCode is required.");
                if (line.Units <= 0) return BadRequest("Units must be >= 1.");
                if (line.ChargeAmount <= 0) return BadRequest("ChargeAmount must be > 0.");

                claim.ServiceLines.Add(new ClaimServiceLine
                {
                    Id = Guid.NewGuid(),
                    ServiceCode = line.ServiceCode.Trim().ToUpperInvariant(),
                    Units = line.Units,
                    ChargeAmount = line.ChargeAmount
                });
            }

            claim.TotalCharge = claim.ServiceLines.Sum(l => l.Units * l.ChargeAmount);

            _db.Claims.Add(claim);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetById), new { claimId = claim.Id }, new ClaimSummaryDto(
                claim.Id, claim.ClinicId, claim.PatientId, claim.ProviderId, claim.PayerId,
                claim.Status.ToString(), claim.TotalCharge, claim.CreatedUtc
            ));
        }

        /// <summary>
        /// Retrieves a paginated list of claim summaries filtered by clinic, payer, status, and creation date range.
        /// </summary>
        /// <remarks>The date range is exclusive of the end date. Both fromUtc and toUtc, if specified,
        /// must be in UTC and fromUtc must be earlier than toUtc. The status filter is case-insensitive and must
        /// correspond to a valid claim status value.</remarks>
        /// <param name="clinicId">The unique identifier of the clinic to filter claims by. If null, claims from all clinics are included.</param>
        /// <param name="payerId">The unique identifier of the payer to filter claims by. If null, claims from all payers are included.</param>
        /// <param name="status">The status value to filter claims by. Must match a valid claim status. If null or empty, claims of all
        /// statuses are included.</param>
        /// <param name="fromUtc">The start of the creation date range, in UTC. Only claims created on or after this date are included. Must
        /// be a UTC DateTime if specified.</param>
        /// <param name="toUtc">The end of the creation date range, in UTC. Only claims created before this date are included. Must be a UTC
        /// DateTime if specified.</param>
        /// <param name="take">The maximum number of claim summaries to return. Must be between 1 and 200. Defaults to 25.</param>
        /// <param name="skip">The number of claim summaries to skip before starting to collect the result set. Must be zero or greater.
        /// Defaults to 0.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>An IActionResult containing a list of claim summaries that match the specified filters with pagination
        /// applied. Returns a 200 OK response with the results, or a 400 Bad Request response if any filter is invalid.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<ClaimSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> List(
            [FromQuery] Guid? clinicId,
            [FromQuery] Guid? payerId,
            [FromQuery] string? status,
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc,
            [FromQuery] int take = 25,
            [FromQuery] int skip = 0,
            CancellationToken ct = default)
        {
            take = Math.Clamp(take, 1, 200);
            skip = Math.Max(skip, 0);

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

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<ClaimStatus>(status, ignoreCase: true, out var parsed))
                    return BadRequest("Invalid status filter.");

                q = q.Where(c => c.Status == parsed);
            }

            var results = await q
                .OrderByDescending(c => c.CreatedUtc)
                .Skip(skip)
                .Take(take)
                .Select(c => new ClaimSummaryDto(
                    c.Id,
                    c.ClinicId,
                    c.PatientId,
                    c.ProviderId,
                    c.PayerId,
                    c.Status.ToString(),
                    c.TotalCharge,
                    c.CreatedUtc
                ))
                .ToListAsync(ct);

            return Ok(results);
        }

        /// <summary>
        /// Retrieves the details of a claim by its unique identifier.
        /// </summary>
        /// <param name="claimId">The unique identifier of the claim to retrieve.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>An <see cref="IActionResult"/> containing the claim details if found; otherwise, a NotFound result.</returns>
        [HttpGet("{claimId:guid}")]
        public async Task<IActionResult> GetById(Guid claimId, CancellationToken ct)
        {
            var claim = await _db.Claims.AsNoTracking()
                .Include(c => c.ServiceLines)
                .FirstOrDefaultAsync(c => c.Id == claimId, ct);

            if (claim is null) return NotFound();

            return Ok(new ClaimDetailDto(
                claim.Id, claim.ClinicId, claim.PatientId, claim.ProviderId, claim.PayerId,
                claim.Status.ToString(), claim.TotalCharge,
                claim.AllowedAmount, claim.PayerPaid, claim.PatientResponsibility,
                claim.DenialReasonCode,
                claim.CreatedUtc,
                claim.ServiceLines.Select(l => new CreateClaimServiceLineDto(l.ServiceCode, l.Units, l.ChargeAmount)).ToList()
            ));
        }

        /// <summary>
        /// Submits the specified claim for processing using an idempotency key provided in the request header.
        /// </summary>
        /// <remarks>The request must include an 'Idempotency-Key' header. Submitting the same claim with
        /// the same idempotency key is idempotent and will not result in duplicate submissions. If the claim has
        /// already been submitted with a different idempotency key, a conflict response is returned.</remarks>
        /// <param name="claimId">The unique identifier of the claim to submit.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>An IActionResult indicating the result of the submission. Returns 200 OK with claim status if successful,
        /// 400 Bad Request if the idempotency key is missing, 404 Not Found if the claim does not exist, or 409
        /// Conflict if the claim has already been submitted with a different idempotency key.</returns>
        [HttpPost("{claimId:guid}/submit")]
        public async Task<IActionResult> Submit(Guid claimId, CancellationToken ct)
        {
            var idemKey = Request.Headers["Idempotency-Key"].ToString().Trim();
            if (string.IsNullOrWhiteSpace(idemKey))
                return BadRequest("Idempotency-Key header is required.");

            var claim = await _db.Claims
                .FirstOrDefaultAsync(c => c.Id == claimId, ct);

            if (claim is null) return NotFound();

            // already submitted with same key => idempotent success
            if (claim.SubmissionIdempotencyKey == idemKey && claim.Status == ClaimStatus.Submitted)
                return Ok(new { claim.Id, Status = claim.Status.ToString() });

            // submitted with different key => conflict
            if (claim.Status != ClaimStatus.Draft && claim.SubmissionIdempotencyKey != idemKey)
                return Conflict("Claim already submitted.");

            // status changes
            claim.Status = ClaimStatus.Submitted;
            claim.SubmissionIdempotencyKey = idemKey;
            claim.UpdatedUtc = DateTime.UtcNow;

            // write ledger row
            _db.ClaimTransactions.Add(new ClaimTransaction
            {
                Id = Guid.NewGuid(),
                ClaimId = claim.Id,
                Type = ClaimTransactionType.Submit,
                Amount = 0m,
                Reference = idemKey,
                CreatedUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
            return Ok(new { claim.Id, Status = claim.Status.ToString() });
        }

        /// <summary>
        /// Adjudicates a submitted claim by evaluating its service lines and updating its status to either Paid or
        /// Denied based on predefined business rules.
        /// </summary>
        /// <remarks>A claim is denied if any of its service codes start with the letter 'X'; otherwise,
        /// the claim is paid with an allowed amount and payment split between payer and patient. The method updates the
        /// claim status and records related transactions. Only claims in the Submitted status can be
        /// adjudicated.</remarks>
        /// <param name="claimId">The unique identifier of the claim to adjudicate.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the adjudication operation.</param>
        /// <returns>An IActionResult containing the adjudication outcome. Returns 404 Not Found if the claim does not exist, 400
        /// Bad Request if the claim is not in the Submitted status, or 200 OK with the adjudication result if
        /// successful.</returns>
        [HttpPost("{claimId:guid}/adjudicate")]
        public async Task<IActionResult> Adjudicate(Guid claimId, CancellationToken ct)
        {
            var claim = await _db.Claims
                .Include(c => c.ServiceLines)
                .FirstOrDefaultAsync(c => c.Id == claimId, ct);

            if (claim is null) return NotFound();
            if (claim.Status != ClaimStatus.Submitted)
                return BadRequest("Claim must be Submitted before adjudication.");

            // simple deterministic logic:
            // deny if any service code starts with "X"
            var deny = claim.ServiceLines.Any(l => l.ServiceCode.StartsWith("X"));

            if (deny)
            {
                claim.Status = ClaimStatus.Denied;
                claim.DenialReasonCode = "DEMO_DENY";
                claim.AllowedAmount = 0m;
                claim.PayerPaid = 0m;
                claim.PatientResponsibility = claim.TotalCharge;
                claim.UpdatedUtc = DateTime.UtcNow;

                _db.ClaimTransactions.Add(new ClaimTransaction
                {
                    Id = Guid.NewGuid(),
                    ClaimId = claim.Id,
                    Type = ClaimTransactionType.Deny,
                    Amount = 0m,
                    Reference = "DEMO_EOB",
                    CreatedUtc = DateTime.UtcNow
                });

                await _db.SaveChangesAsync(ct);
                return Ok(new { claim.Id, Status = claim.Status.ToString(), claim.DenialReasonCode });
            }

            // otherwise pay 80% allowed, patient 20%
            var allowed = Math.Round(claim.TotalCharge * 0.80m, 2);
            var payerPaid = Math.Round(allowed * 0.80m, 2);
            var patientResp = allowed - payerPaid;

            claim.AllowedAmount = allowed;
            claim.PayerPaid = payerPaid;
            claim.PatientResponsibility = patientResp;
            claim.Status = ClaimStatus.Paid;
            claim.DenialReasonCode = null;
            claim.UpdatedUtc = DateTime.UtcNow;

            _db.ClaimTransactions.Add(new ClaimTransaction
            {
                Id = Guid.NewGuid(),
                ClaimId = claim.Id,
                Type = ClaimTransactionType.Adjust,
                Amount = allowed - claim.TotalCharge,
                Reference = "DEMO_ADJ",
                CreatedUtc = DateTime.UtcNow
            });

            _db.ClaimTransactions.Add(new ClaimTransaction
            {
                Id = Guid.NewGuid(),
                ClaimId = claim.Id,
                Type = ClaimTransactionType.Pay,
                Amount = payerPaid,
                Reference = "DEMO_EFT_TRACE",
                CreatedUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
            return Ok(new { claim.Id, Status = claim.Status.ToString(), Allowed = allowed, PayerPaid = payerPaid, PatientResp = patientResp });
        }

        /// <summary>
        /// Retrieves all transactions associated with the specified claim.
        /// </summary>
        /// <param name="claimId">The unique identifier of the claim for which to retrieve transactions.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>An <see cref="IActionResult"/> containing a list of claim transactions if the claim exists; otherwise, a 404
        /// Not Found response.</returns>
        [HttpGet("{claimId:guid}/transactions")]
        [ProducesResponseType(typeof(List<ClaimTransactionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTransactions(Guid claimId, CancellationToken ct)
        {
            var exists = await _db.Claims.AsNoTracking().AnyAsync(c => c.Id == claimId, ct);
            if (!exists) return NotFound();

            var tx = await _db.ClaimTransactions.AsNoTracking()
                .Where(t => t.ClaimId == claimId)
                .OrderBy(t => t.CreatedUtc)
                .Select(t => new ClaimTransactionDto(
                    t.Id,
                    t.ClaimId,
                    t.Type.ToString(),
                    t.Amount,
                    t.Currency,
                    t.Reference,
                    t.CreatedUtc
                ))
                .ToListAsync(ct);

            return Ok(tx);
        }
    }
}