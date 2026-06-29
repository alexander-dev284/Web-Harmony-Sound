using HarmonySound.API.Data;
using HarmonySound.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HarmonySound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FollowsController : ControllerBase
    {
        private readonly HarmonySoundDbContext _context;
        private readonly ILogger<FollowsController> _logger;

        public FollowsController(HarmonySoundDbContext context, ILogger<FollowsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // POST: api/Follows  -> alterna seguir/dejar de seguir (toggle)
        [HttpPost]
        public async Task<IActionResult> ToggleFollow([FromForm] int followerId, [FromForm] int artistId)
        {
            try
            {
                if (followerId == artistId)
                    return BadRequest(new { success = false, message = "No puedes seguirte a ti mismo." });

                var existing = await _context.UserFollows
                    .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.ArtistId == artistId);

                bool isFollowing;
                if (existing != null)
                {
                    _context.UserFollows.Remove(existing);
                    isFollowing = false;
                }
                else
                {
                    _context.UserFollows.Add(new UserFollow
                    {
                        FollowerId = followerId,
                        ArtistId = artistId,
                        FollowDate = DateTimeOffset.UtcNow
                    });
                    isFollowing = true;
                }

                await _context.SaveChangesAsync();

                var followersCount = await _context.UserFollows.CountAsync(f => f.ArtistId == artistId);

                return Ok(new
                {
                    success = true,
                    isFollowing,
                    followersCount,
                    action = isFollowing ? "followed" : "unfollowed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al alternar seguimiento");
                return StatusCode(500, new { success = false, message = "Error interno del servidor" });
            }
        }

        // GET: api/Follows/is-following?followerId=1&artistId=2
        [HttpGet("is-following")]
        public async Task<IActionResult> IsFollowing(int followerId, int artistId)
        {
            var isFollowing = await _context.UserFollows
                .AnyAsync(f => f.FollowerId == followerId && f.ArtistId == artistId);
            return Ok(new { isFollowing });
        }

        // GET: api/Follows/followers/count/{artistId}  -> número de seguidores de un artista
        [HttpGet("followers/count/{artistId}")]
        public async Task<IActionResult> GetFollowersCount(int artistId)
        {
            var count = await _context.UserFollows.CountAsync(f => f.ArtistId == artistId);
            return Ok(new { artistId, followersCount = count });
        }

        // GET: api/Follows/following/{followerId}  -> ids de los artistas que sigue un usuario
        [HttpGet("following/{followerId}")]
        public async Task<IActionResult> GetFollowing(int followerId)
        {
            var artistIds = await _context.UserFollows
                .Where(f => f.FollowerId == followerId)
                .OrderByDescending(f => f.FollowDate)
                .Select(f => f.ArtistId)
                .ToListAsync();
            return Ok(artistIds);
        }

        // GET: api/Follows/top-artists?count=5  -> ranking de artistas más seguidos
        [HttpGet("top-artists")]
        public async Task<IActionResult> GetTopArtists(int count = 5)
        {
            var top = await _context.UserFollows
                .GroupBy(f => f.ArtistId)
                .Select(g => new { ArtistId = g.Key, FollowersCount = g.Count() })
                .OrderByDescending(x => x.FollowersCount)
                .Take(count)
                .ToListAsync();

            var artistIds = top.Select(t => t.ArtistId).ToList();
            var artists = await _context.Users
                .Where(u => artistIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Name, u.ProfileImageUrl })
                .ToListAsync();

            // Conserva el orden del ranking (por número de seguidores)
            var result = top.Select(t =>
            {
                var a = artists.FirstOrDefault(x => x.Id == t.ArtistId);
                return new
                {
                    artistId = t.ArtistId,
                    name = a?.Name ?? "(desconocido)",
                    profileImageUrl = a?.ProfileImageUrl,
                    followersCount = t.FollowersCount
                };
            }).ToList();

            return Ok(result);
        }

        // GET: api/Follows/followers/{artistId}  -> lista de seguidores (datos básicos)
        [HttpGet("followers/{artistId}")]
        public async Task<IActionResult> GetFollowers(int artistId)
        {
            var followers = await _context.UserFollows
                .Where(f => f.ArtistId == artistId)
                .OrderByDescending(f => f.FollowDate)
                .Select(f => new
                {
                    f.FollowerId,
                    f.Follower.Name,
                    f.Follower.ProfileImageUrl,
                    f.FollowDate
                })
                .ToListAsync();
            return Ok(followers);
        }
    }
}
