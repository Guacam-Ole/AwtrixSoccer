using System.Net.Http.Headers;
using System.Text.Json;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using SoccerUlanzi.AwTrix;
using SoccerUlanzi.Entities;
using Team = SoccerUlanzi.Entities.Team;

namespace SoccerUlanzi;

public class FotMob
{
    private const string FotMobUrl = "https://www.fotmob.com";
    private readonly Display _display;
    private readonly ILogger<FotMob> _logger;

    public FotMob(AwTrix.Display display, ILogger<FotMob> logger)
    {
        _display = display;
        _logger = logger;
    }


    private static async Task<HtmlDocument> GetDocumentFromUrl(string url)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true
        };
        httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        var html = await httpClient.GetStringAsync(url);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc;
    }

    private static async Task<JsonElement> GetPageProps(string url)
    {
        var doc = await GetDocumentFromUrl(url);
        var scriptNode = doc.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']");
        var jsonDoc = JsonDocument.Parse(scriptNode.InnerText);
        return jsonDoc.RootElement
            .GetProperty("props")
            .GetProperty("pageProps");
    }

    public async Task<Team?> GetTeamData(string teamId = "8152")
    {
        try
        {
            var url = $"{FotMobUrl}/teams/{teamId}";
            var pageProps = await GetPageProps(url);
            var fallback = pageProps.GetProperty("fallback");

            var jsonDict = fallback.Deserialize<Dictionary<string, JsonElement>>();
            if (jsonDict == null)
            {
                _logger.LogWarning("Cannot retrieve TeamData for team '{Id}'", teamId);
                return null;
            }

            var team = jsonDict.First(dict => dict.Key.StartsWith("team")).Value;
            return team.Deserialize<Team>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot retrieve TeamData for team '{TeamId}'", teamId);
            return null;
        }
    }

    public async Task<(double, List<int>)> GetMatchData(string path)
    {
        var url = $"{FotMobUrl}{path}";
        try
        {
            
            var pageProps = await GetPageProps(url);
            var matchFacts = pageProps
                .GetProperty("content")
                .GetProperty("matchFacts");

            var momentum = matchFacts.GetProperty("momentum").GetProperty("main").GetProperty("data")
                .Deserialize<List<Momentum>>();
            var events = matchFacts.GetProperty("events").GetProperty("events").Deserialize<List<MatchEvent>>();

            var time = 0d;
            if (momentum?.Count > 0) time = momentum.Max(mo => mo.Minute);
            var goalEvents = events?.Where(ev => ev.EventType == "Goal").ToList();

            if (goalEvents == null) return (time, [0, 0]);
            var score = goalEvents.MaxBy(ev => ev.Minute)?.Score ?? [0, 0];
            return goalEvents.Any(ev => ev is { EventType: "Half", Minute: >= 90 }) ? (90, [-1, -1]) : (time, score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot get Matchdata from '{Url}'",url);
            throw;
        }
    }

 
}