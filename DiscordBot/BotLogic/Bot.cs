using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Net;
using Discord.WebSocket;
using Discord;
using System.Runtime.InteropServices;
using Discord.Commands;

namespace DiscordBot.BotLogic
{
    internal class Bot(string token, ILogger<Bot> logger, IServiceProvider serviceProvider) : BackgroundService
    {
        private readonly ILogger<Bot> Logger = logger;
        private readonly string Token = token;

        public DiscordSocketClient? Client;
        private CommandService? CommandService;
        private CommandHandler? CommandHandler;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Start(stoppingToken);
        }

        private async Task Start(CancellationToken stoppingToken)
        {
            var config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All,
            }; 
            Client = new DiscordSocketClient(config);
            Client.Log += OnLog;
            Client.Ready += OnReady;
            await Client.LoginAsync(TokenType.Bot, Token);
            await Client.StartAsync();
            
            await InitializeCommands();

            await Task.Delay(-1);
        }

        private async Task InitializeCommands()
        {
            var config = new CommandServiceConfig()
            {
                ThrowOnError = false,
                CaseSensitiveCommands = false,
            };
            CommandService = new CommandService(config);

            CommandHandler = new CommandHandler(Client!, CommandService, serviceProvider.CreateAsyncScope().ServiceProvider);
            await CommandHandler.InstallCommandsAsync();
        }

        private Task OnLog(LogMessage logMessage)
        {
            LogLevel logLevel = logMessage.Severity switch
            {
                LogSeverity.Debug => LogLevel.Debug,
                LogSeverity.Verbose => LogLevel.Trace,
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Critical => LogLevel.Critical,
                _ => LogLevel.Information
            };
            Logger.Log(logLevel, logMessage.Message);
            return Task.CompletedTask;
        }

        private Task OnReady()
        {
            Logger.LogInformation("Bot is ready to use!");
            return Task.CompletedTask;
        }
    }
}
