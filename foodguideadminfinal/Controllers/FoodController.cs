using System.Globalization;
using FoodGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodGuideAdmin.Controllers
{
    public class FoodController : AdminControllerBase
    {
        public async Task<IActionResult> Index(int page = 1, int pageSize = 5, string search = "", int? poiId = null)
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var foodTask = GetListAsync<FoodItemViewModel>("FoodItems");
            var poiTask = GetListAsync<PoiItemViewModel>("POIs");
            await Task.WhenAll(foodTask, poiTask);

            var foods = foodTask.Result;
            var pois = poiTask.Result.OrderBy(x => x.Name).ToList();

            var filtered = foods.Where(x =>
                    (string.IsNullOrWhiteSpace(search)
                     || x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                     || x.Description.Contains(search, StringComparison.OrdinalIgnoreCase))
                    && (!poiId.HasValue || x.PoiId == poiId.Value))
                .OrderByDescending(x => x.Id);

            ViewBag.Pois = pois;
            ViewBag.SelectedPoiId = poiId;
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

            var pois = (await GetListAsync<PoiItemViewModel>("POIs")).OrderBy(x => x.Name).ToList();
            if (!pois.Any())
            {
                SetAlert("warning", "Bạn cần thêm quán ăn trước khi thêm món ăn.");
                return RedirectToAction("Create", "Poi");
            }

            ViewBag.Pois = pois;
            return View(new FoodEditViewModel { IsAvailable = true, Price = 0 });
        }

        [HttpPost]
        public async Task<IActionResult> Create(IFormCollection form)
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var pois = (await GetListAsync<PoiItemViewModel>("POIs")).OrderBy(x => x.Name).ToList();
            ViewBag.Pois = pois;

            var model = BuildFoodModel(form);
            if (!model.PoiId.HasValue)
            {
                ViewBag.Error = "Vui lòng chọn quán ăn.";
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ViewBag.Error = "Vui lòng nhập tên món ăn.";
                return View(model);
            }

            var result = await SendAsync(HttpMethod.Post, "FoodItems", new
            {
                model.Id,
                PoiId = model.PoiId.Value,
                model.Name,
                model.Description,
                Price = model.Price ?? 0,
                model.ImageUrl,
                model.IsAvailable
            });

            if (!result.Success)
            {
                ViewBag.Error = $"Thêm món ăn thất bại: {result.Error}";
                return View(model);
            }

            SetAlert("success", $"Thêm món ăn \"{model.Name}\" thành công.");
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

            var food = await GetItemAsync<FoodEditViewModel>($"FoodItems/{id}");
            if (food == null)
            {
                SetAlert("danger", "Không tìm thấy món ăn.");
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Pois = (await GetListAsync<PoiItemViewModel>("POIs")).OrderBy(x => x.Name).ToList();
            return View(food);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, IFormCollection form)
        {
            var redirect = EnsureAuthenticated();
            if (redirect != null)
            {
                return redirect;
            }

            var pois = (await GetListAsync<PoiItemViewModel>("POIs")).OrderBy(x => x.Name).ToList();
            ViewBag.Pois = pois;

            var model = BuildFoodModel(form);
            model.Id = id;

            if (!model.PoiId.HasValue)
            {
                ViewBag.Error = "Vui lòng chọn quán ăn.";
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ViewBag.Error = "Vui lòng nhập tên món ăn.";
                return View(model);
            }

            var result = await SendAsync(HttpMethod.Put, $"FoodItems/{id}", new
            {
                model.Id,
                PoiId = model.PoiId.Value,
                model.Name,
                model.Description,
                Price = model.Price ?? 0,
                model.ImageUrl,
                model.IsAvailable
            });

            if (!result.Success)
            {
                ViewBag.Error = $"Cập nhật món ăn thất bại: {result.Error}";
                return View(model);
            }

            SetAlert("success", $"Cập nhật món ăn \"{model.Name}\" thành công.");
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

            var result = await DeleteAsync($"FoodItems/{id}");
            SetAlert(result.Success ? "success" : "danger",
                result.Success ? "Xóa món ăn thành công." : $"Xóa món ăn thất bại: {result.Error}");
            return RedirectToAction(nameof(Index));
        }

        private static FoodEditViewModel BuildFoodModel(IFormCollection form)
        {
            return new FoodEditViewModel
            {
                PoiId = ParseNullableInt(form["PoiId"]),
                Name = form["Name"].ToString().Trim(),
                Description = form["Description"].ToString().Trim(),
                Price = ParseDouble(form["Price"], 0),
                ImageUrl = form["ImageUrl"].ToString().Trim(),
                IsAvailable = IsChecked(form, "IsAvailable")
            };
        }

        private static int? ParseNullableInt(string rawValue)
        {
            var value = rawValue?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return int.TryParse(value, out var parsed) ? parsed : null;
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
