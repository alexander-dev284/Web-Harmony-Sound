using System.ComponentModel.DataAnnotations;

namespace HarmonySound.API.DTOs
{
    public class CreateContentDto
    {
        [Required]
        public string Title { get; set; }

        [Required]
        public string Type { get; set; }  // "Canción", "Podcast"

        public TimeSpan Duration { get; set; }

        public int ArtistId { get; set; }  
        public int? AlbumId { get; set; }
    }
}
