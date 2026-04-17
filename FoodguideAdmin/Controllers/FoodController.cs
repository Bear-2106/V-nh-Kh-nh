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

            ViewBag.Pois = await GetListAsync<PoiItemViewModel>("POIs");
            return View(new FoodEditViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(FoodEditViewModel model)
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

            var result = await SendAsync(HttpMethod.Post, "FoodItems", model);
            if (!result.Success)
            {
                ViewBag.Error = $"Thêm món ăn thất bại: {result.Error}";
                ViewBag.Pois = await GetListAsync<PoiItemViewModel>("POIs");
                return View(model);
            }

            SetAlert("success", "Thêm món ăn thành công.");
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

            ViewBag.Pois = await GetListAsync<PoiItemViewModel>("POIs");
            return View(food);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, FoodEditViewModel model)
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
            var result = await SendAsync(HttpMethod.Put, $"FoodItems/{id}", model);
            if (!result.Success)
            {
                ViewBag.Error = $"Cập nhật món ăn thất bại: {result.Error}";
                ViewBag.Pois = await GetListAsync<PoiItemViewModel>("POIs");
                return View(model);
            }

            SetAlert("success", "Cập nhật món ăn thành công.");
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
            SetAlert(result.Success ? "success" : "danger", result.Success ? "Xóa món ăn thành công." : $"Xóa món ăn thất bại: {result.Error}");
            return RedirectToAction(nameof(Index));
        }
    }
}
