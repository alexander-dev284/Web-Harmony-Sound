using HarmonySound.API.Consumer;
using HarmonySound.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HarmonySound.MVC.Controllers
{
    public class UsersPlansController : Controller
    {
        // GET: UsersPlansController
        public ActionResult Index()
        {
            var data = Crud<UserPlan>.GetAll();
            return View(data);
        }

        // GET: UsersPlansController/Details/5
        public ActionResult Details(int id)
        {
            var data = Crud<UserPlan>.GetById(id);
            return View(data);
        }

        // GET: UsersPlansController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: UsersPlansController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(UserPlan data)
        {
            try
            {
                Crud<UserPlan>.Create(data);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }

        // GET: UsersPlansController/Edit/5
        public ActionResult Edit(int id)
        {
            var data = Crud<UserPlan>.GetById(id);
            return View(data);
        }

        // POST: UsersPlansController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, UserPlan data)
        {
            try
            {
                Crud<UserPlan>.Update(id, data);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }

        // GET: UsersPlansController/Delete/5
        public ActionResult Delete(int id)
        {
            var data = Crud<UserPlan>.GetById(id);
            return View(data);
        }

        // POST: UsersPlansController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, UserPlan data)
        {
            try
            {
                Crud<UserPlan>.Delete(id);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }
    }
}
