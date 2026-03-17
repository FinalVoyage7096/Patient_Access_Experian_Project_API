namespace Patient_Access_Experian_Project_API.Data_Transfer_Objects
{
    public record CreateClaimServiceLineDto(string ServiceCode, int Units, decimal ChargeAmount);

    public record CreateClaimRequest(
        Guid ClinicId,
        Guid PatientId,
        Guid ProviderId,
        Guid PayerId,
        Guid? AppointmentId,
        List<CreateClaimServiceLineDto> ServiceLines
    );

    public record ClaimSummaryDto(
        Guid Id,
        Guid ClinicId,
        Guid PatientId,
        Guid ProviderId,
        Guid PayerId,
        string Status,
        decimal TotalCharge,
        DateTime CreatedUtc
    );

    public record ClaimDetailDto(
        Guid Id,
        Guid ClinicId,
        Guid PatientId,
        Guid ProviderId,
        Guid PayerId,
        string Status,
        decimal TotalCharge,
        decimal? AllowedAmount,
        decimal? PayerPaid,
        decimal? PatientResponsibility,
        string? DenialReasonCode,
        DateTime CreatedUtc,
        List<CreateClaimServiceLineDto> ServiceLines
    );
}