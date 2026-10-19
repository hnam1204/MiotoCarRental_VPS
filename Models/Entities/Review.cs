namespace MiotoCarRental.Models.Entities
{
    public class Review
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;

        public int ReviewerId { get; set; }
        public User Reviewer { get; set; } = null!;

        public int Rating { get; set; } = 5; // 1 -> 5 sao
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
