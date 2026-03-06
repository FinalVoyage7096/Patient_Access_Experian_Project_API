namespace Patient_Access_Experian_Project_API.Models;

public class Provider
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;

    public List<AvailabilityWindow> AvailabilityWindows { get; set; } = new();

}
