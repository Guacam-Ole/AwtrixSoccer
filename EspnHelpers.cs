namespace SoccerUlanzi;

public static class EspnHelpers
{
    public static bool IsWithinNext(this DateTime? matchDate, TimeSpan timespan)
    {
        return matchDate != null && (matchDate.Value.ToLocalTime() - DateTime.Now) < timespan;
    }
}