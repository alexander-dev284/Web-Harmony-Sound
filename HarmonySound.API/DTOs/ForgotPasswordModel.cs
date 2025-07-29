using System.ComponentModel.DataAnnotations;

namespace HarmonySound.API.DTOs
{
    public class ForgotPasswordModel
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string Email { get; set; } = "";
    }
}