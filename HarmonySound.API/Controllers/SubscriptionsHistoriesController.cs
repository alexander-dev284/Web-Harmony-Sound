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
    public class SubscriptionsHistoriesController : ControllerBase
    {
        private readonly HarmonySoundDbContext _context;

        public SubscriptionsHistoriesController(HarmonySoundDbContext context)
        {
            _context = context;
        }

        // GET: api/SubscriptionsHistories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubscriptionHistory>>> GetSubscriptionHistory()
        {
            return await _context.SubscriptionsHistories.ToListAsync();
        }

        // GET: api/SubscriptionsHistories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SubscriptionHistory>> GetSubscriptionHistory(int id)
        {
            var subscriptionHistory = await _context.SubscriptionsHistories.FindAsync(id);

            if (subscriptionHistory == null)
            {
                return NotFound();
            }

            return subscriptionHistory;
        }

        // PUT: api/SubscriptionsHistories/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSubscriptionHistory(int id, SubscriptionHistory subscriptionHistory)
        {
            if (id != subscriptionHistory.Id)
            {
                return BadRequest();
            }

            _context.Entry(subscriptionHistory).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SubscriptionHistoryExists(id))
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

        // POST: api/SubscriptionsHistories
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<SubscriptionHistory>> PostSubscriptionHistory(SubscriptionHistory subscriptionHistory)
        {
            _context.SubscriptionsHistories.Add(subscriptionHistory);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSubscriptionHistory", new { id = subscriptionHistory.Id }, subscriptionHistory);
        }

        // DELETE: api/SubscriptionsHistories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubscriptionHistory(int id)
        {
            var subscriptionHistory = await _context.SubscriptionsHistories.FindAsync(id);
            if (subscriptionHistory == null)
            {
                return NotFound();
            }

            _context.SubscriptionsHistories.Remove(subscriptionHistory);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SubscriptionHistoryExists(int id)
        {
            return _context.SubscriptionsHistories.Any(e => e.Id == id);
        }
    }
}
