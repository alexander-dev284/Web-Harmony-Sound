using System.ComponentModel.DataAnnotations;

namespace HarmonySound.MVC.Models
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Nombre")]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [RegularExpression(@"^[^@\s]+@utn\.edu\.ec$", ErrorMessage = "El correo debe pertenecer al dominio @utn.edu.ec.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Debes seleccionar un rol")]
        [Display(Name = "Rol")]
        public string Role { get; set; }

        public IEnumerable<string> Roles { get; set; }

        // Token de invitación a plan compartido (cuando el usuario llega desde un correo de invitación).
        public string? InvitationToken { get; set; }
    }
}