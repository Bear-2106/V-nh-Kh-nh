namespace FoodGuideAPI.Models
{
    public class PoiLocalization
    {
        public int Id { get; set; }
        public int PoiId { get; set; }
        public string? Language { get; set; }
        public string? LocalizedName { get; set; }
        public string? LocalizedDescription { get; set; }
        public string? TtsText { get; set; }
    }
}
