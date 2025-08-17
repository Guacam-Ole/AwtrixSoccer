namespace SoccerUlanzi;

public static class EspnHelpers
{
    public static bool IsWithinNextHours(this DateTime? matchDate, int hours)
    {
        return matchDate != null && (matchDate.Value.ToLocalTime() - DateTime.Now).TotalHours < hours;
    }
    
    public static bool IsWithinNext(this DateTime? matchDate, TimeSpan timespan)
    {
        return matchDate != null && (matchDate.Value.ToLocalTime() - DateTime.Now) < timespan;
    }


    public static bool IsWithinPreviusHours(this DateTime? matchDate, int hours)
    {
        return matchDate != null && (DateTime.Now - matchDate.Value.ToLocalTime()).TotalHours < hours;
    }
}