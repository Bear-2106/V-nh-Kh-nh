using DoAnVinhKhanh.Models;
using Microsoft.Maui.Devices.Sensors;
using System.Text;

namespace DoAnVinhKhanh;

public partial class MainPage : ContentPage
{
    private readonly ApiService _apiService;
    private List<Poi> _allPois = new();
    private readonly HashSet<int> _activeGeofencePoiIds = new();
    private bool _isMonitoringLocation;
    private bool _isNavigating;

    public MainPage(ApiService apiService)
    {
        _apiService = apiService;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isMonitoringLocation = false;
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

            // SỬA LỖI GEOLOCATION: Gọi qua Geolocation.Default
            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10)));

            if (location == null) return;

            // Gọi API
            _allPois = await _apiService.GetPoisAsync();

            if (_allPois != null)
            {
                foreach (var poi in _allPois)
                {
                    poi.Distance = Location.CalculateDistance(
                        location.Latitude, location.Longitude,
                        poi.Latitude, poi.Longitude,
                        DistanceUnits.Kilometers);
                }

                var sortedPois = _allPois.OrderBy(p => p.Distance).ToList();
                PoiCollectionView.ItemsSource = sortedPois;

                DrawMap(location, sortedPois);
                await CheckGeofencesAsync(location);
                StartLocationMonitoring();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi Kết Nối", ex.Message, "OK");
        }
    }

    private async void StartLocationMonitoring()
    {
        if (_isMonitoringLocation) return;
        _isMonitoringLocation = true;

        while (_isMonitoringLocation)
        {
            try
            {
                var location = await Geolocation.Default.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10)));

                if (location != null)
                {
                    foreach (var poi in _allPois)
                    {
                        poi.Distance = Location.CalculateDistance(
                            location.Latitude, location.Longitude,
                            poi.Latitude, poi.Longitude,
                            DistanceUnits.Kilometers);
                    }

                    var sortedPois = _allPois.OrderBy(p => p.Distance).ToList();

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        PoiCollectionView.ItemsSource = sortedPois;
                        await UpdateUserLocationOnMap(location);
                    });

                    await CheckGeofencesAsync(location);
                }
            }
            catch { }
            await Task.Delay(5000); // Kiểm tra lại sau 5 giây
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
            var distanceKm = Location.CalculateDistance(userLocation.Latitude, userLocation.Longitude, poi.Latitude, poi.Longitude, DistanceUnits.Kilometers);
            if (distanceKm <= (poi.Radius / 1000.0))
            {
                if (!_activeGeofencePoiIds.Contains(poi.Id))
                {
                    _activeGeofencePoiIds.Add(poi.Id);
                    MainThread.BeginInvokeOnMainThread(async () => {
                        await DisplayAlert("Thông báo", $"Bạn đã vào vùng: {poi.Name}", "OK");
                    });
                }
            }
            else { _activeGeofencePoiIds.Remove(poi.Id); }
        }

        return Task.CompletedTask;
    }

    private void DrawMap(Location userLocation, List<Poi> pois)
    {
        MapWebView.Source = new HtmlWebViewSource { Html = BuildLeafletMapHtml(userLocation, pois) };
    }

    private string BuildLeafletMapHtml(Location userLocation, List<Poi> pois)
    {
        var sbMarkers = new StringBuilder();
        var userLatText = userLocation.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var userLngText = userLocation.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);

        sbMarkers.AppendLine($@"
            window.userMarker = L.circleMarker([{userLatText}, {userLngText}], {{radius: 9, color: 'blue', fillColor: 'blue', fillOpacity: 0.9}}).addTo(map);
            window.userFence = L.circle([{userLatText}, {userLngText}], {{radius: 150, color: 'blue', fillOpacity: 0.12}}).addTo(map);");

        foreach (var poi in pois)
        {
            var lat = poi.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var lng = poi.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            sbMarkers.AppendLine($@"L.marker([{lat}, {lng}]).addTo(map).bindPopup('<b>{EscapeJs(poi.Name)}</b>');");
        }

        return $@"<html><head><link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/></head>
            <body style='margin:0;'><div id='map' style='width:100%;height:100vh;'></div>
            <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
            <script>
                var map = L.map('map').setView([{userLatText}, {userLngText}], 14);
                L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png').addTo(map);
                {sbMarkers}
                function updateUserLocation(lat, lng) {{
                    if(window.userMarker && window.userFence) {{
                        var pt = new L.LatLng(lat, lng);
                        window.userMarker.setLatLng(pt);
                        window.userFence.setLatLng(pt);
                    }}
                }}
            </script></body></html>";
    }

    private string EscapeJs(string? v) => v?.Replace("'", "\\'") ?? "";

    private async void OnPoiSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_isNavigating || e.CurrentSelection.FirstOrDefault() is not Poi selected) return;
        try
        {
            _isNavigating = true;
            ((CollectionView)sender).SelectedItem = null;
            await Navigation.PushAsync(new PoiDetailPage(selected));
        }
        finally { _isNavigating = false; }
    }
}
