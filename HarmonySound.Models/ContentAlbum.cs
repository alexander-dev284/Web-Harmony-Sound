using System.ComponentModel.DataAnnotations;

namespace HarmonySound.Models
{
    public class ContentAlbum
    {
        [Key] public int Id { get; set; }
        public int ContentId { get; set; }
        public int AlbumId { get; set; }
        public Content? Content { get; set; }
        public Album? Album { get; set; }
    }
}
