using InsuranceManagement.Data;
using InsuranceManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InsuranceManagement.Controllers
{
    /// <summary>
    /// Trang tổng quan dành cho Admin sau khi đăng nhập thành công.
    /// Hiển thị các số liệu tóm tắt toàn hệ thống.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /AdminDashboard/Index
        public async Task<IActionResult> Index()
        {
            // ── Thống kê nhanh ─────────────────────────────────────────────
            int tongNhanVien = await _context.NhanViens.CountAsync();

            int tongClaim   = await _context.YeuCauClaims.CountAsync();
            int choDuyet    = await _context.YeuCauClaims
                                .CountAsync(c => c.TrangThai == TrangThaiClaimValues.ChoDuyet);
            int daDuyet     = await _context.YeuCauClaims
                                .CountAsync(c => c.TrangThai == TrangThaiClaimValues.DaDuyet);
            int tuChoi      = await _context.YeuCauClaims
                                .CountAsync(c => c.TrangThai == TrangThaiClaimValues.TuChoi);

            int chuaThanhToan = await _context.KyDongBaoHiems
                                  .CountAsync(k => k.TrangThaiThanhToan == TrangThaiThanhToanValues.ChuaThanhToan);

            // ── Tháng gần nhất đã tính ─────────────────────────────────────
            string? thangGanNhat = await _context.KyDongBaoHiems
                .Select(k => k.ThangNam)
                .Distinct()
                .OrderByDescending(t => t)
                .FirstOrDefaultAsync();

            // ── 5 claim mới nhất đang chờ duyệt ───────────────────────────
            var claimChoPheDuyet = await _context.YeuCauClaims
                .Include(c => c.NhanVien)
                .Where(c => c.TrangThai == TrangThaiClaimValues.ChoDuyet)
                .OrderByDescending(c => c.NgayYeuCau)
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            ViewData["TongNhanVien"]     = tongNhanVien;
            ViewData["TongClaim"]        = tongClaim;
            ViewData["ChoDuyet"]         = choDuyet;
            ViewData["DaDuyet"]          = daDuyet;
            ViewData["TuChoi"]           = tuChoi;
            ViewData["ChuaThanhToan"]    = chuaThanhToan;
            ViewData["ThangGanNhat"]     = thangGanNhat ?? "Chưa có";
            ViewData["ClaimChoPheDuyet"] = claimChoPheDuyet;

            return View();
        }
    }
}
