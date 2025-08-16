using Microsoft.Extensions.Logging;

namespace SoccerUlanzi;

public class Looper
{
    private readonly ILogger<Looper> _logger;
    private readonly Config _config;
    private readonly Espn _espn;
    private readonly AwTrix _awTrix;


    public Looper(ILogger<Looper> logger, Config config, Espn espn, AwTrix awTrix)
    {
        _logger = logger;
        _config = config;
        _espn = espn;
        _awTrix = awTrix;
    }

  


    public async Task Loop()
    {
        await _awTrix.DeleteApps(null);
        while (true)
        {
            foreach (var teamId in _config.TeamIds)
            {
                await _espn.GetGamesFor(teamId);
                await _espn.DisplayNextOrCurrentGame(teamId);
                Thread.Sleep(TimeSpan.FromSeconds(5));    
            }
        }
    }
}