﻿// <auto-generated />
using System;
using Dwight;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Dwight.Migrations
{
    [DbContext(typeof(DwightDbContext))]
    [Migration("20210807210459_cascade")]
    partial class cascade
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.8")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("Dwight.ClashMember", b =>
                {
                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("DiscordId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("MainTag")
                        .HasColumnType("integer");

                    b.Property<string[]>("Tags")
                        .HasColumnType("text[]");

                    b.HasKey("GuildId", "DiscordId");

                    b.HasIndex("DiscordId");

                    b.ToTable("clan_members");
                });

            modelBuilder.Entity("Dwight.FwaRep", b =>
                {
                    b.Property<decimal>("GuildId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("DiscordId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<float>("TimeZone")
                        .HasColumnType("real");

                    b.HasKey("GuildId", "DiscordId");

                    b.HasIndex("DiscordId");

                    b.ToTable("fwa_reps");
                });

            modelBuilder.Entity("Dwight.GuildSettings", b =>
                {
                    b.Property<decimal>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("CalendarLink")
                        .HasColumnType("text");

                    b.Property<string>("ClanTag")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("CoLeaderRoleId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("ElderRoleId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("GeneralChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("RepRoleId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("StartTimeChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("UnverifiedRoleId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("VerifiedRoleId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("WarChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("WelcomeChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("GuildId");

                    b.HasIndex("GuildId")
                        .IsUnique();

                    b.ToTable("guild_settings");
                });

            modelBuilder.Entity("Dwight.ClashMember", b =>
                {
                    b.HasOne("Dwight.GuildSettings", null)
                        .WithMany("Members")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Dwight.FwaRep", b =>
                {
                    b.HasOne("Dwight.GuildSettings", null)
                        .WithMany("FwaReps")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Dwight.ClashMember", "Member")
                        .WithOne()
                        .HasForeignKey("Dwight.FwaRep", "GuildId", "DiscordId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Member");
                });

            modelBuilder.Entity("Dwight.GuildSettings", b =>
                {
                    b.Navigation("FwaReps");

                    b.Navigation("Members");
                });
#pragma warning restore 612, 618
        }
    }
}
