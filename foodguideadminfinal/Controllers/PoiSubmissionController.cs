using Microsoft.AspNetCore.Mvc;

namespace FoodGuideAdmin.Controllers
{
    public class PoiSubmissionController : AdminControllerBase
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Poi");
        }

        [HttpGet]
        public IActionResult Approve(int id)
        {
            return RedirectToAction("Index", "Poi");
        }

        [HttpGet]
        public IActionResult Reject(int id)
        {
            return RedirectToAction("Index", "Poi");
        }
    }
}
