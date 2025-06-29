using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HarmonySound.MVC.Controllers
{
    public class UserRolesController : Controller
    {
        // GET: UserRolesController
        public ActionResult Index()
        {
            return View();
        }

        // GET: UserRolesController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: UserRolesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: UserRolesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: UserRolesController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: UserRolesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: UserRolesController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: UserRolesController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
