using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HarmonySound.API.Data;
using Microsoft.EntityFrameworkCore;

namespace HarmonySound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdsController : ControllerBase
    {
        private readonly HarmonySoundDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AdsController(HarmonySoundDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet("random")]
        public async Task<IActionResult> GetRandomAd()
        {
            // Verificar que los archivos existan
            var webRootPath = _environment.WebRootPath;
            var adsPath = Path.Combine(webRootPath, "ads");
            
            var ads = new List<object>();
            
            // Verificar cada archivo antes de agregarlo
            if (System.IO.File.Exists(Path.Combine(adsPath, "ad1.mp3")))
                ads.Add(new { url = "/ads/ad1.mp3", duration = 30, title = "Suscríbete a Premium" });
            
            if (System.IO.File.Exists(Path.Combine(adsPath, "ad2.mp3")))
                ads.Add(new { url = "/ads/ad2.mp3", duration = 20, title = "Disfruta sin anuncios" });
            
            if (System.IO.File.Exists(Path.Combine(adsPath, "ad3.mp3")))
                ads.Add(new { url = "/ads/ad3.mp3", duration = 20, title = "Premium por solo $1" });

            // Si no hay anuncios, retornar uno por defecto
            if (ads.Count == 0)
            {
                ads.Add(new { url = "/ads/default.mp3", duration = 15, title = "Suscríbete a Premium" });
            }

            var random = new Random();
            var selectedAd = ads[random.Next(ads.Count)];
            
            return Ok(selectedAd);
        }

        // Endpoint para verificar qué anuncios están disponibles
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableAds()
        {
            var webRootPath = _environment.WebRootPath;
            var adsPath = Path.Combine(webRootPath, "ads");
            
            var availableAds = new List<string>();
            
            if (Directory.Exists(adsPath))
            {
                var files = Directory.GetFiles(adsPath, "*.mp3");
                availableAds = files.Select(f => Path.GetFileName(f)).ToList();
            }
            
            return Ok(new { 
                adsPath = adsPath,
                availableAds = availableAds,
                totalAds = availableAds.Count
            });
        }
    }
}
