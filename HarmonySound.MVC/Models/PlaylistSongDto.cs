namespace HarmonySound.MVC.Models
{
    public class PlaylistSongDto
    {
        public int ContentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string UrlMedia { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
    }
}
