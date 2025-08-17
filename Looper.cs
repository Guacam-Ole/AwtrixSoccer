using System.Runtime.InteropServices.JavaScript;
using Microsoft.Extensions.Logging;
using SoccerUlanzi.Entities.Espn;

namespace SoccerUlanzi;

public class Looper
{
    private readonly ILogger<Looper> _logger;
    private readonly Config _config;
    private readonly Espn _espn;
    private readonly AwTrix _awTrix;
    private const string FakeHome = "270";
    private const string FakeGuest = "127";


    public Looper(ILogger<Looper> logger, Config config, Espn espn, AwTrix awTrix)
    {
        _logger = logger;
        _config = config;
        _espn = espn;
        _awTrix = awTrix;
    }


    private int _currentDelay;


    private async Task ChangeDelay(int newDelay)
    {
        if (_currentDelay == newDelay) return;
        _currentDelay = newDelay;
        await _awTrix.ChangeDelay(newDelay);
    }

    private async Task Demo()
    {
        await SendFakeBefore();
        Thread.Sleep(TimeSpan.FromSeconds(10));
        await SendFakeEvent(4, AwTrix.GamesStates.Playing, 0,0);
        Thread.Sleep(TimeSpan.FromSeconds(10));
        await SendFakeEvent(45, AwTrix.GamesStates.Pause, 2,0);
        Thread.Sleep(TimeSpan.FromSeconds(10));
        await SendFakeEvent(102, AwTrix.GamesStates.OverTime, 2,2);
        Thread.Sleep(TimeSpan.FromSeconds(10));
        await SendFakeEvent(102, AwTrix.GamesStates.Finished, 12,2);
        Thread.Sleep(TimeSpan.FromSeconds(10));
    }

    private async Task SendFakeBefore()
    {
        var homeTeam = FakeTeam("270", 0);
        var guestTeam = FakeTeam("127", 0);

        var date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 20, 30,0);
        if (date < DateTime.Now) date = date.AddDays(1);
        await _awTrix.ShowPreview24h(homeTeam, guestTeam, date, "270");
    }

    private Team FakeTeam(string teamId, int goals)
    {
        return new Team
        {
            Goals = goals,
            IconPath = $"./cache/{teamId}.png",
            IconUrl = ""
        };
    }
    
    private async Task SendFakeEvent(int minutesIntoGame, AwTrix.GamesStates state, int homeGoals, int guestGoals)
    {
        var homeTeam = FakeTeam("270", homeGoals);
        var guestTeam = FakeTeam("127", guestGoals);

        await _awTrix.SendNewStandings(homeTeam, guestTeam, minutesIntoGame, state, "270");
    }

    private async Task Uninstall()
    {
        await _awTrix.ChangeDelay(_config.DisplayDelayWhenOff);
        _logger.LogInformation("SorrerUlanzi is now Uninstalled from device");
    }

    public async Task Loop()
    {
        _currentDelay = _config.DisplayDelayWhenOff;
        await _awTrix.DeleteApps(null);
        if (_config.Uninstall)
        {
            await Uninstall();
            return;
        }

        if (_config.DemoMode)
        {
            await Demo();
            return;
        }

        while (true)
        {
            foreach (var teamId in _config.TeamIds)
            {
                await _espn.GetGamesFor(teamId);
            }

            var isAnyActiveGame = _espn.AnyActiveGame();

            await ChangeDelay(isAnyActiveGame ? _config.DisplayDelayOnGames : _config.DisplayDelayWhenOff);

            foreach (var teamId in _config.TeamIds)
            {
                await _espn.DisplayNextOrCurrentGame(teamId);
                Thread.Sleep(_config.DelayOnActiveGames);
            }

            if (!isAnyActiveGame) Thread.Sleep(_config.DelayWhenIdle);
        }
    }
}