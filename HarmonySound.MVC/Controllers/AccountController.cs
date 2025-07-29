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
                        
                        // ✅ VERIFICAR: Si es un error de cuenta suspendida desde la API
                        if (errorContent.Contains("suspendida") || errorContent.Contains("suspended"))
                        {
                            return RedirectToAction("AccountSuspended");
                        }
                        
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

                    // ✅ VERIFICAR: Estado del usuario antes de procesar el token
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(loginResult.Token);
                    var claimsList = jwt.Claims.ToList();

                    // ✅ EXTRAER: UserId para verificar estado
                    var userIdClaim = claimsList.FirstOrDefault(c => 
                        c.Type == "UserId" || 
                        c.Type == "sub" || 
                        c.Type == ClaimTypes.NameIdentifier);

                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                    {
                        // ✅ VERIFICAR: Estado del usuario en la API
                        var userStatusResponse = await client.GetAsync($"https://localhost:7120/api/Users/{userId}");
                        if (userStatusResponse.IsSuccessStatusCode)
                        {
                            var userStatusJson = await userStatusResponse.Content.ReadAsStringAsync();
                            var userStatus = JsonConvert.DeserializeObject<UserStatusDto>(userStatusJson);

                            // ✅ REDIRIGIR: Usuarios suspendidos a vista específica
                            if (userStatus?.State == "Suspended")
                            {
                                return RedirectToAction("AccountSuspended");
                            }

                            // ✅ BLOQUEAR: Usuarios inactivos
                            if (userStatus?.State == "Inactive")
                            {
                                ModelState.AddModelError("", "Tu cuenta está inactiva. Contacta al administrador para reactivarla.");
                                return View(model);
                            }

                            // ✅ SOLO PERMITIR: Usuarios activos
                            if (userStatus?.State != "Active")
                            {
                                ModelState.AddModelError("", "Estado de cuenta no válido. Contacta al administrador.");
                                return View(model);
                            }
                        }
                    }

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
            Console.WriteLine("🔧 MVC AdminLogin POST iniciado");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("❌ MVC ModelState inválido");
                return View(model);
            }

            var apiModel = new
            {
                Email = model.Email,
                Password = model.Password
            };

            var json = JsonConvert.SerializeObject(apiModel);
            Console.WriteLine($"🔧 JSON enviado: {json}");

            try
            {
                using (var client = new HttpClient())
                {
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    Console.WriteLine("🔧 Llamando a API admin-login...");

                    var response = await client.PostAsync("https://localhost:7120/api/Auth/admin-login", content);
                    Console.WriteLine($"🔧 Respuesta API: {response.StatusCode}");

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"❌ Error API: {errorContent}");
                        ModelState.AddModelError("", "Error: " + errorContent);
                        return View(model);
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"🔧 Contenido respuesta: {responseContent}");

                    var adminLoginResult = JsonConvert.DeserializeObject<AdminLoginResult>(responseContent);
                    Console.WriteLine($"🔧 Token deserializado: {!string.IsNullOrEmpty(adminLoginResult?.Token)}");

                    if (string.IsNullOrEmpty(adminLoginResult?.Token))
                    {
                        Console.WriteLine("❌ Token vacío");
                        ModelState.AddModelError("", "No se pudo iniciar sesión como administrador.");
                        return View(model);
                    }

                    // ✅ USAR MÉTODO AUXILIAR
                    var userId = ExtractUserIdFromJwt(adminLoginResult.Token);

                    if (string.IsNullOrEmpty(userId))
                    {
                        Console.WriteLine("❌ No se encontró claim de userId");
                        ModelState.AddModelError("", "Error en la autenticación. Token inválido.");
                        return View(model);
                    }

                    Console.WriteLine($"🔧 UserId extraído: {userId}");

                    Console.WriteLine("🔧 Generando código 2FA...");
                    var twoFactorRequest = new
                    {
                        UserId = int.Parse(userId)
                    };

                    var twoFactorJson = JsonConvert.SerializeObject(twoFactorRequest);
                    var twoFactorContent = new StringContent(twoFactorJson, System.Text.Encoding.UTF8, "application/json");

                    Console.WriteLine("🔧 Llamando a generate-2fa-code...");
                    var twoFactorResponse = await client.PostAsync("https://localhost:7120/api/Auth/generate-2fa-code", twoFactorContent);
                    Console.WriteLine($"🔧 Respuesta 2FA: {twoFactorResponse.StatusCode}");

                    if (!twoFactorResponse.IsSuccessStatusCode)
                    {
                        var twoFactorError = await twoFactorResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"❌ Error 2FA: {twoFactorError}");
                        ModelState.AddModelError("", "Error al generar código de verificación: " + twoFactorError);
                        return View(model);
                    }

                    var twoFactorResponseContent = await twoFactorResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"✅ 2FA generado: {twoFactorResponseContent}");

                    // Guardar el token temporalmente y redirigir a verificación 2FA
                    TempData["AdminToken"] = adminLoginResult.Token;
                    TempData["AdminUserId"] = userId; // ✅ USAR userId extraído

                    Console.WriteLine("🔧 Redirigiendo a AdminVerify2FA...");
                    return RedirectToAction("AdminVerify2FA", new { secretKey = GenerateSecretKey() });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción MVC: {ex.Message}");
                Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
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

            var adminToken = TempData["AdminToken"]?.ToString();
            string? adminEmail = null;

            if (!string.IsNullOrEmpty(adminToken))
            {
                try
                {
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(adminToken);

                    var emailClaim = jwt.Claims.FirstOrDefault(c =>
                        c.Type == "email" ||
                        c.Type == ClaimTypes.Email ||
                        c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");
                    adminEmail = emailClaim?.Value;
                }
                catch
                {
                    // Si hay error al extraer el email, continuar sin él
                }
            }

            // ✅ PRESERVAR: Mantener TempData para el POST
            TempData.Keep("AdminToken");
            TempData.Keep("AdminUserId");

            var model = new Admin2FAViewModel
            {
                SecretKey = secretKey,
                AdminEmail = adminEmail,
                ExpirationTime = DateTime.UtcNow.AddMinutes(5)
            };

            return View(model);
        }

        // ✅ NUEVO: POST AdminVerify2FA - Verificación del código 2FA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminVerify2FA(Admin2FAViewModel model)
        {
            Console.WriteLine("🔧 AdminVerify2FA POST iniciado");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("❌ ModelState inválido");
                return View(model);
            }

            var adminToken = TempData["AdminToken"]?.ToString();
            var adminUserId = TempData["AdminUserId"]?.ToString();

            Console.WriteLine($"🔧 AdminToken presente: {!string.IsNullOrEmpty(adminToken)}");
            Console.WriteLine($"🔧 AdminUserId presente: {!string.IsNullOrEmpty(adminUserId)}");
            Console.WriteLine($"🔧 Código ingresado: {model.Code}");

            if (string.IsNullOrEmpty(adminToken) || string.IsNullOrEmpty(adminUserId))
            {
                Console.WriteLine("❌ Sesión expirada - TempData faltante");
                ModelState.AddModelError("", "Sesión expirada. Inicia sesión nuevamente.");
                return RedirectToAction("AdminLogin");
            }

            try
            {
                Console.WriteLine("🔧 Verificando código 2FA...");

                var verifyRequest = new
                {
                    UserId = int.Parse(adminUserId),
                    Code = model.Code
                };

                var json = JsonConvert.SerializeObject(verifyRequest);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                using (var client = new HttpClient())
                {
                    Console.WriteLine("🔧 Llamando a verify-2fa-code...");
                    var response = await client.PostAsync("https://localhost:7120/api/Auth/verify-2fa-code", content);
                    Console.WriteLine($"🔧 Respuesta verificación: {response.StatusCode}");

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"❌ Error verificación: {errorContent}");
                        ModelState.AddModelError("", "Error al verificar el código.");
                        TempData.Keep("AdminToken");
                        TempData.Keep("AdminUserId");
                        return View(model);
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"🔧 Contenido verificación: {responseContent}");


                    // Por la siguiente línea:
                    var verifyResult = JsonConvert.DeserializeObject<VerifyResult>(responseContent);

                    bool isValid = verifyResult?.IsValid ?? false;

                    Console.WriteLine($"🔧 VerifyResult.IsValid: {verifyResult?.IsValid}");
                    Console.WriteLine($"🔧 Código válido final: {isValid}");

                    if (!isValid)
                    {
                        Console.WriteLine("❌ Código inválido");
                        ModelState.AddModelError("", "Código inválido o expirado.");
                        TempData.Keep("AdminToken");
                        TempData.Keep("AdminUserId");
                        return View(model);
                    }

                    Console.WriteLine("✅ Código válido, procediendo con autenticación...");

                    // Código válido, proceder con el login completo
                    var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(adminToken);
                    var claimsList = jwt.Claims.ToList();

                    // ✅ MEJORADO: Mejor manejo de NameIdentifier
                    if (!claimsList.Any(c => c.Type == ClaimTypes.NameIdentifier))
                    {
                        var idClaim = claimsList.FirstOrDefault(c =>
                            c.Type == "sub" ||
                            c.Type == "UserId" ||
                            c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

                        if (idClaim != null)
                        {
                            claimsList.Add(new Claim(ClaimTypes.NameIdentifier, idClaim.Value));
                            Console.WriteLine($"🔧 NameIdentifier agregado: {idClaim.Value}");
                        }
                        else
                        {
                            Console.WriteLine("❌ No se pudo determinar NameIdentifier");
                            claimsList.Add(new Claim(ClaimTypes.NameIdentifier, adminUserId));
                        }
                    }

                    var claimsIdentity = new ClaimsIdentity(claimsList, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties { IsPersistent = true };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties
                    );

                    Console.WriteLine("✅ Usuario autenticado, redirigiendo a Dashboard...");

                    // Limpiar TempData
                    TempData.Remove("AdminToken");
                    TempData.Remove("AdminUserId");

                    // Redirigir al dashboard de administrador
                    return RedirectToAction("Dashboard", "Admin");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción en verificación 2FA: {ex.Message}");
                Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                ModelState.AddModelError("", "Error de red o de servidor: " + ex.Message);
                TempData.Keep("AdminToken");
                TempData.Keep("AdminUserId");
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

        // ✅ AGREGAR MÉTODO AUXILIAR
        private string? ExtractUserIdFromJwt(string token)
        {
            try
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);

                var userIdClaimTypes = new[]
                {
                    "UserId",
                    ClaimTypes.NameIdentifier,
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
                    "sub",
                    "nameid"
                };

                foreach (var claimType in userIdClaimTypes)
                {
                    var claim = jwt.Claims.FirstOrDefault(c => c.Type == claimType);
                    if (claim != null && !string.IsNullOrEmpty(claim.Value))
                    {
                        Console.WriteLine($"🔧 UserId encontrado en claim '{claimType}': {claim.Value}");
                        return claim.Value;
                    }
                }

                Console.WriteLine("❌ Claims disponibles:");
                foreach (var claim in jwt.Claims)
                {
                    Console.WriteLine($"  - {claim.Type}: {claim.Value}");
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error extrayendo UserId: {ex.Message}");
                return null;
            }
        }

        // ✅ AGREGAR: Clase para deserialización tipada
        public class VerifyResult
        {
            public bool IsValid { get; set; }
        }

        // ✅ NUEVO: Agregar estos métodos al AccountController existente

        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // ✅ CORREGIDO: Solo enviar Email, sin Password
            var apiModel = new
            {
                Email = model.Email
            };

            var json = JsonConvert.SerializeObject(apiModel);

            try
            {
                using (var client = new HttpClient())
                {
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("https://localhost:7120/api/Auth/forgot-password", content);

                    if (response.IsSuccessStatusCode)
                    {
                        // Guardar email en TempData para la siguiente vista
                        TempData["RecoveryEmail"] = model.Email;
                        return RedirectToAction("ForgotPasswordConfirmation");
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        
                        // ✅ MEJOR MANEJO DE ERRORES
                        try
                        {
                            var errorObj = JsonConvert.DeserializeObject<dynamic>(errorContent);
                            if (errorObj != null && errorObj.message != null)
                            {
                                ModelState.AddModelError("", errorObj.message.ToString());
                            }
                            else
                            {
                                ModelState.AddModelError("", "Error al procesar la solicitud.");
                            }
                        }
                        catch
                        {
                            ModelState.AddModelError("", errorContent);
                        }
                        
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

        // GET: /Account/ForgotPasswordConfirmation
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            ViewBag.RecoveryEmail = TempData["RecoveryEmail"];
            return View();
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            // Buscar el usuario para obtener su ID
            try
            {
                using (var client = new HttpClient())
                {
                    // Usar el endpoint de usuarios para buscar por email
                    var response = await client.GetAsync($"https://localhost:7120/api/Users");
                    if (response.IsSuccessStatusCode)
                    {
                        var usersJson = await response.Content.ReadAsStringAsync();
                        var users = JsonConvert.DeserializeObject<List<UserDto>>(usersJson);
                        var user = users?.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

                        if (user != null)
                        {
                            var model = new ResetPasswordViewModel
                            {
                                UserId = user.Id,
                                Email = email
                            };
                            return View(model);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al procesar la solicitud: " + ex.Message;
            }

            return RedirectToAction("ForgotPassword");
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var apiModel = new
            {
                UserId = model.UserId,
                Code = model.Code,
                NewPassword = model.NewPassword
            };

            var json = JsonConvert.SerializeObject(apiModel);

            try
            {
                using (var client = new HttpClient())
                {
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("https://localhost:7120/api/Auth/reset-password", content);

                    if (response.IsSuccessStatusCode)
                    {
                        TempData["Success"] = "Contraseña restablecida correctamente. Ya puedes iniciar sesión.";
                        return RedirectToAction("Login");
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        ModelState.AddModelError("", "Error: " + errorContent);
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

        // ✅ AGREGAR: Clase para deserializar usuarios
        public class UserDto
        {
            public int Id { get; set; }
            public string Email { get; set; } = "";
            public string Name { get; set; } = "";
        }

        // ✅ AGREGAR: DTO para el estado del usuario
        public class UserStatusDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
            public string State { get; set; } = "";
        }

        private string GetStateErrorMessage(string state)
        {
            return state switch
            {
                "Suspended" => "Tu cuenta ha sido suspendida temporalmente. Si crees que esto es un error, contacta al administrador en admin@harmonysound.com",
                "Inactive" => "Tu cuenta está inactiva. Para reactivarla, contacta al soporte técnico.",
                "Banned" => "Tu cuenta ha sido bloqueada permanentemente por violación de términos de servicio.",
                _ => "Estado de cuenta no válido. Contacta al administrador para más información."
            };
        }

        // ✅ AGREGAR: Action para mostrar vista de cuenta suspendida
        [HttpGet]
        public IActionResult AccountSuspended()
        {
            // Limpiar cualquier autenticación previa
            if (User.Identity?.IsAuthenticated == true)
            {
                HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            
            return View();
        }
    }
}