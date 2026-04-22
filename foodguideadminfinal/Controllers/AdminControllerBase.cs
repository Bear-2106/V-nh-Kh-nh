using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FoodGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FoodGuideAdmin.Controllers
{
    public abstract class AdminControllerBase : Controller
    {
        private const string DefaultApiBaseUrl = "http://localhost:5074/api/";

        protected string ApiBaseUrl
        {
            get
            {
                var configuration = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
                var configuredUrl = configuration?["ApiSettings:BaseUrl"];
                var url = string.IsNullOrWhiteSpace(configuredUrl) ? DefaultApiBaseUrl : configuredUrl;

                return url.EndsWith('/') ? url : $"{url}/";
            }
        }

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
                : (false, ExtractErrorMessage(body, (int)response.StatusCode));
        }

        protected async Task<(bool Success, string Error)> SendWithoutBodyAsync(HttpMethod method, string endpoint)
        {
            using var client = CreateApiClient();
            using var request = new HttpRequestMessage(method, endpoint);
            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode
                ? (true, string.Empty)
                : (false, ExtractErrorMessage(body, (int)response.StatusCode));
        }

        protected async Task<(bool Success, string Error)> DeleteAsync(string endpoint)
        {
            using var client = CreateApiClient();
            var response = await client.DeleteAsync(endpoint);
            var body = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode
                ? (true, string.Empty)
                : (false, ExtractErrorMessage(body, (int)response.StatusCode));
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

        private static string ExtractErrorMessage(string body, int statusCode)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return $"HTTP {statusCode}";
            }

            try
            {
                using var document = JsonDocument.Parse(body);
                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var key in new[] { "message", "error", "title" })
                    {
                        if (document.RootElement.TryGetProperty(key, out var property)
                            && property.ValueKind == JsonValueKind.String)
                        {
                            var value = property.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                return value;
                            }
                        }
                    }
                }
            }
            catch (JsonException)
            {
            }

            return body;
        }
    }
}
