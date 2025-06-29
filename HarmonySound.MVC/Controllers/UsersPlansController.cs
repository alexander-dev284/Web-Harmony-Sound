using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HarmonySound.MVC.Controllers
{
    public class UsersPlansController : Controller
    {
        // GET: UsersPlansController
        public ActionResult Index()
        {
            return View();
        }

        // GET: UsersPlansController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: UsersPlansController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: UsersPlansController/Create
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

        // GET: UsersPlansController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: UsersPlansController/Edit/5
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

        // GET: UsersPlansController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: UsersPlansController/Delete/5
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
