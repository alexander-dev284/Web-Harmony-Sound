using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HarmonySound.API.DTOs
{
    public class CreateAlbumDto
    {
        [Required]
        public string Title { get; set; }

        public int ArtistId { get; set; }
        public IFormFile? ImageFile { get; set; } // ✅ NUEVA PROPIEDAD
    }
}
