using HarmonySound.API.Consumer;
using HarmonySound.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HarmonySound.MVC.Controllers
{
    public class UsersController : Controller
    {
        // GET: UsersController
        public ActionResult Index()
        {
            var data = Crud<User>.GetAll();
            return View(data);
        }

        // GET: UsersController/Details/5
        public ActionResult Details(int id)
        {
            var data = Crud<User>.GetById(id);
            return View(data);
        }

        // GET: UsersController/Create
        //public ActionResult Create()
        //{
        //    return View();
        //}

        //// POST: UsersController/Create
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Create(IFormCollection collection)
        //{
        //    try
        //    {
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}

        //// GET: UsersController/Edit/5
        //public ActionResult Edit(int id)
        //{
        //    return View();
        //}

        //// POST: UsersController/Edit/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Edit(int id, IFormCollection collection)
        //{
        //    try
        //    {
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}

        // GET: UsersController/Delete/5
        public ActionResult Delete(int id)
        {
            var data = Crud<User>.GetById(id);
            return View(data);
        }

        // POST: UsersController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, User data)
        {
            try
            {
                Crud<User>.Delete(id);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }
    }
}
