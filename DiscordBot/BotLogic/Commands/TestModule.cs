using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.BotLogic.Commands
{
    public class TestModule : ModuleBase<SocketCommandContext>
    {
        [Command("echo")]
        [Summary("Echoes a message")]
        public async Task EchoCommand([Remainder] [Summary("The text to echo")] string text)
        {
            await ReplyAsync(text);
        }

        [Command("ping")]
        [Summary("Ping")]
        public async Task PingCommand()
        {
            await ReplyAsync($"{Context.Client.Latency}ms");
        }
    }
}
