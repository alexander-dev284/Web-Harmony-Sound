using HarmonySound.API.DTOs;
using HarmonySound.Models;
using HarmonySound.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace HarmonySound.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;
        private readonly IJwtService _jwtService;
        private readonly I2FAService _twoFactorAuthService;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<Role> roleManager,
            IConfiguration configuration,
            IEmailSender emailSender,
            IJwtService jwtService,
            I2FAService twoFactorAuthService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _emailSender = emailSender;
            _jwtService = jwtService;
            _twoFactorAuthService = twoFactorAuthService;
        }

        // Registro de usuario con envío de email de confirmación
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!await _roleManager.RoleExistsAsync(model.Role))
                return BadRequest(new { Message = "El rol especificado no existe." });

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                Name = model.Name,
                RegisterDate = DateTimeOffset.UtcNow,
                State = "Active"
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
            if (!roleResult.Succeeded)
                return BadRequest(roleResult.Errors);

            //Usar ApiUrl en lugar de AppUrl para endpoints de API
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var tokenEncoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            
            //ApiUrl para endpoints de la API
            var apiUrl = _configuration["ApiUrl"] ?? "https://localhost:7120";
            var confirmationLink = $"{apiUrl}/api/Auth/confirm-email?userId={user.Id}&token={tokenEncoded}";

            await _emailSender.SendEmailAsync(user.Email, "Confirma tu email", $"Confirma tu cuenta aquí: {confirmationLink}");

            return Ok(new { Message = "Usuario registrado correctamente. Por favor verifica tu email." });
        }

        // Confirmación de email
        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(int userId, string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                    return BadRequest("Token no proporcionado.");

                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                    return BadRequest("Usuario no encontrado.");

                string decodedToken;
                try
                {
                    decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
                }
                catch
                {
                    return BadRequest("Token malformado.");
                }

                var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

                if (result.Succeeded)
                    return Ok("Email confirmado correctamente.");
                else
                    return BadRequest("Token inválido o expirado.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        // Login de usuario con JWT
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(new { Message = "Credenciales inválidas" });

            // Estado del usuario con mensajes específicos
            if (user.State == "Suspended")
                return Unauthorized(new { 
                    Message = "Tu cuenta ha sido suspendida. Contacta al administrador para más información.",
                    StatusCode = "ACCOUNT_SUSPENDED",
                    RedirectAction = "AccountSuspended"
                });

            if (user.State == "Inactive")
                return Unauthorized(new { 
                    Message = "Tu cuenta está inactiva. Contacta al administrador para reactivarla.",
                    StatusCode = "ACCOUNT_INACTIVE"
                });

            if (user.State != "Active")
                return Unauthorized(new { 
                    Message = "Estado de cuenta no válido. Contacta al administrador.",
                    StatusCode = "ACCOUNT_INVALID_STATE"
                });

            // Verifica si el email está confirmado
            if (!await _userManager.IsEmailConfirmedAsync(user))
                return Unauthorized(new { 
                    Message = "Debes confirmar tu email antes de iniciar sesión.",
                    StatusCode = "EMAIL_NOT_CONFIRMED"
                });

            var token = await _jwtService.GenerateTokenAsync(user);

            return Ok(new { Token = token });
        }

        // Recuperación de contraseña: envío de código por email
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                return BadRequest("Usuario no encontrado o email no confirmado.");

            // Genera código de recuperación y lo envía por email
            var code = await _twoFactorAuthService.GenerateCodeAsync(user.Id);
            await _twoFactorAuthService.SendCodeByEmailAsync(user.Email, code);

            return Ok(new { Message = "Se ha enviado el código de recuperación a tu email." });
        }

        // Restablecimiento de contraseña usando el código enviado

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            if (user == null)
                return BadRequest("Usuario no encontrado.");

            var isValid = await _twoFactorAuthService.ValidateCodeAsync(user.Id, model.Code);
            if (!isValid)
                return BadRequest("Código inválido o expirado.");

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);

            if (result.Succeeded)
                return Ok("Contraseña restablecida correctamente.");
            else
                return BadRequest(result.Errors);
        }

        // Reenvío de correo de confirmación
        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest("Usuario no encontrado.");

            if (await _userManager.IsEmailConfirmedAsync(user))
                return BadRequest("El email ya está confirmado.");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var tokenEncoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            
            //ApiUrl para endpoints de la API
            var apiUrl = _configuration["ApiUrl"] ?? "https://localhost:7120";
            var confirmationLink = $"{apiUrl}/api/Auth/confirm-email?userId={user.Id}&token={tokenEncoded}";

            await _emailSender.SendEmailAsync(user.Email, "Confirma tu email", $"Confirma tu cuenta aquí: {confirmationLink}");

            return Ok(new { Message = "Correo de confirmación reenviado. Revisa tu bandeja de entrada." });
        }

        //Endpoint específico para login de admin
        [HttpPost("admin-login")]
        public async Task<IActionResult> AdminLogin([FromBody] LoginModel model)
        {
            Console.WriteLine($"AdminLogin iniciado para: {model.Email}");
            
            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState inválido");
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            Console.WriteLine($"Usuario encontrado: {user != null}");
            
            if (user == null)
            {
                Console.WriteLine("Usuario no encontrado");
                return Unauthorized(new { Message = "Credenciales inválidas - Usuario no encontrado" });
            }

            Console.WriteLine($"Email confirmado: {user.EmailConfirmed}");
            Console.WriteLine($"Estado usuario: {user.State}");

            var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            Console.WriteLine($"Contraseña válida: {passwordValid}");
            
            if (!passwordValid)
            {
                Console.WriteLine("Contraseña incorrecta");
                return Unauthorized(new { Message = "Credenciales inválidas - Contraseña incorrecta" });
            }

            // Solo admins pueden usar este endpoint
            var roles = await _userManager.GetRolesAsync(user);
            Console.WriteLine($"Roles del usuario: [{string.Join(", ", roles)}]");
            
            if (!roles.Contains("Admin"))
            {
                Console.WriteLine("Usuario no tiene rol Admin");
                return Unauthorized(new { Message = "Acceso denegado. Solo administradores." });
            }

            // Cuenta activa
            if (user.State != "Active")
            {
                Console.WriteLine($"Cuenta no activa: {user.State}");
                return Unauthorized(new { Message = "Cuenta de administrador desactivada" });
            }

            Console.WriteLine("Generando token...");
            var token = await _jwtService.GenerateTokenAsync(user);
            Console.WriteLine("AdminLogin exitoso");

            return Ok(new
            {
                Token = token,
                RequiresTwoFactor = true,
                Message = "Login exitoso"
            });
        }

        // Endpoint para generar código 2FA
        [HttpPost("generate-2fa-code")]
        public async Task<IActionResult> Generate2FACode([FromBody] TwoFactorRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId.ToString());
                if (user == null)
                    return BadRequest("Usuario no encontrado");

                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Admin"))
                    return Unauthorized("Solo administradores pueden usar 2FA");

                var code = await _twoFactorAuthService.GenerateCodeAsync(user.Id);
                await _twoFactorAuthService.SendCodeByEmailAsync(user.Email, code);

                return Ok(new { Message = "Código 2FA enviado al email" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // Endpoint para verificar código 2FA
        [HttpPost("verify-2fa-code")]
        public async Task<IActionResult> Verify2FACode([FromBody] VerifyTwoFactorRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId.ToString());
                if (user == null)
                    return BadRequest("Usuario no encontrado");

                var isValid = await _twoFactorAuthService.ValidateCodeAsync(user.Id, request.Code);
                
                return Ok(new { IsValid = isValid });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        public class TwoFactorRequest
        {
            public int UserId { get; set; }
        }

        public class VerifyTwoFactorRequest
        {
            public int UserId { get; set; }
            public string Code { get; set; } = "";
        }
        public class ResetPasswordModel
        {
            public int UserId { get; set; }
            public string Code { get; set; }
            public string NewPassword { get; set; }
        }
    }
}