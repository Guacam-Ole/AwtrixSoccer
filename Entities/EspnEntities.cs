using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SoccerUlanzi.Entities.Espn;

public class Event
{
    public string Id { get; set; }
    [JsonPropertyName("date")] public DateTime? MatchDate { get; set; }
    public required string Name { get; set; }
    public List<Competition> Competitions { get; set; }
    public List<Link> Links { get; set; }
    [JsonIgnore]
    public AwTrix.GamesStates GameState { get; set; }
}

public class Competition
{
    public string Id { get; set; }
    [JsonPropertyName("date")] public DateTime MatchDate { get; set; }
    public EventStatus Status { get; set; }
    public List<Competitor> Competitors { get; set; }
}

public class EventStatusType
{
    public string Name { get; set; }
    public string State { get; set; }
    [JsonPropertyName("completed")] public bool IsCompleted { get; set; }
}

public class EventStatus
{
    [JsonPropertyName("type")] 
    public EventStatusType StatusType { get; set; }
    
    public string DisplayClock { get; set; }

    public int Minutes
    {
        get
        {
            if (DisplayClock == null) return 0;
            var parts = DisplayClock.Split("'");
            //var numbersOnly = Regex.Replace(DisplayClock, @"[^0-9]", "");
            return int.TryParse(parts[0], out var minutes) ? minutes : 0;
        }
    }
}

public class Competitor
{
    public Team Team { get; set; }
    public string HomeAway { get; set; }
    public bool IsHome
    {
        get { return HomeAway == "home"; }
    }
}

public class Team
{
    public required string Id { get; set; }
    [JsonPropertyName("displayName")] public string Name { get; set; }
    public string? Color { get; set; }
    public List<Logo>? Logos { get; set; }
    [JsonPropertyName("nextEvent")] public List<Event> NextEvents { get; set; }

    [JsonIgnore]
    public Event? NextEvent
    {
        get { return NextEvents.OrderBy(q => q.MatchDate).FirstOrDefault(); }
    }
}

public class Logo
{
    public required string Href { get; set; }
    public required int Width { get; set; }
    public required int Height { get; set; }
}

public class Link
{
    public string Text { get; set; }
    public string Href { get; set; }
}

public class TeamWrapper
{
    [JsonPropertyName("team")] public Team Team { get; set; }
}

public class Timing
{
    public string? Url { get; set; }
    public DateTime? LastChecked { get; set; }
    public DateTime? NextCheck { get; set; }
    public Event? Game { get; set; }
}