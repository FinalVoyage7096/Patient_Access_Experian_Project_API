namespace Patient_Access_Experian_Project_API.Data_Transfer_Objects.Reconciliation
{
    public record ClaimTransactionDto(
        Guid Id,
        Guid ClaimId,
        string Type,
        decimal Amount,
        string Currency,
        string? Reference,
        DateTime CreatedUtc
    );
}
