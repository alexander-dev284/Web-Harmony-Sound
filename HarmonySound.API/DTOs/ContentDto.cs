namespace HarmonySound.API.DTOs
{
    public class ContentDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Type { get; set; } 
        public string UrlMedia { get; set; } = ""; 
        public TimeSpan Duration { get; set; }
        public DateTimeOffset UploadDate { get; set; }
        public int ArtistId { get; set; } 

        public string? ArtistName { get; set; } 
        public string? AlbumTitle { get; set; }   

        public string FormattedDuration => Duration.ToString(@"mm\:ss");
    }
}
