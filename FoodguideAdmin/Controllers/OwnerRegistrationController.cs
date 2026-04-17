using FoodGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodGuideAdmin.Controllers
{
    public class OwnerRegistrationController : AdminControllerBase
    {
        public async Task<IActionResult> Index(int page = 1, int pageSize = 5, string search = "", string status = "All")
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var items = await GetListAsync<OwnerRegistrationViewModel>("PoiOwnerRegistrations");
            var filtered = items.Where(x =>
                (string.IsNullOrWhiteSpace(search)
                    || x.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.BusinessName.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.Phone.Contains(search, StringComparison.OrdinalIgnoreCase))
                && (status == "All" || string.Equals(x.Status, status, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(x => x.CreatedAt);

            ViewBag.SelectedStatus = status;
            return View(BuildPagedResult(filtered, page, pageSize, search));
        }

        [HttpGet]
        public async Task<IActionResult> Approve(int id)
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var result = await SendWithoutBodyAsync(HttpMethod.Put, $"PoiOwnerRegistrations/{id}/approve");
            SetAlert(result.Success ? "success" : "danger", result.Success ? "Duyệt đăng ký chủ quán thành công." : $"Duyệt thất bại: {result.Error}");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Reject(int id)
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var result = await SendWithoutBodyAsync(HttpMethod.Put, $"PoiOwnerRegistrations/{id}/reject");
            SetAlert(result.Success ? "success" : "danger", result.Success ? "Từ chối đăng ký chủ quán thành công." : $"Từ chối thất bại: {result.Error}");
            return RedirectToAction(nameof(Index));
        }
    }
}
