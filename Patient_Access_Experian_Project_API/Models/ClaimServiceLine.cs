namespace Patient_Access_Experian_Project_API.Models
{
    public class ClaimServiceLine
    {
        public Guid Id { get; set; }

        public Guid ClaimId { get; set; }
        public Claim Claim { get; set; } = null!;

        public string ServiceCode { get; set; } = string.Empty;
        public int Units { get; set; } = 1;
        public decimal ChargeAmount { get; set; } // per line
    }
}