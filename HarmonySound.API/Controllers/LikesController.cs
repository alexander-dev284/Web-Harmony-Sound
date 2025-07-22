using HarmonySound.API.Data;
using HarmonySound.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HarmonySound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LikesController : ControllerBase
    {
        private readonly HarmonySoundDbContext _context;
        private readonly ILogger<LikesController> _logger;

        public LikesController(HarmonySoundDbContext context, ILogger<LikesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Likes/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserLikes(int userId)
        {
            try
            {
                // Obtener likes directos de UserLikes
                var directLikes = await _context.UserLikes
                    .Where(ul => ul.UserId == userId)
                    .Select(ul => ul.ContentId)
                    .ToListAsync();

                // ✅ NUEVO: También obtener canciones de playlist "Favoritos"
                var favoritesPlaylist = await _context.Playlist
                    .Include(p => p.PlaylistContents)
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.Name == "Favoritos");

                var favoritesLikes = new List<int>();
                if (favoritesPlaylist != null)
                {
                    favoritesLikes = favoritesPlaylist.PlaylistContents
                        .Select(pc => pc.ContentId)
                        .ToList();
                }

                // Combinar y eliminar duplicados
                var allLikedContentIds = directLikes
                    .Union(favoritesLikes)
                    .Distinct()
                    .ToList();

                return Ok(allLikedContentIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener likes del usuario {userId}");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // POST: api/Likes
        [HttpPost]
        public async Task<IActionResult> LikeContent([FromForm] int contentId, [FromForm] int userId)
        {
            try
            {
                // Verificar si ya existe el like
                var existingLike = await _context.UserLikes
                    .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.ContentId == contentId);

                // ✅ NUEVO: También verificar si está en playlist Favoritos
                var favoritesPlaylist = await _context.Playlist
                    .Include(p => p.PlaylistContents)
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.Name == "Favoritos");

                var isInFavorites = favoritesPlaylist?.PlaylistContents
                    ?.Any(pc => pc.ContentId == contentId) ?? false;

                if (existingLike != null || isInFavorites)
                {
                    return BadRequest("Ya has dado like a este contenido");
                }

                // Crear nuevo like
                var userLike = new UserLike
                {
                    UserId = userId,
                    ContentId = contentId,
                    LikeDate = DateTimeOffset.UtcNow
                };

                _context.UserLikes.Add(userLike);

                // Añadir a playlist de favoritos automáticamente
                await AddToFavoritesPlaylist(userId, contentId);

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Like agregado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar like");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // DELETE: api/Likes/{userId}/{contentId}
        [HttpDelete("{userId}/{contentId}")]
        public async Task<IActionResult> UnlikeContent(int userId, int contentId)
        {
            try
            {
                var userLike = await _context.UserLikes
                    .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.ContentId == contentId);

                if (userLike == null)
                {
                    return NotFound("Like no encontrado");
                }

                _context.UserLikes.Remove(userLike);

                // ✅ AGREGAR: Remover de playlist de favoritos automáticamente
                await RemoveFromFavoritesPlaylist(userId, contentId);

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Like removido correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al remover like");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // ✅ NUEVO: Método privado para agregar a playlist de favoritos
        private async Task AddToFavoritesPlaylist(int userId, int contentId)
        {
            try
            {
                // Buscar o crear playlist de favoritos
                var favoritesPlaylist = await _context.Playlist
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.Name == "Favoritos");

                if (favoritesPlaylist == null)
                {
                    favoritesPlaylist = new Playlist
                    {
                        Name = "Favoritos",
                        UserId = userId,
                        ImageUrl = null // Se puede agregar una imagen predeterminada si se desea
                    };
                    _context.Playlist.Add(favoritesPlaylist);
                    await _context.SaveChangesAsync(); // Guardar para obtener el ID
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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al agregar contenido {contentId} a playlist de favoritos del usuario {userId}");
                // No lanzamos la excepción para que el like siga funcionando aunque falle la playlist
            }
        }

        // ✅ NUEVO: Método privado para remover de playlist de favoritos
        private async Task RemoveFromFavoritesPlaylist(int userId, int contentId)
        {
            try
            {
                // Buscar playlist de favoritos
                var favoritesPlaylist = await _context.Playlist
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.Name == "Favoritos");

                if (favoritesPlaylist != null)
                {
                    // Remover de la playlist
                    var playlistContent = await _context.PlaylistContents
                        .FirstOrDefaultAsync(pc => pc.PlaylistId == favoritesPlaylist.Id && pc.ContentId == contentId);

                    if (playlistContent != null)
                    {
                        _context.PlaylistContents.Remove(playlistContent);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al remover contenido {contentId} de playlist de favoritos del usuario {userId}");
                // No lanzamos la excepción para que el unlike siga funcionando aunque falle la playlist
            }
        }
    }
}