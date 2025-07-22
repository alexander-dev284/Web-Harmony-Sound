using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HarmonySound.MVC.Models;
using System.Text.Json;
using System.Security.Claims;

namespace HarmonySound.MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AdminController> _logger;

        public AdminController(HttpClient httpClient, ILogger<AdminController> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        // Dashboard principal del admin
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var stats = await GetAdminStats();
                return View(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar dashboard admin");
                ViewBag.Error = $"Error al cargar el dashboard: {ex.Message}";
                return View(new AdminDashboardViewModel());
            }
        }

        // ✅ NUEVO: Gestión de usuarios
        public async Task<IActionResult> Users()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://localhost:7120/api/Users");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<List<AdminUserViewModel>>(json, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return View(users);
                }
                TempData["Error"] = "Error al cargar usuarios";
                return View(new List<AdminUserViewModel>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users");
                TempData["Error"] = "Error al cargar usuarios";
                return View(new List<AdminUserViewModel>());
            }
        }

        // ✅ NUEVO: Gestión de contenido
        public async Task<IActionResult> Content()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://localhost:7120/api/Contents");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var contents = JsonSerializer.Deserialize<List<AdminContentViewModel>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return View(contents);
                }
                TempData["Error"] = "Error al cargar contenido";
                return View(new List<AdminContentViewModel>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading content");
                TempData["Error"] = "Error al cargar contenido";
                return View(new List<AdminContentViewModel>());
            }
        }

        // ✅ NUEVO: Gestión de reportes
        public async Task<IActionResult> Reports()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://localhost:7120/api/Reports");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var reports = JsonSerializer.Deserialize<List<AdminReportViewModel>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return View(reports);
                }
                TempData["Error"] = "Error al cargar reportes";
                return View(new List<AdminReportViewModel>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reports");
                TempData["Error"] = "Error al cargar reportes";
                return View(new List<AdminReportViewModel>());
            }
        }

        // ✅ NUEVO: Gestión de planes
        public async Task<IActionResult> Plans()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://localhost:7120/api/Plans");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var plans = JsonSerializer.Deserialize<List<AdminPlanViewModel>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return View(plans);
                }
                TempData["Error"] = "Error al cargar planes";
                return View(new List<AdminPlanViewModel>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading plans");
                TempData["Error"] = "Error al cargar planes";
                return View(new List<AdminPlanViewModel>());
            }
        }

        // ✅ NUEVO: Acción para suspender usuario
        [HttpPost]
        public async Task<IActionResult> SuspendUser(int userId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"https://localhost:7120/api/Users/{userId}/suspend", null);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Usuario suspendido correctamente";
                }
                else
                {
                    TempData["Error"] = "Error al suspender usuario";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suspending user");
                TempData["Error"] = "Error al suspender usuario";
            }
            return RedirectToAction("Users");
        }

        // ✅ NUEVO: Acción para eliminar contenido
        [HttpPost]
        public async Task<IActionResult> DeleteContent(int contentId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"https://localhost:7120/api/Contents/{contentId}");
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Contenido eliminado correctamente";
                }
                else
                {
                    TempData["Error"] = "Error al eliminar contenido";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting content");
                TempData["Error"] = "Error al eliminar contenido";
            }
            return RedirectToAction("Content");
        }

        // ✅ NUEVO: Método privado para obtener estadísticas
        private async Task<AdminDashboardViewModel> GetAdminStats()
        {
            var stats = new AdminDashboardViewModel();

            try
            {
                // Obtener total de usuarios
                var usersResponse = await _httpClient.GetAsync("https://localhost:7120/api/Users");
                if (usersResponse.IsSuccessStatusCode)
                {
                    var usersJson = await usersResponse.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<List<object>>(usersJson);
                    stats.TotalUsers = users?.Count ?? 0;
                }

                // Obtener total de contenido
                var contentsResponse = await _httpClient.GetAsync("https://localhost:7120/api/Contents");
                if (contentsResponse.IsSuccessStatusCode)
                {
                    var contentsJson = await contentsResponse.Content.ReadAsStringAsync();
                    var contents = JsonSerializer.Deserialize<List<object>>(contentsJson);
                    stats.TotalSongs = contents?.Count ?? 0;
                }

                // Obtener total de álbumes
                var albumsResponse = await _httpClient.GetAsync("https://localhost:7120/api/Albums");
                if (albumsResponse.IsSuccessStatusCode)
                {
                    var albumsJson = await albumsResponse.Content.ReadAsStringAsync();
                    var albums = JsonSerializer.Deserialize<List<object>>(albumsJson);
                    stats.TotalAlbums = albums?.Count ?? 0;
                }

                // Obtener total de reportes
                var reportsResponse = await _httpClient.GetAsync("https://localhost:7120/api/Reports");
                if (reportsResponse.IsSuccessStatusCode)
                {
                    var reportsJson = await reportsResponse.Content.ReadAsStringAsync();
                    var reports = JsonSerializer.Deserialize<List<object>>(reportsJson);
                    stats.PendingReports = reports?.Count ?? 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin stats");
            }

            return stats;
        }
    }
}