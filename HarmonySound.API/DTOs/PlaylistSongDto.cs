namespace HarmonySound.API.DTOs
{
    public class PlaylistSongDto
    {
        public int ContentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string UrlMedia { get; set; } = string.Empty;
    }
}
