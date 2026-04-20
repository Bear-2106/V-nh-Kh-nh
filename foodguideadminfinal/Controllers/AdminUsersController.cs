using FoodGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodGuideAdmin.Controllers
{
    public class AdminUsersController : AdminControllerBase
    {
        public async Task<IActionResult> Index(int page = 1, int pageSize = 5, string search = "")
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var users = await GetListAsync<AdminUserViewModel>("AdminUsers");
            var filtered = users.Where(x =>
                    string.IsNullOrWhiteSpace(search)
                    || x.Username.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.FullName.Contains(search, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.Id);

            return View(BuildPagedResult(filtered, page, pageSize, search));
        }

        [HttpGet]
        public IActionResult Create()
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            return View(new AdminUserEditViewModel { Role = "Admin" });
        }

        [HttpPost]
        public async Task<IActionResult> Create(AdminUserEditViewModel model)
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            model.Role = "Admin";
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await SendAsync(HttpMethod.Post, "AdminUsers", model);
            if (!result.Success)
            {
                ViewBag.Error = $"Thêm tài khoản admin thất bại: {result.Error}";
                return View(model);
            }

            SetAlert("success", "Thêm tài khoản admin thành công.");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var user = await GetItemAsync<AdminUserEditViewModel>($"AdminUsers/{id}");
            if (user == null)
            {
                SetAlert("danger", "Không tìm thấy tài khoản admin.");
                return RedirectToAction(nameof(Index));
            }

            user.Role = "Admin";
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, AdminUserEditViewModel model)
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            model.Id = id;
            model.Role = "Admin";
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await SendAsync(HttpMethod.Put, $"AdminUsers/{id}", model);
            if (!result.Success)
            {
                ViewBag.Error = $"Cập nhật tài khoản admin thất bại: {result.Error}";
                return View(model);
            }

            SetAlert("success", "Cập nhật tài khoản admin thành công.");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var result = await DeleteAsync($"AdminUsers/{id}");
            SetAlert(result.Success ? "success" : "danger", result.Success ? "Xóa tài khoản admin thành công." : $"Xóa thất bại: {result.Error}");
            return RedirectToAction(nameof(Index));
        }
    }
}
