using System.ComponentModel.DataAnnotations;

namespace HarmonySound.MVC.Models
{
    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "La contraseña es requerida")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = "";
    }

    public class Admin2FAViewModel
    {
        [Required(ErrorMessage = "El código es requerido")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "El código debe tener 6 dígitos")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "El código debe contener solo números")]
        [Display(Name = "Código de verificación")]
        public string Code { get; set; } = "";

        public string SecretKey { get; set; } = "";
    }
}