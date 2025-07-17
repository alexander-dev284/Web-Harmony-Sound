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

        // ============= ENDPOINTS DE NEGOCIO (ORIGINALES) =============

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

        // MÉTODO ORIGINAL: Mantenido para compatibilidad (modo prueba)
        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
        {
            try
            {
                // Desactivar plan actual si existe
                var existingPlan = await _context.UsersPlans
                    .FirstOrDefaultAsync(up => up.UserId == request.UserId && up.Active);

                if (existingPlan != null)
                {
                    existingPlan.Active = false;
                    existingPlan.EndDate = DateTimeOffset.UtcNow;
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
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error interno del servidor", details = ex.Message });
            }
        }

        // NUEVO: Método para procesar suscripción después del pago con PayPal
        [HttpPost("process-subscription")]
        public async Task<IActionResult> ProcessSubscription([FromBody] ProcessSubscriptionRequest request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== ProcessSubscription llamado ===");
                System.Diagnostics.Debug.WriteLine($"UserId: {request.UserId}, PlanId: {request.PlanId}");
                System.Diagnostics.Debug.WriteLine($"Amount: {request.Amount}, PaymentMethod: {request.PaymentMethod}");
                System.Diagnostics.Debug.WriteLine($"PayReference: {request.PayReference}, PaymentState: {request.PaymentState}");

                // Verificar que el plan existe
                var plan = await _context.Plans.FindAsync(request.PlanId);
                if (plan == null)
                {
                    System.Diagnostics.Debug.WriteLine("Plan no encontrado");
                    return BadRequest("Plan not found");
                }

                // Verificar que el usuario existe
                var user = await _context.Users.FindAsync(request.UserId);
                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine("Usuario no encontrado");
                    return BadRequest("User not found");
                }

                // Desactivar plan actual si existe
                var existingPlan = await _context.UsersPlans
                    .FirstOrDefaultAsync(up => up.UserId == request.UserId && up.Active);

                if (existingPlan != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Desactivando plan existente: {existingPlan.Id}");
                    existingPlan.Active = false;
                    existingPlan.EndDate = DateTimeOffset.UtcNow;
                    _context.UsersPlans.Update(existingPlan);
                }

                // Calcular fecha de expiración
                var expirationDate = DateTimeOffset.UtcNow.AddDays(30);
                
                // Crear nueva suscripción
                var newUserPlan = new UserPlan
                {
                    UserId = request.UserId,
                    PlanId = request.PlanId,
                    StartDate = DateTimeOffset.UtcNow,
                    EndDate = expirationDate,
                    Active = true
                };

                _context.UsersPlans.Add(newUserPlan);

                // Crear historial de suscripción
                var subscriptionHistory = new SubscriptionHistory
                {
                    UserId = request.UserId,
                    PlanId = request.PlanId,
                    TransactionDate = DateTimeOffset.UtcNow,
                    Amount = request.Amount,
                    State = request.PaymentState,
                    PaymentMethod = request.PaymentMethod,
                    PayReference = request.PayReference,
                    ExpirationDate = expirationDate
                };

                _context.SubscriptionsHistories.Add(subscriptionHistory);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine("Suscripción procesada exitosamente");

                // **SOLUCIÓN: Devolver solo datos simples sin relaciones circulares**
                var result = new
                {
                    message = "Suscripción procesada exitosamente",
                    userPlan = new
                    {
                        id = newUserPlan.Id,
                        userId = newUserPlan.UserId,
                        planId = newUserPlan.PlanId,
                        startDate = newUserPlan.StartDate,
                        endDate = newUserPlan.EndDate,
                        active = newUserPlan.Active,
                        planName = plan.PlanName,
                        planPrice = plan.Price
                    },
                    subscriptionHistory = new
                    {
                        id = subscriptionHistory.Id,
                        amount = subscriptionHistory.Amount,
                        paymentMethod = subscriptionHistory.PaymentMethod,
                        payReference = subscriptionHistory.PayReference,
                        transactionDate = subscriptionHistory.TransactionDate,
                        expirationDate = subscriptionHistory.ExpirationDate
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ProcessSubscription: {ex.Message}");
                return StatusCode(500, new { error = "Error al procesar suscripción", details = ex.Message });
            }
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> CancelSubscription([FromBody] CancelSubscriptionRequest request)
        {
            try
            {
                var userPlan = await _context.UsersPlans
                    .FirstOrDefaultAsync(up => up.UserId == request.UserId && up.Active);

                if (userPlan == null)
                    return NotFound("No active subscription found");

                userPlan.Active = false;
                userPlan.EndDate = DateTimeOffset.UtcNow;

                _context.UsersPlans.Update(userPlan);

                // Registrar cancelación en historial
                var cancelationHistory = new SubscriptionHistory
                {
                    UserId = request.UserId,
                    PlanId = userPlan.PlanId,
                    TransactionDate = DateTimeOffset.UtcNow,
                    Amount = 0,
                    State = "Cancelled",
                    PaymentMethod = "System",
                    PayReference = "CANCEL-" + Guid.NewGuid().ToString()[..8],
                    ExpirationDate = DateTimeOffset.UtcNow
                };

                _context.SubscriptionsHistories.Add(cancelationHistory);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Suscripción cancelada exitosamente",
                    cancelationHistory = cancelationHistory
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al cancelar suscripción", details = ex.Message });
            }
        }

        [HttpGet("is-premium/{userId}")]
        public async Task<IActionResult> IsUserPremium(int userId)
        {
            try
            {
                var userPlan = await _context.UsersPlans
                    .Include(up => up.Plan)
                    .FirstOrDefaultAsync(up => up.UserId == userId && up.Active && up.EndDate > DateTimeOffset.UtcNow);

                var isPremium = userPlan?.Plan?.Price > 0;
                
                return Ok(new { 
                    isPremium = isPremium,
                    planName = userPlan?.Plan?.PlanName,
                    expirationDate = userPlan?.EndDate
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al verificar estado premium", details = ex.Message });
            }
        }

        // NUEVO: Método para verificar suscripciones vencidas
        [HttpPost("check-expired")]
        public async Task<IActionResult> CheckExpiredSubscriptions()
        {
            try
            {
                var expiredPlans = await _context.UsersPlans
                    .Where(up => up.Active && up.EndDate <= DateTimeOffset.UtcNow)
                    .ToListAsync();

                foreach (var plan in expiredPlans)
                {
                    plan.Active = false;
                    _context.UsersPlans.Update(plan);
                }

                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = $"Se desactivaron {expiredPlans.Count} suscripciones vencidas",
                    expiredCount = expiredPlans.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al verificar suscripciones vencidas", details = ex.Message });
            }
        }

        // NUEVO: Método para obtener historial de suscripciones de un usuario
        [HttpGet("history/{userId}")]
        public async Task<IActionResult> GetSubscriptionHistory(int userId)
        {
            try
            {
                var history = await _context.SubscriptionsHistories
                    .Include(sh => sh.Plan)
                    .Where(sh => sh.UserId == userId)
                    .OrderByDescending(sh => sh.TransactionDate)
                    .ToListAsync();

                return Ok(history);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener historial", details = ex.Message });
            }
        }

        // ============= ENDPOINTS CRUD (DE UsersPlansController) =============

        // GET: api/UserPlans
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserPlan>>> GetAllUserPlans()
        {
            return await _context.UsersPlans
                .Include(up => up.User)
                .Include(up => up.Plan)
                .ToListAsync();
        }

        // GET: api/UserPlans/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserPlan>> GetUserPlanById(int id)
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

        // PUT: api/UserPlans/5
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

        // POST: api/UserPlans
        [HttpPost]
        public async Task<ActionResult<UserPlan>> PostUserPlan(UserPlan userPlan)
        {
            _context.UsersPlans.Add(userPlan);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserPlanById), new { id = userPlan.Id }, userPlan);
        }

        // DELETE: api/UserPlans/5
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

    // ============= CLASES DE REQUEST =============

    public class SubscribeRequest
    {
        public int UserId { get; set; }
        public int PlanId { get; set; }
    }

    public class CancelSubscriptionRequest
    {
        public int UserId { get; set; }
    }

    // NUEVA: Request para procesar suscripción después del pago
    public class ProcessSubscriptionRequest
    {
        public int UserId { get; set; }
        public int PlanId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string PayReference { get; set; }
        public string PaymentState { get; set; }
    }
}