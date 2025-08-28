using System.Collections;
using Microsoft.Extensions.Logging;

namespace SoccerUlanzi;

public class Looper
{
    private readonly ILogger<Looper> _logger;
    private readonly Config _config;
    private readonly Espn _espn;
    private readonly AwTrix _awTrix;
    private readonly Fake _fake;
    private int _currentDelay;

    public Looper(ILogger<Looper> logger, Config config, Espn espn, AwTrix awTrix, Fake fake)
    {
        _logger = logger;
        _config = config;
        _espn = espn;
        _awTrix = awTrix;
        _fake = fake;
    }

    private async Task ChangeDelay(int newDelay)
    {
        if (_currentDelay == newDelay) return;
        _currentDelay = newDelay;
        await _awTrix.ChangeDelay(newDelay);
    }

    private async Task Uninstall()
    {
        await _awTrix.ChangeDelay(_config.DisplayDelayWhenOff);
        _logger.LogInformation("SoccerUlanzi is now uninstalled from device");
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
            await _fake.Demo();
            return;
        }

        while (true)
        {
            var requestTasks = _config.TeamIds.Select(teamId => _espn.GetGamesFor(teamId)).ToList();

            Task.WaitAll(requestTasks);

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