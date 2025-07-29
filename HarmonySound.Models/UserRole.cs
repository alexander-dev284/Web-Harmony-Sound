using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;

namespace HarmonySound.Models
{
    public class UserRole : IdentityUserRole<int>
    {
        [JsonIgnore] // ✅ AGREGAR: Evitar ciclo de referencia
        public virtual User? User { get; set; }
        
        [JsonIgnore] // ✅ AGREGAR: Evitar ciclo de referencia
        public virtual Role? Role { get; set; }
    }
}
