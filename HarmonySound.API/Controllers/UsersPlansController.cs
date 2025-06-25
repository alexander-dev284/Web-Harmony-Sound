using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HarmonySound.Models;

namespace HarmonySound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersPlansController : ControllerBase
    {
        private readonly HarmonySoundDbContext _context;

        public UsersPlansController(HarmonySoundDbContext context)
        {
            _context = context;
        }

        // GET: api/UsersPlans
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserPlan>>> GetUsersPlans()
        {
            return await _context.UsersPlans
                .Include(up => up.User)
                .Include(up => up.Plan)
                .ToListAsync();
        }

        // GET: api/UsersPlans/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserPlan>> GetUserPlan(int id)
        {
            var userPlan = await _context.UsersPlans
                .Include(up => up.User)
                .Include(up => up.Plan)
                .FirstOrDefaultAsync(up => up.Id == id);

            if (userPlan == null)
            {
                return NotFound();
            }

            return userPlan;
        }

        // PUT: api/UsersPlans/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUserPlan(int id, UserPlan userPlan)
        {
            if (id != userPlan.Id)
            {
                return BadRequest();
            }

            _context.Entry(userPlan).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserPlanExists(id))
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

        // POST: api/UsersPlans
        [HttpPost]
        public async Task<ActionResult<UserPlan>> PostUserPlan(UserPlan userPlan)
        {
            _context.UsersPlans.Add(userPlan);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserPlan), new { id = userPlan.Id }, userPlan);
        }

        // DELETE: api/UsersPlans/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserPlan(int id)
        {
            var userPlan = await _context.UsersPlans.FindAsync(id);
            if (userPlan == null)
            {
                return NotFound();
            }

            _context.UsersPlans.Remove(userPlan);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserPlanExists(int id)
        {
            return _context.UsersPlans.Any(e => e.Id == id);
        }
    }
}
