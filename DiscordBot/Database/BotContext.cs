using DiscordBot.Database.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace DiscordBot.Database
{
    public class BotContext : DbContext
    {
        public BotContext(DbContextOptions<BotContext> options) : base(options) { }

        public DbSet<DiscordServerModel> DiscordServers { get; set; }
    }
}
