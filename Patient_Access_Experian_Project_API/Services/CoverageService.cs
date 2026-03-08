using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Patient_Access_Experian_Project_API.Data;
using Patient_Access_Experian_Project_API.Data_Transfer_Objects;
using Patient_Access_Experian_Project_API.Models;
using Microsoft.Extensions.Logging;

namespace Patient_Access_Experian_Project_API.Services

{
    public class CoverageService
    {
        private readonly PatientAccessDbContext _db;
        private readonly ILogger<CoverageService> _logger;

        public CoverageService(PatientAccessDbContext db, ILogger<CoverageService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<(bool Success, string? Error, CoverageEligibilityResponse? Response)> CheckEligibilityAsync(
            CoverageEligibilityRequest request,
            CancellationToken ct = default)
        {
            var started = DateTime.UtcNow;

            _logger.LogInformation(
                "Coverage eligibility check started. PatientId={PatientId} ClinicId={ClinicId} " +
                "ProviderId={ProviderId} ServiceCode={ServiceCode} " +
                "ScheduledStartUtc={ScheduledStartUtc}",
                request.PatientId, request.ClinicId, request.ProviderId, request.ServiceCode, request.ScheduledStartUtc);

            try
            {
                if (string.IsNullOrWhiteSpace(request.ServiceCode))
                    return (false, "ServiceCode is required.", null);

                if (request.ScheduledStartUtc.Kind != DateTimeKind.Utc)
                    return (false, "ScheduledStartUtc must be a UTC DateTime (DateTimeKind.Utc).", null);

                // Existence checks
                if (!await _db.Patients.AnyAsync(p => p.Id == request.PatientId, ct))
                    return (false, "Patient not found.", null);

                if (!await _db.Clinics.AnyAsync(c => c.Id == request.ClinicId, ct))
                    return (false, "Clinic not found.", null);

                if (!await _db.Providers.AnyAsync(p => p.Id == request.ProviderId, ct))
                    return (false, "Provider not found.", null);

                // Mock rules engine
                // (Deterministic but fake logic for demonstration purposes)
                var normalized = request.ServiceCode.Trim().ToUpperInvariant();

                // Simple rule: some service codes require active coverage, some produce different copays
                decimal copay = normalized switch
                {
                    "99213" => 35m, // Office visit
                    "93000" => 50m, // EKG
                    "80053" => 20m, // metabolic panel
                    _ => 40m // default for unknown codes
                };

                // Deterministic pseudo "deductible remaining" based on patientId hash
                var seed = Math.Abs(request.PatientId.GetHashCode());
                decimal deductibleRemaining = (seed % 900) + 50; // 50..949

                // "Coverage active" rule: if scheduled date is weekend --> inactive (demo rule)
                var isWeekend = request.ScheduledStartUtc.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                var eligible = !isWeekend; // Not eligible if scheduled on weekend

                var coverageStatus = eligible ? "Active" : "Inactive";

                // Estimate: copay + (if deductible remaining > 0, add a small coinsurnace piece)
                decimal estimated = copay + (eligible ? Math.Min(25m, deductibleRemaining * 0.05m) : 0m);

                var referenceId = Guid.NewGuid(); // In real life, this would be a reference to the actual coverage check record

                var response = new CoverageEligibilityResponse(
                    referenceId,
                    eligible,
                    coverageStatus,
                    copay,
                    deductibleRemaining,
                    estimated
                    );

                // Audit trail
                var log = new CoverageCheckLog
                {
                    Id = referenceId,
                    PatientId = request.PatientId,
                    ClinicId = request.ClinicId,
                    ProviderId = request.ProviderId,
                    ServiceCode = normalized,
                    ScheduledStartUtc = request.ScheduledStartUtc,
                    RequestJson = JsonSerializer.Serialize(request),
                    ResponseJson = JsonSerializer.Serialize(response),
                    Eligible = response.Eligible,
                    CoverageStatus = response.CoverageStatus,
                    Copay = response.Copay,
                    DeductibleRemaining = response.DeductibleRemaining,
                    EstimatedPatientResponsibility = response.EstimatedPatientResponsibility,
                    CreatedUtc = DateTime.UtcNow
                };

                _db.CoverageCheckLogs.Add(log);
                await _db.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Coverage eligibility check completed. ReferenceId = {ReferenceId} Eligible={Eligible} " +
                    "CoverageStatus={CoverageStatus} Copay={Copay} DurationMs={DurationMs}",
                    response.ReferenceId, response.Eligible, response.CoverageStatus, response.Copay,
                    (DateTime.UtcNow - started).TotalMilliseconds);

                return (true, null, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                ex,
                "Coverage eligibility check failed. PatientId={PatientId} ClinicId={ClinicId} ProviderId={ProviderId} ServiceCode={ServiceCode}",
                request.PatientId, request.ClinicId, request.ProviderId, request.ServiceCode);

                throw;
            }
        }
    }
}
