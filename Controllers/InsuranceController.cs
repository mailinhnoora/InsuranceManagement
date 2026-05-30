using InsuranceManagement.Data;
using InsuranceManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace InsuranceManagement.Controllers
{
    public class InsuranceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InsuranceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ════════════════════════════════════════════════════════════════════
        //  NHÂN VIÊN — xem lịch sử đóng bảo hiểm của mình
        // ════════════════════════════════════════════════════════════════════

        // GET: /Insurance/MyHistory
        [Authorize(Roles = "NhanVien")]
        public async Task<IActionResult> MyHistory(string? nam)
        {
            var accountIdStr = User.FindFirstValue("AccountId");
            if (!int.TryParse(accountIdStr, out int accountId))
                return Unauthorized();

            var nhanVien = await _context.NhanViens
                .AsNoTracking()
                .FirstOrDefaultAsync(nv => nv.TaiKhoanID == accountId);

            if (nhanVien == null)
            {
                TempData["Error"] = "Không tìm thấy hồ sơ nhân viên.";
                return RedirectToAction("MyProfile", "Profile");
            }

            // Lấy danh sách năm có dữ liệu để hiển thị bộ lọc
            var cacNam = await _context.KyDongBaoHiems
                .Where(k => k.NhanVienID == nhanVien.NhanVienID)
                .Select(k => k.ThangNam.Substring(3, 4)) // "MM/YYYY" → lấy từ vị trí 3
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            var query = _context.KyDongBaoHiems
                .Where(k => k.NhanVienID == nhanVien.NhanVienID)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(nam))
                query = query.Where(k => k.ThangNam.EndsWith(nam));

            var lichSu = await query
                .OrderByDescending(k => k.ThangNam)
                .ToListAsync();

            ViewData["CacNam"]    = cacNam;
            ViewData["NamChon"]   = nam;
            ViewData["NhanVien"]  = nhanVien;

            return View(lichSu);
        }

        // ════════════════════════════════════════════════════════════════════
        //  ADMIN — xem tổng quan toàn bộ kỳ đóng
        // ════════════════════════════════════════════════════════════════════

        // GET: /Insurance/AdminHistory
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminHistory(string? thangNam)
        {
            // Lấy danh sách tháng/năm đã có dữ liệu để hiển thị bộ lọc
            var cacKy = await _context.KyDongBaoHiems
                .Select(k => k.ThangNam)
                .Distinct()
                .OrderByDescending(t => t)
                .ToListAsync();

            var query = _context.KyDongBaoHiems
                .Include(k => k.NhanVien)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(thangNam))
                query = query.Where(k => k.ThangNam == thangNam);

            var danhSach = await query
                .OrderBy(k => k.ThangNam)
                .ThenBy(k => k.NhanVien!.HoTen)
                .ToListAsync();

            ViewData["CacKy"]      = cacKy;
            ViewData["ThangNamChon"] = thangNam;

            return View(danhSach);
        }

        // ─── POST: /Insurance/MarkAsPaid ─────────────────────────────────────
        // Admin đánh dấu đã nộp tiền cho toàn bộ nhân viên trong 1 tháng
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(string thangNam)
        {
            if (string.IsNullOrWhiteSpace(thangNam))
            {
                TempData["Error"] = "Vui lòng chọn tháng/năm.";
                return RedirectToAction(nameof(AdminHistory));
            }

            var records = await _context.KyDongBaoHiems
                .Where(k => k.ThangNam == thangNam &&
                            k.TrangThaiThanhToan == TrangThaiThanhToanValues.ChuaThanhToan)
                .ToListAsync();

            if (!records.Any())
            {
                TempData["Error"] = $"Không có bản ghi nào cần cập nhật cho tháng {thangNam}.";
                return RedirectToAction(nameof(AdminHistory), new { thangNam });
            }

            foreach (var record in records)
                record.TrangThaiThanhToan = TrangThaiThanhToanValues.DaThanhToan;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã đánh dấu đã nộp tiền cho tháng {thangNam} ({records.Count} bản ghi).";
            return RedirectToAction(nameof(AdminHistory), new { thangNam });
        }

        // ════════════════════════════════════════════════════════════════════
        //  ADMIN — tính toán bảo hiểm hàng tháng
        // ════════════════════════════════════════════════════════════════════

        // GET: /Insurance/Calculate
        [Authorize(Roles = "Admin")]
        public IActionResult Calculate()
        {
            return View();
        }

        // POST: /Insurance/CalculateMonthlyInsurance
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CalculateMonthlyInsurance(string thangNam)
        {
            // ── Validate định dạng MM/YYYY ─────────────────────────────────
            if (string.IsNullOrWhiteSpace(thangNam) ||
                !Regex.IsMatch(thangNam, @"^\d{2}/\d{4}$"))
            {
                TempData["Error"] = "Định dạng Tháng/Năm không hợp lệ. Vui lòng nhập theo dạng MM/YYYY (ví dụ: 06/2026).";
                return RedirectToAction(nameof(Calculate));
            }

            // Kiểm tra tháng/năm hợp lý
            var parts = thangNam.Split('/');
            int thang = int.Parse(parts[0]);
            int nam   = int.Parse(parts[1]);

            if (thang < 1 || thang > 12)
            {
                TempData["Error"] = "Tháng không hợp lệ (phải từ 01 đến 12).";
                return RedirectToAction(nameof(Calculate));
            }

            // ── Kiểm tra tháng này đã tính chưa ──────────────────────────
            bool daTon = await _context.KyDongBaoHiems
                .AnyAsync(k => k.ThangNam == thangNam);

            if (daTon)
            {
                TempData["Error"] = $"Tháng {thangNam} đã được tính toán trước đó. Không thể tính lại.";
                return RedirectToAction(nameof(AdminHistory), new { thangNam });
            }

            // ── Lấy toàn bộ nhân viên hiện tại ────────────────────────────
            var danhSachNhanVien = await _context.NhanViens
                .AsNoTracking()
                .ToListAsync();

            if (!danhSachNhanVien.Any())
            {
                TempData["Error"] = "Hiện chưa có nhân viên nào trong hệ thống.";
                return RedirectToAction(nameof(Calculate));
            }

            // ── Vòng lặp tính toán cho từng nhân viên ─────────────────────
            var ketQuaList = new List<KyDongBaoHiem>();

            foreach (var nv in danhSachNhanVien)
            {
                decimal luong = nv.LuongDongBaoHiem;

                decimal bhxh_nv = Math.Round(luong * 0.080m, 0);
                decimal bhyt_nv = Math.Round(luong * 0.015m, 0);
                decimal bhtn_nv = Math.Round(luong * 0.010m, 0);
                decimal tongNV  = bhxh_nv + bhyt_nv + bhtn_nv;
                decimal tongCty = Math.Round(luong * 0.220m, 0);

                ketQuaList.Add(new KyDongBaoHiem
                {
                    NhanVienID          = nv.NhanVienID,
                    ThangNam            = thangNam,
                    LuongGoc            = luong,
                    TienBHXH_NV         = bhxh_nv,
                    TienBHYT_NV         = bhyt_nv,
                    TienBHTN_NV         = bhtn_nv,
                    TongTienNhanVien    = tongNV,
                    TongTienCongTy      = tongCty,
                    TrangThaiThanhToan  = TrangThaiThanhToanValues.ChuaThanhToan
                });
            }

            await _context.KyDongBaoHiems.AddRangeAsync(ketQuaList);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"✅ Hệ thống đã tính xong tiền bảo hiểm tháng {thangNam} cho {ketQuaList.Count} nhân viên.";
            return RedirectToAction(nameof(AdminHistory), new { thangNam });
        }
    }
}
