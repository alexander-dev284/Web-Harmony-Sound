namespace HarmonySound.Models
{
    public class ContentAlbum
    {
        public int ContentId { get; set; }
        public Content Content { get; set; }

        public int AlbumId { get; set; }
        public Album Album { get; set; }
    }
}
