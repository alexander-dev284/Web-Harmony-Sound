using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace HarmonySound.Models
{
    public class Role : IdentityRole<int>
    {
        [Required]
        [MaxLength(20)]
        public string RoleName { get; set; }

        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
