using HarmonySound.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HarmonySound.API.DTOs;

namespace HarmonySound.MVC.Controllers
{
    public class PlaylistsController : Controller
    {
        private readonly HttpClient _httpClient;

        public PlaylistsController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Mostrar todas las canciones y playlists del usuario
        public async Task<IActionResult> Index()
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                
                // Usar el endpoint específico del usuario
                var playlistsResponse = await _httpClient.GetAsync($"https://localhost:7120/api/Playlists/user/{userId}");
                
                if (playlistsResponse.IsSuccessStatusCode)
                {
                    var playlistsJson = await playlistsResponse.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var playlistsData = JsonSerializer.Deserialize<JsonElement[]>(playlistsJson, options);
                    
                    // Convertir a PlaylistDto con canciones
                    var userPlaylists = playlistsData.Select(p => new PlaylistDto
                    {
                        Id = p.GetProperty("id").GetInt32(),
                        Name = p.GetProperty("name").GetString(),
                        Songs = p.TryGetProperty("songs", out var songs) 
                            ? songs.EnumerateArray().Select(s => new PlaylistSongDto
                            {
                                ContentId = s.GetProperty("contentId").GetInt32(),
                                Title = s.GetProperty("title").GetString(),
                                UrlMedia = s.GetProperty("urlMedia").GetString()
                            }).ToList() 
                            : new List<PlaylistSongDto>()
                    }).ToList();
                        
                    return View(userPlaylists);
                }
                
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
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var content = new StringContent(contentId.ToString(), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"https://localhost:7120/api/Playlists/{playlistId}/add", content);

            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Canción agregada a la playlist.";
            else
                TempData["Error"] = "No se pudo agregar la canción.";

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
