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

        public DbSet<HarmonySound.Models.Album> Album { get; set; } = default!;

public DbSet<HarmonySound.Models.Content> Content { get; set; } = default!;

public DbSet<HarmonySound.Models.ContentAlbum> ContentAlbum { get; set; } = default!;

public DbSet<HarmonySound.Models.Plan> Plan { get; set; } = default!;

public DbSet<HarmonySound.Models.Report> Report { get; set; } = default!;

public DbSet<HarmonySound.Models.Role> Role { get; set; } = default!;

public DbSet<HarmonySound.Models.Statistic> Statistic { get; set; } = default!;

public DbSet<HarmonySound.Models.SubscriptionHistory> SubscriptionHistory { get; set; } = default!;

public DbSet<HarmonySound.Models.User> User { get; set; } = default!;

public DbSet<HarmonySound.Models.UserPlan> UserPlan { get; set; } = default!;

public DbSet<HarmonySound.Models.UserRole> UserRole { get; set; } = default!;
    }
