using HarmonySound.API.DTOs;
using HarmonySound.Models;
using HarmonySound.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.WebUtilities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

            // Genera el token de confirmación de email
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var tokenEncoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var confirmationLink = $"{_configuration["AppUrl"]}/api/Auth/confirm-email?userId={user.Id}&token={tokenEncoded}";

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

            // Opcional: Verifica si el email está confirmado
            if (!await _userManager.IsEmailConfirmedAsync(user))
                return Unauthorized(new { Message = "Debes confirmar tu email antes de iniciar sesión." });

            var token = await _jwtService.GenerateTokenAsync(user);

            return Ok(new { Token = token });
        }

        // Recuperación de contraseña: envío de código por email
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                return BadRequest("Usuario no encontrado o email no confirmado.");

            // Genera código de recuperación y lo envía por email
            var code = await _twoFactorAuthService.GenerateCodeAsync(user.Id);
            await _twoFactorAuthService.SendCodeByEmailAsync(user.Email, code);

            return Ok(new { Message = "Se ha enviado el código de recuperación a tu email." });
        }

        // Restablecimiento de contraseña usando el código enviado
        public class ResetPasswordModel
        {
            public int UserId { get; set; }
            public string Code { get; set; }
            public string NewPassword { get; set; }
        }

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
            var confirmationLink = $"{_configuration["AppUrl"]}/api/Auth/confirm-email?userId={user.Id}&token={tokenEncoded}";

            await _emailSender.SendEmailAsync(user.Email, "Confirma tu email", $"Confirma tu cuenta aquí: {confirmationLink}");

            return Ok(new { Message = "Correo de confirmación reenviado. Revisa tu bandeja de entrada." });
        }
    }
}