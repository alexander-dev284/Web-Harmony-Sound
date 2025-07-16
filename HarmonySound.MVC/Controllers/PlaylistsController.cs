using HarmonySound.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HarmonySound.MVC.Models; // Usar el namespace local

namespace HarmonySound.MVC.Controllers
{
    public class PlaylistsController : Controller
    {
        private readonly HttpClient _httpClient;

        public PlaylistsController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Mostrar todas las playlists del usuario con opción de agregar contenido
        public async Task<IActionResult> Index(int? contentId = null)
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                
                // Obtener información del contenido si se está agregando
                Content selectedContent = null;
                if (contentId.HasValue)
                {
                    var contentResponse = await _httpClient.GetAsync($"https://localhost:7120/api/Contents/{contentId.Value}");
                    if (contentResponse.IsSuccessStatusCode)
                    {
                        var contentJson = await contentResponse.Content.ReadAsStringAsync();
                        selectedContent = JsonSerializer.Deserialize<Content>(contentJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                }
                
                // Obtener playlists del usuario
                var playlistsResponse = await _httpClient.GetAsync($"https://localhost:7120/api/Playlists/user/{userId}");
                
                if (playlistsResponse.IsSuccessStatusCode)
                {
                    var playlistsJson = await playlistsResponse.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var playlistsData = JsonSerializer.Deserialize<JsonElement[]>(playlistsJson, options);
                    
                    var userPlaylists = playlistsData.Select(p => new PlaylistDto
                    {
                        Id = p.GetProperty("id").GetInt32(),
                        Name = p.GetProperty("name").GetString() ?? "",
                        Songs = p.TryGetProperty("songs", out var songs) 
                            ? songs.EnumerateArray().Select(s => new PlaylistSongDto
                            {
                                ContentId = s.GetProperty("contentId").GetInt32(),
                                Title = s.GetProperty("title").GetString() ?? "",
                                UrlMedia = s.GetProperty("urlMedia").GetString() ?? "",
                                ArtistName = s.TryGetProperty("artistName", out var artistName) 
                                    ? artistName.GetString() ?? "Artista desconocido"
                                    : "Artista desconocido"
                            }).ToList() 
                            : new List<PlaylistSongDto>()
                    }).ToList();
                    
                    ViewBag.SelectedContent = selectedContent;
                    ViewBag.ContentId = contentId;
                    
                    return View(userPlaylists);
                }
                
                ViewBag.SelectedContent = selectedContent;
                ViewBag.ContentId = contentId;
                return View(new List<PlaylistDto>());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in PlaylistsController.Index: {ex.Message}");
                return View(new List<PlaylistDto>());
            }
        }

        // Agregar canción a playlist
        [HttpPost]
        public async Task<IActionResult> AddToPlaylist(int playlistId, int contentId)
        {
            try
            {
                var content = new StringContent(contentId.ToString(), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"https://localhost:7120/api/Playlists/{playlistId}/add", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Canción agregada a la playlist exitosamente.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"No se pudo agregar la canción: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al agregar la canción: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // GET: Playlists/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Playlists/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Playlist playlist)
        {
            if (!ModelState.IsValid)
                return View(playlist);

            // Asigna el UserId antes de enviar a la API
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            playlist.UserId = userId;

            var json = JsonSerializer.Serialize(playlist);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://localhost:7120/api/Playlists", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Playlist creada correctamente.";
                return RedirectToAction("Index");
            }

            var errorMsg = await response.Content.ReadAsStringAsync();
            TempData["Error"] = $"Error al crear la playlist: {errorMsg}";
            return View(playlist);
        }

        // GET: PlaylistsController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: PlaylistsController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: PlaylistsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: PlaylistsController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: PlaylistsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
