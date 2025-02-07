using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.BotLogic;
using DiscordBot.Database;
using DiscordBot.Database.Models;
using DiscordBot.Services.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordBot.Services
{
    public static class BotExtensions
    {
        public static async Task<SocketGuildUser?> GetBotGuildUserInChannel(this SocketCommandContext context)
        {
            IUser botUser = await context.Channel.GetUserAsync(context.Client.CurrentUser.Id);
            SocketGuildUser? botGuildUser = botUser as SocketGuildUser;
            return botGuildUser;
        }

        public static async Task<ConnectToVoiceChannelResult> ConnectToServerIfNeeded(this SocketCommandContext context, VoiceService voiceService, IVoiceChannel voiceChannel)
        {
            var semaphoreSlim = BotSynchronization.Instance.GetSemaphoreSlim(context.Guild.Id);
            semaphoreSlim.Wait();
            ConnectToVoiceChannelResult result;
            IAudioClient? audioClient;
            bool isBotAlreadyInThisVC = await context.CheckIsBotAlreadyInVC(voiceChannel);
            if (!isBotAlreadyInThisVC)
            {
                audioClient = await voiceChannel.ConnectAsync();
                voiceService.SetAudioClient(context.Guild.Id, audioClient);

                result = ConnectToVoiceChannelResult.Connected;
            }
            else
            {
                audioClient = voiceService.GetAudioClient(context.Guild.Id);
                if (audioClient == null)
                {
                    await voiceChannel.DisconnectAsync();
                    audioClient = await voiceChannel.ConnectAsync();
                    voiceService.SetAudioClient(context.Guild.Id, audioClient);
                    result = ConnectToVoiceChannelResult.ReconnectedFromAnotherVC;
                }
                result = ConnectToVoiceChannelResult.AlreadyInVC;
            }
            semaphoreSlim.Release();
            return result;
        }

        public static async Task<bool> CheckIsCallerInServer(this SocketCommandContext context, IGuildUser? guildUser)
        {
            if (guildUser == null)
            {
                await context.Message.ReplyAsync("Use this command only in servers.");
                return false;
            }
            return true;
        }

        public static async Task<bool> CheckIsCallerInVC(this SocketCommandContext context, IGuildUser guildUser)
        {
            if (guildUser.VoiceChannel == null)
            {
                await context.Message.ReplyAsync("You should firstly join any voice channel to use this command.");
                return false;
            }
            return true;
        }

        public static async Task<bool> CheckIsBotAlreadyInVC(this SocketCommandContext context, IVoiceChannel voiceChannelToCheck)
        {
            SocketGuildUser? botGuildUser = await context.GetBotGuildUserInChannel();
            if (botGuildUser != null && botGuildUser.VoiceChannel == voiceChannelToCheck)
            {
                return true;
            }
            return false;
        }

        public static async Task TryAddDiscordServerIntoDB(this BotContext dbContext, ulong guildId)
        {
            DiscordServerModel? server = await dbContext.DiscordServers.FindAsync(guildId);
            if (server == null)
            {
                await dbContext.DiscordServers.AddAsync(new DiscordServerModel() { Id = guildId });
                await dbContext.SaveChangesAsync();
            }
        }

        public static string GetVoiceChannelLink(this SocketCommandContext context, ulong voiceChannelId)
        {
            return $"https://discord.com/channels/{context.Guild.Id}/{voiceChannelId}";
        }


        /// <summary>
        /// Waits until condition is met
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cond">Condition to be met</param>
        /// <param name="frequency">In milliseconds</param>
        /// <returns></returns>
        public static async Task WaitUntil(this object obj, Func<bool> cond, TimeSpan? timeout, int frequency = 50)
        {
            DateTime end = DateTime.MaxValue;
            if(timeout != null)
            {
                end = DateTime.Now.Add(timeout.Value);
            }
            while (DateTime.Now < end)
            {
                if (cond())
                    return;

                await Task.Delay(frequency);
            }
        }

        public static string CreateMD5Hash(this string text)
        {
            byte[] arr = Encoding.UTF8.GetBytes(text);
            byte[] hash = MD5.HashData(arr);

            return Convert.ToHexString(hash);
        }

        public static bool ValidateUrlWithRegex(this string url)
        {
            var urlRegex = new Regex(
                @"^(https?|ftps?):\/\/(?:[a-zA-Z0-9]" +
                        @"(?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}" +
                        @"(?::(?:0|[1-9]\d{0,3}|[1-5]\d{4}|6[0-4]\d{3}" +
                        @"|65[0-4]\d{2}|655[0-2]\d|6553[0-5]))?" +
                        @"(?:\/(?:[-a-zA-Z0-9@%_\+.~#?&=]+\/?)*)?$",
                RegexOptions.IgnoreCase);

            urlRegex.Matches(url);

            return urlRegex.IsMatch(url);
        }
    }
}
