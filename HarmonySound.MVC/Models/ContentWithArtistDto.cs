using System;

namespace HarmonySound.MVC.Models
{
    public class ContentWithArtistDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Type { get; set; } = "";
        public string UrlMedia { get; set; } = "";
        public TimeSpan Duration { get; set; }
        public DateTimeOffset UploadDate { get; set; }
        public int ArtistId { get; set; }
        public string ArtistName { get; set; } = "Artista desconocido";
        
        // **NUEVA PROPIEDAD: Duración formateada**
        public string FormattedDuration => Duration.TotalSeconds > 0 
            ? Duration.ToString(@"mm\:ss") 
            : "--:--";
    }
}