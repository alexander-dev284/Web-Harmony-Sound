using HarmonySound.API.DTOs;
using HarmonySound.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace HarmonySound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContentsController : ControllerBase
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
                    Duration = TimeSpan.Zero,
                    ArtistId = model.ArtistId
                };

                _context.Contents.Add(content);
                await _context.SaveChangesAsync();

                return Ok(new { content.Id, content.Title, content.UrlMedia });
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

        private bool ContentExists(int id)
        {
            return _context.Contents.Any(e => e.Id == id);
        }
    }
}
