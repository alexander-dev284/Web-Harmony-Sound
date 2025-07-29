namespace HarmonySound.MVC.Models
{
    public class ProfileEditViewModel
    {
        public int Id { get; set; }
        public string Email { get; set; } 
        public string Name { get; set; }
        public string Biography { get; set; }
        public string? ProfileImageUrl { get; set; }
        public IFormFile? ProfileImageFile { get; set; } 
    }
}
