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

        // Obtener playlists de un usuario
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserPlaylists(int userId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetUserPlaylists called with userId: {userId}");
                
                // Simplificar la consulta para evitar problemas
                var playlists = await _context.Playlist
                    .Where(p => p.UserId == userId)
                    .ToListAsync();
                
                System.Diagnostics.Debug.WriteLine($"Found {playlists.Count} playlists for user {userId}");
                
                // Mapear a objetos simples
                var result = playlists.Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    userId = p.UserId
                }).ToList();
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetUserPlaylists: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                return StatusCode(500, new { error = ex.Message, innerError = ex.InnerException?.Message });
            }
        }


        // Agregar canción a playlist
        [HttpPost("{playlistId}/add")]
        public async Task<IActionResult> AddContentToPlaylist(int playlistId, [FromBody] int contentId)
        {
            var exists = await _context.PlaylistContents.AnyAsync(pc => pc.PlaylistId == playlistId && pc.ContentId == contentId);
            if (exists)
                return BadRequest("La canción ya está en la playlist.");

            var pc = new PlaylistContent { PlaylistId = playlistId, ContentId = contentId };
            _context.PlaylistContents.Add(pc);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PlaylistCreateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name) || dto.UserId == 0)
                return BadRequest("Datos de playlist inválidos.");

            var playlist = new Playlist
            {
                Name = dto.Name,
                UserId = dto.UserId
            };

            _context.Playlist.Add(playlist);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserPlaylists), new { userId = playlist.UserId }, playlist);
        }
    }
}
