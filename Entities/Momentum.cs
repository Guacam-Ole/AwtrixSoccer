using System.Text.Json.Serialization;

namespace SoccerUlanzi.Entities;

public class Momentum
{
    [JsonPropertyName("minute")]
    public double Minute { get; set; }
    [JsonPropertyName("value")]
    public int Value { get; set; }
}