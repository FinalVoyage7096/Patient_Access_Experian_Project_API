namespace Patient_Access_Experian_Project_API.Models;

public class Appointment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    public Guid ProviderId { get; set; }
    public Provider? Provider { get; set; }

    public Guid PatientId { get; set; } 
    public Patient? Patient { get; set; }

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }


    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public enum AppointmentStatus
{
    Scheduled = 0,
    Cancelled = 1,
    Completed = 2
}
