using System.Net.Http.Json;
using DoAnVinhKhanh.Models;
using Microsoft.Maui.Devices;

namespace DoAnVinhKhanh
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private const string BackendScheme = "https";
        private const int BackendPort = 7183;

        public ApiService()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(GetBaseUrl()),
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        private static string GetBaseUrl()
        {
            var host = DeviceInfo.Current.Platform == DevicePlatform.Android
                ? "10.0.2.2"
                : "localhost";

            return $"{BackendScheme}://{host}:{BackendPort}/";
        }

        public async Task<List<Poi>> GetPoisAsync()
        {
            try
            {
                var data = await _httpClient.GetFromJsonAsync<List<Poi>>("api/POIs");
                return data ?? new List<Poi>();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception(
                    $"Khong the ket noi den API {_httpClient.BaseAddress}. " +
                    "Hay kiem tra backend ASP.NET da chay dung cong 7183 chua. " +
                    $"Chi tiet: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception(
                    $"Ket noi den API {_httpClient.BaseAddress} bi timeout. " +
                    "Backend co the chua chay hoac tra loi qua cham. " +
                    $"Chi tiet: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Loi ket noi API ({_httpClient.BaseAddress}): {ex.Message}");
            }
        }

        public async Task<Poi?> GetPoiByIdAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<Poi>($"api/POIs/{id}");
        }
    }
}
