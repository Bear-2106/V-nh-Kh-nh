using System.Net.Http.Json;
using DoAnVinhKhanh.Models;

namespace DoAnVinhKhanh
{
    public class ApiService
    {
        private readonly HttpClientHandler _handler;

        public ApiService()
        {
            _handler = new HttpClientHandler();
            _handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        }

        private static IReadOnlyList<string> GetBaseUrls()
        {
            return new[]
            {
                "http://foodguidekhanh.somee.com/"
            };
        }

        private HttpClient CreateClient(string baseUrl)
        {
            return new HttpClient(_handler, disposeHandler: false)
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        public async Task<List<Poi>> GetPoisAsync()
        {
            var errors = new List<string>();

            foreach (var baseUrl in GetBaseUrls())
            {
                try
                {
                    using var client = CreateClient(baseUrl);
                    using var response = await client.GetAsync("api/POIs");

                    if (!response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        errors.Add($"{baseUrl} => {(int)response.StatusCode} {response.ReasonPhrase}: {TrimErrorBody(body)}");
                        continue;
                    }

                    var data = await response.Content.ReadFromJsonAsync<List<Poi>>();
                    data ??= new List<Poi>();

                    await ApplyPoiLocalizationsAsync(client, data);

                    return data;
                }
                catch (Exception ex)
                {
                    errors.Add($"{baseUrl} => {ex.Message}");
                }
            }

            throw new Exception(
                "Khong the tai danh sach POI tu API. " +
                "Hay kiem tra http://foodguidekhanh.somee.com/api/Health. " +
                $"Chi tiet: {string.Join(" | ", errors)}");
        }

        public async Task<Poi?> GetPoiByIdAsync(int id)
        {
            var errors = new List<string>();

            foreach (var baseUrl in GetBaseUrls())
            {
                try
                {
                    using var client = CreateClient(baseUrl);
                    using var response = await client.GetAsync($"api/POIs/{id}");

                    if (!response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        errors.Add($"{baseUrl} => {(int)response.StatusCode} {response.ReasonPhrase}: {TrimErrorBody(body)}");
                        continue;
                    }

                    var poi = await response.Content.ReadFromJsonAsync<Poi>();
                    if (poi != null)
                    {
                        await ApplyPoiLocalizationsAsync(client, new List<Poi> { poi });
                    }

                    return poi;
                }
                catch (Exception ex)
                {
                    errors.Add($"{baseUrl} => {ex.Message}");
                }
            }

            throw new Exception($"Khong the tai POI Id={id}. Chi tiet: {string.Join(" | ", errors)}");
        }

        private static async Task ApplyPoiLocalizationsAsync(HttpClient client, List<Poi> pois)
        {
            if (pois.Count == 0)
                return;

            List<PoiLocalization>? localizations;

            try
            {
                localizations = await client.GetFromJsonAsync<List<PoiLocalization>>("api/PoiLocalizations");
            }
            catch
            {
                ApplyBuiltinPoiTranslations(pois);
                return;
            }

            if (localizations == null || localizations.Count == 0)
            {
                ApplyBuiltinPoiTranslations(pois);
                return;
            }

            var poisById = pois.ToDictionary(p => p.Id);

            foreach (var localization in localizations)
            {
                if (!poisById.TryGetValue(localization.PoiId, out var poi))
                    continue;

                ApplyPoiLocalization(poi, localization);
            }

            ApplyBuiltinPoiTranslations(pois);
        }

        private static void ApplyPoiLocalization(Poi poi, PoiLocalization localization)
        {
            var language = NormalizeLanguage(localization.Language);

            switch (language)
            {
                case "en":
                    if (!string.IsNullOrWhiteSpace(localization.LocalizedDescription))
                        poi.DescriptionEn = localization.LocalizedDescription;
                    if (!string.IsNullOrWhiteSpace(localization.TtsText))
                        poi.TtsTextEn = localization.TtsText;
                    break;
                case "zh":
                    if (!string.IsNullOrWhiteSpace(localization.LocalizedDescription))
                        poi.DescriptionZh = localization.LocalizedDescription;
                    if (!string.IsNullOrWhiteSpace(localization.TtsText))
                        poi.TtsTextZh = localization.TtsText;
                    break;
                case "fr":
                    if (!string.IsNullOrWhiteSpace(localization.LocalizedDescription))
                        poi.DescriptionFr = localization.LocalizedDescription;
                    if (!string.IsNullOrWhiteSpace(localization.TtsText))
                        poi.TtsTextFr = localization.TtsText;
                    break;
                case "ru":
                    if (!string.IsNullOrWhiteSpace(localization.LocalizedDescription))
                        poi.DescriptionRu = localization.LocalizedDescription;
                    if (!string.IsNullOrWhiteSpace(localization.TtsText))
                        poi.TtsTextRu = localization.TtsText;
                    break;
            }
        }

        private static void ApplyBuiltinPoiTranslations(List<Poi> pois)
        {
            foreach (var poi in pois)
            {
                var descriptionKey = NormalizeLookupText(poi.Description);
                if (DescriptionFallbacks.TryGetValue(descriptionKey, out var descriptionFallback))
                {
                    poi.DescriptionEn = UseExistingOrFallback(poi.DescriptionEn, descriptionFallback.En);
                    poi.DescriptionZh = UseExistingOrFallback(poi.DescriptionZh, descriptionFallback.Zh);
                    poi.DescriptionFr = UseExistingOrFallback(poi.DescriptionFr, descriptionFallback.Fr);
                    poi.DescriptionRu = UseExistingOrFallback(poi.DescriptionRu, descriptionFallback.Ru);
                }
            }
        }

        private static string UseExistingOrFallback(string existingText, string fallbackText)
        {
            return string.IsNullOrWhiteSpace(existingText)
                ? fallbackText
                : existingText;
        }

        private static string NormalizeLookupText(string? value)
        {
            var text = (value ?? string.Empty).Trim();

            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }

            return text;
        }

        private sealed record TranslationFallback(string En, string Zh, string Fr, string Ru);

        private static readonly Dictionary<string, TranslationFallback> DescriptionFallbacks = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Bún đậu chuẩn vị Hà Nội"] = new(
                "Authentic Hanoi-style bun dau mam tom",
                "正宗河内风味炸豆腐米线配虾酱",
                "Bun dau mam tom authentique au style de Hanoï",
                "Бун дау с мам том в аутентичном ханойском стиле"),
            ["Cơm sườn chuẩn vị"] = new(
                "Authentic broken rice with grilled pork chop",
                "正宗烤猪排碎米饭",
                "Riz brisé authentique au porc grillé",
                "Аутентичный ком там со свиной отбивной"),
            ["Bánh mì ngon nổi tiếng"] = new(
                "Famous delicious banh mi",
                "有名的美味越南法棍",
                "Banh mi réputé et savoureux",
                "Известный вкусный бань ми"),
            ["Phở bò truyền thống"] = new(
                "Traditional beef pho",
                "传统牛肉河粉",
                "Pho au bœuf traditionnel",
                "Традиционный фо с говядиной"),
            ["Bún bò Huế chuẩn vị"] = new(
                "Authentic Hue-style spicy beef noodle soup",
                "正宗顺化牛肉粉",
                "Soupe de nouilles au bœuf épicée authentique de Hué",
                "Аутентичный острый суп бун бо Хюэ"),
            ["Update thử"] = new(
                "Test update",
                "测试更新",
                "Mise à jour de test",
                "Тестовое обновление")
        };


        private static string NormalizeLanguage(string? language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return "vi";

            var normalized = language.Trim().ToLowerInvariant();

            if (normalized.StartsWith("en"))
                return "en";

            if (normalized.StartsWith("zh") || normalized.StartsWith("cn"))
                return "zh";

            if (normalized.StartsWith("fr"))
                return "fr";

            if (normalized.StartsWith("ru"))
                return "ru";

            return "vi";
        }

        private static string TrimErrorBody(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return "Khong co noi dung loi";

            var oneLine = body.Replace("\r", " ").Replace("\n", " ").Trim();
            return oneLine.Length <= 500 ? oneLine : oneLine[..500] + "...";
        }
    }
}
