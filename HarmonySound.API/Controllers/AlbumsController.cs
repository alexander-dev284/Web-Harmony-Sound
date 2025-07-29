using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HarmonySound.API.Data;
using HarmonySound.API.DTOs;
using HarmonySound.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HarmonySound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlbumsController : ControllerBase
    {
        private readonly HarmonySoundDbContext _context;
        //Agrega el campo privado para IConfiguration
        private readonly IConfiguration _configuration;

        public AlbumsController(HarmonySoundDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: api/Albums
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AlbumDto>>> GetAlbums()
        {
            var albums = await _context.Albums
                .Include(a => a.ContentAlbums)
                    .ThenInclude(ca => ca.Content)
                .Include(a => a.Artist)
                .ToListAsync();

            var result = albums.Select(a => new AlbumDto
            {
                Id = a.Id,
                Title = a.Title,
                CreationDate = a.CreationDate,
                ArtistName = a.Artist?.Name,
                ImageUrl = a.ImageUrl,
                Contents = a.ContentAlbums?.Select(ca => new ContentDto
                {
                    Id = ca.Content.Id,
                    Title = ca.Content.Title,
                    Type = ca.Content.Type,
                    UrlMedia = ca.Content.UrlMedia,
                    Duration = ca.Content.Duration,
                    UploadDate = ca.Content.UploadDate,
                    ArtistName = ca.Content.Artist?.Name,
                    AlbumTitle = a.Title
                }).ToList() ?? new List<ContentDto>()
            }).ToList();

            return Ok(result);
        }

        // GET: api/Albums/ByArtist/5
        [HttpGet("ByArtist/{artistId}")]
        public async Task<ActionResult<IEnumerable<AlbumDto>>> GetAlbumsByArtist(int artistId)
        {
            try
            {
                Console.WriteLine($"Buscando álbumes para artista ID: {artistId}");

                var albums = await _context.Albums
                    .Where(a => a.ArtistId == artistId)
                    .Include(a => a.ContentAlbums)
                        .ThenInclude(ca => ca.Content)
                    .Include(a => a.Artist)
                    .ToListAsync();

                Console.WriteLine($"Encontrados {albums.Count} álbumes para artista {artistId}");

                //Convertir A DTOs para evitar referencias circulares
                var result = albums.Select(a => new AlbumDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    CreationDate = a.CreationDate,
                    ArtistName = a.Artist?.Name,
                    ImageUrl = a.ImageUrl,
                    Contents = a.ContentAlbums?.Select(ca => new ContentDto
                    {
                        Id = ca.Content.Id,
                        Title = ca.Content.Title,
                        Type = ca.Content.Type,
                        UrlMedia = ca.Content.UrlMedia,
                        Duration = ca.Content.Duration,
                        UploadDate = ca.Content.UploadDate,
                        ArtistName = ca.Content.Artist?.Name,
                        AlbumTitle = a.Title
                    }).ToList() ?? new List<ContentDto>()
                }).ToList();

                Console.WriteLine($"DTOs generados correctamente");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetAlbumsByArtist: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // GET: api/Albums/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AlbumDto>> GetAlbum(int id)
        {
            var album = await _context.Albums
                .Include(a => a.ContentAlbums).ThenInclude(ca => ca.Content)
                .Include(a => a.Artist)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (album == null)
                return NotFound();

            var dto = new AlbumDto
            {
                Id = album.Id,
                Title = album.Title,
                CreationDate = album.CreationDate,
                ArtistName = album.Artist?.Name,
                ImageUrl = album.ImageUrl,
                Contents = album.ContentAlbums?.Select(ca => new ContentDto
                {
                    Id = ca.Content.Id,
                    Title = ca.Content.Title,
                    Type = ca.Content.Type,
                    UrlMedia = ca.Content.UrlMedia,
                    Duration = ca.Content.Duration,
                    UploadDate = ca.Content.UploadDate,
                    ArtistName = ca.Content.Artist?.Name,
                    AlbumTitle = album.Title
                }).ToList() ?? new List<ContentDto>()
            };

            return Ok(dto);
        }

        // POST: api/Albums
        [HttpPost]
        public async Task<ActionResult<Album>> PostAlbum([FromForm] CreateAlbumDto dto)
        {
            string? imageUrl = null;

            //Subir imagen si se proporciona
            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                imageUrl = await UploadImageToAzure(dto.ImageFile, "albums");
            }

            var album = new Album
            {
                Title = dto.Title,
                ArtistId = dto.ArtistId,
                CreationDate = DateTimeOffset.UtcNow,
                ImageUrl = imageUrl 
            };

            _context.Albums.Add(album);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAlbum), new { id = album.Id }, album);
        }

        // PUT: api/Albums/5 
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAlbum(int id, [FromForm] CreateAlbumDto dto)
        {
            var album = await _context.Albums.FindAsync(id);
            if (album == null) return NotFound();

            // Guardar URL de imagen anterior
            string? oldImageUrl = album.ImageUrl;

            // Actualizar título
            album.Title = dto.Title;

            // Manejar nueva imagen si se proporciona
            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                // Subir nueva imagen
                string newImageUrl = await UploadImageToAzure(dto.ImageFile, "albums");
                album.ImageUrl = newImageUrl;

                // Eliminar imagen anterior de Azure
                if (!string.IsNullOrEmpty(oldImageUrl))
                {
                    await DeleteImageFromAzure(oldImageUrl);
                }
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Albums/5 
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlbum(int id)
        {
            var album = await _context.Albums.FindAsync(id);
            if (album == null) return NotFound();

            // Eliminar imagen de Azure antes de eliminar el álbum
            if (!string.IsNullOrEmpty(album.ImageUrl))
            {
                await DeleteImageFromAzure(album.ImageUrl);
            }

            _context.Albums.Remove(album);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/Albums/{id}/UpdateSongs
        [HttpPost("{id}/UpdateSongs")]
        public async Task<IActionResult> UpdateSongs(int id, [FromBody] List<int> songIds)
        {
            var album = await _context.Albums
                .Include(a => a.ContentAlbums)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (album == null)
                return NotFound();

            // Verificar qué canciones están llegando
            System.Diagnostics.Debug.WriteLine($"=== UpdateSongs para álbum {id} ===");
            System.Diagnostics.Debug.WriteLine($"Canciones recibidas: [{string.Join(", ", songIds ?? new List<int>())}]");

            // Elimina relaciones actuales
            _context.ContentsAlbums.RemoveRange(album.ContentAlbums);

            // Agrega nuevas relaciones
            if (songIds != null)
            {
                foreach (var songId in songIds)
                {
                    System.Diagnostics.Debug.WriteLine($"Agregando canción {songId} al álbum {id}");
                    _context.ContentsAlbums.Add(new ContentAlbum { AlbumId = id, ContentId = songId });
                }
            }

            await _context.SaveChangesAsync();
            
            // Verificar el estado final
            var finalAlbum = await _context.Albums
                .Include(a => a.ContentAlbums)
                    .ThenInclude(ca => ca.Content)
                .FirstOrDefaultAsync(a => a.Id == id);
            
            System.Diagnostics.Debug.WriteLine($"Álbum final tiene {finalAlbum?.ContentAlbums?.Count ?? 0} canciones:");
            if (finalAlbum?.ContentAlbums != null)
            {
                foreach (var ca in finalAlbum.ContentAlbums)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {ca.Content?.Title} (ID: {ca.Content?.Id}) - URL: {ca.Content?.UrlMedia}");
                }
            }
            
            return NoContent();
        }

        // POST: api/Albums/{id}/AddSong
        [HttpPost("{id}/AddSong")]
        public async Task<IActionResult> AddSong(int id, [FromBody] int songId)
        {
            var exists = await _context.ContentsAlbums.AnyAsync(ca => ca.AlbumId == id && ca.ContentId == songId);
            if (!exists)
            {
                _context.ContentsAlbums.Add(new ContentAlbum { AlbumId = id, ContentId = songId });
                await _context.SaveChangesAsync();
            }
            return NoContent();
        }

        // DELETE: api/Albums/{id}/RemoveSong/{songId}
        [HttpDelete("{id}/RemoveSong/{songId}")]
        public async Task<IActionResult> RemoveSong(int id, int songId)
        {
            var ca = await _context.ContentsAlbums.FirstOrDefaultAsync(ca => ca.AlbumId == id && ca.ContentId == songId);
            if (ca != null)
            {
                _context.ContentsAlbums.Remove(ca);
                await _context.SaveChangesAsync();
            }
            return NoContent();
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

        //Método para eliminar imagen del contenedor compartido
        private async Task<bool> DeleteImageFromAzure(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl)) return true;

                var blobConnectionString = _configuration["AzureBlobStorage:ConnectionString"];
                var blobContainerName = _configuration["AzureBlobStorage:ContentImagesContainer"];

                var blobServiceClient = new BlobServiceClient(blobConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);

                // Extraer el nombre del blob desde la URL
                var uri = new Uri(imageUrl);
                var blobName = uri.AbsolutePath.Substring(1);
                if (blobName.StartsWith(blobContainerName + "/"))
                {
                    blobName = blobName.Substring(blobContainerName.Length + 1);
                }

                var blobClient = containerClient.GetBlobClient(blobName);
                await blobClient.DeleteIfExistsAsync();

                Console.WriteLine($"Imagen eliminada de Azure: {blobName}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar imagen de Azure: {ex.Message}");
                return false;
            }
        }
    }
}
