using System.ComponentModel.DataAnnotations;

namespace HarmonySound.Models
{
    public class Content
    {
        [Key] public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Title { get; set; }

        public string Type { get; set; }  // Ejemplo: "Canción", "Podcast"

        [Required]
        public string UrlMedia { get; set; }  // Ruta o URL del archivo

        public TimeSpan Duration { get; set; }

        public DateTimeOffset UploadDate { get; set; }

        public int ArtistId { get; set; }
        public User? Artist { get; set; }

        // Relación con Álbumes
        public List<ContentAlbum>? ContentAlbumes { get; set; }
    }
}
