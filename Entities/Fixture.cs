using System.Text.Json.Serialization;

namespace SoccerUlanzi.Entities;

public class Fixture
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("pageUrl")]
    public string Url { get; set; }
    [JsonPropertyName("home")]
    public FixtureTeam Home { get; set; }
    [JsonPropertyName("away")]
    public FixtureTeam Guest { get; set; }
    [JsonPropertyName("status")] 
    public FixtureStatus Status { get; set; }
    
    public DateTime LocalTime => Status.StartDateUtc.ToLocalTime();
}

public class FixtureStatus
{
    [JsonPropertyName("utcTime")]
    public DateTime StartDateUtc { get; set; }

    

    [JsonPropertyName("started")]
    public bool Started { get; set; }
    [JsonPropertyName("finished")]
    public bool Finished { get; set; }
    [JsonPropertyName("cancelled")]
    public bool Cancelled { get; set; }
}

public class FixtureTeam
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("score")]
    public int Score { get; set; }
    public string Url { get; set; }
}