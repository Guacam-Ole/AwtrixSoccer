using System.Text;
using Microsoft.Extensions.Logging;

namespace SoccerUlanzi;

public class Rest
{
    private readonly ILogger<Rest> _logger;

    public Rest(ILogger<Rest> logger)
    {
        _logger = logger;
    }

    public async Task Post(string url, string contents)
    {
        using var httpClient = new HttpClient();
        await httpClient.PostAsync(url, new StringContent(contents, Encoding.UTF8));
    }

    public async Task<HttpResponseMessage?> Get(string url)
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode) return response;
        _logger.LogWarning("Failed on Get request to '{Url}'. Status: '{Status}'", url, response.StatusCode);
        return null;
    }

    public async Task<string?> GetString(string url)
    {
        try
        {
            using var httpClient = new HttpClient();
            return await httpClient.GetStringAsync(url);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed receiving string from '{Url}'", url);
            return null;
        }
    }
}