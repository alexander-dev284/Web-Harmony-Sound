using System.ComponentModel.DataAnnotations;

namespace HarmonySound.Models
{
    public class Content
    {
        [Key] public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; }

        [Required]
        public string Type { get; set; }  // Ejemplo: "Canción", "Podcast"

        public string UrlMedia { get; set; }  // Ruta o URL del archivo

        public TimeSpan Duration { get; set; }

        public DateTime UploadDate { get; set; }

        public int ArtistId { get; set; }
        public User Artist { get; set; }

        // Relación con Álbumes
        public List<ContentAlbum> ContentAlbumes { get; set; }
    }
}
