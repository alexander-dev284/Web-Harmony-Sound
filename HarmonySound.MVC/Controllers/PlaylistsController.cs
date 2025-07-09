using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HarmonySound.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
            // Obtener usuario autenticado
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            // Obtener canciones
            var songsResponse = await _httpClient.GetAsync("https://localhost:7120/api/Contents");
            var songsJson = await songsResponse.Content.ReadAsStringAsync();
            var songs = JsonSerializer.Deserialize<List<Content>>(songsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Obtener playlists del usuario
            var playlistsResponse = await _httpClient.GetAsync($"https://localhost:7120/api/Playlists/user/{userId}");
            var playlistsJson = await playlistsResponse.Content.ReadAsStringAsync();
            var playlists = JsonSerializer.Deserialize<List<Playlist>>(playlistsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            ViewBag.Playlists = playlists;
            return View(songs);
        }

        // Agregar canción a playlist
        [HttpPost]
        public async Task<IActionResult> AddToPlaylist(int playlistId, int contentId)
        {
            var content = new StringContent(contentId.ToString(), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"https://localhost:7120/api/Playlists/{playlistId}/add", content);

            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Canción agregada a la playlist.";
            else
                TempData["Error"] = "No se pudo agregar la canción.";

            return RedirectToAction("Index");
        }

        // GET: PlaylistsController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: PlaylistsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: PlaylistsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
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
