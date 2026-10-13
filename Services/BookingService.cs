using Microsoft.EntityFrameworkCore;
using MiotoCarRental.Data;
using MiotoCarRental.Models.Entities;
using MiotoCarRental.ViewModels;

namespace MiotoCarRental.Services
{
    public interface IBookingService
    {
        Task<bool> IsVehicleAvailableAsync(int vehicleId, DateTime startDate, DateTime endDate);
        Task<(bool Success, string Message, Booking? Booking)> CreateBookingAsync(int renterId, CreateBookingViewModel model);
        Task<bool> ConfirmDepositAsync(int bookingId, string transactionCode);
        Task<bool> CheckinVehicleAsync(int bookingId, int odo, int batteryPercent);
        Task<bool> CheckoutVehicleAsync(int bookingId, int odo, int batteryPercent);
    }

    public class BookingService : IBookingService
    {
        private readonly MiotoDbContext _context;

        public BookingService(MiotoDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsVehicleAvailableAsync(int vehicleId, DateTime startDate, DateTime endDate)
        {
            // Chống Double Booking: ExistingStart < ReqEnd AND ExistingEnd > ReqStart
            var hasConflict = await _context.CalendarBlockeds
                .AnyAsync(c => c.VehicleId == vehicleId &&
                               c.StartDate < endDate &&
                               c.EndDate > startDate);

            return !hasConflict;
        }

        public async Task<(bool Success, string Message, Booking? Booking)> CreateBookingAsync(int renterId, CreateBookingViewModel model)
        {
            if (model.StartTime >= model.EndTime)
            {
                return (false, "Thời gian trả xe phải sau thời gian nhận xe.", null);
            }

            var vehicle = await _context.Vehicles.FindAsync(model.VehicleId);
            if (vehicle == null || vehicle.Status != VehicleStatus.Active)
            {
                return (false, "Xe máy điện không tồn tại hoặc tạm ngưng cho thuê.", null);
            }

            // Kiểm tra chống trùng lịch xe
            var isAvailable = await IsVehicleAvailableAsync(model.VehicleId, model.StartTime, model.EndTime);
            if (!isAvailable)
            {
                return (false, "Chiếc xe máy điện này đã có người đặt trong khoảng thời gian bạn chọn.", null);
            }

            // Tính toán tài chính
            var timeSpan = model.EndTime - model.StartTime;
            int totalDays = (int)Math.Ceiling(timeSpan.TotalDays);
            if (totalDays < 1) totalDays = 1;

            decimal rentalFee = vehicle.PricePerDay * totalDays;
            decimal insuranceFee = 15000 * totalDays; // 15.000đ/ngày bảo hiểm xe máy
            decimal deliveryFee = model.NeedsDelivery ? 30000 : 0; // Giao xe máy tận nơi: 30k
            decimal totalAmount = rentalFee + insuranceFee + deliveryFee;
            decimal depositAmount = Math.Round(totalAmount * 0.3m, 0); // 30% cọc giữ chỗ

            var bookingCode = $"EB-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

            var booking = new Booking
            {
                BookingCode = bookingCode,
                RenterId = renterId,
                VehicleId = model.VehicleId,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                TotalDays = totalDays,
                RentalFee = rentalFee,
                InsuranceFee = insuranceFee,
                DeliveryFee = deliveryFee,
                TotalAmount = totalAmount,
                DepositAmount = depositAmount,
                Status = vehicle.InstantBooking ? BookingStatus.AwaitingPayment : BookingStatus.Pending
            };

            // Khóa lịch
            var blockedDate = new CalendarBlocked
            {
                VehicleId = model.VehicleId,
                StartDate = model.StartTime,
                EndDate = model.EndTime,
                Reason = "BOOKED"
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                blockedDate.BookingId = booking.Id;
                _context.CalendarBlockeds.Add(blockedDate);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return (true, "Đặt xe máy điện thành công!", booking);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Lỗi hệ thống: {ex.Message}", null);
            }
        }

        public async Task<bool> ConfirmDepositAsync(int bookingId, string transactionCode)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return false;

            booking.Status = BookingStatus.Confirmed;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CheckinVehicleAsync(int bookingId, int odo, int batteryPercent)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return false;

            booking.CheckinOdo = odo;
            booking.CheckinBatteryPercent = batteryPercent;
            booking.CheckinTime = DateTime.UtcNow;
            booking.Status = BookingStatus.InProgress;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CheckoutVehicleAsync(int bookingId, int odo, int batteryPercent)
        {
            var booking = await _context.Bookings.Include(b => b.Vehicle).FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking == null) return false;

            booking.CheckoutOdo = odo;
            booking.CheckoutBatteryPercent = batteryPercent;
            booking.CheckoutTime = DateTime.UtcNow;
            booking.Status = BookingStatus.Completed;

            // Tính phụ phí nếu vượt quãng đường ODO cam kết
            if (booking.CheckinOdo.HasValue && booking.Vehicle != null)
            {
                int distanceDriven = odo - booking.CheckinOdo.Value;
                int maxAllowedDistance = booking.Vehicle.LimitKmPerDay * booking.TotalDays;
                if (distanceDriven > maxAllowedDistance)
                {
                    int extraKm = distanceDriven - maxAllowedDistance;
                    booking.ExtraKmFee = extraKm * booking.Vehicle.OverKmFee;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
