# 🛡️ Hệ Thống Quản Lý Bảo Hiểm Nhân Viên

ASP.NET Core 8 MVC — Insurance Management System

---

## 📁 Cấu trúc project

```
InsuranceManagement/
├── Controllers/
│   ├── AccountController.cs       # Đăng nhập / đăng xuất
│   ├── ProfileController.cs       # Hồ sơ NV (Admin CRUD + NV xem)
│   ├── InsuranceController.cs     # Lịch sử đóng BH + tính toán
│   ├── ClaimController.cs         # Yêu cầu claim (NV tạo / Admin duyệt)
│   └── AdminDashboardController.cs# Tổng quan Admin
├── Data/
│   ├── ApplicationDbContext.cs    # EF Core DbContext
│   └── DbSeeder.cs                # Seed dữ liệu mẫu
├── Models/
│   ├── TaiKhoan.cs
│   ├── NhanVien.cs
│   ├── KyDongBaoHiem.cs
│   └── YeuCauClaim.cs
├── Views/
│   ├── Shared/_Layout.cshtml      # Layout chung (sidebar + header)
│   ├── Account/                   # Login, AccessDenied
│   ├── Profile/                   # CRUD hồ sơ NV
│   ├── Insurance/                 # Lịch sử + tính toán
│   ├── Claim/                     # MyClaims, CreateClaim, AdminClaims, ClaimDetails
│   └── AdminDashboard/            # Dashboard tổng quan
├── wwwroot/uploads/claims/        # File chứng từ upload
├── Program.cs
└── appsettings.json
```

---

## ⚙️ Cài đặt & Chạy

### 1. Yêu cầu
- .NET 8 SDK
- SQL Server (LocalDB hoặc SQL Server Express)

### 2. Cấu hình Connection String
Mở `appsettings.json`, sửa connection string:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=InsuranceManagementDB;Trusted_Connection=True;"
}
```
Nếu dùng SQL Server Express:
```
Server=.\\SQLEXPRESS;Database=InsuranceManagementDB;Trusted_Connection=True;
```

### 3. Chạy project
```bash
cd InsuranceManagement
dotnet run
```
Database sẽ tự động được tạo và seed dữ liệu mẫu khi chạy lần đầu.

---

## 🔑 Tài khoản demo

| Vai trò     | Tên đăng nhập | Mật khẩu  |
|-------------|---------------|-----------|
| 👑 Admin    | `admin`       | `Admin@123` |
| 👤 NV 1     | `nv001`       | `Nv@123`  |
| 👤 NV 2     | `nv002`       | `Nv@123`  |
| 👤 NV 3     | `nv003`       | `Nv@123`  |

---

## 🗺️ Các trang chính

### Admin
| URL | Mô tả |
|-----|-------|
| `/AdminDashboard/Index` | Tổng quan hệ thống |
| `/Profile/Index` | Danh sách nhân viên |
| `/Profile/Create` | Thêm nhân viên |
| `/Profile/Edit/{id}` | Sửa hồ sơ |
| `/Insurance/Calculate` | Tính BH tháng mới |
| `/Insurance/AdminHistory` | Lịch sử đóng toàn bộ |
| `/Claim/AdminClaims` | Danh sách claim |
| `/Claim/ClaimDetails/{id}` | Chi tiết + phê duyệt |

### Nhân viên
| URL | Mô tả |
|-----|-------|
| `/Profile/MyProfile` | Thông tin cá nhân |
| `/Insurance/MyHistory` | Lịch sử đóng tiền |
| `/Claim/MyClaims` | Danh sách đơn của mình |
| `/Claim/CreateClaim` | Tạo đơn yêu cầu mới |

---

## 📌 Lưu ý khi nộp / triển khai

1. **Mật khẩu:** Hiện tại lưu plain-text. Trước khi triển khai, cần thay bằng BCrypt hoặc ASP.NET Identity `IPasswordHasher`.
2. **Upload file:** Chứng từ lưu tại `wwwroot/uploads/claims/`. Cần tạo thư mục này nếu chưa có.
3. **Connection string:** Đổi sang SQL Server thực tế nếu không dùng LocalDB.
