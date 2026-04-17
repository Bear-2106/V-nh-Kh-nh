using FoodGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodGuideAdmin.Controllers
{
    public class RolesController : AdminControllerBase
    {
        public async Task<IActionResult> Index(int page = 1, int pageSize = 5, string search = "")
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var roles = await GetListAsync<RoleViewModel>("Roles");
            var filtered = roles.Where(x =>
                    string.IsNullOrWhiteSpace(search)
                    || x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.Permissions.Contains(search, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.Name);

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

            return View(new RoleEditViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(RoleEditViewModel model)
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

            var result = await SendAsync(HttpMethod.Post, "Roles", model);
            if (!result.Success)
            {
                ViewBag.Error = $"Thêm vai trò thất bại: {result.Error}";
                return View(model);
            }

            SetAlert("success", "Thêm vai trò thành công.");
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

            var role = await GetItemAsync<RoleEditViewModel>($"Roles/{id}");
            if (role == null)
            {
                SetAlert("danger", "Không tìm thấy vai trò.");
                return RedirectToAction(nameof(Index));
            }

            return View(role);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, RoleEditViewModel model)
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

            model.Id = id;
            var result = await SendAsync(HttpMethod.Put, $"Roles/{id}", model);
            if (!result.Success)
            {
                ViewBag.Error = $"Cập nhật vai trò thất bại: {result.Error}";
                return View(model);
            }

            SetAlert("success", "Cập nhật vai trò thành công.");
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

            var result = await DeleteAsync($"Roles/{id}");
            SetAlert(result.Success ? "success" : "danger", result.Success ? "Xóa vai trò thành công." : $"Xóa vai trò thất bại: {result.Error}");
            return RedirectToAction(nameof(Index));
        }
    }
}
