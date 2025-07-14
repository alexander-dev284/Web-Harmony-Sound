using HarmonySound.API.Consumer;
using HarmonySound.API.DTOs; // Importa el namespace de los DTOs
using HarmonySound.Models;
using HarmonySound.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        public async Task<IActionResult> Create(CreateAlbumDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Asigna el ArtistId del usuario autenticado
            model.ArtistId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Serializa y envía el DTO a la API
            var json = System.Text.Json.JsonSerializer.Serialize(model);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://localhost:7120/api/Albums", content);

            if (response.IsSuccessStatusCode)
                return RedirectToAction(nameof(Index));

            // Si hay error, muestra el modelo con los errores
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
            ViewBag.AllSongs = allSongs;

            return View(album);
        }

        // POST: Albums/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AlbumDto model, List<int> selectedSongIds)
        {
            try
            {
                // Actualiza el título del álbum
                var updateDto = new CreateAlbumDto
                {
                    Title = model.Title,
                    ArtistId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)
                };

                var json = System.Text.Json.JsonSerializer.Serialize(updateDto);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"https://localhost:7120/api/Albums/{id}", content);

                if (!response.IsSuccessStatusCode)
                    throw new Exception("No se pudo actualizar el álbum.");

                // Actualiza las canciones del álbum usando la API
                var songsJson = System.Text.Json.JsonSerializer.Serialize(selectedSongIds ?? new List<int>());
                var songsContent = new StringContent(songsJson, System.Text.Encoding.UTF8, "application/json");
                var songsResponse = await _httpClient.PostAsync($"https://localhost:7120/api/Albums/{id}/UpdateSongs", songsContent);

                if (!songsResponse.IsSuccessStatusCode)
                    throw new Exception("No se pudo actualizar las canciones del álbum.");

                return RedirectToAction(nameof(Index));
            }
            catch
            {
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
                ModelState.AddModelError("", "Error al guardar los cambios.");
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
            var allSongs = System.Text.Json.JsonSerializer.Deserialize<List<ContentDto>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return allSongs.Where(c => c.ArtistId == artistId).ToList();
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
            var response = await _httpClient.DeleteAsync($"https://localhost:7120/api/Albums/{id}");
            return RedirectToAction(nameof(Index));
        }
    }
}