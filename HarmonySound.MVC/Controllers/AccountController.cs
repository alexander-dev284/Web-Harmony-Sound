using HarmonySound.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

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
        public async Task<ActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using (var client = new HttpClient())
            {
                var response = await client.PostAsJsonAsync("https://localhost:7120/api/Auth/login", model);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<LoginResult>(
                        await response.Content.ReadAsStringAsync());

                    // Guarda el token en una cookie o sesión
                    HttpContext.Session.SetString("Token", result.Token);

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Usuario o contraseña incorrectos");
                    return View(model);
                }
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
    }

    public class LoginResult
    {
        public string Token { get; set; }
    }
}