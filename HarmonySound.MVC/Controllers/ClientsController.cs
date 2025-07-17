using HarmonySound.API.Consumer;
using HarmonySound.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HarmonySound.MVC.Models;
using System.Text.Json;
namespace HarmonySound.MVC.Controllers
{
    [Authorize(Roles = "client")]
    public class ClientsController : Controller
    {
        public async Task<IActionResult> Index()
        {
            // Agregar esta línea para obtener el userId
            int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
            ViewBag.UserId = userId; // ESTO ES LO QUE FALTA

            Crud<Content>.EndPoint = "https://localhost:7120/api/Contents";
            var contents = Crud<Content>.GetAll();
            
            // Obtener información de los artistas para cada contenido
            var contentsWithArtists = new List<ContentWithArtistDto>();
            
            using (var client = new HttpClient())
            {
                var userIds = contents.Select(c => c.ArtistId).Distinct().ToList();
                var artists = new Dictionary<int, string>();
                
                foreach (var artistId in userIds)
                {
                    var artistResponse = await client.GetAsync($"https://localhost:7120/api/Users/profile/{artistId}");
                    if (artistResponse.IsSuccessStatusCode)
                    {
                        var artistJson = await artistResponse.Content.ReadAsStringAsync();
                        var artist = System.Text.Json.JsonSerializer.Deserialize<User>(artistJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        artists[artistId] = artist?.Name ?? "Artista desconocido";
                    }
                    else
                    {
                        artists[artistId] = "Artista desconocido";
                    }
                }

                // Convertir a ContentWithArtistDto
                contentsWithArtists = contents.Select(c => new ContentWithArtistDto
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

            return View(contentsWithArtists);
        }
        public async Task<IActionResult> EditProfile()
        {
            int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"https://localhost:7120/api/Users/profile/{userId}");
                if (!response.IsSuccessStatusCode)
                    return View("Error");

                var json = await response.Content.ReadAsStringAsync();
                var dto = System.Text.Json.JsonSerializer.Deserialize<ProfileEditViewModel>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return View(dto);
            }
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
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"https://localhost:7120/api/Users/profile/{model.Id}");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var dto = System.Text.Json.JsonSerializer.Deserialize<ProfileEditViewModel>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        model.ProfileImageUrl = dto?.ProfileImageUrl;
                    }
                }
            }
            else
            {
                using (var client = new HttpClient())
                {
                    var form = new MultipartFormDataContent();
                    form.Add(new StringContent(model.Id.ToString()), "UserId");
                    form.Add(new StreamContent(model.ProfileImageFile.OpenReadStream()), "file", model.ProfileImageFile.FileName);

                    var response = await client.PostAsync("https://localhost:7120/api/Users/upload-profile-image", form);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var result = System.Text.Json.JsonDocument.Parse(json);
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

            using (var client = new HttpClient())
            {
                var json = System.Text.Json.JsonSerializer.Serialize(userUpdate);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await client.PutAsync($"https://localhost:7120/api/Users/profile/{model.Id}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError("", $"Error updating profile: {error}");
                    return View(model);
                }
            }
            return RedirectToAction("Home");
        }

        public async Task<IActionResult> Home(string query)
        {
            int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
            var model = new SearchResultsViewModel { Query = query };

            // Obtén el perfil del usuario
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"https://localhost:7120/api/Users/profile/{userId}");
                if (!response.IsSuccessStatusCode)
                    return View("Error");

                var json = await response.Content.ReadAsStringAsync();
                model.Profile = System.Text.Json.JsonSerializer.Deserialize<ProfileEditViewModel>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            // Si hay búsqueda, consulta artistas y contenidos
            if (!string.IsNullOrWhiteSpace(query))
            {
                using (var client = new HttpClient())
                {
                    // Buscar artistas
                    var artistsResponse = await client.GetAsync($"https://localhost:7120/api/Users/search?query={Uri.EscapeDataString(query)}");
                    if (artistsResponse.IsSuccessStatusCode)
                    {
                        var artistsJson = await artistsResponse.Content.ReadAsStringAsync();
                        model.Artists = System.Text.Json.JsonSerializer.Deserialize<List<User>>(artistsJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                    }

                    // Buscar contenidos
                    var contentsResponse = await client.GetAsync($"https://localhost:7120/api/Contents/search?query={Uri.EscapeDataString(query)}");
                    if (contentsResponse.IsSuccessStatusCode)
                    {
                        var contentsJson = await contentsResponse.Content.ReadAsStringAsync();
                        var contents = System.Text.Json.JsonSerializer.Deserialize<List<Content>>(contentsJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                        
                        // Obtener información de los artistas para cada contenido
                        var userIds = contents.Select(c => c.ArtistId).Distinct().ToList();
                        var artists = new Dictionary<int, string>();
                        
                        foreach (var artistId in userIds)
                        {
                            var artistResponse = await client.GetAsync($"https://localhost:7120/api/Users/profile/{artistId}");
                            if (artistResponse.IsSuccessStatusCode)
                            {
                                var artistJson = await artistResponse.Content.ReadAsStringAsync();
                                var artist = System.Text.Json.JsonSerializer.Deserialize<User>(artistJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
            }

            ViewBag.Success = TempData["Success"];
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> LikeContent(int contentId, int userId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(userId);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await client.PostAsync($"https://localhost:7120/api/Contents/{contentId}/like", content);

                    if (response.IsSuccessStatusCode)
                    {
                        return Json(new { success = true, message = "Like agregado" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Error al agregar like" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UnlikeContent(int contentId, int userId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(userId);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await client.PostAsync($"https://localhost:7120/api/Contents/{contentId}/unlike", content);

                    if (response.IsSuccessStatusCode)
                    {
                        return Json(new { success = true, message = "Like removido" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Error al remover like" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetContentLikes(int contentId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"https://localhost:7120/api/Contents/{contentId}/likes");
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var result = System.Text.Json.JsonSerializer.Deserialize<dynamic>(json);
                        return Json(result);
                    }
                    else
                    {
                        return Json(new { likes = 0 });
                    }
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
                int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
                System.Diagnostics.Debug.WriteLine($"=== MVC GetUserPlaylists called with userId: {userId} ===");
                
                using (var client = new HttpClient())
                {
                    var apiUrl = $"https://localhost:7120/api/Playlists/user/{userId}";
                    System.Diagnostics.Debug.WriteLine($"Calling API: {apiUrl}");
                    
                    var response = await client.GetAsync(apiUrl);
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
                        
                        var playlists = System.Text.Json.JsonSerializer.Deserialize<JsonElement[]>(json);
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
                
                using (var client = new HttpClient())
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(contentId);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    
                    var apiUrl = $"https://localhost:7120/api/Playlists/{playlistId}/add";
                    System.Diagnostics.Debug.WriteLine($"Calling API: {apiUrl} with content: {json}");
                    
                    var response = await client.PostAsync(apiUrl, content);
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
                int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
                
                System.Diagnostics.Debug.WriteLine($"Creating playlist: {name} for user: {userId} with content: {contentId}");
                
                var playlistDto = new
                {
                    Name = name,
                    UserId = userId
                };

                using (var client = new HttpClient())
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(playlistDto);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    
                    System.Diagnostics.Debug.WriteLine($"Sending to API: {json}");
                    
                    var response = await client.PostAsync("https://localhost:7120/api/Playlists", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"API Response: {responseContent}");
                        
                        // Si se creó correctamente y hay un contentId, agregarlo a la playlist
                        if (contentId.HasValue)
                        {
                            var createdPlaylist = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);
                            var playlistId = createdPlaylist.GetProperty("id").GetInt32();
            
                            System.Diagnostics.Debug.WriteLine($"Adding content {contentId} to playlist {playlistId}");
                            
                            // Agregar el contenido a la playlist recién creada
                            var contentJson = System.Text.Json.JsonSerializer.Serialize(contentId.Value);
                            var contentToAdd = new StringContent(contentJson, System.Text.Encoding.UTF8, "application/json");
                            var addResponse = await client.PostAsync($"https://localhost:7120/api/Playlists/{playlistId}/add", contentToAdd);
                            
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
                int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
                
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"https://localhost:7120/api/UserPlans/is-premium/{userId}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var result = System.Text.Json.JsonSerializer.Deserialize<dynamic>(json);
                        return Json(result);
                    }
                    else
                    {
                        return Json(new { isPremium = false });
                    }
                }
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
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync("https://localhost:7120/api/Ads/random");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var result = System.Text.Json.JsonSerializer.Deserialize<dynamic>(json);
                        return Json(result);
                    }
                    else
                    {
                        return Json(new { url = "/ads/ad1.mp3", duration = 15, title = "Suscríbete a Premium" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { url = "/ads/ad1.mp3", duration = 15, title = "Suscríbete a Premium" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserLikes()
        {
            try
            {
                int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
                
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"https://localhost:7120/api/Contents/user-likes/{userId}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var likedContentIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(json);
                        return Json(likedContentIds ?? new List<int>());
                    }
                    else
                    {
                        return Json(new List<int>());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en GetUserLikes: {ex.Message}");
                return Json(new List<int>());
            }
        }
    }
}
