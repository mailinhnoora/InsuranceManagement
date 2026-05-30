using InsuranceManagement.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace InsuranceManagement.Data
{
    /// <summary>
    /// Seed dữ liệu mẫu vào database khi ứng dụng khởi động lần đầu.
    /// Gọi DbSeeder.Initialize(app) trong Program.cs sau app.Build().
    /// </summary>
    public static class DbSeeder
    {
        public static void Initialize(IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Chạy migration nếu chưa có
            context.Database.EnsureCreated();

            // Nếu đã có dữ liệu thì bỏ qua
            if (context.TaiKhoans.Any()) return;

            // ─── Tài khoản ────────────────────────────────────────────────────
            // Trong thực tế, hash mật khẩu bằng IPasswordHasher<> của ASP.NET Core Identity.
            // Ở đây dùng placeholder rõ ràng để nhóm biết phải thay.
            var taiKhoans = new[]
            {
                new TaiKhoan
                {
                    TenDangNhap = "admin",
                    // TODO: Thay bằng: _passwordHasher.HashPassword(null, "Admin@123")
                    MatKhau     = "Admin@123",
                    VaiTro      = "Admin",
                    NgayTao     = new DateTime(2026, 1, 1)
                },
                new TaiKhoan
                {
                    TenDangNhap = "nv001",
                    MatKhau     = "Nv@123",
                    VaiTro      = "NhanVien",
                    NgayTao     = new DateTime(2026, 1, 1)
                },
                new TaiKhoan
                {
                    TenDangNhap = "nv002",
                    MatKhau     = "Nv@123",
                    VaiTro      = "NhanVien",
                    NgayTao     = new DateTime(2026, 1, 1)
                },
                new TaiKhoan
                {
                    TenDangNhap = "nv003",
                    MatKhau     = "Nv@123",
                    VaiTro      = "NhanVien",
                    NgayTao     = new DateTime(2026, 1, 1)
                },
            };

            context.TaiKhoans.AddRange(taiKhoans);
            context.SaveChanges(); // Lưu để lấy TaiKhoanID tự động

            // ─── Hồ sơ nhân viên ──────────────────────────────────────────────
            var nhanViens = new[]
            {
                new NhanVien
                {
                    TaiKhoanID        = taiKhoans[1].TaiKhoanID,  // nv001
                    HoTen             = "Nguyễn Văn An",
                    SoSoBHXH          = "0123456789",
                    SoTheBHYT         = "DN4010012345678",
                    NoiDangKyKCB      = "Bệnh viện Bạch Mai – Hà Nội",
                    LuongDongBaoHiem  = 8_000_000m,
                    MucHuongChinhSach = "BHYT hưởng 80%; Thai sản hưởng 100% lương đóng BHXH trong 6 tháng",
                    NguoiThuHuong     = "Nguyễn Thị Bình – Vợ – 0912 345 678"
                },
                new NhanVien
                {
                    TaiKhoanID        = taiKhoans[2].TaiKhoanID,  // nv002
                    HoTen             = "Trần Thị Bích",
                    SoSoBHXH          = "0987654321",
                    SoTheBHYT         = "DN4010098765432",
                    NoiDangKyKCB      = "Bệnh viện Việt Đức – Hà Nội",
                    LuongDongBaoHiem  = 12_000_000m,
                    MucHuongChinhSach = "BHYT hưởng 80%; Ốm đau hưởng 75% lương đóng BHXH; Thai sản hưởng 100%",
                    NguoiThuHuong     = "Trần Văn Cường – Chồng – 0988 765 432"
                },
                new NhanVien
                {
                    TaiKhoanID        = taiKhoans[3].TaiKhoanID,  // nv003
                    HoTen             = "Lê Minh Hoàng",
                    SoSoBHXH          = "0111222333",
                    SoTheBHYT         = "DN4010011122233",
                    NoiDangKyKCB      = "Bệnh viện Đa khoa tỉnh Bắc Ninh",
                    LuongDongBaoHiem  = 18_000_000m,
                    MucHuongChinhSach = "BHYT hưởng 95% (đúng tuyến); Tai nạn lao động hưởng theo mức suy giảm",
                    NguoiThuHuong     = "Lê Thị Mai – Mẹ – 0933 111 222"
                },
            };

            context.NhanViens.AddRange(nhanViens);
            context.SaveChanges();

            // ─── Lịch sử đóng bảo hiểm ────────────────────────────────────────
            // Hàm tính nhanh để seed (giống logic Controller)
            static KyDongBaoHiem TinhKyDong(int nhanVienID, string thangNam, decimal luong, string tttt)
            {
                var bhxh = Math.Round(luong * 0.080m, 0);
                var bhyt = Math.Round(luong * 0.015m, 0);
                var bhtn = Math.Round(luong * 0.010m, 0);
                return new KyDongBaoHiem
                {
                    NhanVienID          = nhanVienID,
                    ThangNam            = thangNam,
                    LuongGoc            = luong,
                    TienBHXH_NV         = bhxh,
                    TienBHYT_NV         = bhyt,
                    TienBHTN_NV         = bhtn,
                    TongTienNhanVien    = bhxh + bhyt + bhtn,
                    TongTienCongTy      = Math.Round(luong * 0.220m, 0),
                    TrangThaiThanhToan  = tttt
                };
            }

            var nv1ID = nhanViens[0].NhanVienID;
            var nv2ID = nhanViens[1].NhanVienID;
            var nv3ID = nhanViens[2].NhanVienID;

            var kyDongs = new[]
            {
                TinhKyDong(nv1ID, "03/2026", 8_000_000m,  TrangThaiThanhToanValues.DaThanhToan),
                TinhKyDong(nv1ID, "04/2026", 8_000_000m,  TrangThaiThanhToanValues.DaThanhToan),
                TinhKyDong(nv1ID, "05/2026", 8_000_000m,  TrangThaiThanhToanValues.ChuaThanhToan),

                TinhKyDong(nv2ID, "03/2026", 12_000_000m, TrangThaiThanhToanValues.DaThanhToan),
                TinhKyDong(nv2ID, "04/2026", 12_000_000m, TrangThaiThanhToanValues.DaThanhToan),
                TinhKyDong(nv2ID, "05/2026", 12_000_000m, TrangThaiThanhToanValues.ChuaThanhToan),

                TinhKyDong(nv3ID, "03/2026", 18_000_000m, TrangThaiThanhToanValues.DaThanhToan),
                TinhKyDong(nv3ID, "04/2026", 18_000_000m, TrangThaiThanhToanValues.DaThanhToan),
                TinhKyDong(nv3ID, "05/2026", 18_000_000m, TrangThaiThanhToanValues.ChuaThanhToan),
            };

            context.KyDongBaoHiems.AddRange(kyDongs);
            context.SaveChanges();

            // ─── Dữ liệu mẫu YeuCauClaim ──────────────────────────────────────
            var claims = new[]
            {
                new YeuCauClaim
                {
                    NhanVienID    = nv1ID,
                    LoaiCheDo     = "Ốm đau",
                    NgayYeuCau    = new DateTime(2026, 4, 10),
                    SoTienYeuCau  = 2_000_000m,
                    MoTaLyDo      = "Nằm viện 5 ngày do viêm phổi, đã có giấy ra viện.",
                    ChungTuDinhKem = "uploads/claims/claim_001.pdf",
                    TrangThai     = TrangThaiClaimValues.DaDuyet,
                    LyDoTuChoi    = null,
                    NgayXuly      = new DateTime(2026, 4, 12)
                },
                new YeuCauClaim
                {
                    NhanVienID    = nv2ID,
                    LoaiCheDo     = "Thai sản",
                    NgayYeuCau    = new DateTime(2026, 4, 20),
                    SoTienYeuCau  = 15_000_000m,
                    MoTaLyDo      = "Sinh con ngày 15/04/2026, xin hưởng chế độ thai sản.",
                    ChungTuDinhKem = "uploads/claims/claim_002.pdf",
                    TrangThai     = TrangThaiClaimValues.TuChoi,
                    LyDoTuChoi    = "Hồ sơ thiếu giấy khai sinh của con. Đề nghị bổ sung và nộp lại.",
                    NgayXuly      = new DateTime(2026, 4, 22)
                },
                new YeuCauClaim
                {
                    NhanVienID    = nv3ID,
                    LoaiCheDo     = "Tai nạn lao động",
                    NgayYeuCau    = new DateTime(2026, 5, 5),
                    SoTienYeuCau  = 5_000_000m,
                    MoTaLyDo      = "Bị ngã cầu thang tại văn phòng ngày 03/05/2026, gãy xương cổ tay phải.",
                    ChungTuDinhKem = "uploads/claims/claim_003.jpg",
                    TrangThai     = TrangThaiClaimValues.ChoDuyet,
                    LyDoTuChoi    = null,
                    NgayXuly      = null   // Chưa xử lý
                },
                new YeuCauClaim
                {
                    NhanVienID    = nv1ID,
                    LoaiCheDo     = "Ốm đau",
                    NgayYeuCau    = new DateTime(2026, 5, 1),
                    SoTienYeuCau  = 500_000m,
                    MoTaLyDo      = "Nghỉ ốm 2 ngày.",
                    ChungTuDinhKem = null,
                    TrangThai     = TrangThaiClaimValues.NhanVienHuy,
                    LyDoTuChoi    = null,
                    NgayXuly      = null
                },
            };

            context.YeuCauClaims.AddRange(claims);
            context.SaveChanges();
        }
    }
}
