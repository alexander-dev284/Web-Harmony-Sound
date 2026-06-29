using HarmonySound.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using HarmonySound.MVC.Models; 

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
                        ImageUrl = p.TryGetProperty("imageUrl", out var imageUrl) 
                            ? imageUrl.GetString() 
                            : null,
                        Songs = p.TryGetProperty("songs", out var songs) 
                            ? songs.EnumerateArray().Select(s => new PlaylistSongDto
                            {
                                ContentId = s.GetProperty("contentId").GetInt32(),
                                Title = s.GetProperty("title").GetString() ?? "",
                                UrlMedia = s.GetProperty("urlMedia").GetString() ?? "",
                                ArtistName = s.TryGetProperty("artistName", out var artistName) 
                                    ? artistName.GetString() ?? "Artista desconocido"
                                    : "Artista desconocido",
                                // Mapear Duration
                                Duration = s.TryGetProperty("duration", out var duration) 
                                    ? TimeSpan.Parse(duration.GetString() ?? "00:00:00")
                                    : TimeSpan.Zero
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

        // Agregar varias canciones a la vez a una playlist
        [HttpPost]
        public async Task<IActionResult> AddMultipleToPlaylist(int playlistId, List<int> contentIds)
        {
            if (contentIds == null || !contentIds.Any())
            {
                TempData["Error"] = "No seleccionaste ninguna canción.";
                return RedirectToAction("Details", new { id = playlistId });
            }

            int added = 0, failed = 0;
            foreach (var contentId in contentIds.Distinct())
            {
                try
                {
                    var content = new StringContent(contentId.ToString(), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"https://localhost:7120/api/Playlists/{playlistId}/add", content);
                    if (response.IsSuccessStatusCode)
                        added++;
                    else
                        failed++;
                }
                catch (Exception ex)
                {
                    failed++;
                    System.Diagnostics.Debug.WriteLine($"Error al agregar la canción {contentId}: {ex.Message}");
                }
            }

            if (added > 0)
                TempData["Success"] = $"Se agregaron {added} canción(es) a la playlist." + (failed > 0 ? $" {failed} no se pudieron agregar." : "");
            else
                TempData["Error"] = "No se pudo agregar ninguna canción a la playlist.";

            return RedirectToAction("Details", new { id = playlistId });
        }

        // GET: Playlists/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Playlists/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Playlist playlist, IFormFile? imageFile)
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                
                // Crear formulario multipart
                using var content = new MultipartFormDataContent();
                content.Add(new StringContent(playlist.Name), "Name");
                content.Add(new StringContent(userId.ToString()), "UserId");
                
                // Agregar imagen si existe
                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileContent = new StreamContent(imageFile.OpenReadStream());
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageFile.ContentType);
                    content.Add(fileContent, "ImageFile", imageFile.FileName);
                }

                var response = await _httpClient.PostAsync("https://localhost:7120/api/Playlists", content);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Playlist creada exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Error al crear la playlist.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
            }
            
            return View(playlist);
        }

        // GET: Playlists/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                
                // Pasar UserId a la vista
                ViewBag.UserId = userId;
                
                // Obtener playlist específica
                var response = await _httpClient.GetAsync($"https://localhost:7120/api/Playlists/user/{userId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var playlistsJson = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var playlistsData = JsonSerializer.Deserialize<JsonElement[]>(playlistsJson, options);
                    
                    var playlist = playlistsData.FirstOrDefault(p => p.GetProperty("id").GetInt32() == id);
                    
                    if (playlist.ValueKind == JsonValueKind.Undefined)
                    {
                        return NotFound();
                    }
                    
                    var playlistDto = new PlaylistDto
                    {
                        Id = playlist.GetProperty("id").GetInt32(),
                        Name = playlist.GetProperty("name").GetString() ?? "",
                        ImageUrl = playlist.TryGetProperty("imageUrl", out var imageUrl) 
                            ? imageUrl.GetString() 
                            : null,
                        Songs = playlist.TryGetProperty("songs", out var songs) 
                            ? songs.EnumerateArray().Select(s => new PlaylistSongDto
                            {
                                ContentId = s.GetProperty("contentId").GetInt32(),
                                Title = s.GetProperty("title").GetString() ?? "",
                                UrlMedia = s.GetProperty("urlMedia").GetString() ?? "",
                                ArtistName = s.TryGetProperty("artistName", out var artistName) 
                                    ? artistName.GetString() ?? "Artista desconocido"
                                    : "Artista desconocido",
                                Duration = TimeSpan.TryParse(s.TryGetProperty("duration", out var duration) 
                                    ? duration.GetString() 
                                    : "00:00:00", out var parsedDuration) 
                                    ? parsedDuration 
                                    : TimeSpan.Zero
                            }).ToList()
                            : new List<PlaylistSongDto>()
                    };

                    // Canciones disponibles para añadir (todas las que no están ya en la playlist)
                    var availableSongs = new List<ContentWithArtistDto>();
                    try
                    {
                        var contentsResponse = await _httpClient.GetAsync("https://localhost:7120/api/Contents/with-artists");
                        if (contentsResponse.IsSuccessStatusCode)
                        {
                            var contentsJson = await contentsResponse.Content.ReadAsStringAsync();
                            var allContents = JsonSerializer.Deserialize<List<ContentWithArtistDto>>(contentsJson, options)
                                ?? new List<ContentWithArtistDto>();

                            var existingIds = playlistDto.Songs.Select(s => s.ContentId).ToHashSet();
                            availableSongs = allContents
                                .Where(c => !existingIds.Contains(c.Id))
                                .OrderBy(c => c.Title)
                                .ToList();
                        }
                    }
                    catch (Exception contentsEx)
                    {
                        Console.WriteLine($"No se pudieron cargar las canciones disponibles: {contentsEx.Message}");
                    }
                    ViewBag.AvailableSongs = availableSongs;

                    return View(playlistDto);
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en Details: {ex.Message}");
                return View("Error");
            }
        }

        // GET: PlaylistsController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                
                // Obtener playlist específica
                var response = await _httpClient.GetAsync($"https://localhost:7120/api/Playlists/user/{userId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var playlistsJson = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var playlistsData = JsonSerializer.Deserialize<JsonElement[]>(playlistsJson, options);
                    
                    var playlist = playlistsData.FirstOrDefault(p => p.GetProperty("id").GetInt32() == id);
                    
                    if (playlist.ValueKind == JsonValueKind.Undefined)
                    {
                        TempData["Error"] = "Playlist no encontrada.";
                        return RedirectToAction("Index");
                    }
                    
                    var playlistDto = new PlaylistDto
                    {
                        Id = playlist.GetProperty("id").GetInt32(),
                        Name = playlist.GetProperty("name").GetString() ?? "",
                        // ImageUrl para mostrar imagen actual
                        ImageUrl = playlist.TryGetProperty("imageUrl", out var imageUrl) 
                            ? imageUrl.GetString() 
                            : null,
                        Songs = playlist.TryGetProperty("songs", out var songs) 
                            ? songs.EnumerateArray().Select(s => new PlaylistSongDto
                            {
                                ContentId = s.GetProperty("contentId").GetInt32(),
                                Title = s.GetProperty("title").GetString() ?? "",
                                UrlMedia = s.GetProperty("urlMedia").GetString() ?? "",
                                ArtistName = s.TryGetProperty("artistName", out var artistName) 
                                    ? artistName.GetString() ?? "Artista desconocido"
                                    : "Artista desconocido",
                                // Mapear Duration
                                Duration = s.TryGetProperty("duration", out var duration) 
                                    ? TimeSpan.Parse(duration.GetString() ?? "00:00:00")
                                    : TimeSpan.Zero
                            }).ToList() 
                            : new List<PlaylistSongDto>()
                    };
                    
                    return View(playlistDto);
                }
                
                TempData["Error"] = "No se pudo cargar la playlist.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in PlaylistsController.Edit: {ex.Message}");
                TempData["Error"] = $"Error al cargar la playlist: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        //POST PlaylistsController/Edit/5 - Actualizar playlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, PlaylistDto playlistDto, IFormFile? imageFile)
        {
            try
            {
                // Verificar que el ID coincida
                if (id != playlistDto.Id)
                {
                    TempData["Error"] = "ID de playlist inválido.";
                    return RedirectToAction("Index");
                }

                // Validar modelo
                if (string.IsNullOrWhiteSpace(playlistDto.Name))
                {
                    TempData["Error"] = "El nombre de la playlist es obligatorio.";
                    return View(playlistDto);
                }

                // Crear formulario multipart para manejar imagen
                using var content = new MultipartFormDataContent();
                content.Add(new StringContent(playlistDto.Id.ToString()), "Id");
                content.Add(new StringContent(playlistDto.Name), "Name");
                content.Add(new StringContent(User.FindFirst(ClaimTypes.NameIdentifier).Value), "UserId");
                
                // AGREGAR imagen si se proporciona
                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileContent = new StreamContent(imageFile.OpenReadStream());
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageFile.ContentType);
                    content.Add(fileContent, "ImageFile", imageFile.FileName);
                }

                var response = await _httpClient.PutAsync($"https://localhost:7120/api/Playlists/{id}", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Playlist actualizada correctamente.";
                    return RedirectToAction("Details", new { id = id });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"No se pudo actualizar la playlist: {errorContent}";
                    return View(playlistDto);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in PlaylistsController.Edit POST: {ex.Message}");
                TempData["Error"] = $"Error al actualizar la playlist: {ex.Message}";
                return View(playlistDto);
            }
        }

        // POST: PlaylistsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"https://localhost:7120/api/Playlists/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Playlist eliminada exitosamente.";
                }
                else
                {
                    TempData["Error"] = "No se pudo eliminar la playlist.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar la playlist: {ex.Message}";
            }
            
            return RedirectToAction("Index");
        }

        // POST: Playlists/RemoveFromPlaylist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromPlaylist(int playlistId, int contentId)
        {
            try
            {
                // Crear endpoint para remover canción de playlist en la API
                var response = await _httpClient.DeleteAsync($"https://localhost:7120/api/Playlists/{playlistId}/remove/{contentId}");
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Canción eliminada de la playlist exitosamente.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"No se pudo eliminar la canción de la playlist: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar la canción: {ex.Message}";
            }
            
            return RedirectToAction("Details", new { id = playlistId });
        }
    }
}
