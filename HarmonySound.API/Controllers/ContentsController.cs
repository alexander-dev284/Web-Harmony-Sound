using HarmonySound.API.DTOs;
using HarmonySound.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HarmonySound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContentsController : ControllerBase
    {
        private readonly HarmonySoundDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ContentsController> _logger;

        public ContentsController(HarmonySoundDbContext context, IWebHostEnvironment env, ILogger<ContentsController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
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
                {
                    _logger.LogWarning("Se intentó subir un archivo vacío o nulo.");
                    return BadRequest("Archivo no válido.");
                }

                var extension = Path.GetExtension(model.File.FileName).ToLower();
                if (extension != ".mp3" && extension != ".wav")
                {
                    _logger.LogWarning("Se intentó subir un archivo con una extensión no permitida: {Extension}", extension);
                    return BadRequest("Solo se permiten archivos .mp3 o .wav");
                }

                var folderPath = Path.Combine(_env.WebRootPath, "media");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(folderPath, uniqueFileName);

                // Guarda el archivo
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.File.CopyToAsync(stream);
                }

                // Genera la URL del archivo subido
                var fileUrl = $"{Request.Scheme}://{Request.Host}/media/{uniqueFileName}";

                // Crear una entrada de contenido
                var content = new Content
                {
                    Title = model.Title,
                    Type = model.Type,
                    UrlMedia = fileUrl,
                    UploadDate = DateTimeOffset.UtcNow,
                    Duration = TimeSpan.Zero, // O extraer la duración real del archivo
                    ArtistId = model.ArtistId
                };

                _context.Contents.Add(content);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Archivo subido con éxito: {FileName}", uniqueFileName);

                return Ok(new { content.Id, content.Title, content.UrlMedia });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al intentar subir el archivo.");
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
