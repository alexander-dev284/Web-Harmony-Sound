using HarmonySound.API.Consumer;
using HarmonySound.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HarmonySound.MVC.Controllers
{
    public class RolesController : Controller
    {
        // GET: RolesController
        public ActionResult Index()
        {
            var data = Crud<Role>.GetAll();
            return View(data);
        }

        // GET: RolesController/Details/5
        public ActionResult Details(int id)
        {
            var data = Crud<Role>.GetById(id);
            return View(data);
        }

        // GET: RolesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: RolesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Role data)
        {
            try
            {
                Crud<Role>.Create(data);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }

        // GET: RolesController/Edit/5
        public ActionResult Edit(int id)
        {
            var data = Crud<Role>.GetById(id);
            return View(data);
        }

        // POST: RolesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Role data)
        {
            try
            {
                Crud<Role>.Update(id, data);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }

        // GET: RolesController/Delete/5
        public ActionResult Delete(int id)
        {
            var data = Crud<Role>.GetById(id);
            return View(data);
        }

        // POST: RolesController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, Role data)
        {
            try
            {
                Crud<Role>.Delete(id);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }
    }
}
