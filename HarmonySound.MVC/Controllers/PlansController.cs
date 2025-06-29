using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HarmonySound.MVC.Controllers
{
    public class PlansController : Controller
    {
        // GET: PlansController
        public ActionResult Index()
        {
            return View();
        }

        // GET: PlansController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: PlansController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: PlansController/Create
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

        // GET: PlansController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: PlansController/Edit/5
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

        // GET: PlansController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: PlansController/Delete/5
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
