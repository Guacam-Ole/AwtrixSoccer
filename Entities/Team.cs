using System.Text.Json.Serialization;

namespace SoccerUlanzi.Entities;

public class Team
{
    [JsonPropertyName("tabs")]
    public List<string> Tabs { get; set; }
    [JsonPropertyName("details")]
    public TeamDetails Details { get; set; }
    [JsonPropertyName("overview")]
    public TeamOverview Overview { get; set; }
    
     [JsonPropertyName("sportsTeamJSONLD")]
    public TeamExterneral ExternalData { get; set; }
}

public class TeamExterneral
{
    [JsonPropertyName("logo")]
    public string LogoUrl { get; set; }
}
public class TeamDetails
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("latestSeason")]
    public string LatestSeason { get; set; }
    [JsonPropertyName("shortName")]
    public string ShortName { get; set; }
}

public class TeamOverview
{
    [JsonPropertyName("nextMatch")]
    public Fixture? NextMatch { get; set; }
    [JsonPropertyName("lastMatch")]
    public Fixture? LastMatch { get; set; }
    [JsonPropertyName("hasOngoingMatch")]
    public bool ActiveMatch { get; set; }
}