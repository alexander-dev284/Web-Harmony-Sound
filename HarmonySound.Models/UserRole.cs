using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HarmonySound.Models
{
    public class UserRole : IdentityUserRole<int>
    {
        public User? User { get; set; }
        public Role? Role { get; set; }
    }
}
