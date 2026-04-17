using FoodGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodGuideAdmin.Controllers
{
    public class PoiController : AdminControllerBase
    {
        public async Task<IActionResult> Index(int page = 1, int pageSize = 5, string search = "", string category = "All")
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var pois = await GetListAsync<PoiItemViewModel>("POIs");
            var categories = pois
                .Select(x => string.IsNullOrWhiteSpace(x.Category) ? "Chưa phân loại" : x.Category.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            var filtered = pois.Where(x =>
                (string.IsNullOrWhiteSpace(search)
                    || x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.Address.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || x.Description.Contains(search, StringComparison.OrdinalIgnoreCase))
                && (category == "All" || string.Equals(x.Category, category, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(x => x.Id);

            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = category;
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

            return View(new PoiEditViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(PoiEditViewModel model)
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

            var result = await SendAsync(HttpMethod.Post, "POIs", model);
            if (!result.Success)
            {
                ViewBag.Error = $"Thêm POI thất bại: {result.Error}";
                return View(model);
            }

            SetAlert("success", "Thêm POI thành công.");
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

            var poi = await GetItemAsync<PoiEditViewModel>($"POIs/{id}");
            if (poi == null)
            {
                SetAlert("danger", "Không tìm thấy POI.");
                return RedirectToAction(nameof(Index));
            }

            return View(poi);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, PoiEditViewModel model)
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
            var result = await SendAsync(HttpMethod.Put, $"POIs/{id}", model);
            if (!result.Success)
            {
                ViewBag.Error = $"Cập nhật POI thất bại: {result.Error}";
                return View(model);
            }

            SetAlert("success", "Cập nhật POI thành công.");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var poiTask = GetItemAsync<PoiItemViewModel>($"POIs/{id}");
            var foodsTask = GetListAsync<FoodItemViewModel>($"POIs/{id}/foods");
            var localizationTask = GetListAsync<PoiLocalizationViewModel>($"PoiLocalizations/poi/{id}");

            await Task.WhenAll(poiTask, foodsTask, localizationTask);

            if (poiTask.Result == null)
            {
                SetAlert("danger", "Không tìm thấy POI.");
                return RedirectToAction(nameof(Index));
            }

            return View(new PoiDetailsViewModel
            {
                Poi = poiTask.Result!,
                Foods = foodsTask.Result,
                Localizations = localizationTask.Result
            });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var result = await DeleteAsync($"POIs/{id}");
            SetAlert(result.Success ? "success" : "danger", result.Success ? "Xóa POI thành công." : $"Xóa POI thất bại: {result.Error}");
            return RedirectToAction(nameof(Index));
        }
    }
}
