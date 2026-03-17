using System.Security.Claims;

namespace Patient_Access_Experian_Project_API.Models
{
    public class Payer
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<Claim> Claims { get; set; } = new();
    }
}
