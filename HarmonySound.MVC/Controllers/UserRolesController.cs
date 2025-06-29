using HarmonySound.API.Consumer;
using HarmonySound.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HarmonySound.MVC.Controllers
{
    public class UserRolesController : Controller
    {
        // GET: UserRolesController
        public ActionResult Index()
        {
            var data = Crud<UserRole>.GetAll();
            return View(data);
        }

        // GET: UserRolesController/Details/5
        public ActionResult Details(int id)
        {
            var data = Crud<UserRole>.GetById(id);
            return View(data);
        }

        // GET: UserRolesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: UserRolesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(UserRole data)
        {
            try
            {
                Crud<UserRole>.Create(data);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }

        // GET: UserRolesController/Edit/5
        public ActionResult Edit(int id)
        {
            var data = Crud<UserRole>.GetById(id);
            return View(data);
        }

        // POST: UserRolesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, UserRole data)
        {
            try
            {
                Crud<UserRole>.Update(id, data);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }

        // GET: UserRolesController/Delete/5
        public ActionResult Delete(int id)
        {
            var data = Crud<UserRole>.GetById(id);
            return View(data);
        }

        // POST: UserRolesController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, UserRole data)
        {
            try
            {
                Crud<UserRole>.Delete(id);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }
    }
}
