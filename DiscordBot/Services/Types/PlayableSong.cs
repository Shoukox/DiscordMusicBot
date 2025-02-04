using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Services.Types
{
    public struct PlayableSong
    {
        public Stream? AudioStream;
        public string SongTitle;
    }
}
