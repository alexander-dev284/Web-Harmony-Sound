using HarmonySound.API.Consumer;
using HarmonySound.Models;
using Microsoft.AspNetCore.Mvc;

namespace HarmonySound.MVC.Controllers
{
    public class UsersController : Controller
    {
        // GET: UsersController
        public ActionResult Index()
        {
            var data = Crud<User>.GetAll();
            return View(data);
        }

        // GET: UsersController/Details/5
        public ActionResult Details(int id)
        {
            var data = Crud<User>.GetById(id);
            return View(data);
        }

        // GET: UsersController/Delete/5
        public ActionResult Delete(int id)
        {
            var data = Crud<User>.GetById(id);
            return View(data);
        }

        // POST: UsersController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, User data)
        {
            try
            {
                Crud<User>.Delete(id);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }
    }
}
