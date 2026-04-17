using FoodGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodGuideAdmin.Controllers
{
    public class ProfileController : AdminControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Index()
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

            var profile = await GetItemAsync<ProfileViewModel>($"Auth/me?userId={currentUser.Id}")
                ?? await GetItemAsync<ProfileViewModel>($"AdminUsers/{currentUser.Id}");

            if (profile == null)
            {
                SetAlert("danger", "Không tải được hồ sơ cá nhân.");
                return RedirectToAction("Index", "Home");
            }

            return View(profile);
        }

        [HttpPost]
        public async Task<IActionResult> Index(ProfileViewModel model)
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await SendAsync(HttpMethod.Put, $"AdminUsers/{model.Id}", new AdminUserEditViewModel
            {
                Id = model.Id,
                Username = model.Username,
                FullName = model.FullName,
                Role = model.Role,
                Password = model.Password
            });

            if (!result.Success)
            {
                ViewBag.Error = $"Cập nhật hồ sơ thất bại: {result.Error}";
                return View(model);
            }

            SaveCurrentUser(new AdminSessionUser
            {
                Id = model.Id,
                Username = model.Username,
                FullName = model.FullName,
                Role = model.Role
            });

            SetAlert("success", "Cập nhật hồ sơ thành công.");
            return RedirectToAction(nameof(Index));
        }
    }
}
