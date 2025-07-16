using HarmonySound.Models;
using HarmonySound.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HarmonySound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserPlansController : ControllerBase
    {
        private readonly HarmonySoundDbContext _context;

        public UserPlansController(HarmonySoundDbContext context)
        {
            _context = context;
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<UserPlan>> GetUserPlan(int userId)
        {
            var userPlan = await _context.UsersPlans
                .Include(up => up.Plan)
                .FirstOrDefaultAsync(up => up.UserId == userId && up.Active);

            if (userPlan == null)
                return NotFound();

            return Ok(userPlan);
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
        {
            // Desactivar plan actual si existe
            var existingPlan = await _context.UsersPlans
                .FirstOrDefaultAsync(up => up.UserId == request.UserId && up.Active);

            if (existingPlan != null)
            {
                existingPlan.Active = false;
                _context.UsersPlans.Update(existingPlan);
            }

            // Obtener información del plan
            var plan = await _context.Plans.FindAsync(request.PlanId);
            if (plan == null)
                return BadRequest("Plan not found");

            // Crear nueva suscripción (temporal sin pago)
            var newUserPlan = new UserPlan
            {
                UserId = request.UserId,
                PlanId = request.PlanId,
                StartDate = DateTimeOffset.UtcNow,
                EndDate = DateTimeOffset.UtcNow.AddDays(30), // 30 días de prueba
                Active = true
            };

            _context.UsersPlans.Add(newUserPlan);

            // Crear historial de suscripción
            var subscriptionHistory = new SubscriptionHistory
            {
                UserId = request.UserId,
                PlanId = request.PlanId,
                TransactionDate = DateTimeOffset.UtcNow,
                Amount = 0, // Sin costo por ahora
                State = "Success",
                PaymentMethod = "Test",
                PayReference = "TEST-" + Guid.NewGuid().ToString()[..8],
                ExpirationDate = DateTimeOffset.UtcNow.AddDays(30)
            };

            _context.SubscriptionsHistories.Add(subscriptionHistory);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Suscripción exitosa (modo prueba)" });
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> CancelSubscription([FromBody] CancelSubscriptionRequest request)
        {
            var userPlan = await _context.UsersPlans
                .FirstOrDefaultAsync(up => up.UserId == request.UserId && up.Active);

            if (userPlan == null)
                return NotFound("No active subscription found");

            userPlan.Active = false;
            userPlan.EndDate = DateTimeOffset.UtcNow;

            _context.UsersPlans.Update(userPlan);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Suscripción cancelada exitosamente" });
        }


        [HttpGet("is-premium/{userId}")]
        public async Task<IActionResult> IsUserPremium(int userId)
        {
            var userPlan = await _context.UsersPlans
                .Include(up => up.Plan)
                .FirstOrDefaultAsync(up => up.UserId == userId && up.Active);

            var isPremium = userPlan?.Plan?.Price > 0;
            
            return Ok(new { isPremium = isPremium });
        }
    }

    public class SubscribeRequest
    {
        public int UserId { get; set; }
        public int PlanId { get; set; }
    }

    public class CancelSubscriptionRequest
    {
        public int UserId { get; set; }
    }
}