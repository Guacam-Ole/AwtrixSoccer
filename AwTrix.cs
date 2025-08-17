using System.Numerics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace SoccerUlanzi;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class Team
{
    public string? IconPath { get; init; }
    public int Goals { get; init; }
    public string? IconUrl { get; init; }
    public int[]? ImageContents { get; set; }
}

public class AwTrix
{
    private static string? _previousPayLoad;
    private readonly Config _config;
    private readonly ILogger<AwTrix> _logger;
    private readonly Rest _rest;

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private const string AppUrl = "/api/custom?name=soccer";
    private const string NotifyUrl = "/api/notify";


    public AwTrix(Config config, ILogger<AwTrix> logger, Rest rest)
    {
        _config = config;
        _logger = logger;
        _rest = rest;
    }

    private async Task AddByteArrayToTeam(Team team, int size)
    {
        if (team.IconPath == null) return;
        if (!File.Exists(team.IconPath) && team.IconUrl != null)
        {
            if (!await DownloadAndResizeIcon(team.IconUrl, team.IconPath, size)) return;
        }

        team.ImageContents = ConvertPngToByteArray(team.IconPath, size);
    }

    public async Task ShowPreview(Team home, Team guest, DateTime time, string teamId)
    {
        await AddByteArrayToTeam(home, 6);
        await AddByteArrayToTeam(guest, 6);
        var payload = "{ 'draw': \n[\n";
        payload += GetImagePayload(home, 0, 1, 6);
        payload += GetImagePayload(guest, 25, 1, 6);
        payload += GetTimePayload(time, 7);

        payload += "]";
        payload += ",'hold' : true  ";
        payload += "}";

        await SendApp(payload, teamId);
        await SwitchApp(teamId);
    }

    public async Task DeleteApps(string? teamId)
    {
        var url = GetUrl(AppUrl, teamId);
         await _rest.Post(url, string.Empty);
        _logger.LogInformation("App for '{App}' removed", teamId);
    }

    public enum GamesStates
    {
        Playing,
        Pause,
        OverTime,
        Finished
    }

    public async Task SendNewStandings(Team home, Team guest, int minutes, GamesStates gameState, string teamId)
    {
        await AddByteArrayToTeam(home, 8);
        await AddByteArrayToTeam(guest, 8);

        var payload = "{ 'draw': \n[\n";
        payload += GetImagePayload(home, 0);
        payload += GetGoalPayload(home, 9, true);


        payload += GetGoalPayload(guest, 17, false);
        payload += GetImagePayload(guest, 23);

        payload += GetGameTimePayload(minutes, 90, gameState);

        // Dots:
        payload += "{ 'dp' : [ 15, 3, '#CCCCCC' ]},\n";
        payload += "{ 'dp' : [ 15, 5, '#CCCCCC' ]}\n";

        payload += "]";
        payload += ",'hold' : true  ";
        payload += "}";

        await DismissNotification();
        //await SendNotification(payload);

        await SendApp(payload, teamId);
        await SwitchApp(teamId);
    }

    private string GetGameTimePayload(int minute, int maxMinutes, GamesStates gamesState)
    {
        var color = gamesState switch
        {
            GamesStates.Finished => "#000000",
            GamesStates.OverTime => "#FF0000",
            GamesStates.Pause => "#CCCC00",
            GamesStates.Playing => "#AAAAAA",
            _ => "#000000"
        };

        string payload = "";
        var position = 26 * minute / maxMinutes;
        if (position > 26)
        {
            position = 30; // Overtime
        }

        if (position < 0) position = 0;
        payload += "{ 'dl' : [2,0," + (2 + position) + ",0,'" + color + "' ]},\n";
        if (position < 26)
        {
            payload += "{ 'dl' : [" + (position + 3) + ",0,30,0, '#444444' ] },\n";
        }

        return payload;
    }

    private string GetImagePayload(Team team, int x, int y = 1, int size = 8)
    {
        if (team.ImageContents == null || team.ImageContents.Length == 0) return string.Empty;
        return "{ 'db': [" + x + "," + y + "," + size + "," + size + "," + ByteArrayToPayload(team.ImageContents) +
               "]},\n";
    }

    private string GetTimePayload(DateTime time, int x)
    {
        string payload = "{ 'dt' : [ " + x + ", 2, '" + time.ToLocalTime().ToString("HH:mm") + "', '#FFFFFF' ]}\n";
        return payload;
    }

    private string GetGoalPayload(Team team, int x, bool left)
    {
        var lowerDigitPos = x;
        if (left || team.Goals > 9) lowerDigitPos += 2;

        var payload = string.Empty;
        if (team.Goals > 9)
        {
            payload += "{ 'dl' : [" + x + ",2," + x + ",6, '#FFFFFF' ]},\n";
        }

        payload += "{ 'dt' : [ " + lowerDigitPos + ", 2, '" + team.Goals % 10 + "', '#FFFFFF' ]},\n";

        return payload;
    }

    private async Task<bool> DownloadAndResizeIcon(string url, string filename, int width = 8, int height = 8)
    {
        if (string.IsNullOrEmpty(url)) return false;
        var response = await _rest.Get(url);
        
        if (response==null)
        {
            _logger.LogWarning("Failed to download image.");
            return false;
        }
        

        var stream = await response.Content.ReadAsStreamAsync();
        using var image = await Image.LoadAsync(stream);
        RemoveTransparent(image);
        image.Mutate(x => x.Resize(width, height));

        await image.SaveAsync(filename, new PngEncoder());
        return true;
    }

    private static void RemoveTransparent(Image image)
    {
        image.Mutate(x => x.ProcessPixelRowsAsVector4(row =>
        {
            for (var i = 0; i < row.Length; i++)
            {
                ref var pixel = ref row[i];
                if (pixel.W <= 0.25f) 
                {
                    pixel = new Vector4(0, 0, 0, 1); 
                }
            }
        }));
    }

    private static int[] ConvertPngToByteArray(string imagePath, int size = 8)
    {
        using var image = Image.Load<Rgba32>(imagePath);
        image.Mutate(x => x.Resize(size, size));

        var pixelArray = new int[size * size];

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var pixel = image[x, y];
                var colorValue = (pixel.R << 16) | (pixel.G << 8) | pixel.B;
                pixelArray[y * size + x] = colorValue;
            }
        }

        return pixelArray;
    }

    private string ByteArrayToPayload(int[] pixelArray)
    {
        return JsonSerializer.Serialize(pixelArray, _serializerOptions);
    }

    private string GetUrl(string target, string? suffix = null)
    {
        return $"http://{_config.DeviceIp}{target}{suffix}";
    }



    private async Task DismissNotification()
    {
        var url = GetUrl(NotifyUrl, "/dismiss");
        await _rest.Get(url);
    }


    private async Task SwitchApp(string gameId)
    {
        var url = GetUrl("/api/switch");
        await _rest.Post(url, "{ 'name':'soccer" + gameId + "'}");
    }

    private async Task SendApp(string json, string gameId)
    {
        if (json == _previousPayLoad) return;
        var url = GetUrl(AppUrl, gameId);
        await _rest.Post(url, json);
        _previousPayLoad = json;
    }

    public async Task ChangeDelay(int newDelay)
    {
        var url = GetUrl("/api/settings");
        await _rest.Post(url, "{'ATIME' : " + newDelay + "}");
    }
}