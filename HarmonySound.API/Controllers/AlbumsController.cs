using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HarmonySound.Models;
using HarmonySound.API.Data;
using HarmonySound.API.DTOs;

namespace HarmonySound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlbumsController : ControllerBase
    {
        private readonly HarmonySoundDbContext _context;

        public AlbumsController(HarmonySoundDbContext context)
        {
            _context = context;
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
                Contents = a.ContentAlbums?.Select(ca => new ContentDto
                {
                    Id = ca.Content.Id,
                    Title = ca.Content.Title,
                    Type = ca.Content.Type,
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
        public async Task<ActionResult<IEnumerable<Album>>> GetAlbumsByArtist(int artistId)
        {
            return await _context.Albums
                .Where(a => a.ArtistId == artistId)
                .Include(a => a.ContentAlbums).ThenInclude(ca => ca.Content)
                .ToListAsync();
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
                Contents = album.ContentAlbums?.Select(ca => new ContentDto
                {
                    Id = ca.Content.Id,
                    Title = ca.Content.Title,
                    Type = ca.Content.Type,
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
        public async Task<ActionResult<Album>> PostAlbum(CreateAlbumDto dto)
        {
            var album = new Album
            {
                Title = dto.Title,
                ArtistId = dto.ArtistId,
                CreationDate = DateTimeOffset.UtcNow
            };
            _context.Albums.Add(album);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAlbum), new { id = album.Id }, album);
        }

        // PUT: api/Albums/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAlbum(int id, CreateAlbumDto dto)
        {
            var album = await _context.Albums.FindAsync(id);
            if (album == null) return NotFound();

            album.Title = dto.Title;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Albums/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlbum(int id)
        {
            var album = await _context.Albums.FindAsync(id);
            if (album == null) return NotFound();

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

            // Elimina relaciones actuales
            _context.ContentsAlbums.RemoveRange(album.ContentAlbums);

            // Agrega nuevas relaciones
            if (songIds != null)
            {
                foreach (var songId in songIds)
                {
                    _context.ContentsAlbums.Add(new ContentAlbum { AlbumId = id, ContentId = songId });
                }
            }

            await _context.SaveChangesAsync();
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

        
    }
}
