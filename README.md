# ⚡ EcoBike — Nền tảng Cho thuê Xe máy điện P2P Thông minh

**EcoBike** (`MiotoCarRental`) là hệ thống web kết nối trực tiếp **Chủ xe máy điện nhàn rỗi (Host)** và **Khách thuê xe (Customer)** theo mô hình kinh tế chia sẻ (Peer-to-Peer), được xây dựng theo kiến trúc **Frontend ReactJS (`ClientApp/`) + Backend ASP.NET Core 9.0 Web API (`Endpoints/`) + 3-Layer Services (`Services/`) & Razor Pages MVVM (`Pages/`)** (hoàn toàn không sử dụng mô hình MVC truyền thống), cùng **Entity Framework Core (SQLite)** và **Tailwind CSS**.

---

## 👥 Thành viên Nhóm thực hiện

| STT | Họ và Tên | Vai trò | Nhiệm vụ phụ trách |
| :-: | :--- | :--- | :--- |
| **1** | **Trần Nhất Tâm** | **Backend / Frontend Developer** | Thiết kế CSDL SQLite, ASP.NET Core 9.0 Web API (`Endpoints/`), 3-Layer Services, Phân quyền RBAC |
| **2** | **Đặng Sỹ Phố** | **Frontend / Backend Developer** | Thiết kế giao diện ReactJS (`ClientApp/`) & Razor Pages MVVM (`Pages/`): 4 phân khúc xe, Chủ xe, Admin |
| **3** | **Nguyễn Phương Nam (Nguyễn Hải Nam)** | **Báo cáo, Kiểm thử & Deploy VPS** | Viết tài liệu SRS, Lập Product Backlog & Jira, Kiểm thử 46 Unit Tests, Triển khai Docker VPS |

---

## 🏛️ Kiến trúc Hệ thống (Non-MVC: ReactJS Frontend + ASP.NET Core 9.0 Web API + 3-Layer Services)

Dự án tuân thủ yêu cầu **không sử dụng ASP.NET Core MVC (`Controllers/` & `Views/`)**, tách biệt rõ ràng giữa **Frontend** và **Backend Web API**:

1. **Frontend Layer — ReactJS 18 (`ClientApp/` & `/react`):**
   - Thư mục `ClientApp/` chứa ứng dụng **React 18 + Vite + Tailwind CSS** (`src/App.jsx`, `src/api/ecobikeApi.js`, `src/components/Navbar.jsx`, `src/components/VehicleCard.jsx`, `src/components/BookingModal.jsx`) gọi trực tiếp RESTful API của Backend `.NET Core`.
   - Truy cập nhanh giao diện ReactJS SPA tại: **`/react`**
2. **Backend API Layer — ASP.NET Core 9.0 Minimal Web API (`Endpoints/EcoBikeEndpoints.cs` & `/swagger`):**
   - Cung cấp hệ thống RESTful JSON APIs đầy đủ:
     - `GET /api/vehicles` — Lọc xe theo 4 phân khúc, thành phố, hãng xe, miễn bằng lái A1
     - `GET /api/vehicles/{id}` — Chi tiết thông số xe máy điện
     - `GET /api/stats` — Thống kê KPI toàn sàn (GMV, hoa hồng 15%, số lượng xe/đơn)
     - `GET /api/coupons` — Danh sách mã khuyến mãi đang hoạt động
     - `GET /api/bookings` — Danh sách đơn đặt thuê xe
     - `POST /api/bookings/calculate` — Tính phí thuê xe, phí bảo hiểm, áp mã giảm giá và tiền cọc 30%
     - `GET /api/stations` — Mạng lưới trạm sạc V-GREEN & đổi pin 24/7
   - Truy cập giao diện kiểm thử API trực quan (**Swagger / OpenAPI Explorer**) tại: **`/swagger`**
3. **Business Logic Layer — 3-Layer Services (`Services/`):**
   - Tách biệt hoàn toàn logic nghiệp vụ vào các Service chuyên biệt: `VehicleService`, `BookingService`, `AuthService`, `CouponService`.
4. **Data Access Layer (`Data/MiotoDbContext.cs` & `Models/Entities/`):**
   - Quản lý truy xuất dữ liệu bằng Entity Framework Core 9.0 với cơ sở dữ liệu SQLite (`mioto.db`) và cơ chế Auto-Seeding tự động.

---

## 🛠️ Công nghệ sử dụng (Tech Stack)

| Tầng (Layer) | Công nghệ |
| :--- | :--- |
| **Frontend (Client)** | **ReactJS 18 (`ClientApp/`)**, Vite, Tailwind CSS, Razor Pages MVVM (`Pages/`) |
| **Backend (Web API & BLL)** | **C# 13, ASP.NET Core 9.0 Web API (`Endpoints/`)**, Swagger UI (`/swagger`), 3-Layer Services (`Services/`) |
| **Database (DAL)** | SQLite (`mioto.db`) + Entity Framework Core 9.0 (Code-First & Auto-Seeding) |
| **Testing** | xUnit, In-Memory & Temporary SQLite Database Testing (46/46 Pass) |
| **DevOps / Deploy** | Docker, Docker Compose, GitHub, VPS |

---

## 🔐 Tài khoản Demo (Tự động khởi tạo khi chạy App)

Hệ thống tự động nạp sẵn **16 mẫu xe máy điện** và **3 tài khoản phân quyền** ngay trong lần chạy đầu tiên:

| Vai trò (Role) | Email đăng nhập | Mật khẩu | Quyền hạn chính |
| :--- | :--- | :--- | :--- |
| **Quản trị viên (`Admin`)** | `admin@mioto.vn` | `123456` | Toàn quyền hệ thống, duyệt GPLX/xe, quản lý User/Booking, Kênh chủ xe & Thuê xe |
| **Chủ xe (`Host`)** | `chuxe@gmail.com` | `123456` | Đăng xe mới lên sàn, quản lý đội xe cá nhân, xác nhận đơn khách đặt & Thuê xe |
| **Khách thuê (`Renter`)** | `khachthue@gmail.com` | `123456` | Tìm kiếm/lọc xe theo 4 phân khúc, đặt xe, áp mã giảm giá, xem lịch sử chuyến đi |

---

## 🚀 Hướng dẫn cài đặt & Chạy dự án

### Cách 1: Chạy trực tiếp bằng .NET CLI hoặc Visual Studio 2022
```bash
dotnet run
```
- Cổng chính: **`http://localhost:5287`**
- Giao diện ReactJS SPA: **`http://localhost:5287/react`**
- Giao diện .NET Core Web API Swagger: **`http://localhost:5287/swagger`**

### Cách 2: Chạy riêng ReactJS Dev Server (`ClientApp`)
```bash
cd ClientApp
npm install
npm run dev
```

### Cách 3: Triển khai lên VPS bằng Docker Compose
```bash
docker compose up -d --build
```
Ứng dụng sẽ chạy ở chế độ Production tại cổng **`8080`** (`http://<IP-VPS>:8080`).
