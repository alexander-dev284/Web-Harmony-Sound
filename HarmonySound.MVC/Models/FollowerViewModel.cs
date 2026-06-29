namespace HarmonySound.MVC.Models
{
    // Seguidor de un artista (datos básicos para mostrar en su panel).
    public class FollowerViewModel
    {
        public int FollowerId { get; set; }
        public string Name { get; set; } = "";
        public string? ProfileImageUrl { get; set; }
        public DateTimeOffset FollowDate { get; set; }
    }
}
