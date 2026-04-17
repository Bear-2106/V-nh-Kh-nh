using System.ComponentModel.DataAnnotations.Schema;

namespace FoodGuideAPI.Models
{
    public class POI
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Radius { get; set; }
        public string? TtsTextVi { get; set; }
        public string? TtsTextEn { get; set; }
        public string? TtsTextZh { get; set; }
        public string? TtsTextFr { get; set; }
        public string? TtsTextRu { get; set; }
        public string? ImageUrl { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
    }
}
