using Microsoft.Maui.Controls;

namespace DoAnVinhKhanh.Models
{
    public class Poi
    {
        public string TtsTextVi { get; set; } = "";
        public string TtsTextEn { get; set; } = "";
        public string TtsTextZh { get; set; } = "";
        public string TtsTextFr { get; set; } = "";
        public string TtsTextRu { get; set; } = "";
        private const string PublicApiBaseUrl = "http://foodguidekhanh.somee.com";
        private const string DefaultImagePath = "/images/banh-mi.jpg";

        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string DescriptionEn { get; set; } = "";
        public string DescriptionZh { get; set; } = "";
        public string DescriptionFr { get; set; } = "";
        public string DescriptionRu { get; set; } = "";
        public string Address { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Radius { get; set; }
        public string ImageUrl { get; set; } = "";
        public string Category { get; set; } = "";
        public bool IsActive { get; set; }
        public double Distance { get; set; }
        public string DisplayName { get; set; } = "";
        public string DisplayDescription { get; set; } = "";
        public string DisplayDistanceText { get; set; } = "";

        public string FullImageUrl =>
            BuildAbsoluteUrl(string.IsNullOrWhiteSpace(ImageUrl) ? DefaultImagePath : ImageUrl);


        public ImageSource DisplayImageSource => new UriImageSource
        {
            Uri = new Uri(FullImageUrl),
            CachingEnabled = false
        };

        private static string BuildAbsoluteUrl(string pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl))
                return string.Empty;

            if (pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return pathOrUrl;
            }

            var normalizedPath = pathOrUrl.StartsWith("/") ? pathOrUrl : $"/{pathOrUrl}";
            return $"{PublicApiBaseUrl}{normalizedPath}";
        }
    }
}
