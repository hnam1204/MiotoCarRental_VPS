namespace MiotoCarRental.Models.Entities
{
    public enum UserRole
    {
        Renter,
        Host,
        Admin
    }

    public enum DriverLicenseStatus
    {
        Unverified,
        Pending,
        Approved,
        Rejected
    }

    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Renter;
        public string? AvatarUrl { get; set; }

        // eKYC / Giấy phép lái xe
        public string? DriverLicenseNo { get; set; }
        public string? DriverLicenseImage { get; set; }
        public DriverLicenseStatus DriverLicenseStatus { get; set; } = DriverLicenseStatus.Unverified;
        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Vehicle> OwnedVehicles { get; set; } = new List<Vehicle>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
