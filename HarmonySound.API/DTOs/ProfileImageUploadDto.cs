namespace HarmonySound.API.DTOs
{
    public class ProfileImageUploadDto
    {
        public int UserId { get; set; }
        public IFormFile File { get; set; }
    }
}
