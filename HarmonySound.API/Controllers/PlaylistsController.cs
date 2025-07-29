using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HarmonySound.Models;
using HarmonySound.API.DTOs;
using HarmonySound.API.Data;
namespace HarmonySound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlaylistsController : ControllerBase
    {
        private readonly HarmonySoundDbContext _context;
        // Agrega el campo privado para IConfiguration
        private readonly IConfiguration _configuration;

        public PlaylistsController(HarmonySoundDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: api/Playlists
        [HttpGet]
        public async Task<IActionResult> GetPlaylist()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("GetPlaylist called in API");
                
                var playlists = await _context.Playlist.ToListAsync();
                
                System.Diagnostics.Debug.WriteLine($"Found {playlists.Count} playlists in database");
                
                var result = playlists.Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    userId = p.UserId
                }).ToList();
                
                System.Diagnostics.Debug.WriteLine($"Returning {result.Count} playlists with userId");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in API GetPlaylist: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/Playlists/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetPlaylist(int id)
        {
            var playlist = await _context.Playlist
                .Include(p => p.PlaylistContents)
                .ThenInclude(pc => pc.Content)
                .ThenInclude(c => c.Artist)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (playlist == null)
                return NotFound();

            var result = new {
                id = playlist.Id,
                name = playlist.Name,
                userId = playlist.UserId,
                imageUrl = playlist.ImageUrl, 
                songs = playlist.PlaylistContents.Select(pc => new PlaylistSongDto
                {
                    ContentId = pc.ContentId,
                    Title = pc.Content?.Title ?? "Sin título",
                    UrlMedia = pc.Content?.UrlMedia ?? "",
                    ArtistName = pc.Content?.Artist?.Name ?? "Artista desconocido",
                    Duration = pc.Content?.Duration ?? TimeSpan.Zero
                }).ToList()
            };

            return Ok(result);
        }

        // PUT: api/Playlists/5 
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPlaylist(int id, [FromForm] PlaylistCreateDto dto)
        {
            var playlist = await _context.Playlist.FindAsync(id);
            if (playlist == null) return NotFound();

            // Validar que el usuario sea el propietario
            if (playlist.UserId != dto.UserId)
                return BadRequest("No tienes permisos para editar esta playlist.");

            // Guardar URL de imagen anterior
            string? oldImageUrl = playlist.ImageUrl;

            // Actualizar nombre
            playlist.Name = dto.Name;

            // Manejar nueva imagen si se proporciona
            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                // Subir nueva imagen
                string newImageUrl = await UploadImageToAzure(dto.ImageFile, "playlists");
                playlist.ImageUrl = newImageUrl;

                // Eliminar imagen anterior de Azure
                if (!string.IsNullOrEmpty(oldImageUrl))
                {
                    await DeleteImageFromAzure(oldImageUrl);
                }
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }


        // DELETE: api/Playlists/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlaylist(int id)
        {
            var playlist = await _context.Playlist.FindAsync(id);
            if (playlist == null) return NotFound();

            // Eliminar imagen de Azure antes de eliminar la playlist
            if (!string.IsNullOrEmpty(playlist.ImageUrl))
            {
                await DeleteImageFromAzure(playlist.ImageUrl);
            }

            _context.Playlist.Remove(playlist);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private bool PlaylistExists(int id)
        {
            return _context.Playlist.Any(e => e.Id == id);
        }

        // Método mejorado para obtener playlists con información del artista
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserPlaylists(int userId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== GetUserPlaylists called with userId: {userId} ===");
                
                // Verificar si el usuario existe
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    System.Diagnostics.Debug.WriteLine($"User {userId} not found");
                    return NotFound($"User with id {userId} not found");
                }

                // Obtener playlists básicas primero
                var playlists = await _context.Playlist
                    .Where(p => p.UserId == userId)
                    .ToListAsync();
                
                System.Diagnostics.Debug.WriteLine($"Found {playlists.Count} playlists for user {userId}");

                // Obtener contenidos de cada playlist por separado
                var result = new List<object>();
                
                foreach (var playlist in playlists)
                {
                    System.Diagnostics.Debug.WriteLine($"Processing playlist: {playlist.Name} (ID: {playlist.Id})");
                    
                    // Obtener contenidos de esta playlist con información del artista
                    var playlistContents = await _context.PlaylistContents
                        .Include(pc => pc.Content)
                        .ThenInclude(c => c.Artist)
                        .Where(pc => pc.PlaylistId == playlist.Id)
                        // Se elimina el OrderBy para evitar carga innecesaria
                        .ToListAsync();
                    
                    System.Diagnostics.Debug.WriteLine($"Found {playlistContents.Count} contents for playlist {playlist.Id}");
                    
                    var songs = playlistContents.Select(pc => new PlaylistSongDto
                    {
                        ContentId = pc.ContentId,
                        Title = pc.Content?.Title ?? "Sin título",
                        UrlMedia = pc.Content?.UrlMedia ?? "",
                        ArtistName = pc.Content?.Artist?.Name ?? "Artista desconocido",
                        Duration = pc.Content?.Duration ?? TimeSpan.Zero 
                    }).ToList();
                    
                    result.Add(new {
                        id = playlist.Id,
                        name = playlist.Name,
                        userId = playlist.UserId,
                        imageUrl = playlist.ImageUrl, 
                        songs = songs
                    });
                }
                
                System.Diagnostics.Debug.WriteLine($"Returning {result.Count} playlists with content");
                return Ok(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetUserPlaylists: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                // Devolver solo playlists básicas
                try
                {
                    var simplePlaylists = await _context.Playlist
                        .Where(p => p.UserId == userId)
                        .ToListAsync();
                    
                    var simpleResult = simplePlaylists.Select(p => new {
                        id = p.Id,
                        name = p.Name,
                        userId = p.UserId,
                        songs = new List<PlaylistSongDto>()
                    }).ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"Fallback: returning {simpleResult.Count} playlists without content");
                    return Ok(simpleResult);
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Fallback error: {fallbackEx.Message}");
                    return StatusCode(500, new { 
                        error = "Error al cargar playlists", 
                        details = ex.Message,
                        innerError = ex.InnerException?.Message 
                    });
                }
            }
        }

        [HttpPost("{playlistId}/add")]
        public async Task<IActionResult> AddContentToPlaylist(int playlistId, [FromBody] int contentId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== AddContentToPlaylist: playlist {playlistId}, content {contentId} ===");
                
                // Verificar que la playlist existe
                var playlistExists = await _context.Playlist.AnyAsync(p => p.Id == playlistId);
                if (!playlistExists)
                {
                    System.Diagnostics.Debug.WriteLine($"Playlist {playlistId} not found");
                    return NotFound($"Playlist with id {playlistId} not found");
                }
                
                // Verificar que el contenido existe
                var contentExists = await _context.Contents.AnyAsync(c => c.Id == contentId);
                if (!contentExists)
                {
                    System.Diagnostics.Debug.WriteLine($"Content {contentId} not found");
                    return NotFound($"Content with id {contentId} not found");
                }
                
                // Verificar si ya existe la relación
                var exists = await _context.PlaylistContents.AnyAsync(pc => pc.PlaylistId == playlistId && pc.ContentId == contentId);
                if (exists)
                {
                    System.Diagnostics.Debug.WriteLine($"Content {contentId} already exists in playlist {playlistId}");
                    return BadRequest("La canción ya está en la playlist.");
                }

                // Crear la relación
                var playlistContent = new PlaylistContent 
                { 
                    PlaylistId = playlistId, 
                    ContentId = contentId 
                };
                
                _context.PlaylistContents.Add(playlistContent);
                await _context.SaveChangesAsync();
                
                System.Diagnostics.Debug.WriteLine($"Successfully added content {contentId} to playlist {playlistId}");
                return Ok(new { message = "Contenido agregado exitosamente a la playlist" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in AddContentToPlaylist: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { 
                    error = "Error al agregar contenido a la playlist", 
                    details = ex.Message 
                });
            }
        }

        // Método para crear playlist 
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] PlaylistCreateDto dto)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== Create Playlist: {dto?.Name} for user {dto?.UserId} ===");
                
                if (dto == null || string.IsNullOrWhiteSpace(dto.Name) || dto.UserId == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Invalid playlist data");
                    return BadRequest("Datos de playlist inválidos.");
                }

                // Verificar que el usuario existe
                var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
                if (!userExists)
                {
                    System.Diagnostics.Debug.WriteLine($"User {dto.UserId} not found");
                    return BadRequest($"Usuario con id {dto.UserId} no encontrado.");
                }

                string? imageUrl = null;

                // Subir imagen si se proporciona
                if (dto.ImageFile != null && dto.ImageFile.Length > 0)
                {
                    imageUrl = await UploadImageToAzure(dto.ImageFile, "playlists");
                }

                var playlist = new Playlist
                {
                    Name = dto.Name,
                    UserId = dto.UserId,
                    ImageUrl = imageUrl 
                };

                _context.Playlist.Add(playlist);
                await _context.SaveChangesAsync();
                
                System.Diagnostics.Debug.WriteLine($"Created playlist with ID: {playlist.Id}");

                return Ok(new { 
                    id = playlist.Id, 
                    name = playlist.Name, 
                    userId = playlist.UserId,
                    imageUrl = playlist.ImageUrl
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Create: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { 
                    error = "Error al crear la playlist", 
                    details = ex.Message 
                });
            }
        }

        // DELETE: api/Playlists/5/remove/3
        [HttpDelete("{playlistId}/remove/{contentId}")]
        public async Task<IActionResult> RemoveContentFromPlaylist(int playlistId, int contentId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== RemoveContentFromPlaylist: playlist {playlistId}, content {contentId} ===");
                
                // Buscar la relación
                var playlistContent = await _context.PlaylistContents
                    .FirstOrDefaultAsync(pc => pc.PlaylistId == playlistId && pc.ContentId == contentId);
                
                if (playlistContent == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Content {contentId} not found in playlist {playlistId}");
                    return NotFound($"Content with id {contentId} not found in playlist {playlistId}");
                }
                
                // Eliminar la relación
                _context.PlaylistContents.Remove(playlistContent);
                await _context.SaveChangesAsync();
                
                System.Diagnostics.Debug.WriteLine($"Successfully removed content {contentId} from playlist {playlistId}");
                return Ok(new { message = "Contenido eliminado exitosamente de la playlist" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RemoveContentFromPlaylist: {ex.Message}");
                return StatusCode(500, new { 
                    error = "Error al eliminar contenido de la playlist", 
                    details = ex.Message 
                });
            }
        }

        // Método para subir imágenes al contenedor compartido
        private async Task<string> UploadImageToAzure(IFormFile imageFile, string containerFolder)
        {
            try
            {
                const long maxFileSize = 5 * 1024 * 1024; // 5 MB
                if (imageFile.Length > maxFileSize)
                    throw new Exception("La imagen es demasiado grande. Máximo 5 MB.");

                var extension = Path.GetExtension(imageFile.FileName).ToLower();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                if (!allowedExtensions.Contains(extension))
                    throw new Exception("Solo se permiten imágenes: .jpg, .jpeg, .png, .gif, .webp");

                // USAR CONTENEDOR COMPARTIDO PARA IMÁGENES DE CONTENIDO
                var blobConnectionString = _configuration["AzureBlobStorage:ConnectionString"];
                var blobContainerName = _configuration["AzureBlobStorage:ContentImagesContainer"];

                var blobServiceClient = new BlobServiceClient(blobConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                // MANTENER CARPETAS PARA ORGANIZACIÓN: playlists/ y albums/
                var uniqueFileName = $"{containerFolder}/{Guid.NewGuid()}{extension}";
                var blobClient = containerClient.GetBlobClient(uniqueFileName);

                // Configurar tipo MIME correcto
                string contentType = imageFile.ContentType;
                if (extension == ".jpg" || extension == ".jpeg") contentType = "image/jpeg";
                else if (extension == ".png") contentType = "image/png";
                else if (extension == ".gif") contentType = "image/gif";
                else if (extension == ".webp") contentType = "image/webp";

                using (var stream = imageFile.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });
                }

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al subir imagen: {ex.Message}");
            }
        }

        // Método para eliminar imágenes del contenedor compartido
        private async Task DeleteImageFromAzure(string imageUrl)
        {
            try
            {
                // Extraer nombre del archivo de la URL
                var fileName = Path.GetFileName(imageUrl);

                // USAR CONTENEDOR COMPARTIDO PARA IMÁGENES DE CONTENIDO
                var blobConnectionString = _configuration["AzureBlobStorage:ConnectionString"];
                var blobContainerName = _configuration["AzureBlobStorage:ContentImagesContainer"];

                var blobServiceClient = new BlobServiceClient(blobConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);

                var blobClient = containerClient.GetBlobClient($"playlists/{fileName}");

                // Eliminar blob
                await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al eliminar imagen de Azure: {ex.Message}");
            }
        }
    }
}
