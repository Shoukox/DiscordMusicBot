using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Services.Types
{
    public enum ConnectToVoiceChannelResult
    {
        AlreadyInVC = 0,
        Connected = 1,
        ReconnectedFromAnotherVC = 2,
    }
}
