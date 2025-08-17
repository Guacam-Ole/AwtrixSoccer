namespace SoccerUlanzi;

public class Config
{
    public List<string> TeamIds { get; set; } 
    public string DeviceIp { get; set; } 
    public bool DemoMode { get; set; }
    public bool Uninstall { get; set; }
    public int DisplayDelayOnGames { get; set; } = 40;
    public int DisplayDelayWhenOff { get; set; } = 7;
    public TimeSpan DelayOnActiveGames { get; set; } = new(0, 0, 10);
    public TimeSpan DelayWhenIdle { get; set; } = new(0, 15, 0);

    public string[] LeagueUrls { get; set; } =
    [
        "https://site.api.espn.com/apis/site/v2/sports/soccer/ger.dfb_pokal",
        "https://site.api.espn.com/apis/site/v2/sports/soccer/ger.1",
        "https://site.api.espn.com/apis/site/v2/sports/soccer/ger.2",
        "https://site.api.espn.com/apis/site/v2/sports/soccer/uefa.champions",
        "https://site.api.espn.com/apis/site/v2/sports/soccer/uefa.europa.conf",
        "https://site.api.espn.com/apis/site/v2/sports/soccer/uefa.europa",
        "https://site.api.espn.com/apis/site/v2/sports/soccer/uefa.europa.conf_qual",
        "https://site.api.espn.com/apis/site/v2/sports/soccer/uefa.champions_qual",
        "https://site.api.espn.com/apis/site/v2/sports/soccer/uefa.europa_qual",
        "https://site.api.espn.com/apis/site/v2/sports/soccer/ger.2.promotion.relegation",
        "https://site.api.espn.com/apis/site/v2/sports/soccer/ger.playoff.relegation"
    ]; 
}