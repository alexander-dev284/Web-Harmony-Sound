using System.ComponentModel.DataAnnotations;

namespace HarmonySound.Models
{
    public class Album
    {
        [Key] public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Title { get; set; }
        public DateTimeOffset CreationDate { get; set; }

        public int ArtistId { get; set; }
        public User? Artist { get; set; }

        public List<ContentAlbum>? ContentAlbums { get; set; }
    }
}
