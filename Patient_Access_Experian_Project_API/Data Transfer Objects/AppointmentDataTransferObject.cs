namespace Patient_Access_Experian_Project_API.Data_Transfer_Objects
{
    public record CreateAppointmentRequest(
        Guid ClinicId,
        Guid ProviderId,
        Guid PatientId,
        DateTime StartUtc,
        int DurationMinutes);

    public record AppointmentResponse(
        Guid Id,
        Guid ClinicId,
        Guid ProviderId,
        Guid PatientId,
        DateTime StartUtc,
        DateTime EndUtc,
        string Status);

}
