using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HarmonySound.Models
{
    public class UserRole : IdentityUserRole<int>
    {
        public virtual User? User { get; set; }
        public virtual Role? Role { get; set; }
    }
}
