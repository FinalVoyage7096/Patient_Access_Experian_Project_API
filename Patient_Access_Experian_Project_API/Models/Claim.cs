namespace Patient_Access_Experian_Project_API.Models
{
    public class Claim
    {
        public Guid Id { get; set; }

        public Guid ClinicId { get; set; }
        public Clinic Clinic { get; set; } = null!;

        public Guid PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public Guid ProviderId { get; set; }
        public Provider Provider { get; set; } = null!;

        public Guid PayerId { get; set; }
        public Payer Payer { get; set; } = null!;

        public Guid? AppointmentId { get; set; }

        public ClaimStatus Status { get; set; } = ClaimStatus.Draft;

        public decimal TotalCharge { get; set; }  // sum of service lines
        public decimal? AllowedAmount { get; set; }
        public decimal? PayerPaid { get; set; }
        public decimal? PatientResponsibility { get; set; }

        public string? DenialReasonCode { get; set; }

        public string? SubmissionIdempotencyKey { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

        public List<ClaimServiceLine> ServiceLines { get; set; } = new();
        public List<ClaimTransaction> Transactions { get; set; } = new();
    }
}

