namespace HarmonySound.MVC.Models
{
    public class PlaylistDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public List<PlaylistSongDto>? Songs { get; set; } = new List<PlaylistSongDto>();
    }
}
