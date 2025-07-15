using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace HarmonySound.Models
{
    public class User : IdentityUser<int>
    {
        // Tus propiedades personalizadas
        [Required] public string Name { get; set; }
        [Required][EmailAddress] public override string Email { get; set; }
        [Required] public DateTimeOffset RegisterDate { get; set; }
        public string State { get; set; }

        // Profile fields
        public string? Biography { get; set; }
        public string? ProfileImageUrl { get; set; }

        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<UserPlan> UserPlans { get; set; } = new List<UserPlan>();
        public virtual ICollection<Content> Content { get; set; } = new List<Content>();
        public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
        public virtual ICollection<SubscriptionHistory> SubscriptionHistory { get; set; } = new List<SubscriptionHistory>();
        public virtual ICollection<UserLike> UserLikes { get; set; } = new List<UserLike>();
    }
}
