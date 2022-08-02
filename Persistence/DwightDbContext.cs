using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ClashWrapper.Entities.ClanMembers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Dwight;

public class DwightDbContext : DbContext
{
    public DbSet<ClashMember> Members { get; set; }
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
            entity.Property(member => member.Role)
                .HasConversion(new EnumToNumberConverter<ClanRole, int>());
            entity.ToTable("clan_members");
        });

        modelBuilder.Entity<GuildSettings>(entity =>
        {
            entity.HasIndex(settings => settings.GuildId)
                .IsUnique();
            entity.HasKey(settings => settings.GuildId);
            entity.HasMany(settings => settings.Members)
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