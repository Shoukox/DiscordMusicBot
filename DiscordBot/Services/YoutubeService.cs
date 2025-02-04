using Discord.Audio;
using DiscordBot.Services.Types;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeExplode.Search;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode;
using System.Diagnostics;
using System.IO;
using static System.Net.WebRequestMethods;
using FFmpeg.AutoGen;
using AngleSharp.Dom;
using CliWrap;

namespace DiscordBot.Services
{
    public class YoutubeService(ILogger<YoutubeService> logger)
    {
        public const string YtDlpFileName = "yt-dlp";
        public const string FfmpegFileName = "ffmpeg";

        private string? _searchQuery { get; set; }
        private bool _isUri { get; set; }

        public string GetCommandArgumentsForYTDLP(string query, bool isUri)
        {
            if (!isUri) query = $"ytsearch:\"{query}\"";
            return $"-f bestaudio --quiet -o - {query}";
        }
        public string GetCommandArgumentsForYTDLP_SongTitle(string query, bool isUri)
        {
            if (!isUri) query = $"ytsearch:\"{query}\"";
            return $"--print title {query}";
        }
        public string GetCommandArgumentsForFFMPEG = $"-hide_banner -loglevel panic -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1";

        public PlayableSong? GetSongFromYoutube(string searchQuery, bool isUri)
        {
            _isUri = isUri;
            _searchQuery = searchQuery;

            PlayableSong? playableSong = new PlayableSong()
            {
                AudioStream = GetStandardOutputForFfmpegConvertation(),
                SongTitle = GetSongTitleFromYoutube(),
            };
            return playableSong;
        }

        private Stream GetStandardOutputForFfmpegConvertation()
        {
            Process ytDlp = new Process();
            ytDlp.StartInfo.FileName = YtDlpFileName;
            ytDlp.StartInfo.Arguments = GetCommandArgumentsForYTDLP(_searchQuery!, _isUri);
            ytDlp.StartInfo.UseShellExecute = false;
            ytDlp.StartInfo.RedirectStandardOutput = true;
            ytDlp.Start();
            StreamReader ytDlpStandardOutput = ytDlp.StandardOutput;

            Process ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = FfmpegFileName;
            ffmpeg.StartInfo.Arguments = GetCommandArgumentsForFFMPEG;
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.RedirectStandardInput = true;
            ffmpeg.StartInfo.RedirectStandardOutput = true;
            ffmpeg.Start();

            _ = Task.Run(() =>
            {
                byte[] buffer = new byte[3840];
                int bytesRead;
                while ((bytesRead = ytDlpStandardOutput.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ffmpeg.StandardInput.BaseStream.Write(buffer, 0, bytesRead);
                }
                ffmpeg.StandardInput.Close();
            });

            return ffmpeg.StandardOutput.BaseStream;
        }


        private string GetSongTitleFromYoutube()
        {
            Process ytDlp = new Process();
            ytDlp.StartInfo.FileName = YtDlpFileName;
            ytDlp.StartInfo.Arguments = GetCommandArgumentsForYTDLP_SongTitle(_searchQuery!, _isUri);
            ytDlp.StartInfo.UseShellExecute = false;
            ytDlp.StartInfo.RedirectStandardOutput = true;
            ytDlp.Start();
            return ytDlp.StandardOutput.ReadToEnd();
        }
    }
}
