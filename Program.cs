using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoccerUlanzi;

namespace PngToJsonConverter
{
    internal abstract class Program
    {
        private static ServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();

            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            services.AddSingleton(JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json")) ??
                                          throw new Exception("Config is missing"));
            services.AddScoped<AwTrix>();
            services.AddScoped<Espn>();
            services.AddScoped<Looper>();
            services.AddScoped<Espn>();
            services.AddScoped<Fake>();
            services.AddScoped<Rest>();
            return services.BuildServiceProvider();
        }


        private static void Main()
        {
            var looper = CreateServiceProvider().GetRequiredService<Looper>();
            looper.Loop().Wait();
        }
    }
}