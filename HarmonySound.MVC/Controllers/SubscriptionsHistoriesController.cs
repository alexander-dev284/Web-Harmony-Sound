using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HarmonySound.MVC.Controllers
{
    public class SubscriptionsHistoriesController : Controller
    {
        // GET: SubscriptionsHistoriesController
        public ActionResult Index()
        {
            return View();
        }

        // GET: SubscriptionsHistoriesController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: SubscriptionsHistoriesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: SubscriptionsHistoriesController/Create
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

        // GET: SubscriptionsHistoriesController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: SubscriptionsHistoriesController/Edit/5
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

        // GET: SubscriptionsHistoriesController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: SubscriptionsHistoriesController/Delete/5
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
