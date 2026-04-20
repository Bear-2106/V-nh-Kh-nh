using DoAnVinhKhanh.Models;

namespace DoAnVinhKhanh;

public partial class PoiDetailPage : ContentPage
{
    private readonly Poi _poi;
    private readonly string _language;

    public PoiDetailPage(Poi poi, string language)
    {
        InitializeComponent();

        _poi = poi;
        _language = string.IsNullOrWhiteSpace(language) ? "vi" : language;

        NameLabel.Text = poi.Name;
        DescriptionLabel.Text = string.IsNullOrWhiteSpace(poi.DisplayDescription) ? poi.Description : poi.DisplayDescription;
        LatitudeLabel.Text = $"{GetLocalizedLatitudeText()}: {poi.Latitude}";
        LongitudeLabel.Text = $"{GetLocalizedLongitudeText()}: {poi.Longitude}";
        RadiusLabel.Text = $"{GetLocalizedRadiusText()}: {poi.Radius}";
        OpenMapButton.Text = GetLocalizedDirectionsText();
        PoiImage.Source = poi.FullImageUrl;
    }

    private string GetLocalizedLatitudeText()
    {
        return _language switch
        {
            "en" => "Latitude",
            "zh" => "纬度",
            "fr" => "Latitude",
            "ru" => "Широта",
            _ => "Vĩ độ"
        };
    }

    private string GetLocalizedLongitudeText()
    {
        return _language switch
        {
            "en" => "Longitude",
            "zh" => "经度",
            "fr" => "Longitude",
            "ru" => "Долгота",
            _ => "Kinh độ"
        };
    }

    private string GetLocalizedRadiusText()
    {
        return _language switch
        {
            "en" => "Radius",
            "zh" => "半径",
            "fr" => "Rayon",
            "ru" => "Радиус",
            _ => "Bán kính"
        };
    }

    private string GetLocalizedDirectionsText()
    {
        return _language switch
        {
            "en" => "Directions",
            "zh" => "导航",
            "fr" => "Itinéraire",
            "ru" => "Маршрут",
            _ => "Chỉ đường"
        };
    }

    private async void OnOpenMapClicked(object sender, EventArgs e)
    {
        var lat = _poi.Latitude;
        var lng = _poi.Longitude;

        var url = $"https://www.google.com/maps/dir/?api=1&destination={lat},{lng}";

        await Launcher.Default.OpenAsync(url);
    }
}
