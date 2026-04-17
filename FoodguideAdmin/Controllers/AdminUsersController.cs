using FoodGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodGuideAdmin.Controllers
{
    public class AdminUsersController : AdminControllerBase
    {
        public async Task<IActionResult> Index(int page = 1, int pageSize = 5, string search = "", string role = "All")
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var users = await GetListAsync<AdminUserViewModel>("AdminUsers");
            var roles = users.Select(x => x.Role).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList();
            var filtered = users.Where(x =>
                (string.IsNullOrWhiteSpace(search)
                    || x.Username.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.FullName.Contains(search, StringComparison.OrdinalIgnoreCase))
                && (role == "All" || string.Equals(x.Role, role, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(x => x.Id);

            ViewBag.Roles = roles;
            ViewBag.SelectedRole = role;
            return View(BuildPagedResult(filtered, page, pageSize, search));
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            ViewBag.RoleOptions = await GetRoleOptionsAsync();
            return View(new AdminUserEditViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(AdminUserEditViewModel model)
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            if (!ModelState.IsValid)
            {
                ViewBag.RoleOptions = await GetRoleOptionsAsync();
                return View(model);
            }

            var result = await SendAsync(HttpMethod.Post, "AdminUsers", model);
            if (!result.Success)
            {
                ViewBag.Error = $"Thêm tài khoản admin thất bại: {result.Error}";
                ViewBag.RoleOptions = await GetRoleOptionsAsync();
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

            ViewBag.RoleOptions = await GetRoleOptionsAsync();
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

            if (!ModelState.IsValid)
            {
                ViewBag.RoleOptions = await GetRoleOptionsAsync();
                return View(model);
            }

            model.Id = id;
            var result = await SendAsync(HttpMethod.Put, $"AdminUsers/{id}", model);
            if (!result.Success)
            {
                ViewBag.Error = $"Cập nhật tài khoản admin thất bại: {result.Error}";
                ViewBag.RoleOptions = await GetRoleOptionsAsync();
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

        private async Task<List<string>> GetRoleOptionsAsync()
        {
            var roles = await GetListAsync<RoleViewModel>("Roles");
            return roles.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList();
        }
    }
}
