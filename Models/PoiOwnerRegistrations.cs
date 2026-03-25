namespace FoodGuideAPI.Models
{
    public class PoiOwnerRegistration
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? BusinessName { get; set; }
        public string? BusinessAddress { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}