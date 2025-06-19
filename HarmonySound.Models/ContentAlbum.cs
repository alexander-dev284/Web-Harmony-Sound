namespace HarmonySound.Models
{
    public class ContentAlbum
    {
        public int ContentId { get; set; }
        public int AlbumId { get; set; }
        public Content? Content { get; set; }
        public Album? Album { get; set; }
    }
}
