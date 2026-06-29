using HarmonySound.API.Consumer;
using HarmonySound.Models;
using HarmonySound.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using HarmonySound.API.DTOs;

namespace HarmonySound.MVC.Controllers
{
    [Authorize(Roles = "artist")]
    public class ArtistsController : Controller
    {
        private readonly HttpClient _httpClient;

        public ArtistsController(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromMinutes(10);
        }

        // Método para obtener estadísticas del artista
        [HttpGet]
        public async Task<IActionResult> GetArtistStats()
        {
            try
            {
                int artistId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                Console.WriteLine($"Obteniendo estadísticas para artista ID: {artistId}");

                // Obtener total de canciones del artista
                var songsResponse = await _httpClient.GetAsync("https://localhost:7120/api/Contents");
                var songsJson = await songsResponse.Content.ReadAsStringAsync();
                var allSongs = JsonSerializer.Deserialize<List<Content>>(songsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var artistSongs = allSongs?.Where(c => c.ArtistId == artistId).ToList() ?? new List<Content>();
                var totalSongs = artistSongs.Count;

                // Usar el endpoint específico para álbumes del artista que ahora devuelve DTOs
                var albumsResponse = await _httpClient.GetAsync($"https://localhost:7120/api/Albums/ByArtist/{artistId}");
                var totalAlbums = 0;
                
                Console.WriteLine($"Respuesta de álbumes: {albumsResponse.StatusCode}");
                
                if (albumsResponse.IsSuccessStatusCode)
                {
                    var albumsJson = await albumsResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"JSON de álbumes: {albumsJson}");
                    
                    //Ahora esperamos una lista de AlbumDto
                    var albums = JsonSerializer.Deserialize<List<AlbumDto>>(albumsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    totalAlbums = albums?.Count ?? 0;
                    
                    Console.WriteLine($"Total álbumes encontrados: {totalAlbums}");
                }
                else
                {
                    var errorContent = await albumsResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error al obtener álbumes: {albumsResponse.StatusCode} - {errorContent}");
                }

                //  Obtener likes por canción (y total) usando JsonElement
                var totalLikes = 0;
                var perSongLikes = new List<(string Title, int Likes)>();
                foreach (var song in artistSongs)
                {
                    var songLikes = 0;
                    try
                    {
                        var likesResponse = await _httpClient.GetAsync($"https://localhost:7120/api/Contents/{song.Id}/likes");
                        if (likesResponse.IsSuccessStatusCode)
                        {
                            var likesJson = await likesResponse.Content.ReadAsStringAsync();
                            var likesDocument = JsonDocument.Parse(likesJson);
                            var likesElement = likesDocument.RootElement;

                            if (likesElement.TryGetProperty("likes", out var likesCount))
                            {
                                songLikes = likesCount.GetInt32();
                                totalLikes += songLikes;
                            }
                        }
                    }
                    catch
                    {
                        // Si hay error obteniendo likes de una canción, continuar con las demás
                    }
                    perSongLikes.Add((song.Title ?? "Sin título", songLikes));
                }

                // Obtener total de reproducciones desde Statistics
                var totalReproductions = 0;
                try
                {
                    var statsResponse = await _httpClient.GetAsync("https://localhost:7120/api/Statistics");
                    if (statsResponse.IsSuccessStatusCode)
                    {
                        var statsJson = await statsResponse.Content.ReadAsStringAsync();
                        var allStats = JsonSerializer.Deserialize<List<Statistic>>(statsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Statistic>();
                        var artistSongIds = artistSongs.Select(s => s.Id).ToHashSet();
                        totalReproductions = allStats
                            .Where(s => artistSongIds.Contains(s.ContentId))
                            .Sum(s => s.Reproductions);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error obteniendo reproducciones: {ex.Message}");
                }

                // Top 5 canciones por likes (para tabla y gráfico)
                var topSongs = perSongLikes
                    .OrderByDescending(s => s.Likes)
                    .Take(5)
                    .Select(s => new { title = s.Title, likes = s.Likes })
                    .ToList();

                // Obtener la última canción subida
                var lastUpload = artistSongs
                    .OrderByDescending(c => c.UploadDate)
                    .FirstOrDefault();

                var lastUploadTitle = lastUpload?.Title ?? "Sin subidas";

                Console.WriteLine($" Estadísticas finales:");
                Console.WriteLine($"   - Canciones: {totalSongs}");
                Console.WriteLine($"   - Álbumes: {totalAlbums}");
                Console.WriteLine($"   - Likes: {totalLikes}");
                Console.WriteLine($"   - Reproducciones: {totalReproductions}");
                Console.WriteLine($"   - Última subida: {lastUploadTitle}");

                // Retornar las estadísticas como JSON
                var stats = new
                {
                    totalSongs = totalSongs,
                    totalAlbums = totalAlbums,
                    totalLikes = totalLikes,
                    totalReproductions = totalReproductions,
                    lastUpload = lastUploadTitle,
                    topSongs = topSongs
                };

                // Usar opciones planas para evitar el wrapping de ReferenceHandler.Preserve
                // (configurado globalmente en Program.cs), que convertiría topSongs en {$id,$values:[...]}
                // y rompería el .map() del dashboard en el navegador.
                return new JsonResult(stats, new System.Text.Json.JsonSerializerOptions());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetArtistStats: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // En caso de error, retornar valores por defecto
                var errorStats = new
                {
                    totalSongs = 0,
                    totalAlbums = 0,
                    totalLikes = 0,
                    totalReproductions = 0,
                    lastUpload = "Error al cargar",
                    topSongs = new List<object>()
                };

                return new JsonResult(errorStats, new System.Text.Json.JsonSerializerOptions());
            }
        }

        // Vista Home del artista
        public async Task<IActionResult> Home()
        {
            int userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"https://localhost:7120/api/Users/profile/{userId}");
                if (!response.IsSuccessStatusCode)
                    return View("Error");

                var json = await response.Content.ReadAsStringAsync();
                var dto = System.Text.Json.JsonSerializer.Deserialize<ProfileEditViewModel>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Número de seguidores del artista
                ViewBag.FollowersCount = 0;
                ViewBag.RecentFollowers = new List<FollowerViewModel>();
                try
                {
                    var followersResponse = await client.GetAsync($"https://localhost:7120/api/Follows/followers/count/{userId}");
                    if (followersResponse.IsSuccessStatusCode)
                    {
                        var followersJson = await followersResponse.Content.ReadAsStringAsync();
                        using var doc = System.Text.Json.JsonDocument.Parse(followersJson);
                        ViewBag.FollowersCount = doc.RootElement.GetProperty("followersCount").GetInt32();
                    }

                    // Últimos en seguir (los 5 más recientes)
                    var recentResponse = await client.GetAsync($"https://localhost:7120/api/Follows/followers/{userId}");
                    if (recentResponse.IsSuccessStatusCode)
                    {
                        var recentJson = await recentResponse.Content.ReadAsStringAsync();
                        var followers = System.Text.Json.JsonSerializer.Deserialize<List<FollowerViewModel>>(recentJson,
                            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<FollowerViewModel>();
                        ViewBag.RecentFollowers = followers.Take(5).ToList();
                    }
                }
                catch (Exception followEx)
                {
                    Console.WriteLine($"No se pudo cargar el conteo de seguidores: {followEx.Message}");
                }

                ViewBag.Success = TempData["Success"];
                return View(dto);
            }
        }

        // GET: Editar perfil
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

        // POST: Editar perfil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(ProfileEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Some error message");
                return View(model);
            }

            // Subida de imagen
            if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
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
                        TempData["Error"] = "Error uploading profile image: " + error;
                        return View(model);
                    }
                }
            }
            else
            {
                // Si no se sube imagen, recupera la actual
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
                    TempData["Error"] = "Error updating profile: " + error;
                    return View(model);
                }
            }
            TempData["Success"] = "Perfil actualizado correctamente.";
            return RedirectToAction("Home");
        }

        // Vista Upload - Solo para mostrar el formulario de subida
        public IActionResult Upload()
        {
            return View();
        }

        // UploadAudio - Redirige a Upload después de subir
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAudio(IFormCollection form)
        {
            try
            {
                var file = form.Files["File"];
                if (file == null || file.Length == 0)
                {
                    TempData["Error"] = "Archivo no válido.";
                    return RedirectToAction("Upload");
                }

                System.Diagnostics.Debug.WriteLine($"Nombre: {file.FileName}, Tamaño: {file.Length}");

                if (string.IsNullOrWhiteSpace(form["Title"]) || string.IsNullOrWhiteSpace(form["Type"]) || string.IsNullOrWhiteSpace(form["ArtistId"]))
                {
                    TempData["Error"] = "Todos los campos son obligatorios.";
                    return RedirectToAction("Upload");
                }

                using var content = new MultipartFormDataContent();
                content.Add(new StringContent(form["Title"]), "Title");
                content.Add(new StringContent(form["Type"]), "Type");
                content.Add(new StringContent(form["ArtistId"]), "ArtistId");

                if (file.Length > 50 * 1024 * 1024)
                {
                    TempData["Error"] = "El archivo es demasiado grande.";
                    return RedirectToAction("Upload");
                }

                content.Add(new StreamContent(file.OpenReadStream()), "File", file.FileName);

                var response = await _httpClient.PostAsync("https://localhost:7120/api/Contents/upload", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Archivo subido con éxito.";
                }
                else
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Error al subir el archivo: {errorMsg}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Excepción: " + ex.ToString();
                System.Diagnostics.Debug.WriteLine("Excepción: " + ex.ToString());
            }

            return RedirectToAction("Upload");
        }

        // Index - Para mostrar las canciones con reproductor
        public async Task<IActionResult> Index()
        {
            var nameIdentifierClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (nameIdentifierClaim == null || !int.TryParse(nameIdentifierClaim.Value, out int artistId))
            {
                ViewBag.Error = "Tu sesión no es válida. Por favor, cierra sesión y vuelve a iniciar sesión.";
                return View("Error401");
            }

            Crud<Content>.EndPoint = "https://localhost:7120/api/Contents";
            var allContents = Crud<Content>.GetAll();
            var myContents = allContents.Where(c => c.ArtistId == artistId).ToList();
            return View(myContents);
        }

        // Ver detalles de una canción con nombre del artista
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://localhost:7120/api/Contents/{id}");
                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "No se pudo encontrar la canción.";
                    return RedirectToAction("Index");
                }

                var json = await response.Content.ReadAsStringAsync();
                var content = JsonSerializer.Deserialize<Content>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Verificar que el contenido pertenece al artista actual
                int artistId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                if (content.ArtistId != artistId)
                {
                    TempData["Error"] = "No tienes permisos para ver esta canción.";
                    return RedirectToAction("Index");
                }

                // Obtener nombre del artista
                var artistResponse = await _httpClient.GetAsync($"https://localhost:7120/api/Users/profile/{artistId}");
                if (artistResponse.IsSuccessStatusCode)
                {
                    var artistJson = await artistResponse.Content.ReadAsStringAsync();
                    var artistData = JsonSerializer.Deserialize<ProfileEditViewModel>(artistJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    ViewBag.ArtistName = artistData?.Name ?? "Artista desconocido";
                }
                else
                {
                    ViewBag.ArtistName = "Artista desconocido";
                }

                return View(content);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar los detalles: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET - Editar canción con nombre del artista
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://localhost:7120/api/Contents/{id}");
                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "No se pudo encontrar la canción.";
                    return RedirectToAction("Index");
                }

                var json = await response.Content.ReadAsStringAsync();
                var content = JsonSerializer.Deserialize<Content>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Verificar que el contenido pertenece al artista actual
                int artistId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                if (content.ArtistId != artistId)
                {
                    TempData["Error"] = "No tienes permisos para editar esta canción.";
                    return RedirectToAction("Index");
                }

                // Obtener nombre del artista
                var artistResponse = await _httpClient.GetAsync($"https://localhost:7120/api/Users/profile/{artistId}");
                if (artistResponse.IsSuccessStatusCode)
                {
                    var artistJson = await artistResponse.Content.ReadAsStringAsync();
                    var artistData = JsonSerializer.Deserialize<ProfileEditViewModel>(artistJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    ViewBag.ArtistName = artistData?.Name ?? "Artista desconocido";
                }
                else
                {
                    ViewBag.ArtistName = "Artista desconocido";
                }

                return View(content);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar la canción: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST - Editar canción - Incluir UrlMedia
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string title, string type)
        {
            try
            {
                // Verificar que el contenido existe y pertenece al artista actual
                var getResponse = await _httpClient.GetAsync($"https://localhost:7120/api/Contents/{id}");
                if (!getResponse.IsSuccessStatusCode)
                {
                    TempData["Error"] = "No se pudo encontrar la canción.";
                    return RedirectToAction("Index");
                }

                var json = await getResponse.Content.ReadAsStringAsync();
                var existingContent = JsonSerializer.Deserialize<Content>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                int artistId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                if (existingContent.ArtistId != artistId)
                {
                    TempData["Error"] = "No tienes permisos para editar esta canción.";
                    return RedirectToAction("Index");
                }

                // Validar que el título no esté vacío
                if (string.IsNullOrWhiteSpace(title))
                {
                    // Obtener nombre del artista para la vista
                    var artistResponse = await _httpClient.GetAsync($"https://localhost:7120/api/Users/profile/{artistId}");
                    if (artistResponse.IsSuccessStatusCode)
                    {
                        var artistJson = await artistResponse.Content.ReadAsStringAsync();
                        var artistData = JsonSerializer.Deserialize<ProfileEditViewModel>(artistJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        ViewBag.ArtistName = artistData?.Name ?? "Artista desconocido";
                    }
                    else
                    {
                        ViewBag.ArtistName = "Artista desconocido";
                    }

                    TempData["Error"] = "El título de la canción es obligatorio.";
                    return View(existingContent);
                }

                // Validar longitud máxima del título
                if (title.Length > 20)
                {
                    // Obtener nombre del artista para la vista
                    var artistResponse = await _httpClient.GetAsync($"https://localhost:7120/api/Users/profile/{artistId}");
                    if (artistResponse.IsSuccessStatusCode)
                    {
                        var artistJson = await artistResponse.Content.ReadAsStringAsync();
                        var artistData = JsonSerializer.Deserialize<ProfileEditViewModel>(artistJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        ViewBag.ArtistName = artistData?.Name ?? "Artista desconocido";
                    }
                    else
                    {
                        ViewBag.ArtistName = "Artista desconocido";
                    }

                    TempData["Error"] = "El título no puede exceder los 20 caracteres.";
                    return View(existingContent);
                }

                // Incluir todos los campos requeridos, incluyendo UrlMedia
                var updateModel = new
                {
                    Id = id,
                    Title = title.Trim(),
                    Type = type ?? existingContent.Type,
                    UrlMedia = existingContent.UrlMedia, 
                    Duration = existingContent.Duration, 
                    UploadDate = existingContent.UploadDate, 
                    ArtistId = existingContent.ArtistId 
                };

                var updateJson = JsonSerializer.Serialize(updateModel);
                var content = new StringContent(updateJson, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"https://localhost:7120/api/Contents/{id}", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Canción actualizada correctamente.";
                    return RedirectToAction("Index");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Error al actualizar la canción: {error}";
                    
                    // Obtener nombre del artista para la vista de error
                    var artistResponse = await _httpClient.GetAsync($"https://localhost:7120/api/Users/profile/{artistId}");
                    if (artistResponse.IsSuccessStatusCode)
                    {
                        var artistJson = await artistResponse.Content.ReadAsStringAsync();
                        var artistData = JsonSerializer.Deserialize<ProfileEditViewModel>(artistJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        ViewBag.ArtistName = artistData?.Name ?? "Artista desconocido";
                    }
                    else
                    {
                        ViewBag.ArtistName = "Artista desconocido";
                    }
                    
                    return View(existingContent);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar la canción: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // Eliminar canción (incluye eliminación de Azure)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                // Verificar que el contenido pertenece al artista actual
                var getResponse = await _httpClient.GetAsync($"https://localhost:7120/api/Contents/{id}");
                if (!getResponse.IsSuccessStatusCode)
                {
                    TempData["Error"] = "No se pudo encontrar la canción.";
                    return RedirectToAction("Index");
                }

                var json = await getResponse.Content.ReadAsStringAsync();
                var content = JsonSerializer.Deserialize<Content>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                int artistId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                if (content.ArtistId != artistId)
                {
                    TempData["Error"] = "No tienes permisos para eliminar esta canción.";
                    return RedirectToAction("Index");
                }

                // Eliminar de la base de datos Y de Azure Storage
                var deleteResponse = await _httpClient.DeleteAsync($"https://localhost:7120/api/Contents/{id}");

                if (deleteResponse.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Canción eliminada correctamente (incluido el archivo de Azure).";
                }
                else
                {
                    var error = await deleteResponse.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Error al eliminar la canción: {error}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar la canción: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}