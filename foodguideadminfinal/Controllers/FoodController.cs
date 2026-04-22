using Microsoft.AspNetCore.Mvc;

namespace FoodGuideAdmin.Controllers
{
    public class FoodController : AdminControllerBase
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Poi");
        }

        [HttpGet]
        public IActionResult Create()
        {
            return RedirectToAction("Index", "Poi");
        }

        [HttpPost]
        public IActionResult Create(IFormCollection form)
        {
            return RedirectToAction("Index", "Poi");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            return RedirectToAction("Index", "Poi");
        }

        [HttpPost]
        public IActionResult Edit(int id, IFormCollection form)
        {
            return RedirectToAction("Index", "Poi");
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            return RedirectToAction("Index", "Poi");
        }
    }
}
