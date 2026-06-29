using Microsoft.EntityFrameworkCore;
using HarmonySound.Models;
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
        public DbSet<Statistic> Statistics { get; set; } = default!;
        public DbSet<SubscriptionHistory> SubscriptionsHistories { get; set; } = default!;
        public DbSet<UserPlan> UsersPlans { get; set; } = default!;
        public DbSet<UserLike> UserLikes { get; set; } = default!;
        public DbSet<UserFollow> UserFollows { get; set; } = default!;
        public DbSet<PlanInvitation> PlanInvitations { get; set; } = default!;
        public DbSet<HarmonySound.Models.Playlist> Playlist { get; set; } = default!;
        public DbSet<HarmonySound.Models.PlaylistContent> PlaylistContents { get; set; } = default!;

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

            // Configuración para PlanInvitation
            builder.Entity<PlanInvitation>(entity =>
            {
                entity.HasOne(pi => pi.Inviter)
                    .WithMany()
                    .HasForeignKey(pi => pi.InviterId)
                    .OnDelete(DeleteBehavior.Restrict);
                    
                entity.HasOne(pi => pi.Invitee)
                    .WithMany()
                    .HasForeignKey(pi => pi.InviteeId)
                    .OnDelete(DeleteBehavior.SetNull);
                    
                entity.HasOne(pi => pi.Plan)
                    .WithMany()
                    .HasForeignKey(pi => pi.PlanId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasIndex(pi => pi.InvitationToken)
                    .IsUnique();
            });

            // Configuración para UserFollow (seguimiento usuario -> artista)
            builder.Entity<UserFollow>(follow =>
            {
                follow.HasOne(f => f.Follower)
                    .WithMany(u => u.Following)
                    .HasForeignKey(f => f.FollowerId)
                    .OnDelete(DeleteBehavior.Restrict);

                follow.HasOne(f => f.Artist)
                    .WithMany(u => u.Followers)
                    .HasForeignKey(f => f.ArtistId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Evita seguir dos veces al mismo artista.
                follow.HasIndex(f => new { f.FollowerId, f.ArtistId })
                    .IsUnique();
            });
        }
    }
}
