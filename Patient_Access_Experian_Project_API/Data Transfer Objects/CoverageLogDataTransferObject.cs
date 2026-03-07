namespace Patient_Access_Experian_Project_API.Data_Transfer_Objects;

public record CoverageLogItemDto(
    Guid ReferenceId,
    Guid PatientId,
    Guid ClinicId,
    Guid ProviderId,
    string ServiceCode,
    DateTime ScheduledStartUtc,
    bool Eligible,
    string CoverageStatus,
    decimal Copay,
    decimal DeductibleRemaining,
    decimal EstimatedPatientResponsibility,
    DateTime CreatedUtc
);