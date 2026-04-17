namespace FoodGuideAPI.Models
{
    public class AiUsageLimit
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime UsageDate { get; set; }
        public int Count { get; set; }
    }
}