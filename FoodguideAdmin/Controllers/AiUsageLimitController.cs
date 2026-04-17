using FoodGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodGuideAdmin.Controllers
{
    public class AiUsageLimitController : AdminControllerBase
    {
        public async Task<IActionResult> Index(int page = 1, int pageSize = 5, string search = "", int? userId = null)
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var items = await GetListAsync<AiUsageLimitViewModel>("AiUsageLimits");
            var filtered = items.Where(x =>
                (string.IsNullOrWhiteSpace(search)
                    || x.UserId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.UsageDate.ToString("dd/MM/yyyy").Contains(search, StringComparison.OrdinalIgnoreCase))
                && (!userId.HasValue || x.UserId == userId.Value))
                .OrderByDescending(x => x.UsageDate)
                .ThenByDescending(x => x.Id);

            ViewBag.SelectedUserId = userId;
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

            return View(new AiUsageLimitEditViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(AiUsageLimitEditViewModel model)
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

            var result = await SendAsync(HttpMethod.Post, "AiUsageLimits", model);
            if (!result.Success)
            {
                ViewBag.Error = $"Thêm giới hạn AI thất bại: {result.Error}";
                return View(model);
            }

            SetAlert("success", "Thêm giới hạn AI thành công.");
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

            var item = await GetItemAsync<AiUsageLimitEditViewModel>($"AiUsageLimits/{id}");
            if (item == null)
            {
                SetAlert("danger", "Không tìm thấy dữ liệu AI usage.");
                return RedirectToAction(nameof(Index));
            }

            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, AiUsageLimitEditViewModel model)
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
            var result = await SendAsync(HttpMethod.Put, $"AiUsageLimits/{id}", model);
            if (!result.Success)
            {
                ViewBag.Error = $"Cập nhật giới hạn AI thất bại: {result.Error}";
                return View(model);
            }

            SetAlert("success", "Cập nhật giới hạn AI thành công.");
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

            var result = await DeleteAsync($"AiUsageLimits/{id}");
            SetAlert(result.Success ? "success" : "danger", result.Success ? "Xóa dữ liệu AI usage thành công." : $"Xóa thất bại: {result.Error}");
            return RedirectToAction(nameof(Index));
        }
    }
}
