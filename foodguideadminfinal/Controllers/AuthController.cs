using System.Text;
using System.Text.Json;
using FoodGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;

namespace FoodGuideAdmin.Controllers
{
    public class AuthController : AdminControllerBase
    {
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("userId").HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ tài khoản và mật khẩu.";
                return View();
            }

            using var client = CreateApiClient();
            var payload = new { username, password };
            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("Auth/login", content);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Sai tài khoản, mật khẩu hoặc không đủ quyền vào admin.";
                return View();
            }

            var loginResponse = JsonSerializer.Deserialize<LoginResponse>(json, JsonOptions);
            if (loginResponse?.User == null)
            {
                ViewBag.Error = "Không đọc được thông tin người dùng từ API.";
                return View();
            }

            if (!string.Equals(loginResponse.User.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                HttpContext.Session.Clear();
                ViewBag.Error = "Chỉ tài khoản Admin mới được đăng nhập vào trang quản trị.";
                return View();
            }

            var sessionUser = new AdminSessionUser
            {
                Id = loginResponse.User.Id,
                Username = loginResponse.User.Username,
                FullName = loginResponse.User.FullName,
                Role = loginResponse.User.Role
            };

            SaveCurrentUser(sessionUser);
            HttpContext.Session.SetString("token", loginResponse.AccessToken ?? $"admin-session-{sessionUser.Id}");

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        private sealed class LoginResponse
        {
            public string? AccessToken { get; set; }
            public AdminUserDto? User { get; set; }
        }

        private sealed class AdminUserDto
        {
            public int Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }
    }
}
