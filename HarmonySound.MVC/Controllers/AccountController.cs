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

namespace HarmonySound.MVC.Controllers
{
    public class AccountController : Controller
    {
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
                        ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
                        return View(model);
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine("JSON recibido: " + responseContent);

                    var loginResult = JsonConvert.DeserializeObject<LoginResult>(responseContent);
                    var role = GetRoleFromJwt(loginResult.Token);

                    if (role == "cliente")
                        return Redirect("/Clients/Index"); // para clientes
                    else if (role == "artista")
                        return Redirect("/Artists/Index"); // para artistas
                    else
                        return RedirectToAction("Index", "Home");
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
                        return RedirectToAction("Login", "Account");
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

        // Método auxiliar para obtener los roles desde la API
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
                        var roles = JsonConvert.DeserializeObject<List<RoleDto>>(content);
                        return roles?.Select(r => r.Name).ToList() ?? new List<string>();
                    }
                }
            }
            catch
            {
                // Opcional: loguear el error
            }
            return new List<string>();
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
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
            // El claim de rol puede variar, revisa el nombre exacto en tu JWT
            return payloadData["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]?.ToString();
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
    }

    public class LoginResult
    {
        public string Token { get; set; }
    }
}