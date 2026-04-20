using System.Globalization;
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

            return View(new PoiEditViewModel { IsActive = true, Radius = 50 });
        }

        [HttpPost]
        public async Task<IActionResult> Create(IFormCollection form)
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var model = BuildPoiModel(form);
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ViewBag.Error = "Vui lòng nhập tên quán.";
                return View(model);
            }

            var result = await SendAsync(HttpMethod.Post, "POIs", new
            {
                model.Id,
                model.Name,
                model.Description,
                model.Address,
                Latitude = model.Latitude ?? 0,
                Longitude = model.Longitude ?? 0,
                Radius = model.Radius ?? 50,
                model.AudioUrl,
                model.ImageUrl,
                model.Category,
                model.IsActive
            });

            if (!result.Success)
            {
                ViewBag.Error = $"Thêm quán thất bại: {result.Error}";
                return View(model);
            }

            SetAlert("success", $"Thêm quán \"{model.Name}\" thành công.");
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
                SetAlert("danger", "Không tìm thấy quán.");
                return RedirectToAction(nameof(Index));
            }

            return View(poi);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, IFormCollection form)
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var model = BuildPoiModel(form);
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ViewBag.Error = "Vui lòng nhập tên quán.";
                return View(model);
            }

            model.Id = id;

            var result = await SendAsync(HttpMethod.Put, $"POIs/{id}", new
            {
                model.Id,
                model.Name,
                model.Description,
                model.Address,
                Latitude = model.Latitude ?? 0,
                Longitude = model.Longitude ?? 0,
                Radius = model.Radius ?? 50,
                model.AudioUrl,
                model.ImageUrl,
                model.Category,
                model.IsActive
            });

            if (!result.Success)
            {
                ViewBag.Error = $"Cập nhật quán thất bại: {result.Error}";
                return View(model);
            }

            SetAlert("success", $"Cập nhật quán \"{model.Name}\" thành công.");
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
            var monitoringTask = GetItemAsync<PoiMonitoringSummaryViewModel>($"VisitorMonitoring/poi/{id}");

            await Task.WhenAll(poiTask, foodsTask, monitoringTask);

            if (poiTask.Result == null)
            {
                SetAlert("danger", "Không tìm thấy quán.");
                return RedirectToAction(nameof(Index));
            }

            return View(new PoiDetailsViewModel
            {
                Poi = poiTask.Result!,
                Foods = foodsTask.Result,
                CurrentVisitors = monitoringTask.Result?.CurrentVisitors ?? 0
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
            SetAlert(result.Success ? "success" : "danger",
                result.Success ? "Xóa quán thành công." : $"Xóa quán thất bại: {result.Error}");
            return RedirectToAction(nameof(Index));
        }

        private static PoiEditViewModel BuildPoiModel(IFormCollection form)
        {
            return new PoiEditViewModel
            {
                Name = form["Name"].ToString().Trim(),
                Category = form["Category"].ToString().Trim(),
                Description = form["Description"].ToString().Trim(),
                Address = form["Address"].ToString().Trim(),
                Latitude = ParseDouble(form["Latitude"], 0),
                Longitude = ParseDouble(form["Longitude"], 0),
                Radius = ParseDouble(form["Radius"], 50),
                AudioUrl = form["AudioUrl"].ToString().Trim(),
                ImageUrl = form["ImageUrl"].ToString().Trim(),
                IsActive = IsChecked(form, "IsActive")
            };
        }

        private static double ParseDouble(string rawValue, double defaultValue)
        {
            var value = rawValue?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var invariant))
            {
                return invariant;
            }

            if (double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out var current))
            {
                return current;
            }

            return defaultValue;
        }

        private static bool IsChecked(IFormCollection form, string key)
        {
            var values = form[key];
            return values.Any(v => string.Equals(v, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(v, "on", StringComparison.OrdinalIgnoreCase));
        }
    }
}
