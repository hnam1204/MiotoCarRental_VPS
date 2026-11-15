using Microsoft.EntityFrameworkCore;
using MiotoCarRental.Models.Entities;
using MiotoCarRental.Helpers;

namespace MiotoCarRental.Data
{
    public static class DbInitializer
    {
        public static void Initialize(MiotoDbContext context)
        {
            context.Database.EnsureCreated();

            // Cập nhật schema SQLite tự động cho các cột thanh toán mới nếu đã có database từ trước
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Bookings ADD COLUMN PaymentMethod TEXT;"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Bookings ADD COLUMN PaymentTransactionId TEXT;"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Bookings ADD COLUMN PaymentBankCode TEXT;"); } catch { }
            try { context.Database.ExecuteSqlRaw("ALTER TABLE Bookings ADD COLUMN DepositPaidAt TEXT;"); } catch { }

            // 1. Seed Users (Admin, Host, Renter, PendingRenter) nếu chưa có
            var admin = context.Users.FirstOrDefault(u => u.Email == "admin@mioto.vn");
            if (admin == null)
            {
                admin = new User
                {
                    FullName = "Quản Trị Viên EcoBike",
                    Email = "admin@mioto.vn",
                    Phone = "0901000001",
                    PasswordHash = "admin123",
                    Role = UserRole.Admin,
                    DriverLicenseNo = "GPLX-A1-ADMIN",
                    DriverLicenseStatus = DriverLicenseStatus.Approved
                };
                context.Users.Add(admin);
            }

            var host = context.Users.FirstOrDefault(u => u.Email == "chuxe@gmail.com");
            if (host == null)
            {
                host = new User
                {
                    FullName = "Nguyễn Văn Chủ Xe (Hoàng Long)",
                    Email = "chuxe@gmail.com",
                    Phone = "0902000002",
                    PasswordHash = "host123",
                    Role = UserRole.Host,
                    DriverLicenseNo = "GPLX-A1-79A123",
                    DriverLicenseStatus = DriverLicenseStatus.Approved
                };
                context.Users.Add(host);
            }

            var renter = context.Users.FirstOrDefault(u => u.Email == "khachthue@gmail.com");
            if (renter == null)
            {
                renter = new User
                {
                    FullName = "Trần Khách Thuê (Minh Tuấn)",
                    Email = "khachthue@gmail.com",
                    Phone = "0903000003",
                    PasswordHash = "renter123",
                    Role = UserRole.Renter,
                    DriverLicenseNo = "GPLX-A1-88B999",
                    DriverLicenseStatus = DriverLicenseStatus.Approved
                };
                context.Users.Add(renter);
            }

            var pendingRenter = context.Users.FirstOrDefault(u => u.Email == "khach_cho_duyet@gmail.com");
            if (pendingRenter == null)
            {
                pendingRenter = new User
                {
                    FullName = "Lê Hoàng Nam (Chờ Duyệt GPLX)",
                    Email = "khach_cho_duyet@gmail.com",
                    Phone = "0904000004",
                    PasswordHash = "123456",
                    Role = UserRole.Renter,
                    DriverLicenseNo = "GPLX-A1-99C123",
                    DriverLicenseImage = "https://images.unsplash.com/photo-1589829545856-d10d557cf95f?w=600",
                    DriverLicenseStatus = DriverLicenseStatus.Pending
                };
                context.Users.Add(pendingRenter);
            }
            context.SaveChanges();

            // 2. Seed Xe máy điện (12 xe phân bổ 4 nhóm) nếu chưa có
            if (!context.Vehicles.Any())
            {

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
                ThumbnailUrl = EcoBikeImageHelper.GetVehicleThumbnail("Evo200 Lite"),
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
                ThumbnailUrl = EcoBikeImageHelper.GetVehicleThumbnail("Ocean"),
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
                ThumbnailUrl = EcoBikeImageHelper.GetVehicleThumbnail("Aura S"),
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
                ThumbnailUrl = EcoBikeImageHelper.GetVehicleThumbnail("Voltguard"),
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
                ThumbnailUrl = EcoBikeImageHelper.GetVehicleThumbnail("Evo200 (Pin"),
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
                ThumbnailUrl = EcoBikeImageHelper.GetVehicleThumbnail("Feliz S"),
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
                ThumbnailUrl = EcoBikeImageHelper.GetVehicleThumbnail("Camel"),
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
                ThumbnailUrl = EcoBikeImageHelper.GetVehicleThumbnail("Quantum"),
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
                ThumbnailUrl = EcoBikeImageHelper.GetVehicleThumbnail("Klara S"),
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
                ThumbnailUrl = EcoBikeImageHelper.GetVehicleThumbnail("Vento S"),
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
                ThumbnailUrl = EcoBikeImageHelper.GetVehicleThumbnail("Weaver++"),
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
                ThumbnailUrl = EcoBikeImageHelper.GetVehicleThumbnail("Theon S"),
                Description = "Flagship đỉnh cao nhất của VinFast! Động cơ Center Motor 7100W truyền động dây curoa, phanh đĩa ABS 2 kênh Continental, giảm xóc Showa, PAAK mở khóa qua điện thoại.",
                FeaturesJson = "[\"Công suất khủng 7100W\", \"ABS 2 kênh Continental\", \"Giảm xóc Showa\", \"Tốc độ 99km/h\"]",
                Status = VehicleStatus.Active
            };

                context.Vehicles.AddRange(v_lite, v_yadea_ocean, v_pega, v_yadea_volt, v1, v3, v_selex, v2, v4, v6, v_weaver, v_theon);
                context.SaveChanges();
            }

            // Luôn tự động làm sạch và đồng bộ hình ảnh xe máy điện chính hãng chất lượng cao (loại bỏ triệt để ảnh rác Unsplash)
            var allVehicles = context.Vehicles.Include(v => v.Images).ToList();
            if (allVehicles.Any())
            {
                bool hasImageUpdate = false;
                foreach (var v in allVehicles)
                {
                    // Cập nhật Thumbnail chuẩn xác từ EcoBikeImageHelper
                    var correctThumb = EcoBikeImageHelper.GetVehicleThumbnail(v.Model, null);
                    if (v.ThumbnailUrl != correctThumb)
                    {
                        v.ThumbnailUrl = correctThumb;
                        hasImageUpdate = true;
                    }

                    // Đồng bộ chuẩn xác 100% bộ 2 ảnh nổi bật nhất cho từng dòng xe (1 Thumbnail + 1 ảnh chi tiết nổi bật)
                    var dedicatedGallery = EcoBikeImageHelper.GetDedicatedVehicleGallery(v.Brand, v.Model, correctThumb);
                    var expectedUrls = dedicatedGallery.Skip(1).Select(x => x.Url).ToList();
                    var currentUrls = v.Images?.Select(i => i.ImageUrl).ToList() ?? new List<string>();

                    bool needsRefresh = !currentUrls.SequenceEqual(expectedUrls);
                    if (needsRefresh)
                    {
                        if (v.Images != null && v.Images.Any())
                        {
                            context.VehicleImages.RemoveRange(v.Images);
                        }
                        foreach (var item in dedicatedGallery.Skip(1))
                        {
                            context.VehicleImages.Add(new VehicleImage
                            {
                                VehicleId = v.Id,
                                ImageUrl = item.Url,
                                IsThumbnail = false
                            });
                        }
                        hasImageUpdate = true;
                    }
                }
                if (hasImageUpdate)
                {
                    context.SaveChanges();
                }
            }

            // 3. Seed Bookings mẫu phục vụ thuyết trình bảo vệ đồ án
            if (!context.Bookings.Any(b => b.BookingCode == "EB-DEMO-PAY"))
            {
                var v_evo = context.Vehicles.FirstOrDefault(v => v.Model.Contains("Evo200") && v.RequiresLicenseA1) 
                            ?? context.Vehicles.First();
                var v_feliz = context.Vehicles.FirstOrDefault(v => v.Model.Contains("Feliz S")) 
                              ?? context.Vehicles.First();
                var v_quantum = context.Vehicles.FirstOrDefault(v => v.Model.Contains("Quantum")) 
                                ?? context.Vehicles.First();
                var v_vento = context.Vehicles.FirstOrDefault(v => v.Model.Contains("Vento S")) 
                             ?? context.Vehicles.First();

                // Đơn 1: Chờ thanh toán cọc VietQR 30%
                var b_pay = new Booking
                {
                    BookingCode = "EB-DEMO-PAY",
                    RenterId = renter.Id,
                    VehicleId = v_evo.Id,
                    StartTime = DateTime.UtcNow.AddDays(1),
                    EndTime = DateTime.UtcNow.AddDays(3),
                    TotalDays = 2,
                    RentalFee = 240000,
                    InsuranceFee = 30000,
                    TotalAmount = 270000,
                    DepositAmount = 81000,
                    Status = BookingStatus.AwaitingPayment,
                    PaymentMethod = "VIETQR"
                };

                // Đơn 2: Đã cọc thành công -> Chờ nhận xe & Bàn giao xe (Check-in ODO, pin %)
                var b_checkin = new Booking
                {
                    BookingCode = "EB-DEMO-CHECKIN",
                    RenterId = renter.Id,
                    VehicleId = v_feliz.Id,
                    StartTime = DateTime.UtcNow.AddHours(2),
                    EndTime = DateTime.UtcNow.AddDays(2),
                    TotalDays = 2,
                    RentalFee = 260000,
                    InsuranceFee = 30000,
                    TotalAmount = 290000,
                    DepositAmount = 87000,
                    Status = BookingStatus.Confirmed,
                    PaymentMethod = "VIETQR",
                    PaymentBankCode = "MBBANK",
                    PaymentTransactionId = "TXN-MB-98231",
                    DepositPaidAt = DateTime.UtcNow.AddHours(-1)
                };

                // Đơn 3: Đang trong hành trình thuê -> Chờ trả xe & Quyết toán 70% (Checkout ODO, quá km)
                var b_checkout = new Booking
                {
                    BookingCode = "EB-DEMO-CHECKOUT",
                    RenterId = renter.Id,
                    VehicleId = v_quantum.Id,
                    StartTime = DateTime.UtcNow.AddDays(-2),
                    EndTime = DateTime.UtcNow.AddHours(1),
                    TotalDays = 2,
                    RentalFee = 360000,
                    InsuranceFee = 30000,
                    TotalAmount = 390000,
                    DepositAmount = 117000,
                    Status = BookingStatus.InProgress,
                    PaymentMethod = "VIETQR",
                    PaymentBankCode = "VPBANK",
                    PaymentTransactionId = "TXN-VP-87123",
                    DepositPaidAt = DateTime.UtcNow.AddDays(-2),
                    CheckinOdo = 4000,
                    CheckinBatteryPercent = 100,
                    CheckinTime = DateTime.UtcNow.AddDays(-2)
                };

                // Đơn 4: Đã hủy / Bị chủ xe từ chối kèm lý do
                var b_cancelled = new Booking
                {
                    BookingCode = "EB-DEMO-CANCELLED",
                    RenterId = renter.Id,
                    VehicleId = v_vento.Id,
                    StartTime = DateTime.UtcNow.AddDays(4),
                    EndTime = DateTime.UtcNow.AddDays(6),
                    TotalDays = 2,
                    RentalFee = 400000,
                    InsuranceFee = 30000,
                    TotalAmount = 430000,
                    DepositAmount = 129000,
                    Status = BookingStatus.Cancelled,
                    CancellationReason = "Xe gặp sự cố pin LFP đang chờ kỹ thuật viên bảo hành."
                };

                // Đơn 5: Đã hoàn tất và có đánh giá 5 sao
                var b1 = new Booking
                {
                    BookingCode = "EB-SAMPLE-001",
                    RenterId = renter.Id,
                    VehicleId = v_evo.Id,
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

                // Đơn 6: Đã hoàn tất
                var b2 = new Booking
                {
                    BookingCode = "EB-SAMPLE-002",
                    RenterId = renter.Id,
                    VehicleId = v_quantum.Id,
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

                context.Bookings.AddRange(b_pay, b_checkin, b_checkout, b_cancelled, b1, b2);
                context.SaveChanges();

                // 4. Seed Reviews mẫu nếu chưa có
                if (!context.Reviews.Any())
                {
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
    }
}
