using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HarmonySound.MVC.Models;
using System.Text.Json;
using System.Security.Claims;
using System.Text;
using HarmonySound.API.DTOs;


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
                
                // ✅ CAMBIAR: Usar convención en lugar de ruta explícita
                return View(stats);  // Esto buscará Views/Admin/Dashboard.cshtml automáticamente
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar dashboard admin");
                ViewBag.Error = $"Error al cargar el dashboard: {ex.Message}";
                return View(new AdminDashboardViewModel());
            }
        }

        // ✅ GESTIÓN DE USUARIOS
        public async Task<IActionResult> Users()
        {
            try
            {
                // ✅ USAR endpoint con roles
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

        // ✅ GESTIÓN DE CONTENIDO
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

        // ✅ GESTIÓN DE ROLES Y PERMISOS
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

        // ✅ ASIGNAR ROL A USUARIO
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

        // ✅ REMOVER ROL DE USUARIO
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

        // ✅ SUSPENDER/ACTIVAR USUARIO
        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int userId, string action)
        {
            try
            {
                _logger.LogInformation($"Iniciando {action} para usuario {userId}");
                
                // ✅ NUEVO: Crear modelo específico para el cambio de estado
                var statusRequest = new
                {
                    UserId = userId,
                    NewState = action == "activate" ? "Active" : "Suspended", // ✅ Usar valores exactos del enum
                    Action = action,
                    AdminId = GetCurrentUserId(), // Para auditoría
                    Timestamp = DateTime.UtcNow
                };

                var json = System.Text.Json.JsonSerializer.Serialize(statusRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                // ✅ CORREGIDO: Usar endpoint específico en lugar de genérico
                var endpoint = $"https://localhost:7120/api/Users/{userId}/toggle-status";
                
                _logger.LogInformation($"Llamando a API: {endpoint}");
                
                var response = await _httpClient.PostAsync(endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation($"Respuesta API: {response.StatusCode} - {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var message = action == "activate" ? "Usuario activado" : "Usuario suspendido";
                    TempData["Success"] = $"{message} correctamente";
                    
                    // ✅ OPCIONAL: Log de auditoría
                    _logger.LogInformation($"Usuario {userId} {action} exitosamente por admin {GetCurrentUserId()}");
                }
                else
                {
                    // ✅ MEJOR MANEJO DE ERRORES
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

        // ✅ ELIMINAR CONTENIDO
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

        // ✅ OBTENER ESTADÍSTICAS DEL DASHBOARD
        private async Task<AdminDashboardViewModel> GetAdminStats()
        {
            var stats = new AdminDashboardViewModel
            {
                RecentActivities = new List<RecentActivityViewModel>()
            };

            try
            {
                // ✅ Usuarios con roles
                var usersResponse = await _httpClient.GetAsync("https://localhost:7120/api/Users/with-roles");
                if (usersResponse.IsSuccessStatusCode)
                {
                    var usersJson = await usersResponse.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<List<AdminUserViewModel>>(usersJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    stats.TotalUsers = users?.Count ?? 0;
                    stats.TotalArtists = users?.Count(u => u.Role == "artist") ?? 0;
                    stats.TotalClients = users?.Count(u => u.Role == "client") ?? 0;  // ✅ Cambiar de "Client" a "CLIENT"
                    
                    Console.WriteLine($"🔍 Total usuarios: {stats.TotalUsers}");
                    Console.WriteLine($"🔍 Total artistas: {stats.TotalArtists}");
                    Console.WriteLine($"🔍 Total clientes: {stats.TotalClients}");
                }

                // ✅ Contenido con artistas
                var contentsResponse = await _httpClient.GetAsync("https://localhost:7120/api/Contents/with-artists");
                if (contentsResponse.IsSuccessStatusCode)
                {
                    var contentsJson = await contentsResponse.Content.ReadAsStringAsync();
                    var contents = JsonSerializer.Deserialize<List<AdminContentViewModel>>(contentsJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    stats.TotalSongs = contents?.Count ?? 0;
                    
                    Console.WriteLine($"🔍 Total canciones: {stats.TotalSongs}");
                }

                // ✅ Álbumes
                var albumsResponse = await _httpClient.GetAsync("https://localhost:7120/api/Albums");
                if (albumsResponse.IsSuccessStatusCode)
                {
                    var albumsJson = await albumsResponse.Content.ReadAsStringAsync();
                    var albums = JsonSerializer.Deserialize<List<object>>(albumsJson);
                    stats.TotalAlbums = albums?.Count ?? 0;
                    
                    Console.WriteLine($"🔍 Total álbumes: {stats.TotalAlbums}");
                }

                // ✅ Suscripciones activas
                var subscriptionsResponse = await _httpClient.GetAsync("https://localhost:7120/api/UserPlans");
                if (subscriptionsResponse.IsSuccessStatusCode)
                {
                    var subscriptionsJson = await subscriptionsResponse.Content.ReadAsStringAsync();
                    var subscriptions = JsonSerializer.Deserialize<List<object>>(subscriptionsJson);
                    stats.ActiveSubscriptions = subscriptions?.Count ?? 0;
                    
                    Console.WriteLine($"🔍 Total suscripciones: {stats.ActiveSubscriptions}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin stats");
                Console.WriteLine($"❌ Error obteniendo estadísticas: {ex.Message}");
            }

            return stats;
        }

        // ✅ NUEVO: Método auxiliar para obtener ID del usuario actual
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