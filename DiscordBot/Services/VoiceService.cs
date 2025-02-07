using Discord.Audio;
using Discord.Commands;
using DiscordBot.BotLogic.Data;
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
    public class VoiceService(ILogger<VoiceService> logger, IDiscordServerStateCollection discordServerState, YoutubeService youtubeService, FfmpegService ffmpegService)
    {
        private DiscordServerState? DiscordServerState { get; set; }

        private DiscordServerState GetOrCreateDiscordServerStateByGuildId(ulong guildId)
        {
            if (DiscordServerState == null)
            {
                DiscordServerState = discordServerState.GetOrCreateState(guildId);
            }
            return DiscordServerState!;
        }
        private ConcurrentQueue<PlayableSong> GetSongsQueue(ulong guildId)
        {
            DiscordServerState server = GetOrCreateDiscordServerStateByGuildId(guildId);
            server.SongQueue ??= new ConcurrentQueue<PlayableSong>();
            return server.SongQueue!;
        }
        public int GetQueueLength(ulong guildId)
        {
            ConcurrentQueue<PlayableSong> queue = GetSongsQueue(guildId);
            return queue.Count;
        }

        public IAudioClient? GetAudioClient(ulong guildId)
        {
            DiscordServerState server = GetOrCreateDiscordServerStateByGuildId(guildId);
            return server.audioClient;
        }
        public void SetAudioClient(ulong guildId, IAudioClient audioClient)
        {
            DiscordServerState server = GetOrCreateDiscordServerStateByGuildId(guildId);
            if (server.audioClient != null)
            {
                audioClient.Dispose();
            }
            server.audioClient = audioClient;
        }

        public AudioOutStream? GetAudioOutStream(ulong guildId)
        {
            DiscordServerState server = GetOrCreateDiscordServerStateByGuildId(guildId);
            return server.audioStream;
        }
        public void SetAudioOutStream(ulong guildId, AudioOutStream stream)
        {
            DiscordServerState server = GetOrCreateDiscordServerStateByGuildId(guildId);
            server.audioStream = stream;
        }

        public bool GetIsPlayingSound(ulong guildId)
        {
            DiscordServerState server = GetOrCreateDiscordServerStateByGuildId(guildId);
            return server.IsPlayingSound;
        }
        private void SetIsPlayingSound(ulong guildId, bool value)
        {
            DiscordServerState server = GetOrCreateDiscordServerStateByGuildId(guildId);

            server.IsPlayingSound = value;
        }

        public bool GetIsPaused(ulong guildId)
        {
            DiscordServerState server = GetOrCreateDiscordServerStateByGuildId(guildId);
            return server.IsPaused;
        }
        public void SetIsPaused(ulong guildId, bool value)
        {
            DiscordServerState server = GetOrCreateDiscordServerStateByGuildId(guildId);

            server.IsPaused = value;
        }

        public bool GetShouldSkip(ulong guildId)
        {
            DiscordServerState server = GetOrCreateDiscordServerStateByGuildId(guildId);
            return server.ShoukdSkipCurrentSong;
        }
        public void SetShouldSkip(ulong guildId, bool value)
        {
            DiscordServerState server = GetOrCreateDiscordServerStateByGuildId(guildId);

            server.ShoukdSkipCurrentSong = value;
        }

        public void EnqueueIntoSongsQueue(ulong guildId, PlayableSong sound)
        {
            ConcurrentQueue<PlayableSong> songs = GetSongsQueue(guildId);
            songs.Enqueue(sound);
        }
        public void InsertIntoSongsQueue(ulong guildId, PlayableSong sound, int index)
        {
            DiscordServerState server = GetOrCreateDiscordServerStateByGuildId(guildId);
            ConcurrentQueue<PlayableSong>? songs = server.SongQueue;

            var list = songs!.ToList();
            list.Insert(index, sound);

            var newQueue = new ConcurrentQueue<PlayableSong>();
            foreach (PlayableSong stream in list)
            {
                newQueue.Enqueue(stream);
            }
            server.SongQueue = newQueue;

        }
        public PlayableSong DequeueIntoSongsQueue(ulong guildId)
        {
            ConcurrentQueue<PlayableSong> songs = GetSongsQueue(guildId);

            PlayableSong result;
            songs.TryDequeue(out result);

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
            SetIsPlayingSound(guildId, true);
            try
            {
                IAudioClient? audioClient = GetAudioClient(guildId);
                if (audioClient == null)
                    throw new Exception("Bot is not in a voice channel. Can't play song queue");

                ConcurrentQueue<PlayableSong> queue = GetSongsQueue(guildId);
                while (queue.Count > 0)
                {
                    PlayableSong song;
                    if (!queue.TryDequeue(out song))
                    {
                        throw new Exception("Concurrent queue threw an exception while dequeueing");
                    }
                    using Stream stream = song.AudioStream!;
                    using AudioOutStream audioOutStream = audioClient.CreatePCMStream(AudioApplication.Mixed);

                    SetAudioOutStream(guildId, audioOutStream);

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
                            SetShouldSkip(guildId, false);
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
            SetIsPlayingSound(guildId, false);
        }
        public void PauseQueue(ulong guildId)
        {
            bool isPaused = GetIsPaused(guildId);
            bool isPlaying = GetIsPlayingSound(guildId);
            if (isPaused || !isPlaying) return;

            SetIsPaused(guildId, true);
            Stream? audioOutStream = GetAudioOutStream(guildId);
            if (audioOutStream == null) return;
        }
        public void ResumeQueue(ulong guildId)
        {
            SetIsPaused(guildId, false);
        }
        public void SkipSongInQueue(ulong guildId)
        {
            SetShouldSkip(guildId, true);
        }
    }
}
