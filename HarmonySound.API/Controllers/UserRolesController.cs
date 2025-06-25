using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using HarmonySound.Models;

namespace HarmonySound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserRolesController : ControllerBase
    {
        private readonly HarmonySoundDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public UserRolesController(HarmonySoundDbContext context, UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: api/UserRoles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserRole>>> GetUserRoles()
        {
            return await _context.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .ToListAsync();
        }

        // GET: api/UserRoles/user/5/role/3
        [HttpGet("user/{userId}/role/{roleId}")]
        public async Task<ActionResult<UserRole>> GetUserRole(int userId, int roleId)
        {
            var userRole = await _context.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole == null)
                return NotFound();

            return userRole;
        }

        // POST: api/UserRoles/assign
        [HttpPost("assign")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            if (user == null)
                return NotFound(new { Message = "Usuario no encontrado" });

            if (!await _roleManager.RoleExistsAsync(model.RoleName))
                return NotFound(new { Message = "Rol no encontrado" });

            var result = await _userManager.AddToRoleAsync(user, model.RoleName);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { Message = "Rol asignado correctamente" });
        }

        // POST: api/UserRoles/remove
        [HttpPost("remove")]
        public async Task<IActionResult> RemoveRole([FromBody] AssignRoleModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            if (user == null)
                return NotFound(new { Message = "Usuario no encontrado" });

            if (!await _roleManager.RoleExistsAsync(model.RoleName))
                return NotFound(new { Message = "Rol no encontrado" });

            var result = await _userManager.RemoveFromRoleAsync(user, model.RoleName);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { Message = "Rol removido correctamente" });
        }

        // DELETE: api/UserRoles/user/5/role/3
        [HttpDelete("user/{userId}/role/{roleId}")]
        public async Task<IActionResult> DeleteUserRole(int userId, int roleId)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole == null)
            {
                return NotFound();
            }

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Helper: Verifica si existe la relación usuario-rol
        private bool UserRoleExists(int userId, int roleId)
        {
            return _context.UserRoles.Any(e => e.UserId == userId && e.RoleId == roleId);
        }
    }

    public class AssignRoleModel
    {
        public int UserId { get; set; }
        public string RoleName { get; set; }
    }
}
