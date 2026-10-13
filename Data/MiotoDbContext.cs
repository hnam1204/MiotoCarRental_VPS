using Microsoft.EntityFrameworkCore;
using MiotoCarRental.Models.Entities;

namespace MiotoCarRental.Data
{
    public class MiotoDbContext : DbContext
    {
        public MiotoDbContext(DbContextOptions<MiotoDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<VehicleImage> VehicleImages => Set<VehicleImage>();
        public DbSet<CalendarBlocked> CalendarBlockeds => Set<CalendarBlocked>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<Review> Reviews => Set<Review>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.Phone).IsUnique();
                entity.Property(u => u.Role).HasConversion<string>();
                entity.Property(u => u.DriverLicenseStatus).HasConversion<string>();
            });

            // Vehicle (Electric Motorbike) configuration
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(v => v.Id);
                entity.HasIndex(v => v.LicensePlate).IsUnique();
                entity.Property(v => v.Status).HasConversion<string>();

                entity.HasOne(v => v.Host)
                      .WithMany(u => u.OwnedVehicles)
                      .HasForeignKey(v => v.HostId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Booking configuration
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(b => b.Id);
                entity.HasIndex(b => b.BookingCode).IsUnique();
                entity.Property(b => b.Status).HasConversion<string>();

                entity.HasOne(b => b.Renter)
                      .WithMany(u => u.Bookings)
                      .HasForeignKey(b => b.RenterId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.Vehicle)
                      .WithMany(v => v.Bookings)
                      .HasForeignKey(b => b.VehicleId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Review configuration
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.HasOne(r => r.Booking)
                      .WithMany(b => b.Reviews)
                      .HasForeignKey(r => r.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Reviewer)
                      .WithMany()
                      .HasForeignKey(r => r.ReviewerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
