using DiscordBot.BotLogic;
using DiscordBot.BotLogic.Commands;
using DiscordBot.Database;
using DiscordBot.Logging;
using DiscordBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace DiscordBot
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            string discordBotToken = builder.Configuration.GetSection("DiscordBotToken").Value!;

            // Logging
            builder.Logging.AddConsole();
            builder.Logging.AddConsoleFormatter<CustomConsoleFormatter, CustomConsoleFormatterOptions>();

            // Database
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string"+ "'DefaultConnection' not found.");
            builder.Services.AddDbContextPool<BotContext>(options => options.UseSqlite(connectionString));

            // Services
            builder.Services.AddHostedService<Bot>((sp) => 
                        new Bot(discordBotToken, sp.GetRequiredService<ILogger<Bot>>(), sp));
            builder.Services.AddScoped<VoiceService>();
            builder.Services.AddScoped<YoutubeService>();
            builder.Services.AddScoped<FfmpegService>();

            var app = builder.Build();
            app.Run();
        }
    }
}
