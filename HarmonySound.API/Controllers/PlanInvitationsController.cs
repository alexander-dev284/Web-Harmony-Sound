using HarmonySound.Models;
using HarmonySound.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Security.Cryptography;
using System.Text;

namespace HarmonySound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlanInvitationsController : ControllerBase
    {
        private readonly HarmonySoundDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        public PlanInvitationsController(
            HarmonySoundDbContext context, 
            IEmailSender emailSender,
            IConfiguration configuration)
        {
            _context = context;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        // Obtener invitaciones enviadas por un usuario
        [HttpGet("sent/{userId}")]
        public async Task<IActionResult> GetSentInvitations(int userId)
        {
            try
            {
                var invitations = await _context.PlanInvitations
                    .Include(pi => pi.Plan)
                    .Include(pi => pi.Invitee)
                    .Where(pi => pi.InviterId == userId)
                    .OrderByDescending(pi => pi.InvitedDate)
                    .Select(pi => new
                    {
                        pi.Id,
                        pi.InviteeEmail,
                        InviteeName = pi.Invitee != null ? pi.Invitee.Name : null,
                        PlanName = pi.Plan.PlanName,
                        pi.Status,
                        pi.InvitedDate,
                        pi.AcceptedDate,
                        pi.ExpirationDate
                    })
                    .ToListAsync();

                return Ok(invitations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener invitaciones", details = ex.Message });
            }
        }

        // Enviar invitación
        [HttpPost("send")]
        public async Task<IActionResult> SendInvitation([FromBody] SendInvitationRequest request)
        {
            try
            {
                // Verificar que el usuario tiene un plan activo
                var userPlan = await _context.UsersPlans
                    .Include(up => up.Plan)
                    .FirstOrDefaultAsync(up => up.UserId == request.InviterId && up.Active);

                if (userPlan == null)
                {
                    return BadRequest("El usuario no tiene un plan activo");
                }

                // Contar invitaciones aceptadas activas para este plan
                var activeInvitations = await _context.PlanInvitations
                    .CountAsync(pi => pi.InviterId == request.InviterId && 
                                     pi.Status == "Accepted" && 
                                     pi.PlanId == userPlan.PlanId);

                // Verificar límite de cuentas (el propietario del plan cuenta como 1)
                if (activeInvitations >= userPlan.Plan.AccountLimit - 1)
                {
                    return BadRequest($"Has alcanzado el límite de {userPlan.Plan.AccountLimit} cuentas para tu plan");
                }

                // Verificar si ya existe una invitación pendiente para este email
                var existingInvitation = await _context.PlanInvitations
                    .FirstOrDefaultAsync(pi => pi.InviteeEmail == request.InviteeEmail && 
                                              pi.InviterId == request.InviterId &&
                                              pi.Status == "Pending");

                if (existingInvitation != null)
                {
                    return BadRequest("Ya existe una invitación pendiente para este email");
                }

                // Generar token único
                var token = GenerateInvitationToken();

                // Crear invitación
                var invitation = new PlanInvitation
                {
                    InviterId = request.InviterId,
                    InviteeEmail = request.InviteeEmail,
                    PlanId = userPlan.PlanId,
                    InvitationToken = token,
                    InvitedDate = DateTimeOffset.UtcNow,
                    ExpirationDate = DateTimeOffset.UtcNow.AddDays(7), // Expira en 7 días
                    Status = "Pending",
                    InvitationMessage = request.Message
                };

                _context.PlanInvitations.Add(invitation);
                await _context.SaveChangesAsync();

                // Enviar email
                await SendInvitationEmail(invitation, userPlan);

                return Ok(new { message = "Invitación enviada exitosamente", invitationId = invitation.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al enviar invitación", details = ex.Message });
            }
        }

        // Aceptar invitación
        [HttpPost("accept")]
        public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationRequest request)
        {
            try
            {
                var invitation = await _context.PlanInvitations
                    .Include(pi => pi.Plan)
                    .Include(pi => pi.Inviter)
                    .FirstOrDefaultAsync(pi => pi.InvitationToken == request.Token && pi.Status == "Pending");

                if (invitation == null)
                {
                    return BadRequest("Invitación no encontrada o ya procesada");
                }

                if (invitation.ExpirationDate < DateTimeOffset.UtcNow)
                {
                    invitation.Status = "Expired";
                    await _context.SaveChangesAsync();
                    return BadRequest("La invitación ha expirado");
                }

                // Verificar si el usuario propietario aún tiene el plan activo
                var ownerPlan = await _context.UsersPlans
                    .FirstOrDefaultAsync(up => up.UserId == invitation.InviterId && up.Active);

                if (ownerPlan == null)
                {
                    invitation.Status = "Expired";
                    await _context.SaveChangesAsync();
                    return BadRequest("El plan del usuario que te invitó ya no está activo");
                }

                // Verificar límite de cuentas
                var activeInvitations = await _context.PlanInvitations
                    .CountAsync(pi => pi.InviterId == invitation.InviterId && 
                                     pi.Status == "Accepted" && 
                                     pi.PlanId == invitation.PlanId);

                if (activeInvitations >= invitation.Plan.AccountLimit - 1)
                {
                    return BadRequest($"Se ha alcanzado el límite de {invitation.Plan.AccountLimit} cuentas para este plan");
                }

                // Buscar si el usuario ya existe
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == invitation.InviteeEmail);
                
                if (existingUser != null)
                {
                    // Verificar si ya tiene un plan activo
                    var existingUserPlan = await _context.UsersPlans
                        .FirstOrDefaultAsync(up => up.UserId == existingUser.Id && up.Active);

                    if (existingUserPlan != null)
                    {
                        return BadRequest("El usuario ya tiene un plan activo");
                    }

                    // Asignar el plan al usuario existente
                    var newUserPlan = new UserPlan
                    {
                        UserId = existingUser.Id,
                        PlanId = invitation.PlanId,
                        StartDate = DateTimeOffset.UtcNow,
                        EndDate = ownerPlan.EndDate, // ✅ Misma fecha de expiración que el plan principal
                        Active = true
                    };

                    _context.UsersPlans.Add(newUserPlan);
                    invitation.InviteeId = existingUser.Id;
                }
                else
                {
                    // ✅ Si el usuario no existe, solo marcar como aceptada pero requerir registro
                    return Ok(new { 
                        message = "Para completar la aceptación, necesitas registrarte en HarmonySound",
                        requiresRegistration = true,
                        planName = invitation.Plan.PlanName,
                        inviterName = invitation.Inviter.Name,
                        redirectUrl = $"/Account/Register?email={invitation.InviteeEmail}&token={invitation.InvitationToken}"
                    });
                }

                // Marcar invitación como aceptada
                invitation.Status = "Accepted";
                invitation.AcceptedDate = DateTimeOffset.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Invitación aceptada exitosamente",
                    planName = invitation.Plan.PlanName,
                    inviterName = invitation.Inviter.Name
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al aceptar invitación", details = ex.Message });
            }
        }

        // Rechazar invitación
        [HttpPost("decline/{token}")]
        public async Task<IActionResult> DeclineInvitation(string token)
        {
            try
            {
                var invitation = await _context.PlanInvitations
                    .FirstOrDefaultAsync(pi => pi.InvitationToken == token && pi.Status == "Pending");

                if (invitation == null)
                {
                    return BadRequest("Invitación no encontrada");
                }

                invitation.Status = "Declined";
                await _context.SaveChangesAsync();

                return Ok(new { message = "Invitación rechazada" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al rechazar invitación", details = ex.Message });
            }
        }

        // ✅ AGREGAR método faltante para cancelar invitaciones
        [HttpPost("cancel/{invitationId}")]
        public async Task<IActionResult> CancelInvitation(int invitationId)
        {
            try
            {
                var invitation = await _context.PlanInvitations
                    .FirstOrDefaultAsync(pi => pi.Id == invitationId && pi.Status == "Pending");

                if (invitation == null)
                {
                    return BadRequest("Invitación no encontrada o ya procesada");
                }

                invitation.Status = "Cancelled";
                await _context.SaveChangesAsync();

                return Ok(new { message = "Invitación cancelada exitosamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al cancelar invitación", details = ex.Message });
            }
        }

        private string GenerateInvitationToken()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
            }
        }

        private async Task SendInvitationEmail(PlanInvitation invitation, UserPlan userPlan)
        {
            var inviter = await _context.Users.FindAsync(invitation.InviterId);
            var acceptUrl = $"{_configuration["AppUrl"]}/Plans/AcceptInvitation?token={invitation.InvitationToken}";
            var declineUrl = $"{_configuration["AppUrl"]}/api/PlanInvitations/decline/{invitation.InvitationToken}";

            var emailBody = $@"
                <h2>Invitación a HarmonySound</h2>
                <p>¡Hola!</p>
                <p><strong>{inviter.Name}</strong> te ha invitado a unirte a su plan <strong>{userPlan.Plan.PlanName}</strong> en HarmonySound.</p>
                
                {(!string.IsNullOrEmpty(invitation.InvitationMessage) ? $"<p><em>Mensaje personal: {invitation.InvitationMessage}</em></p>" : "")}
                
                <p>Con este plan podrás disfrutar de:</p>
                <ul>
                    <li>Reproducción sin anuncios</li>
                    <li>Calidad de audio superior</li>
                    <li>Descargas offline</li>
                    <li>Soporte prioritario</li>
                </ul>
                
                <p>
                    <a href='{acceptUrl}' style='background-color: #1db954; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                        Aceptar Invitación
                    </a>
                </p>
                
                <p>Si no deseas aceptar esta invitación, puedes <a href='{declineUrl}'>rechazarla aquí</a>.</p>
                
                <p><small>Esta invitación expira el {invitation.ExpirationDate:dd/MM/yyyy HH:mm}.</small></p>
            ";

            await _emailSender.SendEmailAsync(
                invitation.InviteeEmail, 
                $"Invitación a HarmonySound de {inviter.Name}", 
                emailBody
            );
        }
    }

    // Clases de request
    public class SendInvitationRequest
    {
        public int InviterId { get; set; }
        public string InviteeEmail { get; set; }
        public string? Message { get; set; }
    }

    public class AcceptInvitationRequest
    {
        public string Token { get; set; }
    }
}