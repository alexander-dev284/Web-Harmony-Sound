using HarmonySound.API.Consumer;
using HarmonySound.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HarmonySound.MVC.Controllers
{
    public class StatisticsController : Controller
    {
        // GET: StatisticsController
        public ActionResult Index()
        {
            var data = Crud<Statistic>.GetAll();
            return View(data);
        }

        // GET: StatisticsController/Details/5
        public ActionResult Details(int id)
        {
            var data = Crud<Statistic>.GetById(id);
            return View(data);
        }

        // GET: StatisticsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: StatisticsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Statistic data)
        {
            try
            {
                Crud<Statistic>.Create(data);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }

        // GET: StatisticsController/Edit/5
        public ActionResult Edit(int id)
        {
            var data = Crud<Statistic>.GetById(id);
            return View(data);
        }

        // POST: StatisticsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Statistic data)
        {
            try
            {
                Crud<Statistic>.Update(id, data);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }

        // GET: StatisticsController/Delete/5
        public ActionResult Delete(int id)
        {
            var data = Crud<Statistic>.GetById(id);
            return View(data);
        }

        // POST: StatisticsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, Statistic data)
        {
            try
            {
                Crud<Statistic>.Delete(id);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }
    }
}
