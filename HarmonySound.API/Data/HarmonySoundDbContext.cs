using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HarmonySound.Models;

    public class HarmonySoundDbContext : DbContext
    {
        public HarmonySoundDbContext (DbContextOptions<HarmonySoundDbContext> options)
            : base(options)
        {
        }

        public DbSet<HarmonySound.Models.Album> Albums { get; set; } = default!;

        public DbSet<HarmonySound.Models.Content> Contents { get; set; } = default!;

        public DbSet<HarmonySound.Models.ContentAlbum> ContentsAlbums { get; set; } = default!;

        public DbSet<HarmonySound.Models.Plan> Plans { get; set; } = default!;

        public DbSet<HarmonySound.Models.Report> Reports { get; set; } = default!;

        public DbSet<HarmonySound.Models.Role> Roles { get; set; } = default!;

        public DbSet<HarmonySound.Models.Statistic> Statistics { get; set; } = default!;

        public DbSet<HarmonySound.Models.SubscriptionHistory> SubscriptionsHistories { get; set; } = default!;

        public DbSet<HarmonySound.Models.User> Users { get; set; } = default!;

        public DbSet<HarmonySound.Models.UserPlan> UsersPlans { get; set; } = default!;

        public DbSet<HarmonySound.Models.UserRole> UsersRoles { get; set; } = default!;
            }
