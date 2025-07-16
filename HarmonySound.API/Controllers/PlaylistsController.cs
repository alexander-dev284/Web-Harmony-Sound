using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

        public PlaylistsController(HarmonySoundDbContext context)
        {
            _context = context;
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
        public async Task<ActionResult<Playlist>> GetPlaylist(int id)
        {
            var playlist = await _context.Playlist.FindAsync(id);

            if (playlist == null)
            {
                return NotFound();
            }

            return playlist;
        }

        // PUT: api/Playlists/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPlaylist(int id, Playlist playlist)
        {
            if (id != playlist.Id)
            {
                return BadRequest();
            }

            _context.Entry(playlist).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PlaylistExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }


        // DELETE: api/Playlists/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlaylist(int id)
        {
            var playlist = await _context.Playlist.FindAsync(id);
            if (playlist == null)
            {
                return NotFound();
            }

            _context.Playlist.Remove(playlist);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PlaylistExists(int id)
        {
            return _context.Playlist.Any(e => e.Id == id);
        }

        // Obtener playlists de un usuario con debugging mejorado
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
                    
                    // Obtener contenidos de esta playlist
                    var playlistContents = await _context.PlaylistContents
                        .Include(pc => pc.Content)
                        .Where(pc => pc.PlaylistId == playlist.Id)
                        .ToListAsync();
                    
                    System.Diagnostics.Debug.WriteLine($"Found {playlistContents.Count} contents for playlist {playlist.Id}");
                    
                    var songs = playlistContents.Select(pc => new PlaylistSongDto
                    {
                        ContentId = pc.ContentId,
                        Title = pc.Content?.Title ?? "Sin título",
                        UrlMedia = pc.Content?.UrlMedia ?? ""
                    }).ToList();
                    
                    result.Add(new {
                        id = playlist.Id,
                        name = playlist.Name,
                        userId = playlist.UserId,
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
                
                // Fallback: devolver solo playlists básicas
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


        // Método mejorado para agregar contenido a playlist
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

        // Método para crear playlist con debugging
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PlaylistCreateDto dto)
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

                var playlist = new Playlist
                {
                    Name = dto.Name,
                    UserId = dto.UserId
                };

                _context.Playlist.Add(playlist);
                await _context.SaveChangesAsync();
                
                System.Diagnostics.Debug.WriteLine($"Created playlist with ID: {playlist.Id}");

                return Ok(new { 
                    id = playlist.Id, 
                    name = playlist.Name, 
                    userId = playlist.UserId 
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
    }
}
