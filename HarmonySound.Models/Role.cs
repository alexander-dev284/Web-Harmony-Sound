using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace HarmonySound.Models
{
    public class Role : IdentityRole<int>
    {
        [Required]
        [MaxLength(20)]
        public string RoleName { get; set; }
    }
}
