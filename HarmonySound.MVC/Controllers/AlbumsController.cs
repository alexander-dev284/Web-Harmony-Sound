using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HarmonySound.MVC.Controllers
{
    public class AlbumsController : Controller
    {
        // GET: AlbumsController
        public ActionResult Index()
        {
            return View();
        }

        // GET: AlbumsController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: AlbumsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AlbumsController/Create
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

        // GET: AlbumsController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: AlbumsController/Edit/5
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

        // GET: AlbumsController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: AlbumsController/Delete/5
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
