using FoodGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodGuideAdmin.Controllers
{
    public class PoiLocalizationController : AdminControllerBase
    {
        public async Task<IActionResult> Index(int page = 1, int pageSize = 5, string search = "", int? poiId = null, string language = "All")
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var localizationTask = GetListAsync<PoiLocalizationViewModel>("PoiLocalizations");
            var poiTask = GetListAsync<PoiItemViewModel>("POIs");
            await Task.WhenAll(localizationTask, poiTask);

            var items = localizationTask.Result;
            var pois = poiTask.Result.OrderBy(x => x.Name).ToList();
            var languages = items.Select(x => x.Language).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList();

            var filtered = items.Where(x =>
                (string.IsNullOrWhiteSpace(search)
                    || x.LocalizedName.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.LocalizedDescription.Contains(search, StringComparison.OrdinalIgnoreCase))
                && (!poiId.HasValue || x.PoiId == poiId.Value)
                && (language == "All" || string.Equals(x.Language, language, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(x => x.Id);

            ViewBag.Pois = pois;
            ViewBag.Languages = languages;
            ViewBag.SelectedPoiId = poiId;
            ViewBag.SelectedLanguage = language;
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

            ViewBag.Pois = await GetListAsync<PoiItemViewModel>("POIs");
            return View(new PoiLocalizationEditViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(PoiLocalizationEditViewModel model)
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Pois = await GetListAsync<PoiItemViewModel>("POIs");
                return View(model);
            }

            var result = await SendAsync(HttpMethod.Post, "PoiLocalizations", model);
            if (!result.Success)
            {
                ViewBag.Error = $"Thêm bản dịch thất bại: {result.Error}";
                ViewBag.Pois = await GetListAsync<PoiItemViewModel>("POIs");
                return View(model);
            }

            SetAlert("success", "Thêm bản dịch thành công.");
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

            var item = await GetItemAsync<PoiLocalizationEditViewModel>($"PoiLocalizations/{id}");
            if (item == null)
            {
                SetAlert("danger", "Không tìm thấy bản dịch.");
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Pois = await GetListAsync<PoiItemViewModel>("POIs");
            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, PoiLocalizationEditViewModel model)
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Pois = await GetListAsync<PoiItemViewModel>("POIs");
                return View(model);
            }

            model.Id = id;
            var result = await SendAsync(HttpMethod.Put, $"PoiLocalizations/{id}", model);
            if (!result.Success)
            {
                ViewBag.Error = $"Cập nhật bản dịch thất bại: {result.Error}";
                ViewBag.Pois = await GetListAsync<PoiItemViewModel>("POIs");
                return View(model);
            }

            SetAlert("success", "Cập nhật bản dịch thành công.");
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

            var result = await DeleteAsync($"PoiLocalizations/{id}");
            SetAlert(result.Success ? "success" : "danger", result.Success ? "Xóa bản dịch thành công." : $"Xóa bản dịch thất bại: {result.Error}");
            return RedirectToAction(nameof(Index));
        }
    }
}
