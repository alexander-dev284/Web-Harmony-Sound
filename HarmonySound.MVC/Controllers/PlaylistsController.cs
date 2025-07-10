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
            // Obtén playlists con sus canciones (ajusta según tu API)
            var playlistsResponse = await _httpClient.GetAsync("https://localhost:7120/api/Playlists");
            var playlistsJson = await playlistsResponse.Content.ReadAsStringAsync();
            var playlists = JsonSerializer.Deserialize<List<PlaylistDto>>(playlistsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var songsResponse = await _httpClient.GetAsync("https://localhost:7120/api/Contents");
            var songsJson = await songsResponse.Content.ReadAsStringAsync();
            var songs = JsonSerializer.Deserialize<List<Content>>(songsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            ViewBag.Songs = songs;

            return View(playlists);
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
