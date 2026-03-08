using System.ComponentModel.DataAnnotations;

namespace Patient_Access_Experian_Project_API.Data_Transfer_Objects
{
    public record CoverageEligibilityRequest(
        [Required] Guid PatientId,
        [Required] Guid ClinicId,
        [Required] Guid ProviderId,
        [Required, MinLength(1), MaxLength(10)] string ServiceCode,
        DateTime ScheduledStartUtc
        );

    public record CoverageEligibilityResponse(
        Guid ReferenceId,
        bool Eligible,
        string CoverageStatus,
        decimal Copay,
        decimal DeductibleRemaining,
        decimal EstimatedPatientResponsibility
        );


}
