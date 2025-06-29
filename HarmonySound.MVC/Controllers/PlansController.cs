using HarmonySound.API.Consumer;
using HarmonySound.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HarmonySound.MVC.Controllers
{
    public class PlansController : Controller
    {
        // GET: PlansController
        public ActionResult Index()
        {
            var data = Crud<Plan>.GetAll();
            return View(data);
        }

        // GET: PlansController/Details/5
        public ActionResult Details(int id)
        {
            var data = Crud<Plan>.GetById(id);
            return View(data);
        }

        // GET: PlansController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: PlansController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Plan data)
        {
            try
            {
                Crud<Plan>.Create(data);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }

        // GET: PlansController/Edit/5
        public ActionResult Edit(int id)
        {
            var data = Crud<Plan>.GetById(id);
            return View(data);
        }

        // POST: PlansController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Plan data)
        {
            try
            {
                Crud<Plan>.Update(id, data);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }

        // GET: PlansController/Delete/5
        public ActionResult Delete(int id)
        {
            var data = Crud<Plan>.GetById(id);
            return View(data);
        }

        // POST: PlansController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, Plan data)
        {
            try
            {
                Crud<Plan>.Delete(id);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }
    }
}
