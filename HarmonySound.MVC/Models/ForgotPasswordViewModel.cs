using System.ComponentModel.DataAnnotations;

namespace HarmonySound.MVC.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Ingresa un email válido")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = "";
    }
    public class ResetPasswordViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "El código es obligatorio")]
        [Display(Name = "Código de verificación")]
        public string Code { get; set; } = "";

        [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
        [StringLength(100, ErrorMessage = "La contraseña debe tener al menos {2} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string NewPassword { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nueva contraseña")]
        [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; } = "";

        public string? Email { get; set; }
    }
}
