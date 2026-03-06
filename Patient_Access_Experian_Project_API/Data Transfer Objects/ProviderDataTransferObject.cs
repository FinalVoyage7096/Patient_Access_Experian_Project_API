namespace Patient_Access_Experian_Project_API.Data_Transfer_Objects
{
    public record ProviderSummaryDto(Guid Id, string Name, string Specialty);

    public record ProviderDetailDto(
        Guid Id,
        string Name,
        string Specialty,
        List<AvailabilityWindowDto> Availability);

    public record AvailabilityWindowDto(
        int DayOfWeek,
        int StartMinuteOfDay,
        int EndMinuteOfDay);







}
