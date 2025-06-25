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
        public List<UserRole>? UserRoles { get; set; }
        public List<UserPlan>? UserPlans { get; set; }
        public List<Content>? Content { get; set; }
        public List<Report>? Reports { get; set; }
        public List<SubscriptionHistory>? SubscriptionHistory { get; set; }
    }
}
