using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HarmonySound.MVC.Controllers
{
    public class ContentAlbumsController : Controller
    {
        // GET: ContentAlbumsController
        public ActionResult Index()
        {
            return View();
        }

        // GET: ContentAlbumsController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ContentAlbumsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ContentAlbumsController/Create
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

        // GET: ContentAlbumsController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ContentAlbumsController/Edit/5
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

        // GET: ContentAlbumsController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ContentAlbumsController/Delete/5
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
