# 📋 PRODUCT BACKLOG & PHÂN CÔNG JIRA — DỰ ÁN ECOBIKE (KIẾN TRÚC NON-MVC)

## 1. Danh sách Thành viên Nhóm & Phân công Vai trò

| STT | Họ và Tên | Vai trò trong nhóm | Phụ trách chính | Số lượng Task |
| :-: | :--- | :--- | :--- | :-: |
| **1** | **Trần Nhất Tâm** | **Backend / Frontend Developer** | Thiết kế CSDL SQLite, Đăng nhập & Phân quyền 3 vai trò, 3-Layer Services & Minimal API Đặt xe/Duyệt xe, Docker VPS | **4 việc** |
| **2** | **Đặng Sỹ Phố** | **Frontend / Backend Developer** | Thiết kế giao diện Razor Pages MVVM (`Pages/` & `PageModel`): Navbar, Trang chủ, 4 phân khúc xe, Chi tiết xe, Chủ xe, Admin | **4 việc** |
| **3** | **Nguyễn Phương Nam** | **Báo cáo & Kiểm thử (BA/QA)** | Viết tài liệu SRS, Lập Backlog & Quản lý Jira, Kiểm thử hệ thống (46 Unit Tests), Tài liệu báo cáo | **4 việc** |

---

## 2. Bảng 12 Công việc Cơ bản trên Jira (Chia theo 3 Thành viên — Kiến trúc Razor Pages MVVM & Minimal API)

| Mã Task | Tên công việc trên Jira (Summary) | Mô tả ngắn gọn (Description) | Người phụ trách (Assignee) | Mảng | Sprint | Trạng thái |
| :-: | :--- | :--- | :--- | :-: | :-: | :-: |
| **KAN-5** | **[BE] Thiết kế CSDL SQLite & Khởi tạo dữ liệu mẫu (DbInitializer)** | Xây dựng tầng Data Access Layer (`MiotoDbContext`) với các bảng `Users`, `Vehicles`, `Bookings`, `DiscountCoupons` bằng EF Core và tự động nạp 16 xe điện + 3 tài khoản mẫu. | **Trần Nhất Tâm** | Backend | Sprint 1 | `Done` |
| **KAN-6** | **[BE] Xây dựng chức năng Đăng nhập & Phân quyền 3 vai trò (RBAC)** | Xây dựng `AuthService`, cấu hình Cookie Authentication và kiểm soát quyền truy cập `[Authorize]` trên các `PageModel` cho 3 vai trò: `Admin`, `Host`, `Renter`. | **Trần Nhất Tâm** | Backend | Sprint 1 | `Done` |
| **KAN-7** | **[BE] Xử lý nghiệp vụ Tìm kiếm, Lọc phân khúc xe & Đặt xe (3-Layer Services)** | Xây dựng tầng Business Logic Layer (`VehicleService`, `BookingService`, `CouponService`) và Minimal Web API (`EcoBikeEndpoints`): lọc 4 nhóm xe, chống trùng lịch và tính phí tự động. | **Trần Nhất Tâm** | Backend | Sprint 2 | `Done` |
| **KAN-8** | **[BE] Xây dựng luồng Chủ xe đăng xe, Admin duyệt xe & Cấu hình Docker VPS** | Xây dựng các Minimal API Endpoints duyệt GPLX/duyệt xe, xử lý trạng thái xe và viết `Dockerfile`, `docker-compose.yml` để triển khai lên VPS. | **Trần Nhất Tâm** | Backend | Sprint 3 | `Done` |
| **KAN-9** | **[FE] Thiết kế thanh công cụ (Navbar) & Bố cục chung (`Pages/Shared/_Layout`)** | Thiết kế thanh điều hướng cố định hiệu ứng kính mờ trên Razor Pages, hiển thị menu theo phân quyền và nút chuyển đổi nhanh 3 vai trò Demo. | **Đặng Sỹ Phố** | Frontend | Sprint 1 | `Done` |
| **KAN-10** | **[FE] Giao diện Razor Pages Trang chủ & Danh sách 4 nhóm xe máy điện** | Thiết kế `Pages/Index.cshtml` và `Pages/Vehicles/Index.cshtml` gắn với `VehicleListViewModel` (`PageModel`), căn đều thẻ xe và thanh lọc 4 phân khúc xe điện. | **Đặng Sỹ Phố** | Frontend | Sprint 1 | `Done` |
| **KAN-11** | **[FE] Giao diện Razor Pages Chi tiết xe, Đặt xe & Trang Đăng nhập** | Thiết kế `Pages/Vehicles/Details.cshtml`, `Pages/Bookings/` và `Pages/Account/Login.cshtml` theo mô hình MVVM `PageModel`. | **Đặng Sỹ Phố** | Frontend | Sprint 2 | `Done` |
| **KAN-12** | **[FE] Giao diện Razor Pages Kênh Chủ xe (`Pages/Host`) & Quản trị (`Pages/Admin`)** | Thiết kế form đăng xe mới (`CreateVehicleViewModel`) cho Chủ xe và giao diện bảng quản trị (`AdminDashboardViewModel`) cho Admin. | **Đặng Sỹ Phố** | Frontend | Sprint 3 | `Done` |
| **KAN-13** | **[Báo cáo] Phân tích yêu cầu & Viết tài liệu đặc tả SRS (Kiến trúc Non-MVC)** | Xác định yêu cầu chức năng (FR), phi chức năng (NFR), sơ đồ kiến trúc 3-Layer + Razor Pages MVVM và hoàn thiện tài liệu Word SRS cho dự án EcoBike. | **Nguyễn Phương Nam** | Báo cáo | Sprint 1 | `Done` |
| **KAN-14** | **[Báo cáo] Lập bảng Product Backlog & Quản lý tiến độ trên Jira** | Phân rã 12 công việc theo 3 Sprint cho Trần Nhất Tâm, Đặng Sỹ Phố, Nguyễn Phương Nam trên bảng Jira và xuất file Excel/Word Backlog. | **Nguyễn Phương Nam** | Báo cáo | Sprint 2 | `Done` |
| **KAN-15** | **[Báo cáo] Kiểm thử chức năng hệ thống & Chạy bộ 46 Unit Tests** | Kiểm thử thực tế các luồng Đăng nhập, Đặt xe, Duyệt xe và chạy bộ kiểm thử tự động xUnit (46/46 Pass) trên kiến trúc `PageModel` & `Services`. | **Nguyễn Phương Nam** | Báo cáo | Sprint 3 | `Done` |
| **KAN-16** | **[Báo cáo] Chuẩn bị tài liệu README GitHub & Kịch bản Demo** | Hoàn thiện trang hướng dẫn `README.md` giải thích kiến trúc Razor Pages MVVM + Minimal Web API và tổng hợp kịch bản báo cáo với trợ giảng. | **Nguyễn Phương Nam** | Báo cáo | Sprint 3 | `Done` |
