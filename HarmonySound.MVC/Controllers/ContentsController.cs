using HarmonySound.API.Consumer;
using HarmonySound.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HarmonySound.MVC.Controllers
{
    public class ContentsController : Controller
    {
        // GET: ContentsController
        public ActionResult Index()
        {
            var data = Crud<Content>.GetAll();
            return View(data);
        }

        // GET: ContentsController/Details/5
        public ActionResult Details(int id)
        {
            var data = Crud<Content>.GetById(id);
            return View(data);
        }

        // GET: ContentsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ContentsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Content data)
        {
            try
            {
                Crud<Content>.Create(data);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }

        // GET: ContentsController/Edit/5
        public ActionResult Edit(int id)
        {
            var data = Crud<Content>.GetById(id);
            return View(data);
        }

        // POST: ContentsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Content data)
        {
            try
            {
                Crud<Content>.Update(id, data);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }

        // GET: ContentsController/Delete/5
        public ActionResult Delete(int id)
        {
            var data = Crud<Content>.GetById(id);
            return View(data);
        }

        // POST: ContentsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, Content data)
        {
            try
            {
                Crud<Content>.Delete(id);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }
    }
}
