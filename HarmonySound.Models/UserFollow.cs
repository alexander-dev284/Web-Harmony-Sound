using System.ComponentModel.DataAnnotations;

namespace HarmonySound.Models
{
    // Relación de seguimiento usuario (cliente) -> artista (ambos son User).
    public class UserFollow
    {
        [Key]
        public int Id { get; set; }

        // Usuario (cliente) que sigue.
        public int FollowerId { get; set; }
        public User Follower { get; set; }

        // Artista (usuario) que es seguido.
        public int ArtistId { get; set; }
        public User Artist { get; set; }

        public DateTimeOffset FollowDate { get; set; }
    }
}
