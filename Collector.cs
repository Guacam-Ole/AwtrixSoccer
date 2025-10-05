using System.Drawing;
using Microsoft.Extensions.Logging;
using SoccerUlanzi.AwTrix;
using SoccerUlanzi.Entities;
using Team = SoccerUlanzi.Entities.Team;

namespace SoccerUlanzi;

public class Collector
{
    private readonly FotMob _fotMob;
    private readonly Display _display;
    private readonly Config _config;
    private readonly ILogger<Collector> _logger;
    private const string ImageUrlPrefix = "https://images.fotmob.com/image_resources/logo/teamlogo/";
    private readonly Dictionary<string, DateTime> _nextRequestDates = [];

    public Collector(FotMob fotMob, AwTrix.Display display, Config config, ILogger<Collector> logger)
    {
        _fotMob = fotMob;
        _display = display;
        _config = config;
        _logger = logger;
    }

    public async Task SitAndWait()
    {
        var delay = _config.DisplayDelayWhenOff;
        
        if (_nextRequestDates.Count > 0)
        {
            var nextQueryDate = _nextRequestDates.Min(req => req.Value);
            SleepUntil(nextQueryDate);
        }
        else
        {
            _logger.LogWarning("No nextQueryDate. This is only ok on startup. ");
        }

        foreach (var team in _config.Teams)
        {
            var teamData = await _fotMob.GetTeamData(team.Id);
            if (teamData == null)
            {
                _logger.LogWarning("Cannot get team data for '{Name}'", team.Name);
                continue;
            }

            if (teamData.Overview.ActiveMatch)
            {
                delay = _config.DisplayDelayOnGames;
                _nextRequestDates[team.Id] = DateTime.Now.AddMinutes(1);
                _logger.LogDebug("'{Team}' has an active match", teamData.Details.Name);
                await DisplayRunningGame(teamData);
            }
            else if (teamData.Overview.LastMatch != null &&
                     teamData.Overview.LastMatch.LocalTime > DateTime.Now.AddDays(-1))
            {
                // Keep displaying previous game for 24 hours
                _nextRequestDates[team.Id] = teamData.Overview.LastMatch.LocalTime.AddDays(1);
                _logger.LogDebug("'{Team}' played 24h ago", teamData.Details.Name);
                await DisplayPreviousGame(teamData);
            }
            else if (teamData.Overview.NextMatch != null &&
                     teamData.Overview.NextMatch.LocalTime.Day == DateTime.Today.Day &&
                     teamData.Overview.NextMatch.LocalTime.Month == DateTime.Today.Month)
            {
                // Will start today
                _nextRequestDates[team.Id] = teamData.Overview.NextMatch.LocalTime;
                _logger.LogDebug("'{Team}' has a match today", teamData.Details.Name);
                await DisplayUpcomingGame(teamData);
            }
            else
            {
                // No games at all. Wait for a day
                _nextRequestDates[team.Id] = DateTime.Today.AddDays(1);
                _logger.LogDebug("'{Team}' has no upcoming matches", teamData.Details.Name);
                await RemoveGameFromDisplay(teamData);
            }

         //   await DisplayTeam(teamData);
        }
        await _display.ChangeDelay(delay);
    }

    private void SleepUntil(DateTime until)
    {
        var difference = until.Subtract(DateTime.Now);
        _logger.LogDebug("Sleeping until '{Until}'", until);
        Thread.Sleep(difference);
    }

    private async Task RemoveGameFromDisplay(Team team)
    {
        await _display.DeleteApps(team.Details.Id.ToString());
    }

    private async Task DisplayUpcomingGame(Team team)
    {
        var homeTeam = GetDisplayTeam(team.Overview.NextMatch!.Home, 0);
        var awayTeam = GetDisplayTeam(team.Overview.NextMatch.Guest, 0);

        await _display.ShowPreview(homeTeam, awayTeam, team.Overview.NextMatch.Status.StartDateUtc.ToLocalTime(),
            new TeamConfig
            {
                Id = team.Details.Id.ToString(),
                Name = team.Details.Name
            });
    }

    private async Task DisplayPreviousGame(Team team)
    {
        var (minute, goals) = await _fotMob.GetMatchData(team.Overview.LastMatch.Url);

        var homeTeam = GetDisplayTeam(team.Overview.LastMatch.Home, goals[0]);
        var awayTeam = GetDisplayTeam(team.Overview.LastMatch.Guest, goals[0]);

        await _display.SendNewStandings(homeTeam, awayTeam, (int)minute, Display.GamesStates.Finished,
            new TeamConfig { Id = team.Details.Id.ToString(), Name = team.Details.Name });
    }

    private static AwTrix.Team GetDisplayTeam(FixtureTeam team, int goals)
    {
        return new AwTrix.Team
        {
            Goals = goals,
            IconPath = $"./cache/{team.Id}.png",
            IconUrl = $"{ImageUrlPrefix}{team.Id}.png"
        };
    }

    private async Task DisplayRunningGame(Team team)
    {
        var (minute, goals) = await _fotMob.GetMatchData(team.Overview.NextMatch.Url);

        var homeTeam = GetDisplayTeam(team.Overview.NextMatch.Home, goals[0]);
        var awayTeam = GetDisplayTeam(team.Overview.NextMatch.Guest, goals[0]);

        await _display.SendNewStandings(homeTeam, awayTeam, (int)minute, Display.GamesStates.Playing,
            new TeamConfig { Id = team.Details.Id.ToString(), Name = team.Details.Name });
    }
}