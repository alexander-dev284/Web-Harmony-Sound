
namespace HarmonySound.Models
{
    public class PlaylistContent
    {
        public int PlaylistId { get; set; }
        public int ContentId { get; set; }
        public Playlist Playlist { get; set; }
        public Content Content { get; set; }
    }
}
