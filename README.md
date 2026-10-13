# ⚡ EcoBike — Nền tảng Cho thuê Xe máy điện P2P Thông minh

**EcoBike** (`MiotoCarRental`) là hệ thống web kết nối trực tiếp **Chủ xe máy điện nhàn rỗi (Host)** và **Khách thuê xe (Customer)** theo mô hình kinh tế chia sẻ (Peer-to-Peer), được xây dựng trên nền tảng **ASP.NET Core 9.0 MVC**, **Entity Framework Core (SQLite)** và **Tailwind CSS**.

---

## 👥 Thành viên Nhóm thực hiện

| STT | Họ và Tên | Vai trò | Nhiệm vụ phụ trách |
| :-: | :--- | :--- | :--- |
| **1** | **Trần Nhất Tâm** | **Backend Developer (BE)** | Thiết kế CSDL SQLite, Đăng nhập & Phân quyền 3 vai trò (RBAC), Nghiệp vụ Đặt xe, Duyệt xe, Docker VPS |
| **2** | **Đặng Sỹ Phố** | **Frontend Developer (FE)** | Thiết kế giao diện người dùng (Navbar, Trang chủ, 4 phân khúc xe, Chi tiết xe, Kênh Chủ xe, Admin) |
| **3** | **Nguyễn Phương Nam** | **Báo cáo & Kiểm thử (BA/QA)** | Phân tích yêu cầu & Viết tài liệu SRS, Lập Product Backlog & Jira, Kiểm thử chức năng & Báo cáo |

---

## 🌟 Tính năng nổi bật

1. **Phân loại 4 nhóm xe máy điện thông minh (`VehicleCategory`):**
   - **Phổ thông / Học sinh - Sinh viên (`Economy`):** Vận tốc $\le 50\text{ km/h}$, **không yêu cầu bằng lái A1** (VinFast Evo200 Lite, Yadea Odora S, Pega AuraS+...).
   - **Tầm trung / Đi phố (`MidRange`):** Quãng đường $90 - 150\text{ km}$/sạc, cốp rộng (VinFast Feliz S, Klara S, Honda EM1 e:...).
   - **Đi phượt / Pin trâu (`Touring`):** Quãng đường lên đến $198 - 203\text{ km}$/sạc (VinFast Vento S, Evo200, Dat Bike Weaver++).
   - **Thể thao / Cao cấp (`SportPremium`):** Công suất lớn $5000\text{W} - 6000\text{W}$, phanh ABS, tăng tốc vượt trội (VinFast Theon S, Dat Bike Quantum, BMW CE 04).
2. **Đặt xe & Tính phí tự động (`BookingsController`):**
   - Kiểm tra trùng lịch thuê theo thời gian thực.
   - Tự động tính tổng tiền thuê theo số ngày và áp dụng mã giảm giá (`DiscountCoupon`).
3. **Kênh Chủ xe (`HostController`):**
   - Đăng ký xe cho thuê (Tự động chuyển trạng thái `PendingApproval` chờ Admin duyệt).
   - Quản lý danh sách xe, theo dõi doanh thu và duyệt/hoàn thành đơn đặt xe của khách.
4. **Hệ thống Quản trị (`AdminController`):**
   - Dashboard thống kê tổng quan hệ thống.
   - Phê duyệt/từ chối xe đăng ký mới của Chủ xe (`ApproveVehicle` / `RejectVehicle`).
   - Quản lý người dùng, đơn đặt xe và mã khuyến mãi.
5. **Kiểm thử tự động (Unit & Integration Tests):**
   - Bộ **46 Unit Tests (xUnit)** kiểm thử toàn diện: Phân quyền RBAC, Đặt xe, Duyệt xe Admin, Mã giảm giá và Phân loại xe.

---

## 🛠️ Công nghệ sử dụng (Tech Stack)

| Tầng (Layer) | Công nghệ |
| :--- | :--- |
| **Frontend (FE)** | Razor Views (`.cshtml`), Tailwind CSS, Lucide Icons, Responsive Glassmorphism UI |
| **Backend (BE)** | C# 13, ASP.NET Core 9.0 MVC, Cookie Authentication & Role Authorization |
| **Database (DB)** | SQLite (`mioto.db`) + Entity Framework Core 9.0 (Code-First & Auto-Seeding) |
| **Testing** | xUnit, In-Memory & Temporary SQLite Database Testing (46/46 Pass) |
| **DevOps / Deploy** | Docker, Docker Compose, GitHub |

---

## 🔐 Tài khoản Demo (Tự động khởi tạo khi chạy App)

Hệ thống tự động nạp sẵn **12 mẫu xe máy điện** và **3 tài khoản phân quyền** ngay trong lần chạy đầu tiên:

| Vai trò (Role) | Email đăng nhập | Mật khẩu | Quyền hạn chính |
| :--- | :--- | :--- | :--- |
| **Quản trị viên (`Admin`)** | `admin@mioto.vn` | `123456` | Toàn quyền hệ thống, duyệt xe mới, quản lý User/Booking/Coupon, Kênh chủ xe & Thuê xe |
| **Chủ xe (`Host`)** | `host@mioto.vn` | `123456` | Đăng xe mới lên sàn, quản lý đội xe cá nhân, xác nhận đơn khách đặt & Thuê xe |
| **Khách thuê (`Customer`)** | `khach@mioto.vn` | `123456` | Tìm kiếm/lọc xe theo 4 phân khúc, đặt xe, áp mã giảm giá, xem lịch sử chuyến đi |

> 💡 **Mẹo khi Demo:** Trên trang `/Account/Login`, bạn có thể bấm trực tiếp vào **3 nút điền nhanh tài khoản Demo** để chuyển đổi vai trò chỉ với 1 cú click.

---

## 🚀 Hướng dẫn cài đặt & Chạy dự án

### Cách 1: Chạy trực tiếp bằng .NET CLI hoặc Visual Studio 2022
```bash
# 1. Di chuyển vào thư mục dự án
cd MiotoCarRental

# 2. Chạy ứng dụng (Database SQLite mioto.db sẽ tự động được tạo và nạp dữ liệu mẫu)
dotnet run
```
Truy cập ứng dụng tại: **`http://localhost:5287`**

### Cách 2: Chạy bộ Kiểm thử tự động (Unit Tests)
```bash
dotnet test
```

### Cách 3: Triển khai lên VPS bằng Docker Compose
```bash
docker compose up -d --build
```
Ứng dụng sẽ chạy ở chế độ Production tại cổng **`8080`** (`http://<IP-VPS>:8080`).

---

## 📂 Tài liệu Dự án (SRS & Agile Backlog)

- 📄 **Đặc tả Yêu cầu Phần mềm (SRS chuẩn IEEE):** Xem trong thư mục `docs/SRS_EcoBike_He_Thong_Thue_Xe_May_Dien.docx`
- 📊 **Product & Sprint Backlog (Excel & Word):** Xem trong thư mục `docs/Product_Backlog_EcoBike.xlsx`
- 📋 **File Import Jira (`.csv`):** Xem trong thư mục `docs/Jira_Backlog_Import_EcoBike.csv`
