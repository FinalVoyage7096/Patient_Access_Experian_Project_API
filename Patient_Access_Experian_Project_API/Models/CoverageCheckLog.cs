namespace Patient_Access_Experian_Project_API.Models
{
    public class CoverageCheckLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PatientId { get; set; }
        public Guid ClinicId { get; set; }
        public Guid ProviderId { get; set; }

        public string ServiceCode { get; set; } = string.Empty;
        public DateTime ScheduledStartUtc { get; set; }

        // Store the full request/response as JSON (audit trail)
        public string RequestJson { get; set; } = string.Empty;
        public string ResponseJson { get; set; } = string.Empty;

        public bool Eligible { get; set; }
        public string CoverageStatus { get; set; } = string.Empty;

        public decimal Copay { get; set; }
        public decimal DeductibleRemaining { get; set; }
        public decimal EstimatedPatientResponsibility { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
