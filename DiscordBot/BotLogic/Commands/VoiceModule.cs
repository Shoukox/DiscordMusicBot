using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Commands.Builders;
using Discord.WebSocket;
using DiscordBot.BotLogic.Data;
using DiscordBot.Database;
using DiscordBot.Services;
using DiscordBot.Services.Types;
using FFmpeg.AutoGen;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using static System.Net.Mime.MediaTypeNames;

namespace DiscordBot.BotLogic.Commands
{
    public class VoiceModule(VoiceService voiceService, BotContext dbContext, ILogger<VoiceModule> logger) : ModuleBase<SocketCommandContext>
    {

        [Command("join", RunMode = RunMode.Async)]
        [Summary("Joins voice channel")]
        public async Task JoinVoiceChannelCommand()
        {
            var guildUser = Context.User as SocketGuildUser;
            if (guildUser == null)
            {
                await ReplyAsync("Use this command in servers");
                return;
            }

            // Checks for caller
            bool shouldReturn =
                !await Context.CheckIsCallerInServer(guildUser)
                && !await Context.CheckIsCallerInVC(guildUser!);
            if (shouldReturn) return;

            // Checks for bot
            ConnectToVoiceChannelResult result = await Context.ConnectToServerIfNeeded(voiceService, guildUser);
            if (result.Equals(ConnectToVoiceChannelResult.AlreadyInVC))
            {
                await Context.Message.ReplyAsync("I'm already in a voice chat");
                return;
            }

            var voiceChannelLink = Context.GetVoiceChannelLink(guildUser!.VoiceChannel.Id);
            await ReplyAsync($"Joined {voiceChannelLink}");
        }

        [Command("noise", RunMode = RunMode.Async)]
        [Summary("Plays some noise sound")]
        public async Task PlayNoiseCommand(int seconds = 30)
        {
            var guildUser = (SocketGuildUser)Context.User;

            // Checks for caller
            bool shouldReturn =
                !await Context.CheckIsCallerInServer(guildUser)
                && !await Context.CheckIsCallerInVC(guildUser!);
            if (shouldReturn) return;

            // Checks for bot
            await Context.ConnectToServerIfNeeded(voiceService, guildUser);
            IAudioClient audioClient = voiceService.GetAudioClient(Context.Guild.Id)!;

            PlayableSong noise = new PlayableSong();
            noise.AudioStream = voiceService.GetNoiseStream(audioClient, 30);
            noise.SongTitle = "noise";
            voiceService.EnqueueIntoSongsQueue(Context.Guild.Id, noise);

            int queueCount = voiceService.GetQueueLength(Context.Guild.Id);
            var voiceChannelLink = Context.GetVoiceChannelLink(guildUser!.VoiceChannel.Id);
            await ReplyAsync($"***[Queue: {queueCount}]*** Noise has been queued in voice channel {voiceChannelLink}");

            bool isAlreadyPlaying = voiceService.GetIsPlayingSound(Context.Guild.Id);
            if (!isAlreadyPlaying)
            {
                await voiceService.PlayQueue(Context.Guild.Id, async (song) =>
                {
                    await ReplyAsync($"***[Queue: {voiceService.GetQueueLength(Context.Guild.Id)}]*** **{song.SongTitle}** is playing now!");
                });
            }
        }

        [Command("osusong", RunMode = RunMode.Async)]
        [Summary("Plays first found local osu song")]
        public async Task PlayLocalOsuSongCommand([Remainder] string? text)
        {
            var guildUser = (SocketGuildUser)Context.User;

            // Checks for caller
            bool shouldReturn =
                !await Context.CheckIsCallerInServer(guildUser)
                && !await Context.CheckIsCallerInVC(guildUser!);
            if (shouldReturn) return;

            //"C:\NotSys\Games\osu!\Songs\1579117 Marika Kohno - New story\New story.mp3"
            bool failed;
            string? path = voiceService.GetOsuSongPathBySongNameKeyWords(text!.Split(' '), out failed);
            var songName = path!.Split('\\')[^2];
            if (failed)
            {
                await ReplyAsync("Not found in local osu songs directory.");
                return;
            }

            // Checks for bot
            await Context.ConnectToServerIfNeeded(voiceService, guildUser);
            IAudioClient? audioClient = voiceService.GetAudioClient(Context.Guild.Id);

            PlayableSong playableSong = new PlayableSong();
            playableSong.AudioStream = voiceService.GetSoundStreamFromMP3(path);
            playableSong.SongTitle = songName;
            voiceService.EnqueueIntoSongsQueue(Context.Guild.Id, playableSong);

            int queueCount = voiceService.GetQueueLength(Context.Guild.Id);
            var voiceChannelLink = Context.GetVoiceChannelLink(guildUser!.VoiceChannel.Id);
            await ReplyAsync($"***[Queue: {queueCount}]*** **{songName}** has been queued in voice channel {voiceChannelLink}");

            bool isAlreadyPlaying = voiceService.GetIsPlayingSound(Context.Guild.Id);
            if (!isAlreadyPlaying)
            {
                await voiceService.PlayQueue(Context.Guild.Id, async (song) =>
                {
                    await ReplyAsync($"***[Queue: {voiceService.GetQueueLength(Context.Guild.Id)}]*** **{song.SongTitle}** is playing now!");
                });
            }
        }

        [Command("pause", Aliases = ["p"], RunMode = RunMode.Async)]
        [Summary("Pauses the current playing song")]
        public async Task PauseCommand()
        {
            var guildUser = (SocketGuildUser)Context.User;

            // Checks for caller
            bool shouldReturn =
                !await Context.CheckIsCallerInServer(guildUser)
                && !await Context.CheckIsCallerInVC(guildUser!);
            if (shouldReturn) return;

            // Checks for bot
            await Context.ConnectToServerIfNeeded(voiceService, guildUser);
            IAudioClient? audioClient = voiceService.GetAudioClient(Context.Guild.Id);

            var audioStream = voiceService.GetAudioOutStream(Context.Guild.Id);
            voiceService.PauseQueue(Context.Guild.Id);

            int queueCount = voiceService.GetQueueLength(Context.Guild.Id);
            await ReplyAsync($"***[Queue: {queueCount}]*** Paused");
        }

        [Command("resume", Aliases = ["r"], RunMode = RunMode.Async)]
        [Summary("Resumes currently paused song")]
        public async Task ResumeCommand()
        {
            var guildUser = (SocketGuildUser)Context.User;

            // Checks for caller
            bool shouldReturn =
                !await Context.CheckIsCallerInServer(guildUser)
                && !await Context.CheckIsCallerInVC(guildUser!);
            if (shouldReturn) return;

            // Checks for bot
            await Context.ConnectToServerIfNeeded(voiceService, guildUser);
            IAudioClient? audioClient = voiceService.GetAudioClient(Context.Guild.Id);
            voiceService.ResumeQueue(Context.Guild.Id);

            int queueCount = voiceService.GetQueueLength(Context.Guild.Id);
            await ReplyAsync($"***[Queue: {queueCount}]*** Resumed");
        }

        [Command("skip", Aliases = ["s"], RunMode = RunMode.Async)]
        [Summary("Resumes currently paused song")]
        public async Task SkipCommand()
        {
            var guildUser = (SocketGuildUser)Context.User;

            // Checks for caller
            bool shouldReturn =
                !await Context.CheckIsCallerInServer(guildUser)
                && !await Context.CheckIsCallerInVC(guildUser!);
            if (shouldReturn) return;

            // Checks for bot
            await Context.ConnectToServerIfNeeded(voiceService, guildUser);
            IAudioClient? audioClient = voiceService.GetAudioClient(Context.Guild.Id);
            voiceService.SkipSongInQueue(Context.Guild.Id);
            await ReplyAsync($"***[Queue: {voiceService.GetQueueLength(Context.Guild.Id)}]*** **Skipped!**");
        }

        [Command("play", RunMode = RunMode.Async)]
        [Summary("Plays a song from youtube")]
        public async Task PlayFromYoutubeCommand([Remainder] string? text)
        {
            var guildUser = (SocketGuildUser)Context.User;

            // Checks for caller
            bool shouldReturn =
                !await Context.CheckIsCallerInServer(guildUser)
                && !await Context.CheckIsCallerInVC(guildUser!);
            if (shouldReturn) return;

            // Checks for bot
            await Context.ConnectToServerIfNeeded(voiceService, guildUser);
            IAudioClient? audioClient = voiceService.GetAudioClient(Context.Guild.Id);

            PlayableSong? playableSong = voiceService.GetSoundStreamFromYoutube(text!);
            if (playableSong!.Value.AudioStream == null)
            {
                throw new Exception("Internal error. Failed to get a video stream");
            }
            voiceService.EnqueueIntoSongsQueue(Context.Guild.Id, playableSong.Value);
            int queueCount = voiceService.GetQueueLength(Context.Guild.Id);
            var songName = playableSong.Value.SongTitle;
            var voiceChannelLink = Context.GetVoiceChannelLink(guildUser!.VoiceChannel.Id);
            await ReplyAsync($"***[Queue: {queueCount}]*** **{songName}** has been queued in voice channel {voiceChannelLink}");

            bool isAlreadyPlaying = voiceService.GetIsPlayingSound(Context.Guild.Id);
            if (!isAlreadyPlaying)
            {
                await voiceService.PlayQueue(Context.Guild.Id, async (song) =>
                {
                    await ReplyAsync($"***[Queue: {voiceService.GetQueueLength(Context.Guild.Id)}]*** **{song.SongTitle}** is playing now!");
                });
            }
        }
    }
}
