﻿using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Dwight
{
    public class DwightDbContext : DbContext
    {
        public DbSet<ClashMember> Members { get; set; }
        public DbSet<FwaRep> FwaReps { get; set; }
        public DbSet<GuildSettings> GuildSettings { get; set; }

        public DwightDbContext(DbContextOptions<DwightDbContext> options) : base(options)
        {
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClashMember>(entity =>
            {
                entity.HasIndex(member => member.DiscordId);
                entity.HasKey(member => new { member.GuildId, member.DiscordId });
                entity.ToTable("clan_members");
            });

            modelBuilder.Entity<FwaRep>(entity =>
            {
                entity.HasIndex(rep => rep.DiscordId);
                entity.HasKey(rep => new { rep.GuildId, rep.DiscordId });
                entity.HasOne(rep => rep.Member)
                    .WithOne()
                    .HasForeignKey<FwaRep>(rep => new { rep.GuildId, rep.DiscordId });
                entity.ToTable("fwa_reps");
            });

            modelBuilder.Entity<GuildSettings>(entity =>
            {
                entity.HasIndex(settings => settings.GuildId)
                    .IsUnique();
                entity.HasKey(settings => settings.GuildId);
                entity.HasMany(settings => settings.Members)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(settings => settings.FwaReps)
                    .WithOne()
                    .OnDelete(DeleteBehavior.Cascade);
                entity.ToTable("guild_settings");
            });
        }

        public async ValueTask<GuildSettings> GetOrCreateSettingsAsync(ulong guildId)
        {
            var settings = await GuildSettings.FindAsync(guildId);
            if (settings != null)
                return settings;

            settings = new(guildId);
            await GuildSettings.AddAsync(settings);
            return settings;
        }
        
        public async ValueTask<GuildSettings> GetOrCreateSettingsAsync<TProp>(ulong guildId, Expression<Func<GuildSettings, TProp>> include)
        {
            var settings = await GuildSettings.Include(include).FirstOrDefaultAsync(settings => settings.GuildId == guildId);
            if (settings != null)
                return settings;

            settings = new(guildId);
            await GuildSettings.AddAsync(settings);
            return settings;
        }
    }
}