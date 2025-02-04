using Discord.Audio;
using Discord.Commands;
using DiscordBot.Database;
using DiscordBot.Database.Models;
using DiscordBot.Services.Types;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Videos.Streams;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DiscordBot.Services
{
    public class VoiceService(ILogger<VoiceService> logger, BotContext dbContext, YoutubeService youtubeService, FfmpegService ffmpegService)
    {
        private DiscordServer GetDiscordServerByGuildId(ulong guildId)
        {
            DiscordServer? server = dbContext.DiscordServers.Find(guildId);
            if (server == null)
            {
                throw new Exception("No such server");
            }
            return server;
        }
        private async Task<ConcurrentQueue<PlayableSong>> GetSongsQueue(ulong guildId)
        {
            DiscordServer server = GetDiscordServerByGuildId(guildId);
            server.SongQueue ??= new ConcurrentQueue<PlayableSong>();
            await dbContext.SaveChangesAsync();
            return server.SongQueue!;
        }
        public async Task<int> GetQueueLength(ulong guildId)
        {
            ConcurrentQueue<PlayableSong> queue = await GetSongsQueue(guildId);
            return queue.Count;
        }

        public IAudioClient? GetAudioClient(ulong guildId)
        {
            DiscordServer server = GetDiscordServerByGuildId(guildId);
            return server.audioClient;
        }
        public async Task SetAudioClient(ulong guildId, IAudioClient audioClient)
        {
            DiscordServer server = GetDiscordServerByGuildId(guildId);
            if (server.audioClient != null)
            {
                audioClient.Dispose();
            }
            server.audioClient = audioClient;
            await dbContext.SaveChangesAsync();
        }

        public AudioOutStream? GetAudioOutStream(ulong guildId)
        {
            DiscordServer server = GetDiscordServerByGuildId(guildId);
            return server.audioStream;
        }
        public async Task SetAudioOutStream(ulong guildId, AudioOutStream stream)
        {
            DiscordServer server = GetDiscordServerByGuildId(guildId);
            server.audioStream = stream;
            await dbContext.SaveChangesAsync();
        }

        public bool GetIsPlayingSound(ulong guildId)
        {
            DiscordServer server = GetDiscordServerByGuildId(guildId);
            return server.IsPlayingSound;
        }
        private async Task SetIsPlayingSound(ulong guildId, bool value)
        {
            DiscordServer server = GetDiscordServerByGuildId(guildId);

            server.IsPlayingSound = value;
            await dbContext.SaveChangesAsync();
        }

        public bool GetIsPaused(ulong guildId)
        {
            DiscordServer server = GetDiscordServerByGuildId(guildId);
            return server.IsPaused;
        }
        public async Task SetIsPaused(ulong guildId, bool value)
        {
            DiscordServer server = GetDiscordServerByGuildId(guildId);

            server.IsPaused = value;
            await dbContext.SaveChangesAsync();
        }

        public bool GetShouldSkip(ulong guildId)
        {
            DiscordServer server = GetDiscordServerByGuildId(guildId);
            return server.ShoukdSkipCurrentSong;
        }
        public async Task SetShouldSkip(ulong guildId, bool value)
        {
            DiscordServer server = GetDiscordServerByGuildId(guildId);

            server.ShoukdSkipCurrentSong = value;
            await dbContext.SaveChangesAsync();
        }

        public async Task EnqueueIntoSongsQueue(ulong guildId, PlayableSong sound)
        {
            ConcurrentQueue<PlayableSong> songs = await GetSongsQueue(guildId);
            songs.Enqueue(sound);
            await dbContext.SaveChangesAsync();
        }
        public async Task InsertIntoSongsQueue(ulong guildId, PlayableSong sound, int index)
        {
            DiscordServer server = GetDiscordServerByGuildId(guildId);
            ConcurrentQueue<PlayableSong>? songs = server.SongQueue;

            var list = songs!.ToList();
            list.Insert(index, sound);

            var newQueue = new ConcurrentQueue<PlayableSong>();
            foreach (PlayableSong stream in list)
            {
                newQueue.Enqueue(stream);
            }
            server.SongQueue = newQueue;

            await dbContext.SaveChangesAsync();
        }
        public async Task<PlayableSong> DequeueIntoSongsQueue(ulong guildId)
        {
            ConcurrentQueue<PlayableSong> songs = await GetSongsQueue(guildId);

            PlayableSong result;
            songs.TryDequeue(out result);

            await dbContext.SaveChangesAsync();
            return result;
        }

        public string? GetOsuSongPathBySongNameKeyWords(string[] keyWords, out bool soundNotFound)
        {
            var pathToOsu = "C:\\NotSys\\Games\\osu!\\Songs";
            var directories = Directory.GetDirectories(pathToOsu);
            var songPath = directories.FirstOrDefault(m => Array.TrueForAll(keyWords, (n) => m.ToLowerInvariant().Contains(n.ToLowerInvariant())));
            if (songPath == null)
            {
                soundNotFound = true;
                return null;
            }
            soundNotFound = false;
            return Directory.GetFiles(songPath, "*.mp3")[0];
        }
        public Stream GetNoiseStream(IAudioClient audioClient, int seconds)
        {
            int sampleRate = 48000;     // Discord's sample rate
            int channels = 2;           // Stereo
            int bitsPerSample = 16;     // 16-bit audio (2 bytes per sample)
            int bytesPerSample = bitsPerSample / 8;

            int totalBytes = sampleRate * channels * bytesPerSample * seconds;

            byte[] pcmData = new byte[totalBytes];
            Random.Shared.NextBytes(pcmData); // Fill with random noise
            return new MemoryStream(pcmData);
        }

        public PlayableSong? GetSoundStreamFromYoutube(string searchQuery)
        {
            return youtubeService.GetSongFromYoutube(searchQuery, searchQuery.ValidateUrlWithRegex());
        }

        public Stream GetSoundStreamFromMP3(string path)
        {
            return ffmpegService.GetAudioStreamFromPath(path);
        }

        public async Task PlayQueue(ulong guildId, Func<PlayableSong, Task>? onSongPlay = null)
        {
            await SetIsPlayingSound(guildId, true);
            try
            {
                IAudioClient? audioClient = GetAudioClient(guildId);
                if (audioClient == null)
                    throw new Exception("Bot is not in a voice channel. Can't play song queue");

                ConcurrentQueue<PlayableSong> queue = await GetSongsQueue(guildId);
                while (queue.Count > 0)
                {
                    PlayableSong song;
                    if (queue.TryDequeue(out song))
                    {
                        throw new Exception("Concurrent queue threw an exception while dequeueing");
                    }
                    using Stream stream = song.AudioStream!;
                    using AudioOutStream audioOutStream = audioClient.CreatePCMStream(AudioApplication.Music);
                    await SetAudioOutStream(guildId, audioOutStream);

                    if(onSongPlay != null)
                    {
                        _ = Task.Run(() => onSongPlay(song));
                    }

                    byte[] audioBuffer = new byte[3840]; //discord standard audio buffer size
                    int bytesRead;
                    while ((bytesRead = await stream.ReadAsync(audioBuffer, 0, audioBuffer.Length)) > 0)
                    {
                        if (GetIsPaused(guildId))
                            await this.WaitUntil(() => !GetIsPaused(guildId), null);
                        if (GetShouldSkip(guildId))
                        {
                            await SetShouldSkip(guildId, false);
                            break;
                        }

                        await audioOutStream.WriteAsync(audioBuffer, 0, bytesRead);
                    }
                    await audioOutStream.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            await SetIsPlayingSound(guildId, false);
        }
        public async Task PauseQueue(ulong guildId)
        {
            bool isPaused = GetIsPaused(guildId);
            bool isPlaying = GetIsPlayingSound(guildId);
            if (isPaused || !isPlaying) return;

            await SetIsPaused(guildId, true);
            Stream? audioOutStream = GetAudioOutStream(guildId);
            if (audioOutStream == null) return;
        }
        public async Task ResumeQueue(ulong guildId)
        {
            await SetIsPaused(guildId, false);
        }
        public async Task SkipSongInQueue(ulong guildId)
        {
            await SetShouldSkip(guildId, true);
        }
    }
}
