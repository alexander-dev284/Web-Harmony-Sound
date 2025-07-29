using System.ComponentModel.DataAnnotations;

namespace HarmonySound.MVC.Models
{
    public class Admin2FAViewModel
    {
        [Required(ErrorMessage = "El código es requerido")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "El código debe tener exactamente 6 dígitos")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "El código debe contener solo números")]
        [Display(Name = "Código de verificación")]
        public string Code { get; set; } = string.Empty;

        [Display(Name = "Clave secreta")]
        public string SecretKey { get; set; } = string.Empty;

        // ✅ OPCIONAL: Propiedades adicionales para mejorar UX
        [Display(Name = "Email del administrador")]
        public string? AdminEmail { get; set; }

        [Display(Name = "Tiempo de expiración")]
        public DateTime? ExpirationTime { get; set; }

        // ✅ OPCIONAL: Para mostrar tiempo restante
        public int RemainingMinutes => ExpirationTime.HasValue 
            ? Math.Max(0, (int)(ExpirationTime.Value - DateTime.UtcNow).TotalMinutes)
            : 5; // 5 minutos por defecto
    }
}