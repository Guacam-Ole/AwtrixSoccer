using System.Collections;
using Microsoft.Extensions.Logging;

namespace SoccerUlanzi;

public class Looper
{
    private readonly ILogger<Looper> _logger;
    private readonly Config _config;
    private readonly Collector _collector;
    private readonly AwTrix.Display _awTrix;
 

    public Looper(ILogger<Looper> logger, Config config,  Collector collector,  AwTrix.Display awTrix)
    {
        _logger = logger;
        _config = config;
        _collector = collector;
        _awTrix = awTrix;
    }



    private async Task Uninstall()
    {
        await _awTrix.ChangeDelay(_config.DisplayDelayWhenOff);
        _logger.LogInformation("SoccerUlanzi is now uninstalled from device");
    }

    
    public async Task Loop()
    {
        await _awTrix.DeleteApps(null);
        if (_config.Uninstall)
        {
            await Uninstall();
            return;
        }

        
        while (true)
        {
            await _collector.SitAndWait();

        }
    }
}