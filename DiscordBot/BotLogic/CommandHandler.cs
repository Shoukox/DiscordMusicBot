using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Database;
using DiscordBot.Services;
using FFmpeg.AutoGen;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.BotLogic
{
    public class CommandHandler
    {
        public const char Prefix = '!';
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CommandHandler> logger;

        public CommandHandler(DiscordSocketClient client, CommandService commands, IServiceProvider serviceProvider)
        {
            _commands = commands;
            _client = client;
            _serviceProvider = serviceProvider;
            logger = serviceProvider.GetRequiredService<ILogger<CommandHandler>>();
        }

        public async Task InstallCommandsAsync()
        {
            _client.MessageReceived += HandleMessageAsync;

            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                services: _serviceProvider);

            _commands.CommandExecuted += OnCommandExecutedAsync;
        }

        public async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!string.IsNullOrEmpty(result?.ErrorReason))
            {
                await context.Channel.SendMessageAsync(result.ErrorReason);
            }

            var commandName = command.IsSpecified ? command.Value.Name : "A command";
            logger.LogInformation($"{commandName} was executed.");
        }

        private async Task HandleMessageAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;

            if (message!.Author is IGuildUser guildUser)
            {
                BotSynchronization.Instance.AddNewServerIfNeeded(guildUser.GuildId);

                var dbContext = _serviceProvider.GetRequiredService<BotContext>();
                await dbContext.TryAddDiscordServerIntoDB(guildUser.GuildId);
            }

            int argPos = 0;
            if (message.Author.IsBot)
                return;
            if (!message.HasCharPrefix(Prefix, ref argPos))
                return;

            var context = new SocketCommandContext(_client, message);

            await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: _serviceProvider.CreateScope().ServiceProvider);
        }
    }
}