# 🎬 KỊCH BẢN THUYẾT TRÌNH & DEMO ĐỒ ÁN ECOBIKE
## ĐỀ TÀI: NỀN TẢNG CHO THUÊ VÀ CHIA SẺ XE MÁY ĐIỆN P2P THÔNG MINH
**Hội đồng chấm thi / Khách hàng:** ThS. Lê Thị Bảo Trân  
**Thời lượng dự kiến:** 15 phút (10 phút thuyết trình & Demo + 5 phút Hỏi đáp Q&A)

---

## 👥 PHÂN VAI & PHỤ TRÁCH THUYẾT TRÌNH

| STT | Thành viên | Vai trò phụ trách | Nhiệm vụ trong buổi thuyết trình |
| :-: | :--- | :--- | :--- |
| **1** | **Trần Nhất Tâm** | **Trưởng nhóm / Backend & Database** | • Mở đầu, lý do chọn đề tài, mô hình P2P.<br>• Trình bày Kiến trúc hệ thống Non-MVC (.NET 9.0 Web API, 3-Layer Services).<br>• Demo luồng Khách thuê: Tìm xe 4 phân khúc, cọc 30% VietQR, hợp đồng điện tử. |
| **2** | **Đặng Sỹ Phố** | **Frontend & UI/UX Developer** | • Trình bày thiết kế giao diện (Tailwind CSS, React 18 SPA, Responsive Mobile).<br>• Demo luồng Chủ xe (Host): Đăng ký xe mới, quản lý lịch cho thuê, duyệt đơn. |
| **3** | **Nguyễn Phương Nam** | **Kiểm thử, DevOps & Admin** | • Trình bày tài liệu SRS, Jira Backlog, bộ 46 Unit Tests xUnit (100% Pass).<br>• Demo Kênh Quản trị (Admin): Biểu đồ đường & cột Chart.js, duyệt GPLX, sửa/xóa xe.<br>• Kết luận & điều phối phần trả lời câu hỏi phản biện. |

---

## ⏱️ TIẾN TRÌNH THUYẾT TRÌNH CHI TIẾT (TỪNG PHÚT)

### 🟢 PHẦN 1: MỞ ĐẦU & TỔNG QUAN ĐỀ TÀI (00:00 – 03:00) — Người nói: Trần Nhất Tâm
* **Slide trình chiếu:** Slide 1 (Bìa) & Slide 2 (Mục tiêu & Bài toán thực tế).
* **Lời thoại mẫu:**
  > *"Kính thưa cô Lê Thị Bảo Trân và các bạn,*  
  > *Hôm nay, nhóm em xin phép được báo cáo và demo sản phẩm đồ án: **Hệ thống cho thuê xe máy điện thông minh EcoBike**.*  
  > *Xuất phát từ thực tế xu hướng chuyển dịch xanh tại Việt Nam (VinFast, Dat Bike) và nhu cầu thuê xe ngắn ngày của học sinh, sinh viên, khách du lịch; nhóm em đã xây dựng nền tảng kết nối trực tiếp giữa **Chủ xe nhàn rỗi (Host)** và **Khách thuê (Renter)** theo mô hình kinh tế chia sẻ P2P tương tự Mioto hay Airbnb.*  
  > *Về mặt công nghệ, nhóm quyết định không đi theo lối mòn của ASP.NET Core MVC truyền thống, mà triển khai theo kiến trúc hiện đại tách biệt rõ ràng: **Backend Minimal Web API trên nền .NET 9.0**, tầng nghiệp vụ **3-Layer Services**, giao diện **Razor Pages MVVM kết hợp React 18 SPA**, cơ sở dữ liệu **SQLite** và bộ kiểm thử tự động **46 Unit Tests**."*

---

### 🟢 PHẦN 2: DEMO LUỒNG KHÁCH THUÊ XE & THANH TOÁN (03:00 – 06:30) — Người nói: Trần Nhất Tâm
* **Thao tác trên màn hình:** Mở trình duyệt tại `localhost:5287` hoặc `https://miotocarrental.id.vn`.
* **Kịch bản thao tác & Lời thoại:**
  1. **Bước 1 — Tìm kiếm & Lọc xe (`/Vehicles`):**
     > *"Đầu tiên, em đóng vai trò là một Khách thuê xe. Khi vào trang danh mục xe, em có thể lọc nhanh theo 4 nhóm phân khúc: ví dụ nhóm **Phổ thông (Miễn bằng lái A1)** dành riêng cho học sinh sinh viên, nhóm xe đi phượt đường dài, hoặc xe thể thao.*  
     > *Hệ thống hỗ trợ đầy đủ bộ lọc theo thành phố, hãng xe VinFast/Dat Bike và tầm hoạt động pin."*
  2. **Bước 2 — Chi tiết xe & Đặt xe (`/Vehicles/Details/{id}`):**
     > *"Khi bấm vào chi tiết một chiếc VinFast Feliz S, khách hàng có thể xem đầy đủ thông số: dung lượng pin LFP, tốc độ tối đa, các phụ phí vượt km và đánh giá từ khách trước.*  
     > *Em chọn ngày nhận và ngày trả xe. Hệ thống tự động kích hoạt thuật toán **kiểm tra chống trùng lịch (Double Booking Prevention)** và tính toán chi phí: tiền thuê, phí bảo hiểm và khoản **tiền cọc 30% giữ chỗ** đúng chuẩn nghiệp vụ P2P."*
  3. **Bước 3 — Quét mã cọc VietQR & Hợp đồng điện tử (`/Bookings/Details/{id}`):**
     > *"Sau khi bấm đặt xe, hệ thống điều hướng sang trang chi tiết đơn hàng và sinh **Mã VietQR động** kèm số tiền cọc và mã đơn. Em bấm nút giả lập thanh toán thành công &rarr; Đơn hàng lập tức chuyển sang trạng thái 'Đã cọc giữ chỗ'. Khách hàng có thể bấm vào nút **Xem Hợp đồng điện tử** để đọc các điều khoản cam kết pháp lý."*

---

### 🔵 PHẦN 3: DEMO KÊNH CHỦ XE & THIẾT KẾ GIAO DIỆN (06:30 – 09:30) — Người nói: Đặng Sỹ Phố
* **Thao tác trên màn hình:** Đăng nhập tài khoản `chuxe@gmail.com` / Mật khẩu `123456`.
* **Kịch bản thao tác & Lời thoại:**
  1. **Thiết kế giao diện & Mobile Responsive:**
     > *"Chào cô, tiếp theo em xin trình bày về giao diện và Kênh Chủ xe (Host).*  
     > *Giao diện EcoBike được nhóm thiết kế với tone màu **Bright Emerald (Xanh ngọc tươi sáng)** mang tinh thần năng lượng xanh. Toàn bộ các trang từ danh mục xe, bộ lọc đến dashboard đều được tối ưu hóa **Responsive 100% trên thiết bị di động**, trang bị hệ thống **Floating Toast Notification** tự động thông báo và biến mất sau 4.5 giây."*
  2. **Kênh quản lý Chủ xe (`/Host`):**
     > *"Với vai trò Chủ xe, em có thể xem ngay thống kê đội xe của mình, doanh thu thực nhận (sau khi trừ 15% phí sàn).*  
     > *Chủ xe có thể bấm **Đăng xe mới**, bật/tắt trạng thái cho thuê xe linh hoạt, hoặc duyệt các đơn đặt xe đang chờ xử lý."*

---

### 🟣 PHẦN 4: DEMO QUẢN TRỊ ADMIN, KIỂM THỬ & BẢO MẬT (09:30 – 12:30) — Người nói: Nguyễn Phương Nam
* **Thao tác trên màn hình:** Đăng nhập `admin@mioto.vn` / Mật khẩu `123456` &rarr; vào trang `/Admin`.
* **Kịch bản thao tác & Lời thoại:**
  1. **Bảo mật đăng nhập Admin:**
     > *"Kính thưa cô, vừa qua nhóm đã nâng cấp cơ chế bảo mật: tài khoản Admin **bắt buộc phải nhập mật khẩu chính xác** mới được phép truy cập, hệ thống tự động loại bỏ và ngăn chặn mọi hình thức đăng nhập nhanh lậu."*
  2. **Biểu đồ thống kê trực quan (Chart.js):**
     > *"Tại trung tâm điều hành `/Admin`, nhóm tích hợp 2 biểu đồ trực quan:*  
     > *1. **Biểu đồ đường:** Phân tích xu hướng tăng trưởng tổng giao dịch GMV và 15% phí sàn qua 6 tháng.*  
     > *2. **Biểu đồ cột:** Thống kê phân bổ số lượng xe theo 4 phân khúc thị trường."*
  3. **Bộ công cụ Quản trị Sửa / Xóa & Duyệt GPLX:**
     > *"Admin có toàn quyền:*  
     > *• Phê duyệt hoặc từ chối hồ sơ GPLX hạng A1 của khách thuê.*  
     > *• Nút **Sửa giá thuê** trực tiếp và nút **Xóa xe** khỏi hệ thống (kèm cơ chế bảo vệ an toàn nếu xe đang có khách thuê).*  
     > *• Quản lý và phân quyền vai trò người dùng (Admin, Host, Renter)."*
  4. **Kiểm thử tự động & Tài liệu:**
     > *"Hệ thống được kiểm soát chất lượng bằng **46 Unit Tests** viết bằng xUnit, kiểm thử toàn bộ công thức tính tiền cọc, phụ phí và phân quyền, đạt tỷ lệ Passed 100%. Nhóm cũng đã hoàn thiện đầy đủ tài liệu SRS chuẩn IEEE, Product Backlog và file import Jira."*

---

### ⚪ PHẦN 5: KẾT LUẬN & PHẢN BIỆN (12:30 – 15:00) — Cả nhóm
* **Lời kết:**
  > *"Dạ thưa cô, trên đây là toàn bộ phần báo cáo và demo hệ thống EcoBike của nhóm em. Hệ thống đã sẵn sàng vận hành thực tế. Chúng em xin lắng nghe nhận xét và câu hỏi phản biện từ cô ạ!"*

---

## 🎯 BỘ CÂU HỎI PHẢN BIỆN THƯỜNG GẶP CỦA GIẢNG VIÊN & CÂU TRẢ LỜI CHUẨN

### ❓ Câu hỏi 1: Hệ thống xử lý thế nào để tránh 2 khách thuê cùng đặt 1 chiếc xe trong cùng một khoảng thời gian (Double Booking)?
* **Người trả lời:** Trần Nhất Tâm (Backend).
* **Trả lời mẫu:**
  > *"Dạ thưa cô, nhóm em xử lý bài toán này ở cả 2 tầng:*  
  > *1. **Thuật toán kiểm tra giao thoa thời gian:** Khi khách chọn khoảng `[StartReq, EndReq]`, hệ thống truy vấn bảng `CalendarBlocked` với điều kiện: `(StartDate < EndReq) AND (EndDate > StartReq)`. Nếu tìm thấy bất kỳ bản ghi nào trùng khớp, hệ thống từ chối ngay lập tức.*  
  > *2. **Database Transaction:** Toàn bộ quá trình tạo đơn đặt xe và ghi đè lịch khóa được bọc trong một `BeginTransactionAsync()`. Nếu có 2 yêu cầu gửi tới đồng thời, giao dịch thứ hai sẽ bị chặn và rollback, đảm bảo tính toàn vẹn dữ liệu tuyệt đối ạ."*

### ❓ Câu hỏi 2: Tại sao nhóm không sử dụng ASP.NET Core MVC (Controller-View) truyền thống mà dùng Minimal API kết hợp Razor Pages và React?
* **Người trả lời:** Đặng Sỹ Phố (Frontend / Backend).
* **Trả lời mẫu:**
  > *"Dạ thưa cô, mô hình MVC truyền thống thường bị cồng kềnh (Fat Controller) và khó mở rộng cho ứng dụng di động hoặc đối tác bên thứ ba. Nhóm em lựa chọn kiến trúc hiện đại:*  
  > *• **Minimal Web API (`/api`):** Cung cấp các RESTful JSON Endpoint cực kỳ nhẹ và nhanh, sẵn sàng phục vụ cho ứng dụng Mobile hoặc giao diện React SPA (`/react`).*  
  > *• **Razor Pages MVVM:** Mỗi trang đi kèm một PageModel riêng biệt, tuân thủ nguyên tắc Single Responsibility, giúp mã nguồn sáng sủa và dễ bảo trì hơn nhiều so với Controller tập trung ạ."*

### ❓ Câu hỏi 3: Mật khẩu và bảo mật phân quyền trong hệ thống được triển khai ra sao?
* **Người trả lời:** Nguyễn Phương Nam (Kiểm thử & Security).
* **Trả lời mẫu:**
  > *"Dạ thưa cô, hệ thống sử dụng cơ chế **Cookie Authentication kết hợp Role-Based Access Control (RBAC)** với 3 vai trò rõ rệt: `Admin`, `Host`, `Renter`.*  
  > *Các trang nhạy cảm như `/Admin` đều được bảo vệ bởi attribute `[Authorize(Roles = "Admin")]`. Về mật khẩu, tài khoản Quản trị bắt buộc phải nhập mật khẩu khi đăng nhập, hệ thống ngăn chặn hoàn toàn việc bypass bằng tham số giả mạo. Ngoài ra, các form đều có Anti-Forgery Token chống tấn công CSRF ạ."*
