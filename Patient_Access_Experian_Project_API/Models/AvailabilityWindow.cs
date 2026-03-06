namespace Patient_Access_Experian_Project_API.Models;

public class AvailabilityWindow
{

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProviderId { get; set; }
    public Provider? Provider { get; set; }

    // 0 = sunday, 1 = monday, ..., 6 = saturday
    public int DayOfWeek { get; set; }

    //Minutes since midnight (simple and SQL friendly)
    public int StartMinuteOfDay { get; set; }
    public int EndMinuteOfDay { get; set; }


}
