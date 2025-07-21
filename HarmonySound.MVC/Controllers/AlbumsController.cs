using HarmonySound.API.Consumer;
using HarmonySound.API.DTOs; // Importa el namespace de los DTOs
using HarmonySound.Models;
using HarmonySound.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HarmonySound.MVC.Controllers
{
    [Authorize(Roles = "artist")]
    public class AlbumsController : Controller
    {
        private readonly HttpClient _httpClient;

        public AlbumsController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // GET: Albums
        public async Task<IActionResult> Index()
        {
            var response = await _httpClient.GetAsync("https://localhost:7120/api/Albums");
            var json = await response.Content.ReadAsStringAsync();
            var albums = System.Text.Json.JsonSerializer.Deserialize<List<AlbumDto>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return View(albums);
        }

        // GET: Albums/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var response = await _httpClient.GetAsync($"https://localhost:7120/api/Albums/{id}");
            if (!response.IsSuccessStatusCode)
                return NotFound();

            var json = await response.Content.ReadAsStringAsync();
            var album = System.Text.Json.JsonSerializer.Deserialize<AlbumDto>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            // ✅ DEBUGGING: Verificar las URLs que llegan
            if (album?.Contents != null)
            {
                System.Diagnostics.Debug.WriteLine($"=== Álbum '{album.Title}' con {album.Contents.Count} canciones ===");
                foreach (var content in album.Contents)
                {
                    System.Diagnostics.Debug.WriteLine($"Canción: {content.Title} - URL: {content.UrlMedia ?? "NULL"}");
                    System.Diagnostics.Debug.WriteLine($"  - UrlMedia válida: {!string.IsNullOrEmpty(content.UrlMedia)}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Álbum sin contenidos");
            }
            
            return View(album);
        }

        // GET: Albums/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Albums/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAlbumDto model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Asigna el ArtistId del usuario autenticado
            model.ArtistId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // ✅ CORRECCIÓN: No necesitas asignar imageFile al modelo
            // La línea model.ImageFile = imageFile; debe eliminarse

            // ✅ CREAR formulario multipart
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(model.Title), "Title");
            content.Add(new StringContent(model.ArtistId.ToString()), "ArtistId");

            // ✅ USAR el parámetro imageFile directamente
            if (imageFile != null && imageFile.Length > 0)
            {
                var fileContent = new StreamContent(imageFile.OpenReadStream());
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageFile.ContentType);
                content.Add(fileContent, "ImageFile", imageFile.FileName);
            }

            var response = await _httpClient.PostAsync("https://localhost:7120/api/Albums", content);

            if (response.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            ModelState.AddModelError("", "Could not create album.");
            return View(model);
        }

        // GET: Albums/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var response = await _httpClient.GetAsync($"https://localhost:7120/api/Albums/{id}");
            if (!response.IsSuccessStatusCode)
                return NotFound();

            var json = await response.Content.ReadAsStringAsync();
            var album = System.Text.Json.JsonSerializer.Deserialize<AlbumDto>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            int artistId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
            var allSongs = await GetAllSongsForArtist(artistId);
            ViewBag.AllSongs = allSongs; // ✅ Ahora será List<ContentDto> con UrlMedia

            return View(album);
        }

        // POST: Albums/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AlbumDto model, List<int> selectedSongIds)
        {
            try
            {
                // ✅ CORREGIDO: Usar MultipartFormDataContent para el PUT
                using var content = new MultipartFormDataContent();
                content.Add(new StringContent(model.Title), "Title");
                content.Add(new StringContent(int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value).ToString()), "ArtistId");

                var response = await _httpClient.PutAsync($"https://localhost:7120/api/Albums/{id}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Error en PUT álbum: {response.StatusCode} - {errorContent}");
                    throw new Exception($"No se pudo actualizar el álbum: {response.StatusCode}");
                }

                // ✅ DEPURACIÓN: Verificar qué canciones se están enviando
                Console.WriteLine($"🎵 Canciones seleccionadas: [{string.Join(", ", selectedSongIds ?? new List<int>())}]");

                // Actualizar las canciones del álbum usando la API
                var songsJson = System.Text.Json.JsonSerializer.Serialize(selectedSongIds ?? new List<int>());
                var songsContent = new StringContent(songsJson, System.Text.Encoding.UTF8, "application/json");
                var songsResponse = await _httpClient.PostAsync($"https://localhost:7120/api/Albums/{id}/UpdateSongs", songsContent);

                if (!songsResponse.IsSuccessStatusCode)
                {
                    var songErrorContent = await songsResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Error en UpdateSongs: {songsResponse.StatusCode} - {songErrorContent}");
                    throw new Exception($"No se pudo actualizar las canciones del álbum: {songsResponse.StatusCode}");
                }

                Console.WriteLine("✅ Álbum actualizado correctamente");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción en Edit: {ex.Message}");
                
                // Recargar datos para mostrar la vista con error
                int artistId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var allSongs = await GetAllSongsForArtist(artistId);
                ViewBag.AllSongs = allSongs;

                var response = await _httpClient.GetAsync($"https://localhost:7120/api/Albums/{id}");
                AlbumDto album = null;
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    album = System.Text.Json.JsonSerializer.Deserialize<AlbumDto>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                
                ModelState.AddModelError("", $"Error al guardar los cambios: {ex.Message}");
                return View(album ?? model);
            }
        }

        // Helper para obtener todas las canciones del artista usando HttpClient
        private async Task<List<ContentDto>> GetAllSongsForArtist(int artistId)
        {
            var response = await _httpClient.GetAsync("https://localhost:7120/api/Contents");
            if (!response.IsSuccessStatusCode)
                return new List<ContentDto>();

            var json = await response.Content.ReadAsStringAsync();
            var allSongs = System.Text.Json.JsonSerializer.Deserialize<List<Content>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            // ✅ CONVERTIR Content a ContentDto incluyendo UrlMedia
            var contentDtos = allSongs.Where(c => c.ArtistId == artistId)
                .Select(c => new ContentDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Type = c.Type,
                    UrlMedia = c.UrlMedia, // ✅ INCLUIR UrlMedia
                    Duration = c.Duration,
                    UploadDate = c.UploadDate,
                    ArtistId = c.ArtistId,
                    ArtistName = null, // Se puede llenar si necesitas
                    AlbumTitle = null
                }).ToList();
            
            return contentDtos;
        }

        // POST: Albums/RemoveSong
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveSong(int albumId, int contentId)
        {
            var response = await _httpClient.DeleteAsync($"https://localhost:7120/api/Albums/{albumId}/RemoveSong/{contentId}");
            return RedirectToAction("Edit", new { id = albumId });
        }

        // POST: Albums/AddSong
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSong(int albumId, int songId)
        {
            var content = new StringContent(songId.ToString(), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"https://localhost:7120/api/Albums/{albumId}/AddSong", content);
            return RedirectToAction("Edit", new { id = albumId });
        }

        // POST: Albums/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                Console.WriteLine($"🗑️ Intentando eliminar álbum ID: {id}");
                
                var response = await _httpClient.DeleteAsync($"https://localhost:7120/api/Albums/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✅ Álbum {id} eliminado correctamente");
                    TempData["Success"] = "Álbum eliminado correctamente.";
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Error al eliminar álbum: {response.StatusCode} - {error}");
                    TempData["Error"] = "Error al eliminar el álbum.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción al eliminar álbum: {ex.Message}");
                TempData["Error"] = $"Error al eliminar el álbum: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Albums/RemoveTrackFromAlbum
        [HttpPost]
        public async Task<IActionResult> RemoveTrackFromAlbum(int albumId, int trackId)
        {
            try
            {
                // Usar el endpoint DELETE específico del API
                var response = await _httpClient.DeleteAsync($"https://localhost:7120/api/Albums/{albumId}/RemoveSong/{trackId}");
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "La canción se eliminó del álbum correctamente.";
                }
                else
                {
                    TempData["Error"] = "Error al eliminar la canción del álbum.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar la canción: {ex.Message}";
            }

            return RedirectToAction("Details", new { id = albumId });
        }
    }
}