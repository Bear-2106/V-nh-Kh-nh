using FoodGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodGuideAdmin.Controllers
{
    public class ChangePasswordController : AdminControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Index(ChangePasswordViewModel model)
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var payload = new
            {
                userId = currentUser.Id,
                currentPassword = model.CurrentPassword,
                newPassword = model.NewPassword,
                confirmPassword = model.ConfirmPassword
            };

            var result = await SendAsync(HttpMethod.Post, "Auth/change-password", payload);
            if (!result.Success)
            {
                ViewBag.Error = $"Đổi mật khẩu thất bại: {result.Error}";
                return View(model);
            }

            SetAlert("success", "Đổi mật khẩu thành công.");
            return RedirectToAction(nameof(Index));
        }
    }
}
