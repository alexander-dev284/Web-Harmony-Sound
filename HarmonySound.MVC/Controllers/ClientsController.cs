using HarmonySound.Models;
using HarmonySound.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace HarmonySound.MVC.Controllers
{
    [Authorize(Roles = "client")]
    public class ClientsController : Controller
    {
        // Campo para la URL base de la API
        private readonly string _apiBaseUrl = "https://localhost:7120";
        private readonly HttpClient _httpClient;

        public ClientsController(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromMinutes(10);
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                ViewBag.UserId = userId;

                // Verificar estado de suscripción
                var userPlanResponse = await _httpClient.GetAsync($"https://localhost:7120/api/UserPlans/user/{userId}");
                if (userPlanResponse.IsSuccessStatusCode)
                {
                    var userPlanJson = await userPlanResponse.Content.ReadAsStringAsync();
                    var userPlan = JsonSerializer.Deserialize<UserPlan>(userPlanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    ViewBag.SubscriptionStatus = new {
                        IsCancelled = userPlan?.IsCancelled ?? false,
                        IsActive = userPlan?.Active ?? false,
                        EndDate = userPlan?.EndDate
                    };
                }

                var response = await _httpClient.GetAsync("https://localhost:7120/api/Contents/with-artists");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var contents = JsonSerializer.Deserialize<List<ContentWithArtistDto>>(json, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    return View(contents);
                }
                else
                {
                    ViewBag.Error = "No se pudieron cargar las canciones.";
                    return View(new List<ContentWithArtistDto>());
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al cargar contenido: {ex.Message}";
                return View(new List<ContentWithArtistDto>());
            }
        }

        public async Task<IActionResult> EditProfile()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var response = await _httpClient.GetAsync($"https://localhost:7120/api/Users/profile/{userId}");
            if (!response.IsSuccessStatusCode)
                return View("Error");

            var json = await response.Content.ReadAsStringAsync();
            var dto = JsonSerializer.Deserialize<ProfileEditViewModel>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(ProfileEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    Console.WriteLine(error.ErrorMessage);

                return View(model);
            }

            // Si NO se sube una nueva imagen, recupera la URL actual antes de actualizar
            if (model.ProfileImageFile == null || model.ProfileImageFile.Length == 0)
            {
                var response = await _httpClient.GetAsync($"https://localhost:7120/api/Users/profile/{model.Id}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var dto = JsonSerializer.Deserialize<ProfileEditViewModel>(json, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    model.ProfileImageUrl = dto?.ProfileImageUrl;
                }
            }
            else
            {
                var form = new MultipartFormDataContent();
                form.Add(new StringContent(model.Id.ToString()), "UserId");
                form.Add(new StreamContent(model.ProfileImageFile.OpenReadStream()), "file", model.ProfileImageFile.FileName);

                var response = await _httpClient.PostAsync("https://localhost:7120/api/Users/upload-profile-image", form);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonDocument.Parse(json);
                    if (result.RootElement.TryGetProperty("ProfileImageUrl", out var urlProp) ||
                        result.RootElement.TryGetProperty("profileImageUrl", out urlProp))
                    {
                        model.ProfileImageUrl = urlProp.GetString();
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se recibió la URL de la imagen del servidor.");
                        return View(model);
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"Error uploading profile image: {error}");
                    return View(model);
                }
            }

            // Actualizar el resto del perfil
            var userUpdate = new
            {
                Id = model.Id,
                Name = model.Name,
                Biography = model.Biography,
                ProfileImageUrl = model.ProfileImageUrl,
                Email = model.Email
            };

            var updateJson = JsonSerializer.Serialize(userUpdate);
            var content = new StringContent(updateJson, System.Text.Encoding.UTF8, "application/json");
            var updateResponse = await _httpClient.PutAsync($"https://localhost:7120/api/Users/profile/{model.Id}", content);

            if (!updateResponse.IsSuccessStatusCode)
            {
                var error = await updateResponse.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Error updating profile: {error}");
                return View(model);
            }

            return RedirectToAction("Home");
        }

        public async Task<IActionResult> Home(string query)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            ViewBag.UserId = userId; 
            
            var model = new SearchResultsViewModel { Query = query };

            // Verificar estado de suscripción
            var userPlanResponse = await _httpClient.GetAsync($"https://localhost:7120/api/UserPlans/user/{userId}");
            if (userPlanResponse.IsSuccessStatusCode)
            {
                var userPlanJson = await userPlanResponse.Content.ReadAsStringAsync();
                var userPlan = JsonSerializer.Deserialize<UserPlan>(userPlanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                ViewBag.SubscriptionStatus = new {
                    IsCancelled = userPlan?.IsCancelled ?? false,
                    IsActive = userPlan?.Active ?? false,
                    EndDate = userPlan?.EndDate
                };
            }

            // Obtén el perfil del usuario
            var response = await _httpClient.GetAsync($"https://localhost:7120/api/Users/profile/{userId}");
            if (!response.IsSuccessStatusCode)
                return View("Error");

            var json = await response.Content.ReadAsStringAsync();
            model.Profile = JsonSerializer.Deserialize<ProfileEditViewModel>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Si hay búsqueda, consulta artistas y contenidos
            if (!string.IsNullOrWhiteSpace(query))
            {
                // Buscar artistas
                var artistsResponse = await _httpClient.GetAsync($"https://localhost:7120/api/Users/search?query={Uri.EscapeDataString(query)}");
                if (artistsResponse.IsSuccessStatusCode)
                {
                    var artistsJson = await artistsResponse.Content.ReadAsStringAsync();
                    model.Artists = JsonSerializer.Deserialize<List<User>>(artistsJson, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }

                // Buscar contenidos
                var contentsResponse = await _httpClient.GetAsync($"https://localhost:7120/api/Contents/search?query={Uri.EscapeDataString(query)}");
                if (contentsResponse.IsSuccessStatusCode)
                {
                    var contentsJson = await contentsResponse.Content.ReadAsStringAsync();
                    var contents = JsonSerializer.Deserialize<List<Content>>(contentsJson, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                    
                    // Obtener información de los artistas para cada contenido
                    var userIds = contents.Select(c => c.ArtistId).Distinct().ToList();
                    var artists = new Dictionary<int, string>();
                    
                    foreach (var artistId in userIds)
                    {
                        var artistResponse = await _httpClient.GetAsync($"https://localhost:7120/api/Users/profile/{artistId}");
                        if (artistResponse.IsSuccessStatusCode)
                        {
                            var artistJson = await artistResponse.Content.ReadAsStringAsync();
                            var artist = JsonSerializer.Deserialize<User>(artistJson, 
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            artists[artistId] = artist?.Name ?? "Artista desconocido";
                        }
                        else
                        {
                            artists[artistId] = "Artista desconocido";
                        }
                    }

                    // Convertir a ContentWithArtistDto
                    model.Contents = contents.Select(c => new ContentWithArtistDto
                    {
                        Id = c.Id,
                        Title = c.Title,
                        Type = c.Type,
                        UrlMedia = c.UrlMedia,
                        Duration = c.Duration,
                        UploadDate = c.UploadDate,
                        ArtistId = c.ArtistId,
                        ArtistName = artists.GetValueOrDefault(c.ArtistId, "Artista desconocido")
                    }).ToList();
                }
            }

            ViewBag.Success = TempData["Success"];
            return View(model);
        }
                    
        [HttpGet]
        public async Task<IActionResult> GetContentLikes(int contentId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://localhost:7120/api/Contents/{contentId}/likes");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<dynamic>(json);
                    return Json(result);
                }
                else
                {
                    return Json(new { likes = 0 });
                }
            }
            catch (Exception ex)
            {
                return Json(new { likes = 0 });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserPlaylists()
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                System.Diagnostics.Debug.WriteLine($"=== MVC GetUserPlaylists called with userId: {userId} ===");
                
                var apiUrl = $"https://localhost:7120/api/Playlists/user/{userId}";
                System.Diagnostics.Debug.WriteLine($"Calling API: {apiUrl}");
                
                var response = await _httpClient.GetAsync(apiUrl);
                System.Diagnostics.Debug.WriteLine($"API Response Status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"API Response JSON: {json}");
                    
                    if (string.IsNullOrEmpty(json) || json == "null")
                    {
                        System.Diagnostics.Debug.WriteLine("Empty response from API");
                        return Json(new List<object>());
                    }
                    
                    var playlists = JsonSerializer.Deserialize<JsonElement[]>(json);
                    System.Diagnostics.Debug.WriteLine($"Deserialized {playlists.Length} playlists");
                    
                    var userPlaylists = playlists.Select(p => new {
                        id = p.GetProperty("id").GetInt32(),
                        name = p.GetProperty("name").GetString(),
                        songsCount = p.TryGetProperty("songs", out var songs) ? songs.GetArrayLength() : 0
                    }).ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"Returning {userPlaylists.Count} processed playlists");
                    return Json(userPlaylists);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"API Error: {response.StatusCode} - {errorContent}");
                    return Json(new List<object>());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in GetUserPlaylists: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new List<object>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToPlaylist(int playlistId, int contentId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== MVC AddToPlaylist: playlist {playlistId}, content {contentId} ===");
                
                var json = JsonSerializer.Serialize(contentId);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var apiUrl = $"https://localhost:7120/api/Playlists/{playlistId}/add";
                System.Diagnostics.Debug.WriteLine($"Calling API: {apiUrl} with content: {json}");
                
                var response = await _httpClient.PostAsync(apiUrl, content);
                System.Diagnostics.Debug.WriteLine($"API Response Status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"API Response: {responseContent}");
                    return Json(new { success = true, message = "Contenido agregado a la playlist" });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"API Error: {errorContent}");
                    return Json(new { success = false, message = "Error al agregar a playlist: " + errorContent });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in AddToPlaylist: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePlaylist(string name, int? contentId = null)
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                
                System.Diagnostics.Debug.WriteLine($"Creating playlist: {name} for user: {userId} with content: {contentId}");
                
                var playlistDto = new
                {
                    Name = name,
                    UserId = userId
                };

                var json = JsonSerializer.Serialize(playlistDto);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                System.Diagnostics.Debug.WriteLine($"Sending to API: {json}");
                
                var response = await _httpClient.PostAsync("https://localhost:7120/api/Playlists", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"API Response: {responseContent}");
                    
                    // Si se creó correctamente y hay un contentId, agregarlo a la playlist
                    if (contentId.HasValue)
                    {
                        var createdPlaylist = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        var playlistId = createdPlaylist.GetProperty("id").GetInt32();
        
                        System.Diagnostics.Debug.WriteLine($"Adding content {contentId} to playlist {playlistId}");
                        
                        // Agregar el contenido a la playlist recién creada
                        var contentJson = JsonSerializer.Serialize(contentId.Value);
                        var contentToAdd = new StringContent(contentJson, System.Text.Encoding.UTF8, "application/json");
                        var addResponse = await _httpClient.PostAsync($"https://localhost:7120/api/Playlists/{playlistId}/add", contentToAdd);
                        
                        if (addResponse.IsSuccessStatusCode)
                        {
                            return Json(new { success = true, message = "Playlist creada y contenido agregado correctamente" });
                        }
                        else
                        {
                            var addError = await addResponse.Content.ReadAsStringAsync();
                            System.Diagnostics.Debug.WriteLine($"Error adding content: {addError}");
                            return Json(new { success = true, message = "Playlist creada, pero no se pudo agregar el contenido" });
                        }
                    }
                    
                    return Json(new { success = true, message = "Playlist creada correctamente" });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"API Error: {errorContent}");
                    return Json(new { success = false, message = "Error al crear playlist: " + errorContent });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in CreatePlaylist: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckPremiumStatus()
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/UserPlans/user/{userId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var userPlan = JsonSerializer.Deserialize<UserPlan>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    return Json(new { isPremium = userPlan?.Active == true && userPlan?.IsCancelled != true });
                }
                
                return Json(new { isPremium = false });
            }
            catch (Exception ex)
            {
                return Json(new { isPremium = false });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRandomAd()
        {
            try
            {
                // URLs apuntando al proyecto API (puerto 7120)
                var ads = new[]
                {
                    new { url = "https://localhost:7120/ads/ad1.mp3", duration = 30 },
                    new { url = "https://localhost:7120/ads/ad2.mp3", duration = 25 },
                    new { url = "https://localhost:7120/ads/ad3.mp3", duration = 20 }
                };
                
                var random = new Random();
                var selectedAd = ads[random.Next(ads.Length)];
                
                Console.WriteLine($"Anuncio seleccionado: {selectedAd.url} ({selectedAd.duration}s)");
                
                return Json(selectedAd);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener anuncio: {ex.Message}");
                
                // Anuncio silencioso
                return Json(new { 
                    url = "data:audio/wav;base64,UklGRnoGAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQoGAACBhYqFbF1fdJivrJBhNjVgodDbq2EcBj+a2/LDciUFLIHO8tiJNwgZaLvt559NEAxQp+PwtmMcBjiR1/LMeSwFJHfH8N2QQAoUXrTp66hVFApGn+DyvmMbBDuX3ixOQBNH", 
                    duration = 5 
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReactivateSubscription()
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                var payload = new { UserId = userId };
                var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("https://localhost:7120/api/UserPlans/reactivate", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "¡Suscripción reactivada exitosamente!";
                    return RedirectToAction("ReactivationSuccess", "Plans");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Error al reactivar la suscripción: {error}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al reactivar la suscripción: {ex.Message}";
            }

            return RedirectToAction("Index", "Plans");
        }

        [HttpGet]
        public async Task<IActionResult> GetUserLikes()
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/Likes/user/{userId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var likes = JsonSerializer.Deserialize<List<int>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return Json(likes);
                }
                
                return Json(new List<int>());
            }
            catch (Exception ex)
            {
                return Json(new List<int>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> LikeContent(int contentId, int userId)
        {
            try
            {
                var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(contentId.ToString()), "contentId");
                formData.Add(new StringContent(userId.ToString()), "userId");

                var response = await _httpClient.PostAsync("https://localhost:7120/api/Likes", formData);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return Json(new
                    {
                        success = result.GetProperty("success").GetBoolean(),
                        message = result.GetProperty("message").GetString(),
                        action = result.TryGetProperty("action", out var actionProp) ? actionProp.GetString() : "unknown"
                    });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = errorContent });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UnlikeContent(int contentId, int userId)
        {
            try
            {
                var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(contentId.ToString()), "contentId");
                formData.Add(new StringContent(userId.ToString()), "userId");

                var response = await _httpClient.PostAsync("https://localhost:7120/api/Likes/unlike", formData);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return Json(new
                    {
                        success = result.GetProperty("success").GetBoolean(),
                        message = result.GetProperty("message").GetString(),
                        action = "removed"
                    });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = errorContent });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
}
