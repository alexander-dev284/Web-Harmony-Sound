using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;

namespace HarmonySound.Models
{
    public class UserRole : IdentityUserRole<int>
    {
        [JsonIgnore] 
        public virtual User? User { get; set; }
        
        [JsonIgnore] 
        public virtual Role? Role { get; set; }
    }
}
