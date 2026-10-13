namespace MiotoCarRental.Models.Entities
{
    public class CalendarBlocked
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; } = null!;
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = "BOOKED"; // BOOKED, HOST_BUSY, MAINTENANCE
        public int? BookingId { get; set; }
    }
}
