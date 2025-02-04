using Discord.Audio;
using DiscordBot.Services.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Database.Models
{
    public class DiscordServer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong Id { get; init; }

        [NotMapped]
        public ConcurrentQueue<PlayableSong>? SongQueue { get; set; }

        [NotMapped]
        public IAudioClient? audioClient { get; set; }

        [NotMapped]
        public AudioOutStream? audioStream { get; set; }

        [NotMapped]
        public bool IsPlayingSound { get; set; }

        [NotMapped]
        public bool IsPaused { get; set; }

        [NotMapped]
        public bool ShoukdSkipCurrentSong { get; set; }
    }
}
