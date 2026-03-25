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

        public string? AudioUrl { get; set; }

        public string? ImageUrl { get; set; }

        public string? Category { get; set; }

        public bool IsActive { get; set; }
    }
}