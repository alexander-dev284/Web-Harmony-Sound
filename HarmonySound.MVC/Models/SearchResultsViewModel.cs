using HarmonySound.Models;
using HarmonySound.API.DTOs;

namespace HarmonySound.MVC.Models
{
    public class SearchResultsViewModel
    {
        public string Query { get; set; } = "";
        public List<User> Artists { get; set; } = new();
        public List<ContentWithArtistDto> Contents { get; set; } = new();
        public ProfileEditViewModel Profile { get; set; } = new();

        // ===== Descubrimiento (pantalla de inicio sin búsqueda) =====
        // Canciones más populares del momento (ordenadas por "me gusta").
        public List<ContentWithArtistDto> PopularContents { get; set; } = new();
        // Novedades / canciones recientemente subidas.
        public List<ContentWithArtistDto> RecentContents { get; set; } = new();
        // Álbumes destacados para explorar.
        public List<AlbumDto> FeaturedAlbums { get; set; } = new();
        // Artistas para explorar.
        public List<ArtistCardViewModel> FeaturedArtists { get; set; } = new();
        // Artistas que el usuario sigue.
        public List<ArtistCardViewModel> FollowedArtists { get; set; } = new();
    }

    public class ArtistCardViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Biography { get; set; }
        public string? ProfileImageUrl { get; set; }
    }
}
