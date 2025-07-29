using System.ComponentModel.DataAnnotations;

namespace HarmonySound.API.DTOs
{
    public class AssignRoleModel
    {
        [Required(ErrorMessage = "El ID del usuario es requerido")]
        public int UserId { get; set; }
        
        [Required(ErrorMessage = "El nombre del rol es requerido")]
        [MaxLength(50, ErrorMessage = "El nombre del rol no puede exceder 50 caracteres")]
        public string RoleName { get; set; } = string.Empty;
    }
}