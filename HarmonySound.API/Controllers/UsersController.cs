using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using HarmonySound.Models;
using HarmonySound.API.Data;
using HarmonySound.API.DTOs;
using Microsoft.AspNetCore.Http;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace HarmonySound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly HarmonySoundDbContext _context; // ✅ INYECTAR DataContext

        public UsersController(UserManager<User> userManager, IConfiguration configuration, HarmonySoundDbContext context)
        {
            _userManager = userManager;
            _configuration = configuration;
            _context = context; // ✅ ASIGNAR DataContext
        }

        // GET: api/Users
        [HttpGet]
        public ActionResult<IEnumerable<object>> GetUsers()
        {
            // Devuelve solo información pública
            var users = _userManager.Users.Select(u => new {
                u.Id,
                u.UserName,
                u.Email,
                u.Name,
                u.State,
                u.RegisterDate
            }).ToList();

            return Ok(users);
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound();

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.Name,
                user.State,
                user.RegisterDate
            });
        }

        // GET: api/Users/Artists
        [HttpGet("Artists")]
        public async Task<ActionResult<IEnumerable<object>>> GetArtists()
        {
            var artists = await _userManager.GetUsersInRoleAsync("Artist");
            var result = artists.Select(u => new {
                u.Id,
                u.UserName,
                u.Email,
                u.Name,
                u.State,
                u.RegisterDate
            }).ToList();

            return Ok(result);
        }

        // GET: api/Users/Clients
        [HttpGet("Clients")]
        public async Task<ActionResult<IEnumerable<object>>> GetClients()
        {
            var clients = await _userManager.GetUsersInRoleAsync("Client");
            var result = clients.Select(u => new {
                u.Id,
                u.UserName,
                u.Email,
                u.Name,
                u.State,
                u.RegisterDate
            }).ToList();

            return Ok(result);
        }

        // GET: api/Users/profile/5
        [HttpGet("profile/{id}")]
        public async Task<ActionResult<UserProfileDto>> GetProfile(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound();

            var dto = new UserProfileDto
            {
                Id = user.Id,
                Name = user.Name,
                Biography = user.Biography,
                ProfileImageUrl = user.ProfileImageUrl,
                Email = user.Email
            };
            return Ok(dto);
        }

        // PUT: api/Users/profile/5
        [HttpPut("profile/{id}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UserProfileDto dto)
        {
            if (id != dto.Id)
                return BadRequest();

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound();

            user.Name = dto.Name;
            user.Biography = dto.Biography;
            user.ProfileImageUrl = dto.ProfileImageUrl;
            // Email is not editable

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return NoContent();
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
                return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return NoContent();
        }

        [HttpPost("upload-profile-image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadProfileImage([FromForm] ProfileImageUploadDto model)
        {
            try
            {
                if (model.File == null || model.File.Length == 0)
                    return BadRequest("Invalid file.");

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(model.File.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                    return BadRequest("Only image files are allowed.");

                var connectionString = _configuration["AzureBlobStorage:ConnectionString"];
                if (string.IsNullOrEmpty(connectionString))
                    return StatusCode(500, "Azure Blob Storage connection string is missing.");

                var containerName = "profile-images";
                var fileName = $"{Guid.NewGuid()}{extension}";

                var blobServiceClient = new BlobServiceClient(connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                // 1. Obtener el usuario y la URL anterior
                var user = await _userManager.FindByIdAsync(model.UserId.ToString());
                if (user == null)
                    return NotFound();

                var previousImageUrl = user.ProfileImageUrl;

                // 2. Eliminar la imagen anterior si existe y es de Azure (no la imagen por defecto)
                if (!string.IsNullOrEmpty(previousImageUrl) && previousImageUrl.Contains(containerName))
                {
                    try
                    {
                        var previousBlobName = Path.GetFileName(new Uri(previousImageUrl).LocalPath);
                        var previousBlobClient = containerClient.GetBlobClient(previousBlobName);
                        await previousBlobClient.DeleteIfExistsAsync();
                    }
                    catch
                    {
                        // Si falla la eliminación, no interrumpe el flujo
                    }
                }

                // 3. Subir la nueva imagen
                var blobClient = containerClient.GetBlobClient(fileName);
                using (var stream = model.File.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = model.File.ContentType });
                }

                user.ProfileImageUrl = blobClient.Uri.ToString();
                await _userManager.UpdateAsync(user);

                return Ok(new { ProfileImageUrl = user.ProfileImageUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en UploadProfileImage: " + ex.ToString());
                return StatusCode(500, "Error interno del servidor: " + ex.Message);
            }
        }

        // GET: api/Users/search?query=...
        [HttpGet("search")]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Ok(new List<object>());

            var allArtists = await _userManager.GetUsersInRoleAsync("Artist");
            var filtered = allArtists
                .Where(u =>
                    (!string.IsNullOrEmpty(u.Name) && u.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                )
                .Select(u => new {
                    u.Id,
                    u.Name,
                    u.ProfileImageUrl, 
                    u.State,
                    u.RegisterDate
                })
                .ToList();

            return Ok(filtered);
        }

        // ✅ AGREGAR este método al UsersController existente
        [HttpGet("with-roles")]
        public async Task<ActionResult<IEnumerable<object>>> GetUsersWithRoles()
        {
            try
            {
                var usersWithRoles = await _context.Users
                    .Select(u => new
                    {
                        Id = u.Id,
                        Name = u.Name,
                        Email = u.Email,
                        State = u.State,
                        RegisterDate = u.RegisterDate,
                        ProfileImageUrl = u.ProfileImageUrl,
                        // ✅ OBTENER EL ROL PRINCIPAL DEL USUARIO
                        Role = _context.UserRoles
                            .Where(ur => ur.UserId == u.Id)
                            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.RoleName)
                            .FirstOrDefault() ?? "Sin Rol"
                    })
                    .ToListAsync();

                return Ok(usersWithRoles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error interno del servidor", Error = ex.Message });
            }
        }

        // ✅ AGREGAR: Endpoint específico para toggle de estado de usuario
        [HttpPost("{id}/toggle-status")]
        public async Task<IActionResult> ToggleUserStatus(int id, [FromBody] ToggleUserStatusRequest request)
        {
            try
            {
                
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                   
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                // ✅ PREVENIR: No permitir suspender administradores
                var userRoles = await _userManager.GetRolesAsync(user);
                if (userRoles.Contains("Admin") && request.NewState == "Suspended")
                {
                   
                    return BadRequest(new { message = "No se puede suspender un usuario administrador" });
                }

                // ✅ VALIDAR: Estados válidos
                var validStates = new[] { "Active", "Suspended", "Inactive" };
                if (!validStates.Contains(request.NewState))
                {
                    return BadRequest(new { message = $"Estado inválido. Estados válidos: {string.Join(", ", validStates)}" });
                }

                // ✅ VERIFICAR: Si ya tiene ese estado
                if (user.State == request.NewState)
                {
                    return BadRequest(new { message = $"El usuario ya está en estado {request.NewState}" });
                }

                // ✅ CAMBIAR ESTADO
                var previousState = user.State;
                user.State = request.NewState;

                // ✅ OPCIONAL: Agregar timestamp de cambio si tienes estas propiedades
                if (request.NewState == "Suspended")
                {
                    // Puedes agregar campos como SuspendedDate, SuspendedBy si los tienes en tu modelo
                    
                }
                else if (request.NewState == "Active")
                {
                
                }

                var result = await _userManager.UpdateAsync(user);
                
                if (!result.Succeeded)
                {
                     return BadRequest(new { 
                        message = "Error al actualizar estado del usuario", 
                        errors = result.Errors.Select(e => e.Description) 
                    });
                }

                // ✅ OPCIONAL: Registro de auditoría
                
                return Ok(new { 
                    message = $"Usuario {request.Action} correctamente",
                    userId = user.Id,
                    newState = user.State,
                    previousState = previousState,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        // ✅ NUEVO: Clase para el request de toggle de estado
        public class ToggleUserStatusRequest
        {
            public int UserId { get; set; }
            public string NewState { get; set; } = "";
            public string Action { get; set; } = "";
            public int AdminId { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
