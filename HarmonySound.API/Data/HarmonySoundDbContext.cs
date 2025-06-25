using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HarmonySound.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

public class HarmonySoundDbContext : IdentityDbContext<User, Role, int>
{
    public HarmonySoundDbContext(DbContextOptions<HarmonySoundDbContext> options)
        : base(options)
    {
    }

    public DbSet<Album> Albums { get; set; } = default!;
    public DbSet<Content> Contents { get; set; } = default!;
    public DbSet<ContentAlbum> ContentsAlbums { get; set; } = default!;
    public DbSet<Plan> Plans { get; set; } = default!;
    public DbSet<Report> Reports { get; set; } = default!;
    public DbSet<Role> Roles { get; set; } = default!;
    public DbSet<Statistic> Statistics { get; set; } = default!;
    public DbSet<SubscriptionHistory> SubscriptionsHistories { get; set; } = default!;
    public DbSet<UserPlan> UsersPlans { get; set; } = default!;
    public DbSet<UserRole> UsersRoles { get; set; } = default!;
}
