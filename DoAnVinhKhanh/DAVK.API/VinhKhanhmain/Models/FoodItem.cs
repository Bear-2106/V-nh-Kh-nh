namespace FoodGuideAPI.Models
{
    public class FoodItem
    {
        public int Id { get; set; }
        public int PoiId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public double Price { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
    }
}