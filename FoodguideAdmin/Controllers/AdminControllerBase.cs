using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FoodGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodGuideAdmin.Controllers
{
    public abstract class AdminControllerBase : Controller
    {
        protected const string ApiBaseUrl = "http://localhost:7183/api/";

        protected static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        protected IActionResult? EnsureAuthenticated()
        {
            if (!HttpContext.Session.GetInt32("userId").HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            return null;
        }

        protected HttpClient CreateApiClient()
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(ApiBaseUrl)
            };

            var token = HttpContext.Session.GetString("token");
            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        protected async Task<List<T>> GetListAsync<T>(string endpoint)
        {
            using var client = CreateApiClient();
            var response = await client.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                return new List<T>();
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
        }

        protected async Task<T?> GetItemAsync<T>(string endpoint)
        {
            using var client = CreateApiClient();
            var response = await client.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }

        protected async Task<(bool Success, string Error)> SendAsync<T>(HttpMethod method, string endpoint, T payload)
        {
            using var client = CreateApiClient();
            using var request = new HttpRequestMessage(method, endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode
                ? (true, string.Empty)
                : (false, string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)response.StatusCode}" : body);
        }

        protected async Task<(bool Success, string Error)> SendWithoutBodyAsync(HttpMethod method, string endpoint)
        {
            using var client = CreateApiClient();
            using var request = new HttpRequestMessage(method, endpoint);
            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode
                ? (true, string.Empty)
                : (false, string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)response.StatusCode}" : body);
        }

        protected async Task<(bool Success, string Error)> DeleteAsync(string endpoint)
        {
            using var client = CreateApiClient();
            var response = await client.DeleteAsync(endpoint);
            var body = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode
                ? (true, string.Empty)
                : (false, string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)response.StatusCode}" : body);
        }

        protected PagedResult<T> BuildPagedResult<T>(IEnumerable<T> source, int page, int pageSize, string search = "")
        {
            var items = source.ToList();
            page = page < 1 ? 1 : page;
            pageSize = pageSize <= 0 ? 5 : pageSize;

            return new PagedResult<T>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = items.Count,
                Search = search,
                Items = items.Skip((page - 1) * pageSize).Take(pageSize).ToList()
            };
        }

        protected AdminSessionUser? GetCurrentUser()
        {
            var json = HttpContext.Session.GetString("currentUser");
            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<AdminSessionUser>(json, JsonOptions);
        }

        protected void SaveCurrentUser(AdminSessionUser user)
        {
            HttpContext.Session.SetInt32("userId", user.Id);
            HttpContext.Session.SetString("currentUser", JsonSerializer.Serialize(user));
        }

        protected void SetAlert(string type, string message)
        {
            TempData["AlertType"] = type;
            TempData["AlertMessage"] = message;
        }
    }
}
