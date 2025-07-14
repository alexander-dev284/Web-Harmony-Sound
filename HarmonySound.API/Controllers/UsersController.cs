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

namespace HarmonySound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;

        public UsersController(UserManager<User> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
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
                    u.State,
                    u.RegisterDate
                })
                .ToList();

            return Ok(filtered);
        }
    }
}
