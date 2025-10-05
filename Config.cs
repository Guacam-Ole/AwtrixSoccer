using System.Diagnostics.CodeAnalysis;

namespace SoccerUlanzi;

[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public class Config
{
    public List<TeamConfig> Teams { get; set; } = [];
    public required string DeviceIp { get; set; } 
    public bool Uninstall { get; set; }
    public int DisplayDelayOnGames { get; set; } = 10;
    public int DisplayDelayWhenOff { get; set; } = 7;
    public TimeSpan DelayOnActiveGames { get; set; } = new(0, 0, 10);
    public TimeSpan DelayWhenIdle { get; set; } = new(0, 15, 0);
    
}