namespace Patient_Access_Experian_Project_API.Data_Transfer_Objects.Reconciliation
{
    public record ClaimsSummaryDto(
        Guid? ClinicId,
        Guid? PayerId,
        DateTime? FromUtc,
        DateTime? ToUtc,
        int ClaimsCreated,
        int ClaimsSubmitted,
        int ClaimsPaid,
        int ClaimsDenied,
        decimal DenialRate,
        decimal TotalCharge,
        decimal TotalAllowed,
        decimal TotalPayerPaid,
        decimal TotalPatientResponsibility,
        double? AvgDaysToPay
    );
}
