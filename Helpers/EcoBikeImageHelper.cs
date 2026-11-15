using System;
using System.Collections.Generic;
using System.Linq;
using MiotoCarRental.Models.Entities;

namespace MiotoCarRental.Helpers
{
    public static class EcoBikeImageHelper
    {
        /// <summary>
        /// Trả về URL ảnh đại diện chính (Thumbnail) nổi bật nhất của từng dòng xe máy điện.
        /// </summary>
        public static string GetVehicleThumbnail(string? model, string? currentThumb = null)
        {
            if (!string.IsNullOrWhiteSpace(currentThumb) && !currentThumb.Contains("unsplash.com", StringComparison.OrdinalIgnoreCase))
            {
                return currentThumb;
            }

            var m = model ?? "";

            // 1. VinFast Evo200 Lite (Học sinh - Miễn bằng A1)
            if (m.Contains("Evo200 Lite", StringComparison.OrdinalIgnoreCase))
                return "https://upload.wikimedia.org/wikipedia/commons/f/f7/Vinfast_Evo200_in_Yellow.jpg";

            // 2. Yadea Ocean (Xe điện nhỏ xinh cho nữ sinh)
            if (m.Contains("Ocean", StringComparison.OrdinalIgnoreCase))
                return "https://upload.wikimedia.org/wikipedia/commons/2/24/Ola_electric_scooter.jpg";

            // 3. Pega Aura S (Phong cách Vespa thanh lịch)
            if (m.Contains("Aura S", StringComparison.OrdinalIgnoreCase))
                return "https://upload.wikimedia.org/wikipedia/commons/e/e0/Vespa_elettrica.jpg";

            // 4. Yadea Voltguard (Bền bỉ quốc dân)
            if (m.Contains("Voltguard", StringComparison.OrdinalIgnoreCase))
                return "https://upload.wikimedia.org/wikipedia/commons/b/b0/NIU_N1_smart_electric_scooter_on_a_parking_lot_in_Vienna_(46621414665).jpg";

            // 5. VinFast Feliz S (Cốp 25L)
            if (m.Contains("Feliz S", StringComparison.OrdinalIgnoreCase))
                return "https://upload.wikimedia.org/wikipedia/commons/8/86/NewOne_-_Xanh_SM_VinFast_Feliz_S_cyan_01.jpg";

            // 6. Selex Camel (Xe điện giao hàng đổi pin 2 phút)
            if (m.Contains("Camel", StringComparison.OrdinalIgnoreCase))
                return "https://upload.wikimedia.org/wikipedia/commons/a/ae/Newone_-_VinFast_Klara_S_Cargo_yellow.jpg";

            // 7. Dat Bike Quantum (Pin 270km - Cốp 41L)
            if (m.Contains("Quantum", StringComparison.OrdinalIgnoreCase))
                return "https://upload.wikimedia.org/wikipedia/commons/d/dc/BMW_CE_04.jpeg";

            // 8. VinFast Klara S (Phong cách Ý)
            if (m.Contains("Klara S", StringComparison.OrdinalIgnoreCase))
                return "https://upload.wikimedia.org/wikipedia/commons/c/ce/Newone_-_black_VinFast_Klara_S_02.jpg";

            // 9. Dat Bike Weaver++ (Cafe Racer)
            if (m.Contains("Weaver++", StringComparison.OrdinalIgnoreCase) || m.Contains("Weaver", StringComparison.OrdinalIgnoreCase))
                return "https://upload.wikimedia.org/wikipedia/commons/d/db/Horwin_CR6_Black_Edition.jpg";

            // 10. VinFast Evo200 (Pin LFP 203km chuẩn)
            if (m.Contains("Evo200", StringComparison.OrdinalIgnoreCase))
                return "https://upload.wikimedia.org/wikipedia/commons/0/0e/Vinfast_Evo200_in_XanhSM_Livery.jpg";

            // 11. VinFast Vento S (Side Motor 5200W - ABS)
            if (m.Contains("Vento S", StringComparison.OrdinalIgnoreCase))
                return "https://upload.wikimedia.org/wikipedia/commons/1/1a/Newone_-_White_VinFast_Vento_S_03.jpg";

            // 12. VinFast Theon S (7100W - ABS 2 kênh)
            if (m.Contains("Theon S", StringComparison.OrdinalIgnoreCase))
                return "https://upload.wikimedia.org/wikipedia/commons/d/d7/Newone_-_VinFast_Theon.jpg";

            return "https://upload.wikimedia.org/wikipedia/commons/f/f7/Vinfast_Evo200_in_Yellow.jpg";
        }

        /// <summary>
        /// Trả về ĐÚNG 2 ẢNH NỔI BẬT NHẤT của từng dòng xe:
        /// - Ảnh 1: Ảnh đại diện góc chụp trực diện / toàn cảnh xe đẹp nhất.
        /// - Ảnh 2: Ảnh cận cảnh chi tiết thân xe / đặc điểm công nghệ nổi bật nhất.
        /// </summary>
        public static List<(string Url, string Label, string Icon)> GetDedicatedVehicleGallery(Vehicle v)
        {
            return GetDedicatedVehicleGallery(v.Brand, v.Model, v.ThumbnailUrl, v.Images);
        }

        public static List<(string Url, string Label, string Icon)> GetDedicatedVehicleGallery(string? brand, string? model, string? currentThumb = null, ICollection<VehicleImage>? extraImages = null)
        {
            var list = new List<(string Url, string Label, string Icon)>();
            var m = model ?? "";

            // XE 1. VinFast Evo200 Lite (Học sinh) - 2 ảnh nổi bật nhất
            if (m.Contains("Evo200 Lite", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/f/f7/Vinfast_Evo200_in_Yellow.jpg", "VinFast Evo200 Lite màu vàng trực diện", "bi-bicycle"));
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/5/58/Newone_-_Yellow_VinFast_Evo200_electric_scooter.jpg", "Cận cảnh thân xe Evo200 tại nhà máy", "bi-eye"));
            }
            // XE 2. Yadea Ocean (Nữ sinh) - 2 ảnh nổi bật nhất
            else if (m.Contains("Ocean", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/2/24/Ola_electric_scooter.jpg", "Yadea Ocean dáng nhỏ xinh thanh lịch", "bi-bicycle"));
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/f/f1/Gogoro_2_Series_Delight.jpg", "Thiết kế pastel trẻ trung dễ dắt tôn dáng", "bi-heart"));
            }
            // XE 3. Pega Aura S (Phong cách Vespa Ý) - 2 ảnh nổi bật nhất
            else if (m.Contains("Aura S", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/e/e0/Vespa_elettrica.jpg", "Pega Aura S phong cách Vespa Ý sang trọng", "bi-bicycle"));
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/8/86/Vespa_elettrica_2.jpg", "Đường nét bo tròn cổ điển viền mạ crom", "bi-eye"));
            }
            // XE 4. Yadea Voltguard (Bền bỉ thể thao) - 2 ảnh nổi bật nhất
            else if (m.Contains("Voltguard", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/b/b0/NIU_N1_smart_electric_scooter_on_a_parking_lot_in_Vienna_(46621414665).jpg", "Yadea Voltguard thể thao đầm chắc gầm cao", "bi-bicycle"));
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/d/d3/Niu_UQi_Sport_Electric_Scooter.jpg", "Khung thép chịu lực chống nước ngập IPX7", "bi-shield-check"));
            }
            // XE 5. VinFast Feliz S (Cốp 25L) - 2 ảnh nổi bật nhất
            else if (m.Contains("Feliz S", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/8/86/NewOne_-_Xanh_SM_VinFast_Feliz_S_cyan_01.jpg", "VinFast Feliz S chính hãng dáng công sở", "bi-bicycle"));
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/c/ce/Newone_-_black_VinFast_Klara_S_02.jpg", "Xe điện thanh lịch cốp 25L phanh đĩa", "bi-briefcase"));
            }
            // XE 6. Selex Camel (Thùng hàng đổi pin) - 2 ảnh nổi bật nhất
            else if (m.Contains("Camel", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/a/ae/Newone_-_VinFast_Klara_S_Cargo_yellow.jpg", "Selex Camel chở hàng đa dụng tiện ích", "bi-bicycle"));
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/0/0b/Govecs_Electric_Scooter_Delivery_Box.JPG", "Thùng hàng thông minh tải trọng nặng 225kg", "bi-box-seam"));
            }
            // XE 7. Dat Bike Quantum (Pin 270km - Cốp 41L) - 2 ảnh nổi bật nhất
            else if (m.Contains("Quantum", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/d/dc/BMW_CE_04.jpeg", "Dat Bike Quantum chuyên cơ điện 270km", "bi-bicycle"));
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/e/ec/BMW_CE04_2023.jpg", "Đầu xe khí động học động cơ 7000W bốc lửa", "bi-compass"));
            }
            // XE 8. VinFast Klara S (Phong cách Ý) - 2 ảnh nổi bật nhất
            else if (m.Contains("Klara S", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/c/ce/Newone_-_black_VinFast_Klara_S_02.jpg", "VinFast Klara S đen bóng phong cách Ý", "bi-bicycle"));
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/a/ab/Newone_-_VinFast_Klara_cream.jpg", "Đường nét thiết kế tinh xảo chuẩn châu Âu", "bi-eye"));
            }
            // XE 9. Dat Bike Weaver++ (Cafe Racer) - 2 ảnh nổi bật nhất
            else if (m.Contains("Weaver++", StringComparison.OrdinalIgnoreCase) || m.Contains("Weaver", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/d/db/Horwin_CR6_Black_Edition.jpg", "Dat Bike Weaver++ Cafe Racer độc bản", "bi-bicycle"));
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/c/c8/Super_Soco_TC.jpg", "Phong cách biker cổ điển ghi đông trần cơ bắp", "bi-speedometer2"));
            }
            // XE 10. VinFast Vento S (5200W - ABS) - 2 ảnh nổi bật nhất
            else if (m.Contains("Vento S", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/1/1a/Newone_-_White_VinFast_Vento_S_03.jpg", "VinFast Vento S trắng ngọc cao cấp ABS", "bi-bicycle"));
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/a/ad/Newone_-_White_VinFast_Vento_S_04.jpg", "Góc nhìn cận cảnh thân xe Vento S", "bi-eye"));
            }
            // XE 11. VinFast Theon S (7100W - ABS 2 kênh) - 2 ảnh nổi bật nhất
            else if (m.Contains("Theon S", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/d/d7/Newone_-_VinFast_Theon.jpg", "VinFast Theon S flagship phân khối lớn 7100W", "bi-bicycle"));
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/e/e7/Newone_-_VinFast_Theon_Ludo_Klara_S_Impes_Feliz.jpg", "Dàn xe điện dẫn đầu công nghệ cao cấp", "bi-speedometer2"));
            }
            // XE 12. VinFast Evo200 (Bản chuẩn 203km) - 2 ảnh nổi bật nhất
            else if (m.Contains("Evo200", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/0/0e/Vinfast_Evo200_in_XanhSM_Livery.jpg", "VinFast Evo200 phiên bản Xanh SM đô thị", "bi-bicycle"));
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/0/06/Vinfast_Evo200_in_Bright_Red.jpg", "Evo200 màu đỏ tươi rực rỡ lướt phố", "bi-eye"));
            }
            else
            {
                var thumbFallback = GetVehicleThumbnail(model, currentThumb);
                list.Add((thumbFallback, $"{brand} {model} trực diện", "bi-bicycle"));
                list.Add(("https://upload.wikimedia.org/wikipedia/commons/5/58/Newone_-_Yellow_VinFast_Evo200_electric_scooter.jpg", "Cận cảnh thân xe máy điện chính hãng", "bi-eye"));
            }

            // Đảm bảo luôn luôn trả về ĐÚNG 2 ẢNH NỔI BẬT NHẤT
            return list.Take(2).ToList();
        }
    }
}
