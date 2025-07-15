namespace HarmonySound.MVC.Models
{
    public class ArtistSearchResultViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? ProfileImageUrl { get; set; }
        public int TotalReproductions { get; set; }
        public int TotalLikes { get; set; }
        public int TotalComments { get; set; }
    }
}
