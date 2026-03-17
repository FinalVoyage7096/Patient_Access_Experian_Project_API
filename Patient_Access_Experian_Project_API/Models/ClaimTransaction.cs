namespace Patient_Access_Experian_Project_API.Models
{
    public class ClaimTransaction
    {
        public Guid Id { get; set; }

        public Guid ClaimId { get; set; }
        public Claim Claim { get; set; } = null!;

        public ClaimTransactionType Type { get; set; }
        public decimal Amount { get; set; }          // signed if you want (Adjust negative, etc.)
        public string Currency { get; set; } = "USD";

        public string? Reference { get; set; }       // payer trace # / batch # / etc.
        public string? MetadataJson { get; set; }    // optional JSON blob for demo

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
