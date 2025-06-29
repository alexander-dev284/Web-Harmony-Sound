using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HarmonySound.MVC.Controllers
{
    public class ContentsController : Controller
    {
        // GET: ContentsController
        public ActionResult Index()
        {
            return View();
        }

        // GET: ContentsController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ContentsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ContentsController/Create
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

        // GET: ContentsController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ContentsController/Edit/5
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

        // GET: ContentsController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ContentsController/Delete/5
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
