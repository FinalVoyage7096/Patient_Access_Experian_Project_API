namespace Patient_Access_Experian_Project_API.Data_Transfer_Objects
{
    public record CoverageEligibilityRequest(
        Guid PatientId,
        Guid ClinicId,
        Guid ProviderId,
        string ServiceCode,
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
