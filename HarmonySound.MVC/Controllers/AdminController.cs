using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HarmonySound.MVC.Models;
using System.Text.Json;
using System.Text;


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
                ViewBag.AdminName = User.Identity?.Name ?? "Administrador";
                
                // Usar convención en lugar de ruta explícita
                return View(stats);  // Esto buscará Views/Admin/Dashboard.cshtml automáticamente
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar dashboard admin");
                ViewBag.Error = $"Error al cargar el dashboard: {ex.Message}";
                return View(new AdminDashboardViewModel());
            }
        }

        public async Task<IActionResult> Users()
        {
            try
            {
                // endpoint con roles
                var response = await _httpClient.GetAsync("https://localhost:7120/api/Users/with-roles");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<List<AdminUserViewModel>>(json, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return View(users ?? new List<AdminUserViewModel>());
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

        public async Task<IActionResult> Content()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://localhost:7120/api/Contents/with-artists");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var contents = JsonSerializer.Deserialize<List<AdminContentViewModel>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return View(contents ?? new List<AdminContentViewModel>());
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
        public async Task<IActionResult> UserRoles()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://localhost:7120/api/UserRoles");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var userRoles = JsonSerializer.Deserialize<List<HarmonySound.API.DTOs.UserRoleDto>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    // Obtener todos los roles disponibles
                    var rolesResponse = await _httpClient.GetAsync("https://localhost:7120/api/Roles");
                    var allRoles = new List<string>();
                    if (rolesResponse.IsSuccessStatusCode)
                    {
                        var rolesJson = await rolesResponse.Content.ReadAsStringAsync();
                        var roles = JsonSerializer.Deserialize<List<dynamic>>(rolesJson);
                        ViewBag.AllRoles = new List<string> { "Admin", "Artist", "Client" };
                    }
                    
                    return View(userRoles ?? new List<HarmonySound.API.DTOs.UserRoleDto>());  // ✅ SIN RUTA EXPLÍCITA
                }
                TempData["Error"] = "Error al cargar roles de usuario";
                return View(new List<HarmonySound.API.DTOs.UserRoleDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user roles");
                TempData["Error"] = "Error al cargar roles de usuario";
                return View(new List<HarmonySound.API.DTOs.UserRoleDto>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> AssignRole(int userId, string roleName)
        {
            try
            {
                var assignRequest = new { UserId = userId, RoleName = roleName };
                var json = JsonSerializer.Serialize(assignRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("https://localhost:7120/api/UserRoles/assign", content);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Rol asignado correctamente";
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Error al asignar rol: {error}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role");
                TempData["Error"] = "Error al asignar rol";
            }
            return RedirectToAction("UserRoles");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveRole(int userId, string roleName)
        {
            try
            {
                var removeRequest = new { UserId = userId, RoleName = roleName };
                var json = JsonSerializer.Serialize(removeRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("https://localhost:7120/api/UserRoles/remove", content);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Rol removido correctamente";
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Error al remover rol: {error}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role");
                TempData["Error"] = "Error al remover rol";
            }
            return RedirectToAction("UserRoles");
        }

        // SUSPENDER/ACTIVAR USUARIO
        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int userId, string action)
        {
            try
            {
                _logger.LogInformation($"Iniciando {action} para usuario {userId}");
                
                // Crear modelo específico para el cambio de estado
                var statusRequest = new
                {
                    UserId = userId,
                    NewState = action == "activate" ? "Active" : "Suspended", // Usar valores exactos del enum
                    Action = action,
                    AdminId = GetCurrentUserId(), // Para auditoría
                    Timestamp = DateTime.UtcNow
                };

                var json = System.Text.Json.JsonSerializer.Serialize(statusRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                // Usar endpoint específico en lugar de genérico
                var endpoint = $"https://localhost:7120/api/Users/{userId}/toggle-status";
                
                _logger.LogInformation($"Llamando a API: {endpoint}");
                
                var response = await _httpClient.PostAsync(endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation($"Respuesta API: {response.StatusCode} - {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var message = action == "activate" ? "Usuario activado" : "Usuario suspendido";
                    TempData["Success"] = $"{message} correctamente";
                    
                    // Log de auditoría
                    _logger.LogInformation($"Usuario {userId} {action} exitosamente por admin {GetCurrentUserId()}");
                }
                else
                {
                    // MEJOR MANEJO DE ERRORES
                    try
                    {
                        var errorObj = System.Text.Json.JsonSerializer.Deserialize<dynamic>(responseContent);
                        var errorMessage = errorObj?.GetProperty("message").GetString() ?? "Error desconocido";
                        TempData["Error"] = $"Error al cambiar estado: {errorMessage}";
                    }
                    catch
                    {
                        TempData["Error"] = $"Error al cambiar estado del usuario: {responseContent}";
                    }
                    
                    _logger.LogError($"Error en API al {action} usuario {userId}: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Excepción al {action} usuario {userId}");
                TempData["Error"] = $"Error del sistema al {action} usuario: {ex.Message}";
            }
            
            return RedirectToAction("Users");
        }

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

        private async Task<AdminDashboardViewModel> GetAdminStats()
        {
            var stats = new AdminDashboardViewModel
            {
                RecentActivities = new List<RecentActivityViewModel>()
            };

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            try
            {
                // Usuarios con roles
                var usersResponse = await _httpClient.GetAsync("https://localhost:7120/api/Users/with-roles");
                if (usersResponse.IsSuccessStatusCode)
                {
                    var usersJson = await usersResponse.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<List<AdminUserViewModel>>(usersJson, jsonOptions)
                        ?? new List<AdminUserViewModel>();

                    stats.TotalUsers = users.Count;
                    stats.TotalArtists = users.Count(u => string.Equals(u.Role, "artist", StringComparison.OrdinalIgnoreCase));
                    stats.TotalClients = users.Count(u => string.Equals(u.Role, "client", StringComparison.OrdinalIgnoreCase));

                    // Distribución por rol para gráfico
                    stats.UsersByRole = new Dictionary<string, int>
                    {
                        ["Administradores"] = users.Count(u => string.Equals(u.Role, "admin", StringComparison.OrdinalIgnoreCase)),
                        ["Artistas"] = stats.TotalArtists,
                        ["Clientes"] = stats.TotalClients
                    };

                    // Usuarios recientes (últimos registrados)
                    stats.RecentUsers = users
                        .OrderByDescending(u => u.RegisterDate)
                        .Take(5)
                        .ToList();

                    Console.WriteLine($"Total usuarios: {stats.TotalUsers}");
                }

                // Contenido con conteo de likes (usamos el endpoint de populares con un tope alto)
                var contentsResponse = await _httpClient.GetAsync("https://localhost:7120/api/Contents/popular?count=10000");
                if (contentsResponse.IsSuccessStatusCode)
                {
                    var contentsJson = await contentsResponse.Content.ReadAsStringAsync();
                    var contents = JsonSerializer.Deserialize<List<TopContentViewModel>>(contentsJson, jsonOptions)
                        ?? new List<TopContentViewModel>();

                    stats.TotalSongs = contents.Count;
                    stats.TotalLikes = contents.Sum(c => c.Likes);

                    // Top 5 canciones más populares
                    stats.TopContents = contents
                        .OrderByDescending(c => c.Likes)
                        .Take(5)
                        .ToList();

                    // Distribución por tipo para gráfico
                    stats.ContentByType = contents
                        .GroupBy(c => string.IsNullOrWhiteSpace(c.Type) ? "Otro" : c.Type)
                        .ToDictionary(g => g.Key, g => g.Count());

                    Console.WriteLine($"Total canciones: {stats.TotalSongs}, likes: {stats.TotalLikes}");
                }

                // Álbumes
                var albumsResponse = await _httpClient.GetAsync("https://localhost:7120/api/Albums");
                if (albumsResponse.IsSuccessStatusCode)
                {
                    var albumsJson = await albumsResponse.Content.ReadAsStringAsync();
                    var albums = JsonSerializer.Deserialize<List<object>>(albumsJson);
                    stats.TotalAlbums = albums?.Count ?? 0;
                    
                    Console.WriteLine($"Total álbumes: {stats.TotalAlbums}");
                }

                // Ranking de artistas más seguidos
                var topArtistsResponse = await _httpClient.GetAsync("https://localhost:7120/api/Follows/top-artists?count=5");
                if (topArtistsResponse.IsSuccessStatusCode)
                {
                    var topArtistsJson = await topArtistsResponse.Content.ReadAsStringAsync();
                    stats.TopArtists = JsonSerializer.Deserialize<List<TopArtistViewModel>>(topArtistsJson, jsonOptions)
                        ?? new List<TopArtistViewModel>();

                    Console.WriteLine($"Top artistas más seguidos: {stats.TopArtists.Count}");
                }

                // Suscripciones activas
                var subscriptionsResponse = await _httpClient.GetAsync("https://localhost:7120/api/UserPlans");
                if (subscriptionsResponse.IsSuccessStatusCode)
                {
                    var subscriptionsJson = await subscriptionsResponse.Content.ReadAsStringAsync();
                    var subscriptions = JsonSerializer.Deserialize<List<object>>(subscriptionsJson);
                    stats.ActiveSubscriptions = subscriptions?.Count ?? 0;
                    
                    Console.WriteLine($"Total suscripciones: {stats.ActiveSubscriptions}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin stats");
                Console.WriteLine($"Error obteniendo estadísticas: {ex.Message}");
            }

            return stats;
        }

        // Método auxiliar para obtener ID del usuario actual
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            
            // Fallback: buscar en otros tipos de claims
            var subClaim = User.FindFirst("sub");
            if (subClaim != null && int.TryParse(subClaim.Value, out int subUserId))
            {
                return subUserId;
            }
            
            return 0; // Usuario no identificado
        }
    }
}