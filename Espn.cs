using System.Text.Json;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using SoccerUlanzi.Entities.Espn;

namespace SoccerUlanzi;

public class Espn
{
    private const string ScoreTag = "Gamestrip__Score";
    private readonly ILogger<Espn> _logger;
    private readonly Config _config;
    private readonly AwTrix _awTrix;
    private readonly Rest _rest;

    private static readonly Dictionary<string, List<Timing>> Timings = [];
    private static readonly Dictionary<string, Event> NextGames = [];
    private static readonly Dictionary<string, Event> RunningGames = [];
    private static readonly Dictionary<string, Event> FinishedGames = [];


    public Espn(ILogger<Espn> logger, Config config, AwTrix awTrix, Rest rest)
    {
        _logger = logger;
        _config = config;
        _awTrix = awTrix;
        _rest = rest;
    }


    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };


    public bool AnyActiveGame()
    {
        var isAnyActiveGame = RunningGames.Count > 0;
        isAnyActiveGame |= NextGames.Values.Any(q => q.MatchDate.IsWithinNext(_config.DelayWhenIdle));
        _logger.LogDebug("Any active game: '{ActiveGame}'", isAnyActiveGame);
        return isAnyActiveGame;
    }

    public async Task GetGamesFor(TeamConfig team)
    {
        if (!Timings.ContainsKey(team.Id)) Timings.Add(team.Id, []);
        var teamTimings = Timings[team.Id];

        foreach (var url in _config.LeagueUrls)
        {
            var existingTiming = teamTimings.FirstOrDefault(q => q.Url == url);
            if (existingTiming != null)
            {
                if (existingTiming.NextCheck > DateTime.Now)
                {
                    _logger.LogDebug("Won't check '{Url}' before '{Date}'", url, existingTiming.NextCheck);
                    continue;
                }
            }
            else
            {
                teamTimings.Add(new Timing { Url = url });
                existingTiming = teamTimings.First(q => q.Url == url);
            }

            existingTiming.LastChecked = DateTime.Now;
            var teamGames = await GetNextGame($"{url}/teams/{team.Id}");
            if (teamGames == null)
            {
                existingTiming.NextCheck = DateTime.Now.AddDays(30);
                _logger.LogInformation(
                    "Did not get any information from favorite team '{Team}' on '{Url}. Will not retry for the next 30 days.",
                    team.Name, url);
                continue;
            }

            if (teamGames.NextEvent?.MatchDate == null)
            {
                existingTiming.NextCheck = DateTime.Now.AddDays(1);
                _logger.LogInformation(
                    "Did not get any Fixture from favorite team '{Team}' on '{Url}. Will not retry for the next day.",
                    team.Name, url);
                continue;
            }

            _logger.LogDebug("Added game on '{Date}' for '{Name}' to list of games", teamGames.NextEvent.MatchDate,
                teamGames.NextEvent.Name);

            existingTiming.Game = teamGames.NextEvent;
        }

        AnalyzeFixtures(team);
    }

    private void AnalyzeFixtures(TeamConfig team)
    {
        if (RunningGames.TryGetValue(team.Id, out var runningGame))
        {
            if (runningGame.MatchDate != null && runningGame.MatchDate.Value.ToLocalTime() < DateTime.Now.AddHours(-4))
            {
                _logger.LogInformation("Running game '{Name}' for '{Team}' removed", runningGame.Name, team.Name);
                RunningGames.Remove(team.Id);
            }
        }

        if (!Timings.TryGetValue(team.Id, out var teamTiming)) return;
        FinishedGames.TryGetValue(team.Id, out var finishedGame);
        NextGames.TryGetValue(team.Id, out var nextGame);

        foreach (var timing in teamTiming.Where(q => q.Game != null))
        {
            var nextCompetition = timing.Game!.Competitions.FirstOrDefault();
            if (nextCompetition == null) continue;
            if (nextCompetition.MatchDate > DateTime.Now.AddDays(7)) continue;
            if (nextCompetition.MatchDate < DateTime.Now.AddDays(-1)) continue;
            if (nextCompetition.Status.StatusType.IsCompleted)
            {
                if (finishedGame?.MatchDate == null || finishedGame.MatchDate < nextCompetition.MatchDate)
                {
                    FinishedGames[team.Id] = timing.Game;
                    _logger.LogInformation("Finished Game for '{Team}' set to '{Name}'", team.Name, timing.Game.Name);
                }

                timing.NextCheck = DateTime.Now.AddHours(2);
                continue;
            }

            switch (nextCompetition.Status.StatusType.Name)
            {
                case "STATUS_SCHEDULED":
                    if (nextGame?.MatchDate == null || nextGame.MatchDate < DateTime.Now ||
                        nextGame.MatchDate > nextCompetition.MatchDate)
                    {
                        if (nextGame != timing.Game && timing.Game.MatchDate > DateTime.Now)
                        {
                            NextGames[team.Id] = timing.Game;
                            timing.NextCheck = nextCompetition.MatchDate.ToLocalTime();
                            _logger.LogInformation("Next Game for '{Team}' set to '{Name}'", team.Name, timing.Game.Name);
                        }
                    }

                    break;
                case "STATUS_FIRST_HALF":
                case "STATUS_SECOND_HALF":
                    timing.Game.GameState = AwTrix.GamesStates.Playing;
                    UpdateRunningGame(timing, team);
                    break;
                case "STATUS_OVERTIME":
                case "STATUS_SHOOTOUT":
                    timing.Game.GameState = AwTrix.GamesStates.OverTime;
                    UpdateRunningGame(timing, team);
                    break;

                case "STATUS_HALFTIME":
                case "STATUS_END_OF_REGULATION":
                case "STATUS_HALFTIME_ET":
                case "STATUS_END_OF_EXTRATIME":
                    timing.Game.GameState = AwTrix.GamesStates.Pause;
                    UpdateRunningGame(timing, team);
                    break;

                case "STATUS_FULLTIME":
                    timing.Game.GameState = AwTrix.GamesStates.Finished;
                    UpdateRunningGame(timing, team);
                    break;

                default:
                    _logger.LogWarning("Unknown status '{Status}' for '{Team}' . Will ignore that",
                        nextCompetition.Status.StatusType.Name, team.Name);
                    break;
            }
        }
    }

    private void UpdateRunningGame(Timing timing, TeamConfig team)
    {
        RunningGames.TryGetValue(team.Id, out var runningGame);

        if (runningGame != timing.Game && timing.Game != null)
        {
            runningGame = timing.Game;
            RunningGames[team.Id] = runningGame;
            _logger.LogInformation("Current Game for '{Team}' set to '{Name}'",team.Name, timing.Game.Name);
        }

        timing.NextCheck = DateTime.Now.AddSeconds(4);
    }

    private async Task ShowCurrentGame(Event game, TeamConfig team)
    {
        Timings.TryGetValue(team.Id, out var teamTimings);
        var timing = teamTimings?.FirstOrDefault(q => q.Game == game);
        if (timing == null) return;

        var competition = game.Competitions.First();
        var homeCompetitor = competition.Competitors.First(q => q.IsHome);
        var guestCompetitor = competition.Competitors.First(q => !q.IsHome);

        var url = game.Links
            .FirstOrDefault(q => q.Text == "Statistics" && q.Rel.Contains("stats") && q.Rel.Contains("desktop"))?.Href;
        if (url == null)
        {
            try
            {
                var gameOutPut = JsonSerializer.Serialize(game);
                _logger.LogWarning("Cannot retrieve game url for '{Team}'. Links are: '{Links}'", team.Name, gameOutPut);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Cannot retrieve game url and also cannot serialize it.");
            }

            return;
        }

        var (home, guest) = await GetScoresFromUrl(url);
        if (home == null || guest == null) return;
        var homeTeam = new Team
        {
            Goals = home.Value,
            IconPath = $"./cache/{homeCompetitor.Team.Id}.png",
            IconUrl = homeCompetitor.Team.Logos?.FirstOrDefault()?.Href
        };
        var guestTeam = new Team
        {
            Goals = guest.Value,
            IconPath = $"./cache/{guestCompetitor.Team.Id}.png",
            IconUrl = guestCompetitor.Team.Logos?.FirstOrDefault()?.Href
        };

        await _awTrix.SendNewStandings(homeTeam, guestTeam, competition.Status.Minutes, game.GameState, team);
    }


    private async Task ShowNextGame(TeamConfig team)
    {
        NextGames.TryGetValue(team.Id, out var game);
        var matchDate = game?.MatchDate;
        if (matchDate == null || game == null) return;
        if (matchDate.Value.ToLocalTime() < DateTime.Now) return;

        if ((matchDate.Value.ToLocalTime() - DateTime.Now.ToLocalTime()).Days > 7) return;

        var competition = game.Competitions.First();
        var homeCompetitor = competition.Competitors.First(q => q.IsHome);
        var guestCompetitor = competition.Competitors.First(q => !q.IsHome);

        var home = new Team
        {
            IconUrl = homeCompetitor.Team.Logos?.FirstOrDefault()?.Href,
            IconPath = $"./cache/{homeCompetitor.Team.Id}.6x6.png"
        };
        var guest = new Team
        {
            IconUrl = guestCompetitor.Team.Logos?.FirstOrDefault()?.Href,
            IconPath = $"./cache/{guestCompetitor.Team.Id}.6x6.png"
        };
        await _awTrix.ShowPreview(home, guest, matchDate.Value, team);
    }

    public async Task DisplayNextOrCurrentGame(TeamConfig team)
    {
        FinishedGames.TryGetValue(team.Id, out var finishedGame);
        RunningGames.TryGetValue(team.Id, out var runningGame);
        NextGames.TryGetValue(team.Id, out var nextGame);

        if (finishedGame?.MatchDate != null && finishedGame.MatchDate.Value.ToLocalTime() < DateTime.Now.AddHours(-12))
        {
            finishedGame = null;
            FinishedGames.Remove(team.Id);
        }


        if (runningGame != null)
        {
            if (finishedGame?.Id == runningGame.Id)
            {
                RunningGames.Remove(team.Id);
            }

            await ShowCurrentGame(runningGame, team);
        }
        else if (finishedGame != null)
        {
            finishedGame.GameState = AwTrix.GamesStates.Finished;
            await ShowCurrentGame(finishedGame, team);
        }
        else if (nextGame != null)
        {
            await ShowNextGame(team);
        }
    }


    private async Task<Entities.Espn.Team?> GetNextGame(string url)
    {
        var response = await _rest.GetString(url);
        if (response == null)
        {
            _logger.LogError("Cannot retrieve team fixtures from '{url}'. Will just ignore that", url);
            return null;
        }

        var teamWrapper = JsonSerializer.Deserialize<TeamWrapper>(response, _serializerOptions);
        return teamWrapper?.Team;
    }

    private async Task<(int?, int?)> GetScoresFromUrl(string url)
    {
        try
        {
            var response = await _rest.Get(url);
            if (response == null)
            {
                _logger.LogError("Cannot get scores on '{Url}'", url);
                return (null, null);
            }

            var html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var nodes = doc.DocumentNode.SelectNodes($"//div[contains(@class, '{ScoreTag}')]");
            if (nodes.Count <= 1) return (null, null);
            var home = nodes[0].FirstChild.InnerText;
            var guest = nodes.Last().FirstChild.InnerText;
            if (int.TryParse(home, out var homeScore) && int.TryParse(guest, out var guestScore))
            {
                return (homeScore, guestScore);
            }

            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot get scores on '{Url}", url);
            return (null, null);
        }
    }
}