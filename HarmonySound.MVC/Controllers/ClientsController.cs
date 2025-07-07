using HarmonySound.API.Consumer;
using HarmonySound.Models;
using Microsoft.AspNetCore.Mvc;
namespace HarmonySound.MVC.Controllers
{
    public class ClientsController : Controller
    {
        public async Task<IActionResult> Index()
        {
            Crud<Content>.EndPoint = "https://localhost:7120/api/Contents";
            var contenidos = Crud<Content>.GetAll();
            return View(contenidos);
        }
    }
}
