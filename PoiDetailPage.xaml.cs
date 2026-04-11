using DoAnVinhKhanh.Models;

namespace DoAnVinhKhanh;

public partial class PoiDetailPage : ContentPage
{
    private readonly Poi _poi;

    public PoiDetailPage(Poi poi)
    {
        InitializeComponent();

        _poi = poi;

        NameLabel.Text = poi.Name;
        DescriptionLabel.Text = $"Mô tả: {poi.Description}";
        LatitudeLabel.Text = $"Latitude: {poi.Latitude}";
        LongitudeLabel.Text = $"Longitude: {poi.Longitude}";
        RadiusLabel.Text = $"Radius: {poi.Radius}";
        PoiImage.Source = poi.FullImageUrl;
    }

    private async void OnOpenMapClicked(object sender, EventArgs e)
    {
        var lat = _poi.Latitude;
        var lng = _poi.Longitude;

        var url = $"https://www.google.com/maps/dir/?api=1&destination={lat},{lng}";

        await Launcher.Default.OpenAsync(url);
    }
}