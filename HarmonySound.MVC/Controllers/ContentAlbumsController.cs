using HarmonySound.API.Consumer;
using HarmonySound.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HarmonySound.MVC.Controllers
{
    public class ContentAlbumsController : Controller
    {
        // GET: ContentAlbumsController
        public ActionResult Index()
        {
            var data = Crud<ContentAlbum>.GetAll();
            return View(data);
        }

        // GET: ContentAlbumsController/Details/5
        public ActionResult Details(int id)
        {
            var data = Crud<ContentAlbum>.GetById(id);
            return View(data);
        }

        // GET: ContentAlbumsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ContentAlbumsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ContentAlbum data)
        {
            try
            {
                Crud<ContentAlbum>.Create(data);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }

        // GET: ContentAlbumsController/Edit/5
        public ActionResult Edit(int id)
        {
            var data = Crud<ContentAlbum>.GetById(id);
            return View(data);
        }

        // POST: ContentAlbumsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, ContentAlbum data)
        {
            try
            {
                Crud<ContentAlbum>.Update(id, data);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }

        // GET: ContentAlbumsController/Delete/5
        public ActionResult Delete(int id)
        {
            var data = Crud<ContentAlbum>.GetById(id);
            return View(data);
        }

        // POST: ContentAlbumsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, ContentAlbum data)
        {
            try
            {
                Crud<ContentAlbum>.Delete(id);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }
    }
}
