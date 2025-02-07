using Discord.Audio;
using DiscordBot.Services.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.BotLogic.Data
{
    public class DiscordServerState
    {
        public ConcurrentQueue<PlayableSong>? SongQueue { get; set; }

        public IAudioClient? audioClient { get; set; }

        public AudioOutStream? audioStream { get; set; }

        public bool IsPlayingSound { get; set; }

        public bool IsPaused { get; set; }

        public bool ShoukdSkipCurrentSong { get; set; }
    }
}
