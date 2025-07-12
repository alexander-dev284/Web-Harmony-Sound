namespace HarmonySound.API.DTOs
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Biography { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string Email { get; set; }
    }
}
