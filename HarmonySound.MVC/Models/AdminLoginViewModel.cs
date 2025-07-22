using System.ComponentModel.DataAnnotations;

namespace HarmonySound.MVC.Models
{
    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string Password { get; set; } = "";

        public string SecretKey { get; set; } = "";
    }

    public class Admin2FAViewModel
    {
        [Required(ErrorMessage = "El código es obligatorio")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "El código debe tener 6 dígitos")]
        [RegularExpression("^[0-9]{6}$", ErrorMessage = "El código debe contener solo números")]
        public string Code { get; set; } = "";

        public string SecretKey { get; set; } = "";
    }
}