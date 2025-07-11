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
        public async Task<ActionResult<IEnumerable<PlaylistDto>>> GetPlaylist()
        {
            var playlists = await _context.Playlist
                .Include(p => p.PlaylistContents)
                    .ThenInclude(pc => pc.Content)
                .ToListAsync();

            var result = playlists.Select(p => new PlaylistDto
            {
                Id = p.Id,
                Name = p.Name,
                Songs = p.PlaylistContents?
                    .Where(pc => pc.Content != null)
                    .Select(pc => new PlaylistSongDto
                    {
                        ContentId = pc.Content.Id,
                        Title = pc.Content.Title,
                        UrlMedia = pc.Content.UrlMedia
                    }).ToList() ?? new List<PlaylistSongDto>()
            }).ToList();

            return Ok(result);
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
        public async Task<ActionResult<IEnumerable<Playlist>>> GetUserPlaylists(int userId)
        {
            return await _context.Playlist
                .Where(p => p.UserId == userId)
                .Include(p => p.PlaylistContents)
                    .ThenInclude(pc => pc.Content)
                .ToListAsync();
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
