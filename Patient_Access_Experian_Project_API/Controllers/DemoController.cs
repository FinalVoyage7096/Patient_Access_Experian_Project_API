using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patient_Access_Experian_Project_API.Data;
using Patient_Access_Experian_Project_API.Models;

namespace Patient_Access_Experian_Project_API.Controllers
{
    [ApiController]
    [Route("api/demo")]
    public class DemoController : ControllerBase
    {
        private readonly PatientAccessDbContext _db;
        private readonly ILogger<DemoController> _logger;

        public DemoController(PatientAccessDbContext db, ILogger<DemoController> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Seeds demo claims + ledger transactions for dashboard testing.
        /// Creates a mix of Submitted, Paid, and Denied claims for the given clinic/payer.
        /// </summary>
        [HttpPost("seed-claims")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SeedClaims(
            [FromQuery] Guid clinicId,
            [FromQuery] Guid payerId,
            [FromQuery] int count = 12,
            [FromQuery] int daysBack = 30,
            CancellationToken ct = default)
        {
            count = Math.Clamp(count, 1, 200);
            daysBack = Math.Clamp(daysBack, 1, 365);

            var clinicExists = await _db.Clinics.AsNoTracking().AnyAsync(c => c.Id == clinicId, ct);
            if (!clinicExists) return NotFound("Clinic not found.");

            var payerExists = await _db.Payers.AsNoTracking().AnyAsync(p => p.Id == payerId, ct);
            if (!payerExists) return NotFound("Payer not found.");

            var providers = await _db.Providers.AsNoTracking().Select(p => p.Id).ToListAsync(ct);
            var patients = await _db.Patients.AsNoTracking().Select(p => p.Id).ToListAsync(ct);

            if (providers.Count == 0) return BadRequest("No providers exist. Seed providers first.");
            if (patients.Count == 0) return BadRequest("No patients exist. Seed patients first.");

            var rng = new Random();
            DateTime RandCreatedUtc()
            {
                var start = DateTime.UtcNow.AddDays(-daysBack);
                var spanSeconds = (DateTime.UtcNow - start).TotalSeconds;
                return start.AddSeconds(rng.NextDouble() * spanSeconds);
            }

            // Mix distribution
            // ~45% Paid, ~20% Denied, remainder Submitted
            int paidTarget = (int)Math.Round(count * 0.45);
            int deniedTarget = (int)Math.Round(count * 0.20);
            int submittedTarget = Math.Max(0, count - paidTarget - deniedTarget);

            int created = 0;
            int paid = 0;
            int denied = 0;
            int submitted = 0;

            for (int i = 0; i < count; i++)
            {
                var createdUtc = RandCreatedUtc();
                var providerId = providers[rng.Next(providers.Count)];
                var patientId = patients[rng.Next(patients.Count)];

                var status =
                    paid < paidTarget ? ClaimStatus.Paid :
                    denied < deniedTarget ? ClaimStatus.Denied :
                    ClaimStatus.Submitted;

                if (status == ClaimStatus.Paid) paid++;
                else if (status == ClaimStatus.Denied) denied++;
                else submitted++;

                // Service lines: 1–3
                int lineCount = rng.Next(1, 4);
                var lines = new List<ClaimServiceLine>();

                decimal totalCharge = 0m;
                for (int j = 0; j < lineCount; j++)
                {
                    var serviceCode = status == ClaimStatus.Denied && j == 0
                        ? "X9999"
                        : (rng.Next(0, 3) switch
                        {
                            0 => "99213",
                            1 => "93000",
                            _ => "80053"
                        });

                    int units = rng.Next(1, 3);
                    decimal charge = serviceCode switch
                    {
                        "99213" => 150m,
                        "93000" => 120m,
                        "80053" => 80m,
                        _ => 100m
                    };

                    totalCharge += units * charge;

                    lines.Add(new ClaimServiceLine
                    {
                        Id = Guid.NewGuid(),
                        ServiceCode = serviceCode,
                        Units = units,
                        ChargeAmount = charge
                    });
                }

                var claimId = Guid.NewGuid();
                var claim = new Claim
                {
                    Id = claimId,
                    ClinicId = clinicId,
                    PayerId = payerId,
                    ProviderId = providerId,
                    PatientId = patientId,
                    Status = status == ClaimStatus.Submitted ? ClaimStatus.Submitted : status,
                    TotalCharge = totalCharge,
                    CreatedUtc = createdUtc,
                    UpdatedUtc = status == ClaimStatus.Paid ? createdUtc.AddDays(rng.Next(1, 12)) : createdUtc
                };

                // Attach lines
                foreach (var l in lines)
                {
                    l.ClaimId = claimId;
                    claim.ServiceLines.Add(l);
                }

                // Write ledger
                claim.SubmissionIdempotencyKey = $"demo-{claimId:N}";
                _db.ClaimTransactions.Add(new ClaimTransaction
                {
                    Id = Guid.NewGuid(),
                    ClaimId = claimId,
                    Type = ClaimTransactionType.Submit,
                    Amount = 0m,
                    Reference = claim.SubmissionIdempotencyKey,
                    CreatedUtc = createdUtc
                });

                if (status == ClaimStatus.Denied)
                {
                    claim.DenialReasonCode = "DEMO_DENY";
                    claim.AllowedAmount = 0m;
                    claim.PayerPaid = 0m;
                    claim.PatientResponsibility = totalCharge;

                    _db.ClaimTransactions.Add(new ClaimTransaction
                    {
                        Id = Guid.NewGuid(),
                        ClaimId = claimId,
                        Type = ClaimTransactionType.Deny,
                        Amount = 0m,
                        Reference = "DEMO_EOB",
                        CreatedUtc = createdUtc.AddMinutes(10)
                    });
                }
                else if (status == ClaimStatus.Paid)
                {
                    // adjudication math: allowed = 80% charge, payerPaid = 80% allowed
                    var allowed = Math.Round(totalCharge * 0.80m, 2);
                    var payerPaidAmt = Math.Round(allowed * 0.80m, 2);
                    var patientResp = allowed - payerPaidAmt;

                    claim.AllowedAmount = allowed;
                    claim.PayerPaid = payerPaidAmt;
                    claim.PatientResponsibility = patientResp;

                    // Adjustment write-off
                    _db.ClaimTransactions.Add(new ClaimTransaction
                    {
                        Id = Guid.NewGuid(),
                        ClaimId = claimId,
                        Type = ClaimTransactionType.Adjust,
                        Amount = allowed - totalCharge,
                        Reference = "DEMO_ADJ",
                        CreatedUtc = createdUtc.AddMinutes(10)
                    });

                    _db.ClaimTransactions.Add(new ClaimTransaction
                    {
                        Id = Guid.NewGuid(),
                        ClaimId = claimId,
                        Type = ClaimTransactionType.Pay,
                        Amount = payerPaidAmt,
                        Reference = "DEMO_EFT_TRACE",
                        CreatedUtc = createdUtc.AddMinutes(15)
                    });
                }

                _db.Claims.Add(claim);
                created++;
            }

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Seeded demo claims. ClinicId={ClinicId} PayerId={PayerId} Created={Created} Paid={Paid} Denied={Denied} Submitted={Submitted}",
                clinicId, payerId, created, paid, denied, submitted);

            return Ok(new
            {
                ClinicId = clinicId,
                PayerId = payerId,
                Created = created,
                Paid = paid,
                Denied = denied,
                Submitted = submitted
            });
        }
    }
}