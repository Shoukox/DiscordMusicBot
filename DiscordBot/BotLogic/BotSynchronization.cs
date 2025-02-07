using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.BotLogic
{
    public class BotSynchronization
    {
        private static readonly Lazy<BotSynchronization> instanceHolder = new Lazy<BotSynchronization>(() => new BotSynchronization());
        public static BotSynchronization Instance => instanceHolder.Value;

        private ConcurrentDictionary<ulong, SemaphoreSlim> _syncDict;

        public BotSynchronization()
        {
            _syncDict = new ConcurrentDictionary<ulong, SemaphoreSlim>();
        }

        public bool AddNewServerIfNeeded(ulong guildId)
        {
            return _syncDict.TryAdd(guildId, new SemaphoreSlim(1, 1));
        }

        public SemaphoreSlim GetSemaphoreSlim(ulong guildId)
        {
            return _syncDict.GetOrAdd(guildId, new SemaphoreSlim(1, 1));
        }
    }
}
