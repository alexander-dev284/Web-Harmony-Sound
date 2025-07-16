using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HarmonySound.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace HarmonySound.API.Data
{
    public class HarmonySoundDbContext : IdentityDbContext<User, Role, int, 
        Microsoft.AspNetCore.Identity.IdentityUserClaim<int>,
        UserRole,
        Microsoft.AspNetCore.Identity.IdentityUserLogin<int>,
        Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>,
        Microsoft.AspNetCore.Identity.IdentityUserToken<int>>
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
        public DbSet<Statistic> Statistics { get; set; } = default!;
        public DbSet<SubscriptionHistory> SubscriptionsHistories { get; set; } = default!;
        public DbSet<UserPlan> UsersPlans { get; set; } = default!;
        public DbSet<UserLike> UserLikes { get; set; } = default!;

        // No agregues DbSet<Role> ni DbSet<UserRole> aquí

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configuración para UserRole
            builder.Entity<UserRole>(userRole =>
            {
                userRole.HasKey(ur => new { ur.UserId, ur.RoleId });

                userRole.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();

                userRole.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();
            });

            // Configuración para PlaylistContent (tabla intermedia)
            builder.Entity<PlaylistContent>()
                .HasKey(pc => new { pc.PlaylistId, pc.ContentId });

            builder.Entity<PlaylistContent>()
                .HasOne(pc => pc.Playlist)
                .WithMany(p => p.PlaylistContents)
                .HasForeignKey(pc => pc.PlaylistId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PlaylistContent>()
                .HasOne(pc => pc.Content)
                .WithMany()
                .HasForeignKey(pc => pc.ContentId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configuración para Playlist
            builder.Entity<Playlist>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public DbSet<HarmonySound.Models.Playlist> Playlist { get; set; } = default!;
        public DbSet<HarmonySound.Models.PlaylistContent> PlaylistContents { get; set; } = default!;
    }
}
