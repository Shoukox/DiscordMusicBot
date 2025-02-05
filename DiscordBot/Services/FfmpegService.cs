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
        public const string FfmpegFileName = "./ffmpeg";
        public string GetCommandArgumentsForPCMStream(string path) =>
            $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1";

        private Process GetCommandProcess(string args)
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = FfmpegFileName,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
            if (process == null)
            {
                throw new Exception($"Failed to get {FfmpegFileName} process");
            }
            return process;
        }

        public Stream GetAudioStreamFromPath(string path)
        {
            Process ytp = GetCommandProcess(GetCommandArgumentsForPCMStream(path));
            var output = ytp.StandardOutput.BaseStream;
            return output;
        }

    }
}
