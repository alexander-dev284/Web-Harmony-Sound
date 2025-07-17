using HarmonySound.API.DTOs;
using HarmonySound.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HarmonySound.API.Data;
using NAudio.Wave;
using NAudio.MediaFoundation;

namespace HarmonySound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]  // <- ESTO INDICA QUE ES UN API CONTROLLER
    public class ContentsController : ControllerBase  // <- HEREDA DE ControllerBase, NO de Controller
    {
        private readonly HarmonySoundDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ContentsController> _logger;
        private readonly string _blobConnectionString;
        private readonly string _blobContainerName;

        public ContentsController(HarmonySoundDbContext context, IWebHostEnvironment env, ILogger<ContentsController> logger, IConfiguration configuration)
        {
            _context = context;
            _env = env;
            _logger = logger;
            _blobConnectionString = configuration["AzureBlobStorage:ConnectionString"];
            _blobContainerName = configuration["AzureBlobStorage:ContainerName"];
            
            // Inicializar MediaFoundation para soporte de archivos de audio avanzados
            MediaFoundationApi.Startup();
        }

        // GET: api/Contents
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Content>>> GetContent()
        {
            return await _context.Contents.ToListAsync();
        }

        // GET: api/Contents/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Content>> GetContent(int id)
        {
            var content = await _context.Contents.FindAsync(id);
            if (content == null) return NotFound();
            return content;
        }

        // PUT: api/Contents/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutContent(int id, Content content)
        {
            if (id != content.Id) return BadRequest();
            _context.Entry(content).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ContentExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // POST: api/Contents
        [HttpPost]
        public async Task<ActionResult<Content>> PostContent(Content content)
        {
            _context.Contents.Add(content);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetContent", new { id = content.Id }, content);
        }

        // Método para obtener la duración del audio
        private async Task<TimeSpan> GetAudioDurationAsync(IFormFile file)
        {
            try
            {
                var extension = Path.GetExtension(file.FileName).ToLower();
                
                switch (extension)
                {
                    case ".mp3":
                        using (var stream = new MemoryStream())
                        {
                            await file.CopyToAsync(stream);
                            stream.Position = 0;
                            using (var mp3Reader = new Mp3FileReader(stream))
                            {
                                return mp3Reader.TotalTime;
                            }
                        }
                    
                    case ".wav":
                        using (var stream = new MemoryStream())
                        {
                            await file.CopyToAsync(stream);
                            stream.Position = 0;
                            using (var waveReader = new WaveFileReader(stream))
                            {
                                return waveReader.TotalTime;
                            }
                        }
                    
                    case ".aac":
                    case ".m4a":
                        try
                        {
                            // MediaFoundation requiere archivo físico
                            var tempFile = Path.GetTempFileName();
                            try
                            {
                                // Guardar archivo temporal
                                using (var fileStream = new FileStream(tempFile, FileMode.Create))
                                {
                                    await file.CopyToAsync(fileStream);
                                }
                                
                                // Leer duración desde archivo temporal
                                using (var mediaReader = new MediaFoundationReader(tempFile))
                                {
                                    return mediaReader.TotalTime;
                                }
                            }
                            finally
                            {
                                // Limpiar archivo temporal - CORRECCIÓN AQUÍ
                                if (System.IO.File.Exists(tempFile))
                                {
                                    System.IO.File.Delete(tempFile);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Error procesando archivo AAC/M4A {file.FileName}: {ex.Message}");
                            return TimeSpan.Zero;
                        }
                    
                    case ".ogg":
                    case ".flac":
                        _logger.LogInformation($"Formato {extension} no soportado para cálculo de duración");
                        return TimeSpan.Zero;
                    
                    default:
                        _logger.LogWarning($"Extensión no reconocida: {extension}");
                        return TimeSpan.Zero;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error obteniendo duración del archivo {file.FileName}");
                return TimeSpan.Zero;
            }
        }

        // POST: api/Contents/upload
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAudio([FromForm] ContentUploadDto model)
        {
            try
            {
                if (model.File == null || model.File.Length == 0)
                    return BadRequest("Archivo no válido.");

                if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Type))
                    return BadRequest("El título y el tipo son obligatorios.");

                const long maxFileSize = 200 * 1024 * 1024; // 200 MB
                if (model.File.Length > maxFileSize)
                    return BadRequest("El archivo es demasiado grande.");

                var extension = Path.GetExtension(model.File.FileName).ToLower();
                var allowedExtensions = new[] { ".mp3", ".wav", ".ogg", ".flac", ".aac", ".m4a" };
                if (!allowedExtensions.Contains(extension))
                    return BadRequest("Solo se permiten archivos de audio: .mp3, .wav, .ogg, .flac, .aac, .m4a");

                // **NUEVO: Calcular duración del archivo**
                _logger.LogInformation($"Calculando duración para: {model.File.FileName}");
                var audioDuration = await GetAudioDurationAsync(model.File);
                _logger.LogInformation($"Duración calculada: {audioDuration}");

                // Forzar el tipo MIME correcto para .wav
                string contentType = model.File.ContentType;
                if (extension == ".wav")
                    contentType = "audio/wav";

                // Subir a Azure Blob Storage
                var blobServiceClient = new BlobServiceClient(_blobConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(_blobContainerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var blobClient = containerClient.GetBlobClient(uniqueFileName);

                using (var stream = model.File.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });
                }

                var fileUrl = blobClient.Uri.ToString();

                var content = new Content
                {
                    Title = model.Title,
                    Type = model.Type,
                    UrlMedia = fileUrl,
                    UploadDate = DateTimeOffset.UtcNow,
                    Duration = audioDuration, // **CORRECCIÓN: Usar duración calculada**
                    ArtistId = model.ArtistId
                };

                _context.Contents.Add(content);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Archivo subido exitosamente. Duración: {audioDuration}");

                return Ok(new { 
                    content.Id, 
                    content.Title, 
                    content.UrlMedia,
                    Duration = audioDuration.ToString(@"mm\:ss")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al intentar subir el archivo a Azure Blob Storage.");
                return StatusCode(500, "Error interno del servidor.");
            }
        }

        // DELETE: api/Contents/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContent(int id)
        {
            var content = await _context.Contents.FindAsync(id);
            if (content == null) return NotFound();

            _context.Contents.Remove(content);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Contents/search?query=...
        [HttpGet("search")]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Ok(new List<Content>());

            var results = await _context.Contents
                .Where(c =>
                    (!string.IsNullOrEmpty(c.Title) && c.Title.ToLower().Contains(query.ToLower())) ||
                    (!string.IsNullOrEmpty(c.Type) && c.Type.ToLower().Contains(query.ToLower()))
                )
                .ToListAsync();

            return Ok(results);
        }

        // POST: api/Contents/5/like
        [HttpPost("{id}/like")]
        public async Task<IActionResult> LikeContent(int id, [FromBody] int userId)
        {
            var content = await _context.Contents.FindAsync(id);
            if (content == null)
                return NotFound();

            // Verificar si ya dio like
            var existingLike = await _context.UserLikes
                .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.ContentId == id);

            if (existingLike != null)
                return BadRequest("Ya diste like a este contenido.");

            // Agregar like
            var userLike = new UserLike
            {
                UserId = userId,
                ContentId = id,
                LikeDate = DateTimeOffset.UtcNow
            };

            _context.UserLikes.Add(userLike);

            // Actualizar contador de likes en Statistics
            var statistic = await _context.Statistics
                .FirstOrDefaultAsync(s => s.ContentId == id);

            if (statistic != null)
            {
                statistic.Likes++;
                _context.Entry(statistic).State = EntityState.Modified;
            }
            else
            {
                // Crear nueva estadística si no existe
                var newStatistic = new Statistic
                {
                    ContentId = id,
                    Likes = 1,
                    Reproductions = 0,
                    Comments = 0,
                    ReportDate = DateTimeOffset.UtcNow
                };
                _context.Statistics.Add(newStatistic);
            }

            // Agregar a playlist de favoritos
            await AddToFavoritesPlaylist(userId, id);

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Like agregado y contenido añadido a favoritos." });
        }

        // POST: api/Contents/5/unlike
        [HttpPost("{id}/unlike")]
        public async Task<IActionResult> UnlikeContent(int id, [FromBody] int userId)
        {
            var userLike = await _context.UserLikes
                .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.ContentId == id);

            if (userLike == null)
                return BadRequest("No habías dado like a este contenido.");

            _context.UserLikes.Remove(userLike);

            // Actualizar contador de likes en Statistics
            var statistic = await _context.Statistics
                .FirstOrDefaultAsync(s => s.ContentId == id);

            if (statistic != null && statistic.Likes > 0)
            {
                statistic.Likes--;
                _context.Entry(statistic).State = EntityState.Modified;
            }

            // Remover de playlist de favoritos
            await RemoveFromFavoritesPlaylist(userId, id);

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Like removido y contenido eliminado de favoritos." });
        }

        // GET: api/Contents/5/likes
        [HttpGet("{id}/likes")]
        public async Task<IActionResult> GetContentLikes(int id)
        {
            var likesCount = await _context.UserLikes.CountAsync(ul => ul.ContentId == id);
            return Ok(new { ContentId = id, Likes = likesCount });
        }

        // GET: api/Contents/5/user-liked/1
        [HttpGet("{id}/user-liked/{userId}")]
        public async Task<IActionResult> HasUserLiked(int id, int userId)
        {
            var hasLiked = await _context.UserLikes
                .AnyAsync(ul => ul.UserId == userId && ul.ContentId == id);
            return Ok(new { HasLiked = hasLiked });
        }

        // GET: api/Contents/user-likes/1
        [HttpGet("user-likes/{userId}")]
        public async Task<IActionResult> GetUserLikes(int userId)
        {
            try
            {
                var likedContentIds = await _context.UserLikes
                    .Where(ul => ul.UserId == userId)
                    .Select(ul => ul.ContentId)
                    .ToListAsync();

                return Ok(likedContentIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener likes del usuario {userId}");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // ✅ AGREGAR: Método privado para agregar a playlist de favoritos
        private async Task AddToFavoritesPlaylist(int userId, int contentId)
        {
            // Buscar o crear playlist de favoritos
            var favoritesPlaylist = await _context.Playlist
                .FirstOrDefaultAsync(p => p.UserId == userId && p.Name == "Favoritos");

            if (favoritesPlaylist == null)
            {
                favoritesPlaylist = new Playlist
                {
                    Name = "Favoritos",
                    UserId = userId
                };
                _context.Playlist.Add(favoritesPlaylist);
                await _context.SaveChangesAsync();
            }

            // Verificar si ya está en la playlist
            var exists = await _context.PlaylistContents
                .AnyAsync(pc => pc.PlaylistId == favoritesPlaylist.Id && pc.ContentId == contentId);

            if (!exists)
            {
                var playlistContent = new PlaylistContent
                {
                    PlaylistId = favoritesPlaylist.Id,
                    ContentId = contentId
                };
                _context.PlaylistContents.Add(playlistContent);
            }
        }

        // ✅ AGREGAR: Método privado para remover de playlist de favoritos
        private async Task RemoveFromFavoritesPlaylist(int userId, int contentId)
        {
            var favoritesPlaylist = await _context.Playlist
                .FirstOrDefaultAsync(p => p.UserId == userId && p.Name == "Favoritos");

            if (favoritesPlaylist != null)
            {
                var playlistContent = await _context.PlaylistContents
                    .FirstOrDefaultAsync(pc => pc.PlaylistId == favoritesPlaylist.Id && pc.ContentId == contentId);

                if (playlistContent != null)
                {
                    _context.PlaylistContents.Remove(playlistContent);
                }
            }
        }

        private bool ContentExists(int id)
        {
            return _context.Contents.Any(e => e.Id == id);
        }
    }
}
