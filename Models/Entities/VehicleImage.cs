namespace MiotoCarRental.Models.Entities
{
    public class VehicleImage
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; } = null!;
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsThumbnail { get; set; } = false;
    }
}
