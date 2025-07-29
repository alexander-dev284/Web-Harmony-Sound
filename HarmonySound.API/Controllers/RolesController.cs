using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using HarmonySound.Models;
using HarmonySound.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace HarmonySound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly RoleManager<Role> _roleManager;

        public RolesController(RoleManager<Role> roleManager)
        {
            _roleManager = roleManager;
        }

        // GET: api/Roles
        [HttpGet]
        public ActionResult<IEnumerable<object>> GetRoles()
        {
            var roles = _roleManager.Roles.Select(r => new {
                r.Id,
                r.Name,
                r.RoleName
            }).ToList();

            return Ok(roles);
        }

        // GET: api/Roles/public-roles - EXCLUYE Admin
        [HttpGet("public-roles")]
        public async Task<IActionResult> GetPublicRoles()
        {
            try
            {
                var roles = await _roleManager.Roles
                    .Where(r => r.Name != "Admin") // Excluir Admin
                    .Select(r => new RoleDto { 
                        Id = r.Id, 
                        Name = r.Name,
                        RoleName = r.RoleName 
                    })
                    .ToListAsync();
                    
                return Ok(roles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // GET: api/Roles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetRole(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
                return NotFound();

            return Ok(new {
                role.Id,
                role.Name,
                role.RoleName
            });
        }

        // ✅ MODIFICADO: POST simplificado usando CreateRoleDto
        [HttpPost]
        public async Task<ActionResult<object>> PostRole([FromBody] CreateRoleDto createRoleDto)
        {
            if (string.IsNullOrWhiteSpace(createRoleDto.Name) || 
                string.IsNullOrWhiteSpace(createRoleDto.RoleName))
            {
                return BadRequest("Name y RoleName son requeridos");
            }

            // ✅ Verificar si el rol ya existe
            if (await _roleManager.RoleExistsAsync(createRoleDto.Name))
            {
                return BadRequest($"El rol '{createRoleDto.Name}' ya existe");
            }

            // ✅ Crear el objeto Role con las propiedades necesarias
            var role = new Role
            {
                Name = createRoleDto.Name,
                RoleName = createRoleDto.RoleName
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, new {
                role.Id,
                role.Name,
                role.RoleName
            });
        }

        // PUT: api/Roles/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRole(int id, [FromBody] CreateRoleDto updateRoleDto)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(updateRoleDto.Name) || 
                string.IsNullOrWhiteSpace(updateRoleDto.RoleName))
            {
                return BadRequest("Name y RoleName son requeridos");
            }

            role.Name = updateRoleDto.Name;
            role.RoleName = updateRoleDto.RoleName;

            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return NoContent();
        }

        // DELETE: api/Roles/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
                return NotFound();

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return NoContent();
        }
    }
}
