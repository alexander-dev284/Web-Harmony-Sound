using HarmonySound.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using HarmonySound.API.Consumer;
using HarmonySound.Models;
using System.Text; // ✅ AGREGADO
using System.Text.Json; // ✅ AGREGADO

namespace HarmonySound.MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public AccountController(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        // GET: /Account/Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var apiModel = new
            {
                Email = model.Email,
                Password = model.Password
            };

            var json = JsonConvert.SerializeObject(apiModel);

            try
            {
                using (var client = new HttpClient())
                {
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("https://localhost:7120/api/Auth/Login", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        ModelState.AddModelError("", "Error: " + errorContent);
                        return View(model);
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var loginResult = JsonConvert.DeserializeObject<LoginResult>(responseContent);

                    if (string.IsNullOrEmpty(loginResult?.Token))
                    {
                        ModelState.AddModelError("", "No se pudo iniciar sesión. Verifica tu correo y contraseña, y asegúrate de haber confirmado tu email.");
                        return View(model);
                    }

                    // Decodificar el JWT y extraer los claims
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(loginResult.Token);
                    var claimsList = jwt.Claims.ToList();

                    // Log de claims para depuración
                    System.Diagnostics.Debug.WriteLine("Claims del usuario:");
                    foreach (var claim in claimsList)
                        System.Diagnostics.Debug.WriteLine($"{claim.Type}: {claim.Value}");

                    // Asegurarse de que haya un NameIdentifier
                    if (!claimsList.Any(c => c.Type == ClaimTypes.NameIdentifier))
                    {
                        var sub = claimsList.FirstOrDefault(c => c.Type == "sub")?.Value;
                        if (sub != null)
                            claimsList.Add(new Claim(ClaimTypes.NameIdentifier, sub));
                        else
                            claimsList.Add(new Claim(ClaimTypes.NameIdentifier, model.Email)); // fallback
                    }

                    var claimsIdentity = new ClaimsIdentity(claimsList, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties { IsPersistent = true };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties
                    );

                    // Buscar el rol del usuario
                    var roleClaim = claimsList.FirstOrDefault(c =>
                        c.Type == ClaimTypes.Role ||
                        c.Type == "role" ||
                        c.Type == "roles" ||
                        c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");

                    var role = roleClaim?.Value;

                    if (string.IsNullOrEmpty(role))
                    {
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        ModelState.AddModelError("", "Tu cuenta no tiene un rol asignado. Contacta al administrador.");
                        return View(model);
                    }

                    // Redirigir según el rol (EXCLUYE Admin - deben usar AdminLogin)
                    if (role.Equals("Client", System.StringComparison.OrdinalIgnoreCase))
                        return RedirectToAction("Home", "Clients");
                    else if (role.Equals("Artist", System.StringComparison.OrdinalIgnoreCase))
                        return RedirectToAction("Home", "Artists");
                    else if (role.Equals("Admin", System.StringComparison.OrdinalIgnoreCase))
                    {
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        ModelState.AddModelError("", "Los administradores deben usar el acceso administrativo específico.");
                        return View(model);
                    }
                    else
                    {
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        ModelState.AddModelError("", $"No tienes permisos para acceder con el rol '{role}'.");
                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error de red o de servidor: " + ex.Message);
                return View(model);
            }
        }

        // ✅ NUEVO: GET AdminLogin - Acceso específico para administradores
        [HttpGet]
        public IActionResult AdminLogin()
        {
            return View();
        }

        // ✅ NUEVO: POST AdminLogin - Proceso de login para administradores
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var apiModel = new
            {
                Email = model.Email,
                Password = model.Password
            };

            var json = JsonConvert.SerializeObject(apiModel);

            try
            {
                using (var client = new HttpClient())
                {
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    // Usar el endpoint específico para admin login
                    var response = await client.PostAsync("https://localhost:7120/api/Auth/admin-login", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        ModelState.AddModelError("", "Error: " + errorContent);
                        return View(model);
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var adminLoginResult = JsonConvert.DeserializeObject<AdminLoginResult>(responseContent);

                    if (string.IsNullOrEmpty(adminLoginResult?.Token))
                    {
                        ModelState.AddModelError("", "No se pudo iniciar sesión como administrador.");
                        return View(model);
                    }

                    // Extraer el ID del usuario del JWT para el 2FA
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(adminLoginResult.Token);
                    var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "nameid");
                    
                    if (userIdClaim == null)
                    {
                        ModelState.AddModelError("", "Error en la autenticación. Token inválido.");
                        return View(model);
                    }

                    // Generar código 2FA
                    var twoFactorRequest = new
                    {
                        UserId = int.Parse(userIdClaim.Value)
                    };

                    var twoFactorJson = JsonConvert.SerializeObject(twoFactorRequest);
                    var twoFactorContent = new StringContent(twoFactorJson, System.Text.Encoding.UTF8, "application/json");
                    var twoFactorResponse = await client.PostAsync("https://localhost:7120/api/Auth/generate-2fa-code", twoFactorContent);

                    if (!twoFactorResponse.IsSuccessStatusCode)
                    {
                        ModelState.AddModelError("", "Error al generar código de verificación.");
                        return View(model);
                    }

                    // Guardar el token temporalmente y redirigir a verificación 2FA
                    TempData["AdminToken"] = adminLoginResult.Token;
                    TempData["AdminUserId"] = userIdClaim.Value;
                    
                    return RedirectToAction("AdminVerify2FA", new { secretKey = GenerateSecretKey() });
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error de red o de servidor: " + ex.Message);
                return View(model);
            }
        }

        // ✅ NUEVO: GET AdminVerify2FA - Vista de verificación 2FA
        [HttpGet]
        public IActionResult AdminVerify2FA(string secretKey)
        {
            if (TempData["AdminToken"] == null || TempData["AdminUserId"] == null)
            {
                return RedirectToAction("AdminLogin");
            }

            var model = new Admin2FAViewModel
            {
                SecretKey = secretKey
            };

            return View(model);
        }

        // ✅ NUEVO: POST AdminVerify2FA - Verificación del código 2FA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminVerify2FA(Admin2FAViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var adminToken = TempData["AdminToken"]?.ToString();
            var adminUserId = TempData["AdminUserId"]?.ToString();

            if (string.IsNullOrEmpty(adminToken) || string.IsNullOrEmpty(adminUserId))
            {
                ModelState.AddModelError("", "Sesión expirada. Inicia sesión nuevamente.");
                return RedirectToAction("AdminLogin");
            }

            try
            {
                // Verificar el código 2FA
                var verifyRequest = new
                {
                    UserId = int.Parse(adminUserId),
                    Code = model.Code
                };

                var json = JsonConvert.SerializeObject(verifyRequest);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                using (var client = new HttpClient())
                {
                    var response = await client.PostAsync("https://localhost:7120/api/Auth/verify-2fa-code", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        ModelState.AddModelError("", "Error al verificar el código.");
                        return View(model);
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var verifyResult = JsonConvert.DeserializeObject<dynamic>(responseContent);

                    bool isValid = verifyResult?.IsValid ?? false;

                    if (!isValid)
                    {
                        ModelState.AddModelError("", "Código inválido o expirado.");
                        return View(model);
                    }

                    // Código válido, proceder con el login completo
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(adminToken);
                    var claimsList = jwt.Claims.ToList();

                    // Asegurarse de que haya un NameIdentifier
                    if (!claimsList.Any(c => c.Type == ClaimTypes.NameIdentifier))
                    {
                        var sub = claimsList.FirstOrDefault(c => c.Type == "sub")?.Value;
                        if (sub != null)
                            claimsList.Add(new Claim(ClaimTypes.NameIdentifier, sub));
                    }

                    var claimsIdentity = new ClaimsIdentity(claimsList, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties { IsPersistent = true };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties
                    );

                    // Redirigir al dashboard de administrador
                    return RedirectToAction("Dashboard", "Admin");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error de red o de servidor: " + ex.Message);
                return View(model);
            }
        }

        // GET: /Account/Register
        public async Task<ActionResult> Register()
        {
            var roles = await GetRolesFromApi();
            var model = new RegisterViewModel
            {
                Roles = roles
            };
            return View(model);
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            // Elimina el error de Roles antes de validar
            ModelState.Remove(nameof(model.Roles));

            if (!ModelState.IsValid)
            {
                model.Roles = await GetRolesFromApi();
                return View(model);
            }

            var apiModel = new
            {
                Name = model.Name,
                Email = model.Email,
                Password = model.Password,
                Role = model.Role
            };

            var json = JsonConvert.SerializeObject(apiModel);

            try
            {
                using (var client = new HttpClient())
                {
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("https://localhost:7120/api/Auth/Register", content);

                    var errorContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        // Redirige a la vista de confirmación de registro
                        return RedirectToAction("RegisterConfirmation", "Account");
                    }
                    else
                    {
                        ModelState.AddModelError("", "No se pudo registrar el usuario: " + errorContent);
                        model.Roles = await GetRolesFromApi();
                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error de red o de servidor: " + ex.Message);
                model.Roles = await GetRolesFromApi();
                return View(model);
            }
        }

        // Método auxiliar para obtener roles públicos (EXCLUYE Admin)
        private async Task<List<string>> GetRolesFromApi()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync("https://localhost:7120/api/Roles");
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        
                        // ✅ MEJOR: Usar un tipo anónimo más específico
                        var rolesData = JsonConvert.DeserializeAnonymousType(content, new[]
                        {
                            new { Id = 0, Name = "", RoleName = "" }
                        });
                        
                        if (rolesData != null)
                        {
                            return rolesData
                                .Where(r => !r.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                                .Select(r => r.Name)
                                .ToList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // ✅ OPCIONAL: Log del error para debugging
                System.Diagnostics.Debug.WriteLine($"Error al obtener roles: {ex.Message}");
            }
            return new List<string>();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public IActionResult RegisterConfirmation()
        {
            return View();
        }

        private string GetRoleFromJwt(string token)
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
                return null;

            var payload = parts[1];
            var jsonBytes = Convert.FromBase64String(PadBase64(payload));
            var json = System.Text.Encoding.UTF8.GetString(jsonBytes);

            var payloadData = Newtonsoft.Json.Linq.JObject.Parse(json);

            // Busca varios posibles nombres de claim
            return (payloadData["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ??
                    payloadData["role"] ??
                    payloadData["roles"])?.ToString();
        }

        private string PadBase64(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return base64.Replace('-', '+').Replace('_', '/');
        }

        // ✅ NUEVO: Método auxiliar para generar clave secreta
        private string GenerateSecretKey()
        {
            return Guid.NewGuid().ToString("N")[..16];
        }

        public class LoginResult
        {
            public string Token { get; set; } = "";
        }

        public class AdminLoginResult
        {
            public string Token { get; set; } = "";
            public bool RequiresTwoFactor { get; set; }
        }
    }
}