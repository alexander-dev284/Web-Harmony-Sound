using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using HarmonySound.Models;
using HarmonySound.API.DTOs; 
using HarmonySound.API.Data;

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
        public async Task<ActionResult<IEnumerable<UserRoleDto>>> GetUserRoles()
        {
            try
            {
                var userRoles = await _context.UserRoles
                    .Include(ur => ur.User)
                    .Include(ur => ur.Role)
                    .Where(ur => ur.User != null && ur.Role != null) 
                    .Select(ur => new UserRoleDto
                    {
                        UserId = ur.UserId,
                        UserName = ur.User!.Name,
                        UserEmail = ur.User!.Email,
                        RoleId = ur.RoleId,
                        RoleName = ur.Role!.RoleName
                    })
                    .ToListAsync();

                return Ok(userRoles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error interno del servidor", Error = ex.Message });
            }
        }

        // GET: api/UserRoles/user/5/role/3
        [HttpGet("user/{userId}/role/{roleId}")]
       
        public async Task<ActionResult<UserRoleDto>> GetUserRole(int userId, int roleId)
        {
            try
            {
                var userRole = await _context.UserRoles
                    .Include(ur => ur.User)
                    .Include(ur => ur.Role)
                    .Where(ur => ur.UserId == userId && ur.RoleId == roleId)
                    .Select(ur => new UserRoleDto
                    {
                        UserId = ur.UserId,
                        UserName = ur.User!.Name,
                        UserEmail = ur.User!.Email,
                        RoleId = ur.RoleId,
                        RoleName = ur.Role!.RoleName
                    })
                    .FirstOrDefaultAsync();

                if (userRole == null)
                    return NotFound(new { Message = "Relación usuario-rol no encontrada" });

                return Ok(userRole);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error interno del servidor", Error = ex.Message });
            }
        }

        // POST: api/UserRoles/assign
        [HttpPost("assign")]
        
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            if (user == null)
                return NotFound(new { Message = "Usuario no encontrado" });

            if (!await _roleManager.RoleExistsAsync(model.RoleName))
                return NotFound(new { Message = "Rol no encontrado" });

            // Si ya tiene el rol
            if (await _userManager.IsInRoleAsync(user, model.RoleName))
                return BadRequest(new { Message = "El usuario ya tiene este rol asignado" });

            var result = await _userManager.AddToRoleAsync(user, model.RoleName);
            if (!result.Succeeded)
                return BadRequest(new { Message = "Error al asignar rol", Errors = result.Errors });

            return Ok(new { Message = "Rol asignado correctamente" });
        }

        // POST: api/UserRoles/remove
        [HttpPost("remove")]
     
        public async Task<IActionResult> RemoveRole([FromBody] AssignRoleModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            if (user == null)
                return NotFound(new { Message = "Usuario no encontrado" });

            if (!await _roleManager.RoleExistsAsync(model.RoleName))
                return NotFound(new { Message = "Rol no encontrado" });

            // Si tiene el rol
            if (!await _userManager.IsInRoleAsync(user, model.RoleName))
                return BadRequest(new { Message = "El usuario no tiene este rol asignado" });

            var result = await _userManager.RemoveFromRoleAsync(user, model.RoleName);
            if (!result.Succeeded)
                return BadRequest(new { Message = "Error al remover rol", Errors = result.Errors });

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
                return NotFound(new { Message = "Relación usuario-rol no encontrada" });
            }

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Relación usuario-rol eliminada correctamente" });
        }

        // GET: api/UserRoles/users-with-roles 
        [HttpGet("users-with-roles")]
      
        public async Task<ActionResult<IEnumerable<object>>> GetUsersWithRoles()
        {
            try
            {
                var usersWithRoles = await _context.Users
                    .Select(u => new
                    {
                        UserId = u.Id,
                        UserName = u.Name,
                        Email = u.Email,
                        State = u.State,
                        Roles = _context.UserRoles
                            .Where(ur => ur.UserId == u.Id)
                            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.RoleName)
                            .ToList()
                    })
                    .ToListAsync();

                return Ok(usersWithRoles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error interno del servidor", Error = ex.Message });
            }
        }

        // Verifica si existe la relación usuario-rol
        private bool UserRoleExists(int userId, int roleId)
        {
            return _context.UserRoles.Any(e => e.UserId == userId && e.RoleId == roleId);
        }
    }
 
}
