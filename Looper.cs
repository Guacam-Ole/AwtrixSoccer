using System.Collections;
using Microsoft.Extensions.Logging;

namespace SoccerUlanzi;

public class Looper
{
    private readonly ILogger<Looper> _logger;
    private readonly Config _config;
    private readonly Espn _espn;
    private readonly AwTrix _awTrix;
    private int _currentDelay;

    public Looper(ILogger<Looper> logger, Config config, Espn espn, AwTrix awTrix)
    {
        _logger = logger;
        _config = config;
        _espn = espn;
        _awTrix = awTrix;
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

  

        while (true)
        {
            var requestTasks = _config.Teams.Select(team => _espn.GetGamesFor(team)).ToList();

            Task.WaitAll(requestTasks);

            var isAnyActiveGame = _espn.AnyActiveGame();

            await ChangeDelay(isAnyActiveGame ? _config.DisplayDelayOnGames : _config.DisplayDelayWhenOff);

            foreach (var team in _config.Teams)
            {
                await _espn.DisplayNextOrCurrentGame(team);
                
            }
            Thread.Sleep(_config.DelayOnActiveGames);

            if (!isAnyActiveGame) Thread.Sleep(_config.DelayWhenIdle);
        }
    }
}