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

namespace DiscordBot.Services
{
    public class FfmpegService(ILogger<FfmpegService> logger)
    {
        public const string FfmpegFileName = "ffmpeg";
        public string GetCommandArgumentsForPCMStream(string path) =>
            $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -y -f s16le -ar 48000 pipe:1";

        public Process GetCommandProcess(string args)
        {
            Process ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = FfmpegFileName;
            ffmpeg.StartInfo.Arguments = args;
            ffmpeg.StartInfo.UseShellExecute = false;
            return ffmpeg;
        }

        public Process GetCommandProcessForPCMStream(string path)
        {
            Process ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = FfmpegFileName;
            ffmpeg.StartInfo.Arguments = GetCommandArgumentsForPCMStream(path);
            ffmpeg.StartInfo.UseShellExecute = false;
            return ffmpeg;
        }
    }
}
