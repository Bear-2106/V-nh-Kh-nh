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
                if (TryGetFallbackForPoi(poi, out var descriptionFallback))
                {
                    poi.DescriptionEn = UseExistingOrFallback(poi.DescriptionEn, descriptionFallback.En);
                    poi.DescriptionZh = UseExistingOrFallback(poi.DescriptionZh, descriptionFallback.Zh);
                    poi.DescriptionFr = UseExistingOrFallback(poi.DescriptionFr, descriptionFallback.Fr);
                    poi.DescriptionRu = UseExistingOrFallback(poi.DescriptionRu, descriptionFallback.Ru);

                    poi.TtsTextEn = UseExistingOrFallback(poi.TtsTextEn, descriptionFallback.En);
                    poi.TtsTextZh = UseExistingOrFallback(poi.TtsTextZh, descriptionFallback.Zh);
                    poi.TtsTextFr = UseExistingOrFallback(poi.TtsTextFr, descriptionFallback.Fr);
                    poi.TtsTextRu = UseExistingOrFallback(poi.TtsTextRu, descriptionFallback.Ru);
                }
            }
        }

        private static bool TryGetFallbackForPoi(Poi poi, out TranslationFallback fallback)
        {
            var nameKey = NormalizeLookupText(poi.Name);
            if (PoiNameFallbacks.TryGetValue(nameKey, out fallback!))
                return true;

            var descriptionKey = NormalizeLookupText(poi.Description);
            return DescriptionFallbacks.TryGetValue(descriptionKey, out fallback!);
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

        private static readonly Dictionary<string, TranslationFallback> PoiNameFallbacks = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Bún Bò Huế Đông Ba"] = new(
                "Ingredients: thick rice noodles, beef, pork knuckle, crab cake, congealed blood, lemongrass, Hue shrimp paste, chili oil, and fresh herbs. Preparation: beef bones and pork knuckle are simmered for the broth, seasoned with shrimp paste and lemongrass, then served over noodles with toppings.",
                "配料：粗米粉、牛肉、猪脚、蟹肉丸、猪血、香茅、顺化虾酱、辣油和新鲜香草。做法：用牛骨和猪脚熬汤，加入虾酱和香茅调味，再浇在米粉和配料上。",
                "Ingrédients : grosses nouilles de riz, boeuf, jarret de porc, galette de crabe, sang cuit, citronnelle, pâte de crevettes de Hué, huile pimentée et herbes fraîches. Préparation : le bouillon est mijoté avec des os de boeuf et du jarret de porc, parfumé à la citronnelle, puis versé sur les nouilles.",
                "Ингредиенты: толстая рисовая лапша, говядина, свиная рулька, крабовая котлета, свернувшаяся кровь, лемонграсс, паста из креветок Хюэ, острое масло и зелень. Приготовление: бульон варят на говяжьих костях и рульке, приправляют пастой и лемонграссом, затем подают с лапшой."),
            ["Bánh Mì Huỳnh Hoa"] = new(
                "Ingredients: crispy baguette, pate, butter, Vietnamese pork roll, cold cuts, ham, pork floss, pickled vegetables, cucumber, cilantro, chili, and sauce. Preparation: the bread is warmed, spread with pate and butter, then filled with meats, vegetables, and sauce.",
                "配料：酥脆法棍、肝酱、黄油、越南扎肉、冷切肉、火腿、肉松、酸萝卜、黄瓜、香菜、辣椒和酱汁。做法：面包烤热后抹上肝酱和黄油，再夹入肉类、蔬菜和酱汁。",
                "Ingrédients : baguette croustillante, pâté, beurre, mortadelle vietnamienne, charcuterie, jambon, porc effiloché, légumes marinés, concombre, coriandre, piment et sauce. Préparation : le pain est réchauffé, garni de pâté et de beurre, puis rempli de viandes, légumes et sauce.",
                "Ингредиенты: хрустящий багет, паштет, масло, вьетнамская свиная колбаса, мясная нарезка, ветчина, мясная стружка, маринованные овощи, огурец, кинза, перец чили и соус. Приготовление: хлеб разогревают, намазывают паштетом и маслом, затем добавляют мясо, овощи и соус."),
            ["Bánh Mì Huynh Hoa"] = new(
                "Ingredients: crispy baguette, pate, butter, Vietnamese pork roll, cold cuts, ham, pork floss, pickled vegetables, cucumber, cilantro, chili, and sauce. Preparation: the bread is warmed, spread with pate and butter, then filled with meats, vegetables, and sauce.",
                "配料：酥脆法棍、肝酱、黄油、越南扎肉、冷切肉、火腿、肉松、酸萝卜、黄瓜、香菜、辣椒和酱汁。做法：面包烤热后抹上肝酱和黄油，再夹入肉类、蔬菜和酱汁。",
                "Ingrédients : baguette croustillante, pâté, beurre, mortadelle vietnamienne, charcuterie, jambon, porc effiloché, légumes marinés, concombre, coriandre, piment et sauce. Préparation : le pain est réchauffé, garni de pâté et de beurre, puis rempli de viandes, légumes et sauce.",
                "Ингредиенты: хрустящий багет, паштет, масло, вьетнамская свиная колбаса, мясная нарезка, ветчина, мясная стружка, маринованные овощи, огурец, кинза, перец чили и соус. Приготовление: хлеб разогревают, намазывают паштетом и маслом, затем добавляют мясо, овощи и соус."),
            ["Cơm Tấm Ba Ghiền"] = new(
                "Ingredients: broken rice, pork chop, shredded pork skin, steamed egg meatloaf, fried egg, scallion oil, pickles, and sweet fish sauce. Preparation: the pork chop is marinated and grilled over charcoal, then served with steamed broken rice and side dishes.",
                "配料：碎米、猪排、猪皮丝、蒸蛋肉饼、煎蛋、葱油、腌菜和甜鱼露。做法：猪排腌制后用炭火烤香，再配碎米和配菜一起食用。",
                "Ingrédients : riz brisé, côtelette de porc, couenne de porc émincée, pain de viande aux oeufs, oeuf au plat, huile d'oignon vert, légumes marinés et sauce de poisson sucrée. Préparation : la côtelette est marinée puis grillée au charbon, servie avec le riz brisé.",
                "Ингредиенты: дробленый рис, свиная отбивная, свиная кожа, яичный мясной рулет, жареное яйцо, зеленый лук в масле, маринованные овощи и сладкий рыбный соус. Приготовление: свинину маринуют и жарят на углях, затем подают с рисом и гарнирами."),
            ["Phở Hòa Pasteur"] = new(
                "Ingredients: rice noodles, beef, beef bones, onion, ginger, cinnamon, star anise, cardamom, scallions, herbs, bean sprouts, and lime. Preparation: beef bones are simmered with aromatics for the broth, then hot noodles and beef are served with the broth.",
                "配料：河粉、牛肉、牛骨、洋葱、姜、桂皮、八角、草果、葱、香草、豆芽和青柠。做法：牛骨与香料长时间熬汤，再把热河粉和牛肉配上汤一起食用。",
                "Ingrédients : nouilles de riz, boeuf, os de boeuf, oignon, gingembre, cannelle, anis étoilé, cardamome, ciboule, herbes, pousses de soja et citron vert. Préparation : les os mijotent avec les épices pour le bouillon, servi avec les nouilles et le boeuf.",
                "Ингредиенты: рисовая лапша, говядина, говяжьи кости, лук, имбирь, корица, бадьян, кардамон, зеленый лук, травы, ростки фасоли и лайм. Приготовление: кости долго варят со специями, затем бульон подают с лапшой и говядиной."),
            ["Bún Đậu Cô Khàn"] = new(
                "Ingredients: pressed rice vermicelli, fried tofu, boiled pork, green rice pork cake, fried spring rolls, fresh herbs, cucumber, and fermented shrimp paste. Preparation: tofu and rolls are fried hot, pork is sliced, then everything is served with vermicelli and seasoned shrimp paste.",
                "配料：米粉块、炸豆腐、白切猪肉、青米猪肉饼、炸春卷、新鲜香草、黄瓜和虾酱。做法：豆腐和春卷炸热，猪肉切片，与米粉和调好的虾酱一起食用。",
                "Ingrédients : vermicelles de riz pressés, tofu frit, porc bouilli, galette de porc au jeune riz, nems frits, herbes fraîches, concombre et pâte de crevettes fermentée. Préparation : le tofu et les nems sont frits, le porc est tranché, puis le tout est servi avec la pâte de crevettes.",
                "Ингредиенты: прессованная рисовая вермишель, жареный тофу, отварная свинина, свиная котлета с зеленым рисом, жареные роллы, зелень, огурец и ферментированная креветочная паста. Приготовление: тофу и роллы обжаривают, свинину нарезают и подают с лапшой и пастой."),
        };

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
