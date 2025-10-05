using System.Text.Json.Serialization;

namespace SoccerUlanzi.Entities;

public class MatchEvent
{
    [JsonPropertyName("time")] public double Minute { get; set; }
    [JsonPropertyName("type")] public string? EventType { get; set; }
    [JsonPropertyName("newScore")] public List<int> Score { get; set; } = [];
}