# ⚡ EcoBike — Nền tảng Cho thuê Xe máy điện P2P Thông minh

**EcoBike** (`MiotoCarRental`) là hệ thống web kết nối trực tiếp **Chủ xe máy điện nhàn rỗi (Host)** và **Khách thuê xe (Customer)** theo mô hình kinh tế chia sẻ (Peer-to-Peer), được xây dựng theo kiến trúc **3-Layer Service Architecture kết hợp ASP.NET Core 9.0 Razor Pages (MVVM `PageModel`) & Minimal Web API** (hoàn toàn không sử dụng mô hình MVC truyền thống), cùng **Entity Framework Core (SQLite)** và **Tailwind CSS**.

---

## 👥 Thành viên Nhóm thực hiện

| STT | Họ và Tên | Vai trò | Nhiệm vụ phụ trách |
| :-: | :--- | :--- | :--- |
| **1** | **Trần Nhất Tâm** | **Backend / Frontend Developer** | Thiết kế CSDL SQLite, Đăng nhập & Phân quyền 3 vai trò (RBAC), Nghiệp vụ Đặt xe, Duyệt xe, Docker VPS |
| **2** | **Đặng Sỹ Phố** | **Frontend / Backend Developer** | Thiết kế giao diện người dùng (Navbar, Trang chủ, 4 phân khúc xe, Chi tiết xe, Kênh Chủ xe, Admin) |
| **3** | **Nguyễn Phương Nam** | **Báo cáo & Kiểm thử (BA/QA)** | Phân tích yêu cầu & Viết tài liệu SRS, Lập Product Backlog & Jira, Kiểm thử chức năng & Báo cáo |

---

## 🏛️ Kiến trúc Hệ thống (Non-MVC: Razor Pages MVVM + Minimal Web API + 3-Layer Services)

Dự án tuân thủ yêu cầu **không sử dụng ASP.NET Core MVC (`Controllers/` & `Views/`)**, thay vào đó áp dụng kiến trúc hiện đại được Microsoft khuyến nghị trên .NET 9:

1. **Presentation Layer — Razor Pages MVVM (`Pages/` & `ViewModels/*PageModel`):**
   - Mỗi trang giao diện trong `Pages/` gắn trực tiếp với một lớp `PageModel` (pattern **MVVM** - Model-View-ViewModel) thông qua directive `@page` và `@model`.
   - Xử lý trọn vẹn vòng đời trang bằng các Page Handlers (`OnGetAsync`, `OnPostAsync`) mà không cần tầng `Controller` trung gian.
2. **API & Endpoint Layer — Minimal Web API (`Endpoints/EcoBikeEndpoints.cs`):**
   - Cung cấp các RESTful JSON API (`/api/vehicles`, `/api/vehicles/{id}`, `/api/stats`) và các Endpoint xử lý nghiệp vụ nhanh bằng `IEndpointRouteBuilder`.
3. **Business Logic Layer — 3-Layer Services (`Services/`):**
   - Tách biệt hoàn toàn logic nghiệp vụ vào các Service chuyên biệt: `VehicleService`, `BookingService`, `AuthService`, `CouponService`.
4. **Data Access Layer (`Data/MiotoDbContext.cs` & `Models/Entities/`):**
   - Quản lý truy xuất dữ liệu bằng Entity Framework Core 9.0 với cơ sở dữ liệu SQLite (`mioto.db`) và cơ chế Auto-Seeding tự động.

---

## 🌟 Tính năng nổi bật

1. **Phân loại 4 nhóm xe máy điện thông minh (`VehicleCategory`):**
   - **Phổ thông / Học sinh - Sinh viên (`Economy`):** Vận tốc $\le 50\text{ km/h}$, **không yêu cầu bằng lái A1** (VinFast Evo200 Lite, Yadea Odora S, Pega AuraS+...).
   - **Tầm trung / Đi phố (`MidRange`):** Quãng đường $90 - 150\text{ km}$/sạc, cốp rộng (VinFast Feliz S, Klara S, Honda EM1 e:...).
   - **Đi phượt / Pin trâu (`Touring`):** Quãng đường lên đến $198 - 203\text{ km}$/sạc (VinFast Vento S, Evo200, Dat Bike Weaver++).
   - **Thể thao / Cao cấp (`SportPremium`):** Công suất lớn $5000\text{W} - 6000\text{W}$, phanh ABS, tăng tốc vượt trội (VinFast Theon S, Dat Bike Quantum, BMW CE 04).
2. **Đặt xe & Tính phí tự động (`BookingService` & `Pages/Bookings/`):**
   - Kiểm tra trùng lịch thuê theo thời gian thực.
   - Tự động tính tổng tiền thuê theo số ngày, phí bảo hiểm, cọc thế chấp và áp dụng mã giảm giá (`DiscountCoupon`).
3. **Kênh Chủ xe (`HostDashboardViewModel` & `Pages/Host/`):**
   - Đăng ký xe cho thuê, quản lý danh sách xe, theo dõi doanh thu và duyệt/hoàn thành đơn đặt xe của khách.
4. **Hệ thống Quản trị (`AdminDashboardViewModel` & `Pages/Admin/`):**
   - Dashboard thống kê tổng quan hệ thống, phê duyệt GPLX A1, quản lý đội xe, người dùng và đơn đặt xe.
5. **Kiểm thử tự động (Unit & Integration Tests):**
   - Bộ **46 Unit Tests (xUnit)** kiểm thử toàn diện: Phân quyền RBAC trên `PageModel`, Đặt xe, Duyệt xe Admin, Mã giảm giá và Phân loại xe.

---

## 🛠️ Công nghệ sử dụng (Tech Stack)

| Tầng (Layer) | Công nghệ |
| :--- | :--- |
| **Frontend (UI/MVVM)** | ASP.NET Core 9.0 Razor Pages (`Pages/`), MVVM `PageModel`, Tailwind CSS, Bootstrap Icons |
| **Backend (API & BLL)** | C# 13, .NET 9 Minimal Web API (`Endpoints/`), 3-Layer Business Services (`Services/`), Cookie Auth & RBAC |
| **Database (DAL)** | SQLite (`mioto.db`) + Entity Framework Core 9.0 (Code-First & Auto-Seeding) |
| **Testing** | xUnit, In-Memory & Temporary SQLite Database Testing (46/46 Pass) |
| **DevOps / Deploy** | Docker, Docker Compose, GitHub |

---

## 🔐 Tài khoản Demo (Tự động khởi tạo khi chạy App)

Hệ thống tự động nạp sẵn **16 mẫu xe máy điện** và **3 tài khoản phân quyền** ngay trong lần chạy đầu tiên:

| Vai trò (Role) | Email đăng nhập | Mật khẩu | Quyền hạn chính |
| :--- | :--- | :--- | :--- |
| **Quản trị viên (`Admin`)** | `admin@mioto.vn` | `123456` | Toàn quyền hệ thống, duyệt GPLX/xe, quản lý User/Booking, Kênh chủ xe & Thuê xe |
| **Chủ xe (`Host`)** | `chuxe@gmail.com` | `123456` | Đăng xe mới lên sàn, quản lý đội xe cá nhân, xác nhận đơn khách đặt & Thuê xe |
| **Khách thuê (`Renter`)** | `khachthue@gmail.com` | `123456` | Tìm kiếm/lọc xe theo 4 phân khúc, đặt xe, áp mã giảm giá, xem lịch sử chuyến đi |

> 💡 **Mẹo khi Demo:** Trên thanh công cụ hoặc trang `/Account/Login`, bạn có thể bấm trực tiếp vào **3 nút chuyển đổi nhanh vai trò (Khách thuê / Chủ xe / Admin)** chỉ với 1 cú click.

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
