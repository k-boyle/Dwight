﻿// <auto-generated />
using System;
using Dwight;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dwight.Migrations
{
    [DbContext(typeof(DwightDbContext))]
    partial class DwightDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Dwight.ClashMember", b =>
                {
                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("DiscordId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("MainTag")
                        .HasColumnType("integer");

                    b.Property<int>("Role")
                        .HasColumnType("integer");

                    b.Property<string[]>("Tags")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.HasKey("GuildId", "DiscordId");

                    b.HasIndex("DiscordId");

                    b.ToTable("clan_members", (string)null);
                });

            modelBuilder.Entity("Dwight.GuildSettings", b =>
                {
                    b.Property<decimal>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("ClanTag")
                        .HasColumnType("text");

                    b.Property<decimal>("CoLeaderRoleId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("ElderRoleId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("GeneralChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("UnverifiedRoleId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("VerifiedRoleId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("WarChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("WarRole")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("WelcomeChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("GuildId");

                    b.HasIndex("GuildId")
                        .IsUnique();

                    b.ToTable("guild_settings", (string)null);
                });

            modelBuilder.Entity("Dwight.ClashMember", b =>
                {
                    b.HasOne("Dwight.GuildSettings", null)
                        .WithMany("Members")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Dwight.GuildSettings", b =>
                {
                    b.Navigation("Members");
                });
#pragma warning restore 612, 618
        }
    }
}
