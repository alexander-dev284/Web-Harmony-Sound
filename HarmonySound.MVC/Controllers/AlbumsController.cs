using HarmonySound.API.Consumer;
using HarmonySound.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HarmonySound.MVC.Controllers
{
    public class AlbumsController : Controller
    {
        // GET: AlbumsController
        public ActionResult Index()
        {
            var data = Crud<Album>.GetAll();
            return View(data);
        }

        // GET: AlbumsController/Details/5
        public ActionResult Details(int id)
        {
            var data = Crud<Album>.GetById(id);
            return View(data);
        }

        // GET: AlbumsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AlbumsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Album data)
        {
            try
            {
                Crud<Album>.Create(data);
                return RedirectToAction(nameof(Index));
            }
            catch(Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while creating the album. Please try again.");
                return View(data);
            }
        }

        // GET: AlbumsController/Edit/5
        public ActionResult Edit(int id)
        {
            var data = Crud<Album>.GetById(id);
            return View(data);
        }

        // POST: AlbumsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Album data)
        {
            try
            {
                Crud<Album>.Update(id, data);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }

        // GET: AlbumsController/Delete/5
        public ActionResult Delete(int id)
        {
            var data = Crud<Album>.GetById(id);
            return View(data);
        }

        // POST: AlbumsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, Album data)
        {
            try
            {
                Crud<Album>.Delete(id);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }
    }
}
