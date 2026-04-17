using DoAnVinhKhanh.Models;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Media;
using System.Globalization;
using System.Text;
using System.Threading;

namespace DoAnVinhKhanh;

public partial class MainPage : ContentPage
{
    private readonly ApiService _apiService;
    private List<Poi> _allPois = new();
    private readonly HashSet<int> _activeGeofencePoiIds = new();
    private bool _isNavigating;
    private Location? _currentLocation;

    private const int DefaultMapZoom = 14;
    private const double UserGeofenceRadiusMeters = 150;

    private List<int> _lastDisplayedPoiOrder = new();

    private bool _isSpeaking;
    private readonly List<Poi> _ttsQueue = new();
    private readonly HashSet<int> _queuedPoiIds = new();
    private readonly HashSet<int> _spokenPoiIds = new();
    private CancellationTokenSource? _ttsCts;
    private readonly SemaphoreSlim _ttsGate = new(1, 1);
    private int _manualTtsRequestVersion;

    private string _currentLanguage = "vi";
    private int? _currentSpeakingPoiId = null;
    private string _searchText = "";

    public MainPage(ApiService apiService)
    {
        _apiService = apiService;
        InitializeComponent();
        UpdateSearchPlaceholder();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        StopGpsListening();
        StopCurrentSpeech();
        HideMiniTtsBar();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (permission != PermissionStatus.Granted)
            {
                await DisplayAlert("Lỗi", "Chưa cấp quyền vị trí", "OK");
                return;
            }

            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10)));

            if (location == null)
                return;

            _allPois = await _apiService.GetPoisAsync();

            if (_allPois == null || _allPois.Count == 0)
                return;

            DrawMap(location, _allPois);
            await HandleLocationUpdatedAsync(location);
            await StartGpsListeningAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi Kết Nối", ex.Message, "OK");
        }
    }

    private async Task StartGpsListeningAsync()
    {
        try
        {
            if (Geolocation.Default.IsListeningForeground)
                return;

            Geolocation.Default.LocationChanged -= OnLocationChanged;
            Geolocation.Default.ListeningFailed -= OnLocationListeningFailed;

            Geolocation.Default.LocationChanged += OnLocationChanged;
            Geolocation.Default.ListeningFailed += OnLocationListeningFailed;

            var request = new GeolocationListeningRequest(GeolocationAccuracy.Best);
            var started = await Geolocation.Default.StartListeningForegroundAsync(request);

            if (!started)
            {
                await DisplayAlert("Thông báo", "Không thể bắt đầu theo dõi vị trí.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi GPS", ex.Message, "OK");
        }
    }

    private void StopGpsListening()
    {
        try
        {
            Geolocation.Default.LocationChanged -= OnLocationChanged;
            Geolocation.Default.ListeningFailed -= OnLocationListeningFailed;

            if (Geolocation.Default.IsListeningForeground)
                Geolocation.Default.StopListeningForeground();
        }
        catch
        {
        }
    }

    private async void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        if (e?.Location == null)
            return;

        await HandleLocationUpdatedAsync(e.Location);
    }

    private async void OnLocationListeningFailed(object? sender, GeolocationListeningFailedEventArgs e)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await DisplayAlert("Lỗi GPS", $"Theo dõi vị trí thất bại: {e.Error}", "OK");
        });
    }

    private async Task HandleLocationUpdatedAsync(Location location)
    {
        if (location == null || _allPois == null || _allPois.Count == 0)
            return;

        _currentLocation = location;

        UpdatePoiDistances(location);

        var sortedPois = _allPois.OrderBy(p => p.Distance).ToList();
        UpdatePoiDisplayTexts(sortedPois);
        var visiblePois = GetVisiblePois(sortedPois);
        var orderChanged = HasPoiOrderChanged(visiblePois, _lastDisplayedPoiOrder);

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (orderChanged)
            {
                PoiCollectionView.ItemsSource = visiblePois;
                _lastDisplayedPoiOrder = visiblePois.Select(p => p.Id).ToList();
            }

            await UpdateUserLocationOnMap(location);
        });

        await CheckGeofencesAsync(location);
        await UpdateAutoTtsQueueAsync(location);
    }

    private void UpdatePoiDistances(Location userLocation)
    {
        foreach (var poi in _allPois)
        {
            poi.Distance = Location.CalculateDistance(
                userLocation.Latitude, userLocation.Longitude,
                poi.Latitude, poi.Longitude,
                DistanceUnits.Kilometers);
        }
    }

    private void UpdatePoiDisplayTexts(IEnumerable<Poi> pois)
    {
        foreach (var poi in pois)
        {
            poi.DisplayName = GetLocalizedPoiName(poi);
            poi.DisplayDescription = GetLocalizedPoiDescription(poi);
            poi.DisplayDistanceText = GetLocalizedDistanceText(poi.Distance);
        }
    }

    private void RefreshPoiCollectionView()
    {
        if (_allPois == null || _allPois.Count == 0)
            return;

        var sortedPois = _allPois.OrderBy(p => p.Distance).ToList();
        UpdatePoiDisplayTexts(sortedPois);
        var visiblePois = GetVisiblePois(sortedPois);

        PoiCollectionView.ItemsSource = null;
        PoiCollectionView.ItemsSource = visiblePois;
        _lastDisplayedPoiOrder = visiblePois.Select(p => p.Id).ToList();
        RefreshMap();
    }

    private List<Poi> GetVisiblePois(List<Poi> sortedPois)
    {
        if (string.IsNullOrWhiteSpace(_searchText))
            return sortedPois;

        var searchText = NormalizeSearchText(_searchText);
        return sortedPois
            .Where(p => PoiMatchesSearch(p, searchText))
            .ToList();
    }

    private bool PoiMatchesSearch(Poi poi, string searchText)
    {
        var searchableValues = new[]
        {
            poi.Name,
            poi.Description,
            poi.DisplayDescription,
            poi.DescriptionEn,
            poi.DescriptionZh,
            poi.DescriptionFr,
            poi.DescriptionRu,
            poi.Address,
            poi.Category
        };

        return searchableValues.Any(value =>
            NormalizeSearchText(value).Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeSearchText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value
            .Trim()
            .ToLowerInvariant()
            .Replace('đ', 'd')
            .Replace('Đ', 'd')
            .Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static bool HasPoiOrderChanged(List<Poi> sortedPois, List<int> previousOrder)
    {
        var currentOrder = sortedPois.Select(p => p.Id).ToList();

        if (currentOrder.Count != previousOrder.Count)
            return true;

        for (int i = 0; i < currentOrder.Count; i++)
        {
            if (currentOrder[i] != previousOrder[i])
                return true;
        }

        return false;
    }

    private async Task UpdateAutoTtsQueueAsync(Location userLocation)
    {
        var poisInUserGeofence = _allPois
            .Where(p =>
                Location.CalculateDistance(
                    userLocation.Latitude, userLocation.Longitude,
                    p.Latitude, p.Longitude,
                    DistanceUnits.Kilometers) * 1000 <= UserGeofenceRadiusMeters)
            .OrderBy(p => p.Distance)
            .ToList();

        foreach (var poi in poisInUserGeofence)
        {
            if (_spokenPoiIds.Contains(poi.Id))
                continue;

            if (_queuedPoiIds.Contains(poi.Id))
                continue;

            var textToSpeak = GetTtsTextByLanguage(poi, _currentLanguage);
            if (string.IsNullOrWhiteSpace(textToSpeak))
                continue;

            _ttsQueue.Add(poi);
            _queuedPoiIds.Add(poi.Id);
        }

        _ttsQueue.Sort((a, b) => a.Distance.CompareTo(b.Distance));

        await TrySpeakNextPoiAsync();
    }

    private async Task TrySpeakNextPoiAsync()
    {
        if (_isSpeaking)
            return;

        if (!await _ttsGate.WaitAsync(0))
            return;

        try
        {
            if (_isSpeaking)
                return;

            while (_ttsQueue.Count > 0)
            {
                var nextPoi = _ttsQueue[0];
                _ttsQueue.RemoveAt(0);
                _queuedPoiIds.Remove(nextPoi.Id);

                if (_spokenPoiIds.Contains(nextPoi.Id))
                    continue;

                var textToSpeak = GetTtsTextByLanguage(nextPoi, _currentLanguage);
                if (string.IsNullOrWhiteSpace(textToSpeak))
                    continue;

                try
                {
                    _isSpeaking = true;
                    _currentSpeakingPoiId = nextPoi.Id;

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ShowMiniTtsBar(GetLocalizedNowSpeakingText(GetLocalizedPoiName(nextPoi)));
                        RefreshMap();
                    });

                    _ttsCts?.Cancel();
                    _ttsCts?.Dispose();
                    _ttsCts = new CancellationTokenSource();

                    var options = await BuildSpeechOptionsAsync();

                    await TextToSpeech.Default.SpeakAsync(
                        textToSpeak,
                        options,
                        _ttsCts.Token);

                    _spokenPoiIds.Add(nextPoi.Id);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await DisplayAlert("Lỗi TTS", ex.Message, "OK");
                    });
                }
                finally
                {
                    _isSpeaking = false;
                    _currentSpeakingPoiId = null;
                }

                if (_ttsCts?.IsCancellationRequested == true)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        HideMiniTtsBar();
                        RefreshMap();
                    });
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HideMiniTtsBar();
                    RefreshMap();
                });
            }
        }
        finally
        {
            _ttsGate.Release();
        }
    }

    private async Task<SpeechOptions> BuildSpeechOptionsAsync()
    {
        var preferredLocale = GetPreferredLocaleCode();

        var locales = await TextToSpeech.Default.GetLocalesAsync();
        var locale =
            locales.FirstOrDefault(l =>
                string.Equals(l.Language, preferredLocale, StringComparison.OrdinalIgnoreCase)) ??
            locales.FirstOrDefault(l =>
                l.Language.StartsWith(_currentLanguage, StringComparison.OrdinalIgnoreCase)) ??
            locales.FirstOrDefault();

        return new SpeechOptions
        {
            Locale = locale,
            Pitch = 1.0f,
            Volume = 1.0f
        };
    }

    private string GetPreferredLocaleCode()
    {
        return _currentLanguage switch
        {
            "en" => "en-US",
            "zh" => "zh-CN",
            "fr" => "fr-FR",
            "ru" => "ru-RU",
            _ => "vi-VN"
        };
    }

    private string GetTtsTextByLanguage(Poi poi, string language)
    {
        return language switch
        {
            "en" => string.IsNullOrWhiteSpace(poi.TtsTextEn) ? poi.TtsTextVi : poi.TtsTextEn,
            "zh" => string.IsNullOrWhiteSpace(poi.TtsTextZh) ? poi.TtsTextVi : poi.TtsTextZh,
            "fr" => string.IsNullOrWhiteSpace(poi.TtsTextFr) ? poi.TtsTextVi : poi.TtsTextFr,
            "ru" => string.IsNullOrWhiteSpace(poi.TtsTextRu) ? poi.TtsTextVi : poi.TtsTextRu,
            _ => poi.TtsTextVi
        };
    }

    private string GetLocalizedChooseLanguageText()
    {
        return _currentLanguage switch
        {
            "en" => "Choose language",
            "zh" => "选择语言",
            "fr" => "Choisir la langue",
            "ru" => "Выбрать язык",
            _ => "Chọn ngôn ngữ"
        };
    }

    private string GetLocalizedSearchPlaceholderText()
    {
        return _currentLanguage switch
        {
            "en" => "Search for a restaurant",
            "zh" => "搜索餐馆",
            "fr" => "Rechercher un restaurant",
            "ru" => "Поиск ресторана",
            _ => "Tìm quán muốn đến"
        };
    }

    private string GetLocalizedCancelText()
    {
        return _currentLanguage switch
        {
            "en" => "Cancel",
            "zh" => "取消",
            "fr" => "Annuler",
            "ru" => "Отмена",
            _ => "Hủy"
        };
    }

    private string GetLocalizedNowSpeakingText(string poiName)
    {
        return _currentLanguage switch
        {
            "en" => $"Speaking: {poiName}",
            "zh" => $"正在朗读：{poiName}",
            "fr" => $"Lecture : {poiName}",
            "ru" => $"Озвучивается: {poiName}",
            _ => $"Đang đọc: {poiName}"
        };
    }

    private string GetLocalizedNoNarrationTitle()
    {
        return _currentLanguage switch
        {
            "en" => "Notification",
            "zh" => "通知",
            "fr" => "Information",
            "ru" => "Уведомление",
            _ => "Thông báo"
        };
    }

    private string GetLocalizedNoNarrationMessage(string poiName)
    {
        return _currentLanguage switch
        {
            "en" => $"The restaurant '{poiName}' does not have narration content yet.",
            "zh" => $"餐厅“{poiName}”暂无解说内容。",
            "fr" => $"Le restaurant « {poiName} » n'a pas encore de contenu audio.",
            "ru" => $"Для ресторана «{poiName}» пока нет аудиоописания.",
            _ => $"Quán '{poiName}' chưa có nội dung thuyết minh."
        };
    }

    private string GetLocalizedIdleNarrationText()
    {
        return _currentLanguage switch
        {
            "en" => "No narration playing",
            "zh" => "暂无解说播放",
            "fr" => "Aucune narration en cours",
            "ru" => "Озвучивание не воспроизводится",
            _ => "Chưa đọc thuyết minh"
        };
    }

    private string GetLocalizedDistanceText(double distance)
    {
        return _currentLanguage switch
        {
            "en" => $"Distance: {distance:F2} km",
            "zh" => $"距离：{distance:F2} 公里",
            "fr" => $"Distance : {distance:F2} km",
            "ru" => $"Расстояние: {distance:F2} км",
            _ => $"Khoảng cách: {distance:F2} km"
        };
    }

    private string GetLocalizedPoiName(Poi poi)
    {
        return poi.Name;
    }

    private string GetLocalizedPoiDescription(Poi poi)
    {
        return _currentLanguage switch
        {
            "en" => GetFallbackText(poi.DescriptionEn, poi.Description),
            "zh" => GetFallbackText(poi.DescriptionZh, poi.Description),
            "fr" => GetFallbackText(poi.DescriptionFr, poi.Description),
            "ru" => GetFallbackText(poi.DescriptionRu, poi.Description),
            _ => poi.Description
        };
    }

    private static string GetFallbackText(string? localizedText, string fallbackText)
    {
        return string.IsNullOrWhiteSpace(localizedText)
            ? fallbackText
            : localizedText;
    }

    private void ShowMiniTtsBar(string text)
    {
        MiniTtsBar.IsVisible = true;
        NowPlayingLabel.Text = text;
    }

    private void HideMiniTtsBar()
    {
        MiniTtsBar.IsVisible = false;
        NowPlayingLabel.Text = GetLocalizedIdleNarrationText();
    }

    private void UpdateSearchPlaceholder()
    {
        var searchBar = this.FindByName<SearchBar>("PoiSearchBar");
        if (searchBar != null)
            searchBar.Placeholder = GetLocalizedSearchPlaceholderText();
    }

    private void OnPoiSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue ?? string.Empty;
        RefreshPoiCollectionView();
    }

    private void OnPoiSearchButtonPressed(object sender, EventArgs e)
    {
        if (sender is SearchBar searchBar)
            searchBar.Unfocus();
    }

    private void StopCurrentSpeech()
    {
        try
        {
            _ttsCts?.Cancel();
        }
        catch
        {
        }

        _isSpeaking = false;
        _currentSpeakingPoiId = null;
    }

    private void OnStopTtsClicked(object sender, EventArgs e)
    {
        StopCurrentSpeech();
        HideMiniTtsBar();
        RefreshMap();
    }

    private async void MapWebView_Navigating(object sender, WebNavigatingEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Url))
            return;

        const string speakPrefix = "app://speakpoi/";
        const string languagePrefix = "app://language/";

        if (e.Url.StartsWith(speakPrefix, StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;

            var idText = e.Url.Substring(speakPrefix.Length);
            if (!int.TryParse(idText, out int poiId))
                return;

            var poi = _allPois.FirstOrDefault(p => p.Id == poiId);
            if (poi == null)
                return;

            await SpeakPoiFromMapAsync(poi, false);
            return;
        }

        if (e.Url.StartsWith(languagePrefix, StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;

            var idText = e.Url.Substring(languagePrefix.Length);
            if (!int.TryParse(idText, out int poiId))
                return;

            var poi = _allPois.FirstOrDefault(p => p.Id == poiId);
            if (poi == null)
                return;

            await ShowLanguagePickerAndSpeakAsync(poi);
        }
    }

    private async Task ShowLanguagePickerAndSpeakAsync(Poi poi)
    {
        var title = GetLocalizedChooseLanguageText();
        var cancel = GetLocalizedCancelText();

        const string vietnamese = "Tiếng Việt";
        const string english = "English";
        const string chinese = "中文";
        const string french = "Français";
        const string russian = "Русский";

        var choice = await DisplayActionSheet(title, cancel, null, vietnamese, english, chinese, french, russian);

        if (choice == cancel || string.IsNullOrWhiteSpace(choice))
            return;

        if (choice == vietnamese)
            _currentLanguage = "vi";
        else if (choice == english)
            _currentLanguage = "en";
        else if (choice == chinese)
            _currentLanguage = "zh";
        else if (choice == french)
            _currentLanguage = "fr";
        else if (choice == russian)
            _currentLanguage = "ru";
        else
            return;

        UpdateSearchPlaceholder();
        RefreshMap();
        RefreshPoiCollectionView();
        await SpeakPoiFromMapAsync(poi, true);
    }

    private async Task SpeakPoiFromMapAsync(Poi poi, bool forceReplay = false)
    {
        if (poi == null)
            return;

        var textToSpeak = GetTtsTextByLanguage(poi, _currentLanguage);
        if (string.IsNullOrWhiteSpace(textToSpeak))
        {
            await DisplayAlert(
                GetLocalizedNoNarrationTitle(),
                GetLocalizedNoNarrationMessage(GetLocalizedPoiName(poi)),
                "OK");
            return;
        }

        var requestVersion = Interlocked.Increment(ref _manualTtsRequestVersion);

        StopCurrentSpeech();

        await _ttsGate.WaitAsync();

        var startedSpeaking = false;

        try
        {
            if (requestVersion != _manualTtsRequestVersion)
                return;

            _ttsQueue.RemoveAll(x => x.Id == poi.Id);
            _queuedPoiIds.Remove(poi.Id);

            if (forceReplay)
                _spokenPoiIds.Remove(poi.Id);

            _isSpeaking = true;
            startedSpeaking = true;
            _currentSpeakingPoiId = poi.Id;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ShowMiniTtsBar(GetLocalizedNowSpeakingText(GetLocalizedPoiName(poi)));
            });

            _ttsCts?.Dispose();
            _ttsCts = new CancellationTokenSource();

            var options = await BuildSpeechOptionsAsync();

            await TextToSpeech.Default.SpeakAsync(
                textToSpeak,
                options,
                _ttsCts.Token);

            _spokenPoiIds.Add(poi.Id);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi TTS", ex.Message, "OK");
        }
        finally
        {
            if (startedSpeaking)
            {
                _isSpeaking = false;
                _currentSpeakingPoiId = null;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HideMiniTtsBar();
                });
            }

            _ttsGate.Release();
        }

        if (_ttsQueue.Count > 0)
            await TrySpeakNextPoiAsync();
    }

    private void RefreshMap()
    {
        if (_currentLocation == null || _allPois == null || _allPois.Count == 0)
            return;

        var sortedPois = _allPois.OrderBy(p => p.Distance).ToList();
        UpdatePoiDisplayTexts(sortedPois);
        DrawMap(_currentLocation, GetVisiblePois(sortedPois));
    }

    private async void OnCenterMapClicked(object sender, EventArgs e)
    {
        try
        {
            if (_currentLocation == null)
            {
                await DisplayAlert("Thông báo", "Chưa xác định được vị trí hiện tại.", "OK");
                return;
            }

            var lat = _currentLocation.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lng = _currentLocation.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);

            await MapWebView.EvaluateJavaScriptAsync($"centerToUserLocation({lat}, {lng});");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể đưa bản đồ về vị trí hiện tại: {ex.Message}", "OK");
        }
    }

    private async Task UpdateUserLocationOnMap(Location location)
    {
        var lat = location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var lng = location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await MapWebView.EvaluateJavaScriptAsync($"updateUserLocation({lat}, {lng});");
    }

    private Task CheckGeofencesAsync(Location userLocation)
    {
        foreach (var poi in _allPois)
        {
            var distanceKm = Location.CalculateDistance(
                userLocation.Latitude, userLocation.Longitude,
                poi.Latitude, poi.Longitude,
                DistanceUnits.Kilometers);

            if (distanceKm <= (poi.Radius / 1000.0))
            {
                if (!_activeGeofencePoiIds.Contains(poi.Id))
                {
                    _activeGeofencePoiIds.Add(poi.Id);
                }
            }
            else
            {
                _activeGeofencePoiIds.Remove(poi.Id);
            }
        }

        return Task.CompletedTask;
    }

    private void DrawMap(Location userLocation, List<Poi> pois)
    {
        MapWebView.Source = new HtmlWebViewSource
        {
            Html = BuildLeafletMapHtml(userLocation, pois)
        };
    }

    private string BuildLeafletMapHtml(Location userLocation, List<Poi> pois)
    {
        var sbMarkers = new StringBuilder();
        var userLatText = userLocation.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var userLngText = userLocation.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);

        sbMarkers.AppendLine($@"
            window.userMarker = L.circleMarker([{userLatText}, {userLngText}], {{radius: 9, color: 'blue', fillColor: 'blue', fillOpacity: 0.9}}).addTo(map);
            window.userFence = L.circle([{userLatText}, {userLngText}], {{radius: {UserGeofenceRadiusMeters}, color: 'blue', fillOpacity: 0.12}}).addTo(map);
        ");

        foreach (var poi in pois)
        {
            var lat = poi.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lng = poi.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var safeName = EscapeHtml(GetLocalizedPoiName(poi));
            var chooseLanguageText = EscapeHtml(GetLocalizedChooseLanguageText());

            var popupHtml =
                $"<b>{safeName}</b><br/>" +
                $"<a href=\"app://language/{poi.Id}\">{chooseLanguageText}</a>";
            var popupJs = EscapeJsString(popupHtml);

            sbMarkers.AppendLine($@"
                (function() {{
                    var marker = L.marker([{lat}, {lng}]).addTo(map);
                    marker.bindPopup('{popupJs}');
                    marker.on('click', function() {{
                        marker.openPopup();
                        window.location.href = 'app://speakpoi/{poi.Id}';
                    }});
                }})();
            ");
        }

        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
    <style>
        html, body, #map {{
            width: 100%;
            height: 100%;
            margin: 0;
            padding: 0;
        }}
        a {{
            color: #2563eb;
            text-decoration: none;
            font-weight: 600;
        }}
    </style>
</head>
<body>
    <div id='map'></div>

    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <script>
        var map = L.map('map').setView([{userLatText}, {userLngText}], {DefaultMapZoom});
        L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
            maxZoom: 19
        }}).addTo(map);

        {sbMarkers}

        function updateUserLocation(lat, lng) {{
            if (window.userMarker && window.userFence) {{
                var pt = new L.LatLng(lat, lng);
                window.userMarker.setLatLng(pt);
                window.userFence.setLatLng(pt);
            }}
        }}

        function centerToUserLocation(lat, lng) {{
            var pt = new L.LatLng(lat, lng);
            map.setView(pt, 16);
        }}
    </script>
</body>
</html>";
    }

    private string EscapeHtml(string? value)
    {
        return (value ?? string.Empty)
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    private string EscapeJsString(string? value)
    {
        return (value ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\"", "\\\"");
    }

    private async void OnPoiSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_isNavigating || e.CurrentSelection.FirstOrDefault() is not Poi selected)
            return;

        try
        {
            _isNavigating = true;
            ((CollectionView)sender).SelectedItem = null;
            await Navigation.PushAsync(new PoiDetailPage(selected, _currentLanguage));
        }
        finally
        {
            _isNavigating = false;
        }
    }
}
