using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SoccerUlanzi;

namespace PngToJsonConverter
{
    public class ImageData
    {
        public object[] db { get; set; }
    }

    public class ApiPayload
    {
        public ImageData[] draw { get; set; }
    }

    class Program
    {

        private static ServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();

            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            services.AddSingleton<Config>();
            services.AddScoped<AwTrix>();
            services.AddScoped<Espn>();
            services.AddScoped<Looper>();
            services.AddScoped<Espn>();
            return services.BuildServiceProvider();
        }


        static void Main(string[] args)
        {
            var looper = CreateServiceProvider().GetRequiredService<Looper>();
            looper.Loop().Wait();
        }
    }
}