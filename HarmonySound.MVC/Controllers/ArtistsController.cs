using HarmonySound.Models;
using HarmonySound.API.Consumer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace HarmonySound.MVC.Controllers
{
    [Authorize]
    public class ArtistsController : Controller
    {
        private readonly HttpClient _httpClient;

        public ArtistsController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Este método se invoca cuando el formulario de la vista "UploadAudio" se envía.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAudio(IFormCollection form)
        {
            try
            {
                // Obtener el archivo desde el formulario
                var file = form.Files["File"];
                if (file == null || file.Length == 0)
                {
                    TempData["Error"] = "Archivo no válido.";
                    return RedirectToAction("Index"); // Redirige a la vista Index en caso de error
                }

                // Log de información sobre el archivo
                System.Diagnostics.Debug.WriteLine($"Nombre: {file.FileName}, Tamaño: {file.Length}");

                if (string.IsNullOrWhiteSpace(form["Title"]) || string.IsNullOrWhiteSpace(form["Type"]) || string.IsNullOrWhiteSpace(form["ArtistId"]))
                {
                    TempData["Error"] = "Todos los campos son obligatorios.";
                    return RedirectToAction("Index");
                }

                using var content = new MultipartFormDataContent();
                content.Add(new StringContent(form["Title"]), "Title"); // Título del contenido
                content.Add(new StringContent(form["Type"]), "Type");   // Tipo del contenido
                content.Add(new StringContent(form["ArtistId"]), "ArtistId"); // ID del artista

                // Validar tamaño máximo antes de abrir el stream
                if (file.Length > 50 * 1024 * 1024)
                {
                    TempData["Error"] = "El archivo es demasiado grande.";
                    return RedirectToAction("Index");
                }

                // Agregar el archivo al contenido
                content.Add(new StreamContent(file.OpenReadStream()), "File", file.FileName);

                // Realizar la solicitud POST a la API para cargar el archivo
                var response = await _httpClient.PostAsync("https://localhost:7120/api/Contents/upload", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Archivo subido con éxito.";
                }
                else
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Error al subir el archivo: {errorMsg}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Excepción: " + ex.ToString();
                System.Diagnostics.Debug.WriteLine("Excepción: " + ex.ToString());
            }

            return RedirectToAction("Index"); // Redirige al Index después de intentar subir el archivo
        }

        // Método que muestra todos los contenidos del artista
        public async Task<IActionResult> Index()
        {
            // Obtén el ID del artista (usuario autenticado)
            var nameIdentifierClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (nameIdentifierClaim == null || !int.TryParse(nameIdentifierClaim.Value, out int artistId))
            {
                ViewBag.Error = "Tu sesión no es válida. Por favor, cierra sesión y vuelve a iniciar sesión.";
                return View("Error401"); // Muestra una vista de error si el usuario no tiene sesión
            }

            // Configura el endpoint de la API para obtener todos los contenidos del artista
            Crud<Content>.EndPoint = "https://localhost:7120/api/Contents";
            var allContents = Crud<Content>.GetAll(); // Obtiene todos los contenidos

            // Filtra los contenidos para mostrar solo los del artista actual
            var myContents = allContents.Where(c => c.ArtistId == artistId).ToList();
            return View(myContents); // Pasa los contenidos al Index
        }
    }
}
