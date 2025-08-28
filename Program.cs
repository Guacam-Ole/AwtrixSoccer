using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Loki;
using SoccerUlanzi;

namespace PngToJsonConverter
{
    internal abstract class Program
    {
        private static ServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSerilog(cfg =>
            {
                cfg.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("job", Assembly.GetEntryAssembly()?.GetName().Name)
                    .Enrich.WithProperty("desktop", Environment.GetEnvironmentVariable("DESKTOP_SESSION"))
                    .Enrich.WithProperty("language", Environment.GetEnvironmentVariable("LANGUAGE"))
                    .Enrich.WithProperty("lc", Environment.GetEnvironmentVariable("LC_NAME"))
                    .WriteTo.LokiHttp("http://localhost:3100");
                if (Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ==
                    "Debug")
                {
                    cfg.WriteTo.Console(new RenderedCompactJsonFormatter());
                }
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