using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HarmonySound.Models;
using HarmonySound.API.Data;
namespace HarmonySound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlaylistContentsController : ControllerBase
    {
        private readonly HarmonySoundDbContext _context;

        public PlaylistContentsController(HarmonySoundDbContext context)
        {
            _context = context;
        }

        // GET: api/PlaylistContents
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PlaylistContent>>> GetPlaylistContent()
        {
            return await _context.PlaylistContents.ToListAsync();
        }

        // GET: api/PlaylistContents/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PlaylistContent>> GetPlaylistContent(int id)
        {
            var playlistContent = await _context.PlaylistContents.FindAsync(id);

            if (playlistContent == null)
            {
                return NotFound();
            }

            return playlistContent;
        }

        // PUT: api/PlaylistContents/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPlaylistContent(int id, PlaylistContent playlistContent)
        {
            if (id != playlistContent.PlaylistId)
            {
                return BadRequest();
            }

            _context.Entry(playlistContent).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PlaylistContentExists(id))
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

        // POST: api/PlaylistContents
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<PlaylistContent>> PostPlaylistContent(PlaylistContent playlistContent)
        {
            _context.PlaylistContents.Add(playlistContent);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (PlaylistContentExists(playlistContent.PlaylistId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetPlaylistContent", new { id = playlistContent.PlaylistId }, playlistContent);
        }

        // DELETE: api/PlaylistContents/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlaylistContent(int id)
        {
            var playlistContent = await _context.PlaylistContents.FindAsync(id);
            if (playlistContent == null)
            {
                return NotFound();
            }

            _context.PlaylistContents.Remove(playlistContent);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PlaylistContentExists(int id)
        {
            return _context.PlaylistContents.Any(e => e.PlaylistId == id);
        }
    }
}
