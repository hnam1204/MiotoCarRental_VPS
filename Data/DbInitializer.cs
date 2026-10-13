using Microsoft.EntityFrameworkCore;
using MiotoCarRental.Models.Entities;
using MiotoCarRental.Services;

namespace MiotoCarRental.Data
{
    public static class DbInitializer
    {
        public static void Initialize(MiotoDbContext context, bool seedDemoData, IPasswordService passwordService)
        {
            if (!seedDemoData)
            {
                return;
            }

            context.Database.EnsureCreated();

            if (context.Vehicles.Any())
            {
                return; // Database đã có dữ liệu
            }

            // 1. Seed Users
            var admin = new User
            {
                FullName = "Quản Trị Viên EcoBike",
                Email = "admin@mioto.vn",
                Phone = "0901000001",
                PasswordHash = string.Empty,
                Role = UserRole.Admin,
                DriverLicenseNo = "GPLX-A1-ADMIN",
                DriverLicenseStatus = DriverLicenseStatus.Approved
            };

            var host = new User
            {
                FullName = "Nguyễn Văn Chủ Xe (Hoàng Long)",
                Email = "chuxe@gmail.com",
                Phone = "0902000002",
                PasswordHash = string.Empty,
                Role = UserRole.Host,
                DriverLicenseNo = "GPLX-A1-79A123",
                DriverLicenseStatus = DriverLicenseStatus.Approved
            };

            var renter = new User
            {
                FullName = "Trần Khách Thuê (Minh Tuấn)",
                Email = "khachthue@gmail.com",
                Phone = "0903000003",
                PasswordHash = string.Empty,
                Role = UserRole.Renter,
                DriverLicenseNo = "GPLX-A1-88B999",
                DriverLicenseStatus = DriverLicenseStatus.Approved
            };

            context.Users.AddRange(admin, host, renter);
            admin.PasswordHash = passwordService.HashPassword(admin, "admin123");
            host.PasswordHash = passwordService.HashPassword(host, "host123");
            renter.PasswordHash = passwordService.HashPassword(renter, "renter123");
            context.SaveChanges();

            // 2. Seed Xe máy điện (12 xe phân bổ 4 nhóm: Phổ thông/Không bằng lái, Đô thị, Đường dài, Thể thao)
            
            // --- NHÓM 1: XE PHỔ THÔNG (HỌC SINH - SINH VIÊN - KHÔNG CẦN BẰNG LÁI A1) ---
            var v_lite = new Vehicle
            {
                HostId = host.Id,
                Category = VehicleCategory.Economy,
                Brand = "VinFast",
                Model = "Evo200 Lite (Học Sinh - Không Cần Bằng A1)",
                YearManufacture = 2024,
                LicensePlate = "59-E3 678.90",
                BatteryRangeKm = 205,
                MaxSpeedKmH = 49,
                BatteryType = "Pin LFP 3.5 kWh",
                RequiresLicenseA1 = false,
                PricePerDay = 80000,
                InstantBooking = true,
                Address = "45 Lê Văn Việt, Phường Hiệp Phú",
                City = "TP. Hồ Chí Minh",
                District = "TP. Thủ Đức",
                Latitude = 10.8494,
                Longitude = 106.7725,
                ThumbnailUrl = "https://images.unsplash.com/photo-1558981359-219d6364c9c8?auto=format&fit=crop&w=800&q=80",
                Description = "Dòng xe quốc dân cho học sinh, sinh viên! Giới hạn tốc độ 49 km/h nên KHÔNG CẦN BẰNG LÁI A1. Pin LFP siêu an toàn đi 205km một lần sạc, giá thuê cực rẻ.",
                FeaturesJson = "[\"Không cần bằng lái A1\", \"Giá rẻ học sinh\", \"Pin LFP 205km\", \"Tiết kiệm chi phí\"]",
                Status = VehicleStatus.Active
            };

            var v_yadea_ocean = new Vehicle
            {
                HostId = host.Id,
                Category = VehicleCategory.Economy,
                Brand = "Yadea",
                Model = "Ocean (Xe Điện Nhỏ Xinh Cho Nữ Sinh)",
                YearManufacture = 2024,
                LicensePlate = "59-E5 111.22",
                BatteryRangeKm = 110,
                MaxSpeedKmH = 45,
                BatteryType = "Ắc quy Graphene TTFAR",
                RequiresLicenseA1 = false,
                PricePerDay = 70000,
                InstantBooking = true,
                Address = "120 Nguyễn Huệ, Phường Bến Nghé",
                City = "TP. Hồ Chí Minh",
                District = "Quận 1",
                Latitude = 10.7744,
                Longitude = 106.7032,
                ThumbnailUrl = "https://images.unsplash.com/photo-1517524008697-84bbe3c3fd98?auto=format&fit=crop&w=800&q=80",
                Description = "Yadea Ocean nhỏ nhắn, tôn dáng, thiết kế thanh lịch siêu dễ lái. Thích hợp cho các bạn nữ dạo phố, đi học nhẹ nhàng trong nội thành, không cần bằng lái A1.",
                FeaturesJson = "[\"Miễn bằng lái A1\", \"Tôn dáng nữ sinh\", \"Nhỏ gọn dễ dắt\", \"Đèn LED tròn\"]",
                Status = VehicleStatus.Active
            };

            var v_pega = new Vehicle
            {
                HostId = host.Id,
                Category = VehicleCategory.Economy,
                Brand = "Pega",
                Model = "Aura S (Phong Cách Vespa Thanh Lịch)",
                YearManufacture = 2023,
                LicensePlate = "29-E2 333.44",
                BatteryRangeKm = 100,
                MaxSpeedKmH = 48,
                BatteryType = "Pin Graphene",
                RequiresLicenseA1 = false,
                PricePerDay = 75000,
                InstantBooking = true,
                Address = "88 Cầu Giấy, Phường Quan Hoa",
                City = "Hà Nội",
                District = "Cầu Giấy",
                Latitude = 21.0333,
                Longitude = 105.7958,
                ThumbnailUrl = "https://images.unsplash.com/photo-1558981420-87aa9dad1c89?auto=format&fit=crop&w=800&q=80",
                Description = "Dáng xe tay ga Vespa sang chảnh, sàn để chân rộng rãi tha hồ để balo túi xách, đồng hồ điện tử sắc nét, học sinh cấp 3 và sinh viên đi học tự tin.",
                FeaturesJson = "[\"Miễn bằng lái A1\", \"Dáng Vespa sang\", \"Cốp rộng để mũ\", \"Êm ái lướt phố\"]",
                Status = VehicleStatus.Active
            };

            var v_yadea_volt = new Vehicle
            {
                HostId = host.Id,
                Category = VehicleCategory.Economy,
                Brand = "Yadea",
                Model = "Voltguard (Xe Điện Bền Bỉ Quốc Dân)",
                YearManufacture = 2024,
                LicensePlate = "43-E2 555.66",
                BatteryRangeKm = 120,
                MaxSpeedKmH = 50,
                BatteryType = "Ắc quy Graphene 72V",
                RequiresLicenseA1 = false,
                PricePerDay = 85000,
                InstantBooking = true,
                Address = "15 Nguyễn Văn Linh, Phường Nam Dương",
                City = "Đà Nẵng",
                District = "Hải Châu",
                Latitude = 16.0612,
                Longitude = 108.2185,
                ThumbnailUrl = "https://images.unsplash.com/photo-1568772585407-9361f9bf3a87?auto=format&fit=crop&w=800&q=80",
                Description = "Yadea Voltguard thiết kế góc cạnh, gầm cao, phuộc nhún êm ái, chống nước IPX7 thoải mái đi mưa ngập, phù hợp sinh viên đi làm thêm.",
                FeaturesJson = "[\"Miễn bằng lái A1\", \"Chống nước IPX7\", \"Khung sườn thép\", \"Tiết kiệm tối đa\"]",
                Status = VehicleStatus.Active
            };

            // --- NHÓM 2: XE ĐÔ THỊ & ĐI LÀM (TẦM TRUNG) ---
            var v1 = new Vehicle
            {
                HostId = host.Id,
                Category = VehicleCategory.MidRange,
                Brand = "VinFast",
                Model = "Evo200 (Pin LFP 203km Tiêu Chuẩn)",
                YearManufacture = 2023,
                LicensePlate = "59-E1 888.88",
                BatteryRangeKm = 203,
                MaxSpeedKmH = 70,
                BatteryType = "Pin LFP 3.5 kWh",
                RequiresLicenseA1 = true,
                PricePerDay = 120000,
                InstantBooking = true,
                Address = "120 Nguyễn Huệ, Phường Bến Nghé",
                City = "TP. Hồ Chí Minh",
                District = "Quận 1",
                Latitude = 10.7744,
                Longitude = 106.7032,
                ThumbnailUrl = "https://images.unsplash.com/photo-1558981403-c5f9899a28bc?auto=format&fit=crop&w=800&q=80",
                Description = "Xe máy điện quốc dân VinFast Evo200 bản chuẩn 70km/h, pin LFP chống cháy nổ đi 203km/lần sạc, cốp 22L, kèm củ sạc nhanh và 2 nón bảo hiểm.",
                FeaturesJson = "[\"Pin LFP 203km\", \"Cốp 22L\", \"Chống nước IP67\", \"Kèm củ sạc di động\", \"Cung cấp 2 nón BH\"]",
                Status = VehicleStatus.Active
            };

            var v3 = new Vehicle
            {
                HostId = host.Id,
                Category = VehicleCategory.MidRange,
                Brand = "VinFast",
                Model = "Feliz S (Pin LFP Cốp Rộng 25L)",
                YearManufacture = 2023,
                LicensePlate = "29-E1 123.45",
                BatteryRangeKm = 198,
                MaxSpeedKmH = 78,
                BatteryType = "Pin LFP 3.5 kWh",
                RequiresLicenseA1 = true,
                PricePerDay = 130000,
                InstantBooking = false,
                Address = "88 Cầu Giấy, Phường Quan Hoa",
                City = "Hà Nội",
                District = "Cầu Giấy",
                Latitude = 21.0333,
                Longitude = 105.7958,
                ThumbnailUrl = "https://images.unsplash.com/photo-1558981806-ec527fa84c39?auto=format&fit=crop&w=800&q=80",
                Description = "VinFast Feliz S thanh lịch, gầm cao, phanh đĩa an toàn, cốp 25L đựng vừa 2 mũ bảo hiểm cùng laptop, phù hợp tuyệt đối cho dân công sở đi làm.",
                FeaturesJson = "[\"Pin LFP 198km\", \"Cốp rộng 25L\", \"Đèn Full LED\", \"Chống nước IP67\", \"Kèm áo mưa\"]",
                Status = VehicleStatus.Active
            };

            var v_selex = new Vehicle
            {
                HostId = host.Id,
                Category = VehicleCategory.MidRange,
                Brand = "Selex",
                Model = "Camel (Xe Điện Thùng Hàng Đổi Pin 2 Phút)",
                YearManufacture = 2024,
                LicensePlate = "59-E6 789.01",
                BatteryRangeKm = 150,
                MaxSpeedKmH = 65,
                BatteryType = "Pin Lithium Đổi Trạm",
                RequiresLicenseA1 = true,
                PricePerDay = 110000,
                InstantBooking = true,
                Address = "234 Điện Biên Phủ, Phường 15",
                City = "TP. Hồ Chí Minh",
                District = "Bình Thạnh",
                Latitude = 10.7981,
                Longitude = 106.7088,
                ThumbnailUrl = "https://images.unsplash.com/photo-1558980394-4c7c9299fe96?auto=format&fit=crop&w=800&q=80",
                Description = "Selex Camel chuyên dụng chở hàng, chuyển trọ, chạy dịch vụ. Cơ chế đổi 3 pack pin chỉ trong 2 phút tại mạng lưới trạm tự động, không lo chờ sạc.",
                FeaturesJson = "[\"Đổi pin nhanh 2 phút\", \"Khung tải 225kg\", \"Thùng hàng thông minh\", \"Siêu bền bỉ\"]",
                Status = VehicleStatus.Active
            };

            // --- NHÓM 3: XE ĐƯỜNG DÀI - PIN KHỦNG (TOURING / PHƯỢT) ---
            var v2 = new Vehicle
            {
                HostId = host.Id,
                Category = VehicleCategory.Touring,
                Brand = "Dat Bike",
                Model = "Quantum (Pin Khủng 270km - Cốp 41L)",
                YearManufacture = 2024,
                LicensePlate = "59-E2 567.89",
                BatteryRangeKm = 270,
                MaxSpeedKmH = 100,
                BatteryType = "Pin Lithium 7.2 kWh",
                RequiresLicenseA1 = true,
                PricePerDay = 180000,
                InstantBooking = true,
                Address = "234 Điện Biên Phủ, Phường 15",
                City = "TP. Hồ Chí Minh",
                District = "Bình Thạnh",
                Latitude = 10.7981,
                Longitude = 106.7088,
                ThumbnailUrl = "https://images.unsplash.com/photo-1568772585407-9361f9bf3a87?auto=format&fit=crop&w=800&q=80",
                Description = "Dat Bike Quantum chuyên cơ điện đường dài! Đi 270km một lần sạc, cốp khổng lồ 41L đựng cả vali, sạc siêu nhanh 4 giờ đầy pin, động cơ 7000W bốc lửa đi phượt Vũng Tàu - Đà Lạt.",
                FeaturesJson = "[\"Đi 270km/sạc\", \"Cốp khổng lồ 41L\", \"Cruise Control\", \"Sạc nhanh 4h\", \"Động cơ 7000W\"]",
                Status = VehicleStatus.Active
            };

            var v4 = new Vehicle
            {
                HostId = host.Id,
                Category = VehicleCategory.Touring,
                Brand = "VinFast",
                Model = "Klara S 2022 (Pin LFP Phong Cách Ý)",
                YearManufacture = 2023,
                LicensePlate = "43-E1 777.99",
                BatteryRangeKm = 194,
                MaxSpeedKmH = 78,
                BatteryType = "Pin LFP 3.5 kWh",
                RequiresLicenseA1 = true,
                PricePerDay = 150000,
                InstantBooking = true,
                Address = "15 Nguyễn Văn Linh, Phường Nam Dương",
                City = "Đà Nẵng",
                District = "Hải Châu",
                Latitude = 16.0612,
                Longitude = 108.2185,
                ThumbnailUrl = "https://images.unsplash.com/photo-1558981420-87aa9dad1c89?auto=format&fit=crop&w=800&q=80",
                Description = "Klara S thiết kế tinh tế đậm chất Ý, sàn để chân rộng rãi, thích hợp thuê dạo phố biển Đà Nẵng, đèo Hải Vân cực êm, pin LFP tuổi thọ cao.",
                FeaturesJson = "[\"Phong cách Ý\", \"Pin LFP 194km\", \"Phanh ABS\", \"Kết nối thông minh\", \"Kèm 2 nón BH\"]",
                Status = VehicleStatus.Active
            };

            // --- NHÓM 4: XE THỂ THAO & CAO CẤP (SPORT / PREMIUM) ---
            var v6 = new Vehicle
            {
                HostId = host.Id,
                Category = VehicleCategory.SportPremium,
                Brand = "VinFast",
                Model = "Vento S (Động Cơ Side Motor 5200W - Phanh ABS)",
                YearManufacture = 2023,
                LicensePlate = "59-E4 999.88",
                BatteryRangeKm = 160,
                MaxSpeedKmH = 89,
                BatteryType = "Pin LFP 3.5 kWh",
                RequiresLicenseA1 = true,
                PricePerDay = 200000,
                InstantBooking = true,
                Address = "101 Nguyễn Thị Thập, Tân Phú",
                City = "TP. Hồ Chí Minh",
                District = "Quận 7",
                Latitude = 10.7328,
                Longitude = 106.7214,
                ThumbnailUrl = "https://images.unsplash.com/photo-1558980664-769d59546b3d?auto=format&fit=crop&w=800&q=80",
                Description = "Xe máy điện cao cấp trang bị công nghệ Smartkey điều khiển từ xa, phanh ABS chống bó cứng an toàn, động cơ Side Motor 5200W uy lực bốc như xe 150cc.",
                FeaturesJson = "[\"Phanh ABS\", \"Smartkey thông minh\", \"Động cơ Side Motor 5200W\", \"Tốc độ 89km/h\"]",
                Status = VehicleStatus.Active
            };

            var v_weaver = new Vehicle
            {
                HostId = host.Id,
                Category = VehicleCategory.SportPremium,
                Brand = "Dat Bike",
                Model = "Weaver++ (Phong Cách Cafe Racer Độc Bản)",
                YearManufacture = 2024,
                LicensePlate = "59-E7 666.88",
                BatteryRangeKm = 200,
                MaxSpeedKmH = 90,
                BatteryType = "Pin Lithium 72V",
                RequiresLicenseA1 = true,
                PricePerDay = 190000,
                InstantBooking = true,
                Address = "120 Nguyễn Huệ, Phường Bến Nghé",
                City = "TP. Hồ Chí Minh",
                District = "Quận 1",
                Latitude = 10.7744,
                Longitude = 106.7032,
                ThumbnailUrl = "https://images.unsplash.com/photo-1558981403-c5f9899a28bc?auto=format&fit=crop&w=800&q=80",
                Description = "Thiết kế Cafe Racer cổ điển phong trần, động cơ 7000W gia tốc cực đại từ 0-50km/h chỉ trong 3 giây. Sạc nhanh 3 giờ đầy pin, phong cách biker cực ngầu.",
                FeaturesJson = "[\"Cafe Racer\", \"Động cơ 7000W\", \"Tăng tốc 3s\", \"Sạc siêu tốc 3h\"]",
                Status = VehicleStatus.Active
            };

            var v_theon = new Vehicle
            {
                HostId = host.Id,
                Category = VehicleCategory.SportPremium,
                Brand = "VinFast",
                Model = "Theon S (Đỉnh Cao Công Suất 7100W - ABS 2 Kênh)",
                YearManufacture = 2024,
                LicensePlate = "29-E9 888.99",
                BatteryRangeKm = 150,
                MaxSpeedKmH = 99,
                BatteryType = "Pin LFP 3.5 kWh",
                RequiresLicenseA1 = true,
                PricePerDay = 250000,
                InstantBooking = true,
                Address = "88 Cầu Giấy, Phường Quan Hoa",
                City = "Hà Nội",
                District = "Cầu Giấy",
                Latitude = 21.0333,
                Longitude = 105.7958,
                ThumbnailUrl = "https://images.unsplash.com/photo-1558981806-ec527fa84c39?auto=format&fit=crop&w=800&q=80",
                Description = "Flagship đỉnh cao nhất của VinFast! Động cơ Center Motor 7100W truyền động dây curoa, phanh đĩa ABS 2 kênh Continental, giảm xóc Showa, PAAK mở khóa qua điện thoại.",
                FeaturesJson = "[\"Công suất khủng 7100W\", \"ABS 2 kênh Continental\", \"Giảm xóc Showa\", \"Tốc độ 99km/h\"]",
                Status = VehicleStatus.Active
            };

            context.Vehicles.AddRange(v_lite, v_yadea_ocean, v_pega, v_yadea_volt, v1, v3, v_selex, v2, v4, v6, v_weaver, v_theon);
            context.SaveChanges();

            // 3. Seed Bookings mẫu hoàn tất
            var b1 = new Booking
            {
                BookingCode = "EB-SAMPLE-001",
                RenterId = renter.Id,
                VehicleId = v1.Id,
                StartTime = DateTime.UtcNow.AddDays(-7),
                EndTime = DateTime.UtcNow.AddDays(-5),
                TotalDays = 2,
                RentalFee = 240000,
                InsuranceFee = 30000,
                TotalAmount = 270000,
                DepositAmount = 81000,
                Status = BookingStatus.Completed,
                CheckinOdo = 3200,
                CheckinBatteryPercent = 100,
                CheckoutOdo = 3380,
                CheckoutBatteryPercent = 45
            };

            var b2 = new Booking
            {
                BookingCode = "EB-SAMPLE-002",
                RenterId = renter.Id,
                VehicleId = v2.Id,
                StartTime = DateTime.UtcNow.AddDays(-15),
                EndTime = DateTime.UtcNow.AddDays(-12),
                TotalDays = 3,
                RentalFee = 540000,
                InsuranceFee = 45000,
                TotalAmount = 585000,
                DepositAmount = 175500,
                Status = BookingStatus.Completed,
                CheckinOdo = 5100,
                CheckinBatteryPercent = 100,
                CheckoutOdo = 5450,
                CheckoutBatteryPercent = 30
            };

            context.Bookings.AddRange(b1, b2);
            context.SaveChanges();

            // 4. Seed Reviews mẫu
            context.Reviews.AddRange(
                new Review
                {
                    BookingId = b1.Id,
                    ReviewerId = renter.Id,
                    Rating = 5,
                    Comment = "Xe Evo200 chạy cực kỳ êm và mượt, pin trâu đi cả 2 ngày vòng quanh Sài Gòn chưa hết nửa bình pin!",
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Review
                {
                    BookingId = b2.Id,
                    ReviewerId = renter.Id,
                    Rating = 5,
                    Comment = "Dat Bike Quantum tăng tốc siêu bốc, cốp 41L bỏ vừa 2 ba lô du lịch to. Rất hài lòng!",
                    CreatedAt = DateTime.UtcNow.AddDays(-12)
                }
            );

            context.SaveChanges();
        }
    }
}
