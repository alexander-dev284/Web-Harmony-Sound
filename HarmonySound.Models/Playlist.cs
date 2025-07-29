
namespace HarmonySound.Models
{
    public class Playlist
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int UserId { get; set; }
        public string? ImageUrl { get; set; } 
        public User? User { get; set; }
        public List<PlaylistContent>? PlaylistContents { get; set; }
    }
}
