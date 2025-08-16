namespace SoccerUlanzi;

public class Config
{
    public List<string> TeamIds { get; set; } = new List<string>{"270", "6392", "127"};
    public string DeviceIp { get; set; } = "192.168.178.10";
    public string TeamUrl { get; set; }

    public string[] LeagueUrls { get; set; } = new[]
    {
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
    }; 
}