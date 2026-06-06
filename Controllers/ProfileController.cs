using InsuranceManagement.Data;
using InsuranceManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InsuranceManagement.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ════════════════════════════════════════════════════════════════════
        //  NHÂN VIÊN — xem hồ sơ cá nhân
        // ════════════════════════════════════════════════════════════════════

        // GET: /Profile/MyProfile
        [Authorize(Roles = "NhanVien")]
        public async Task<IActionResult> MyProfile()
        {
            var accountIdStr = User.FindFirstValue("AccountId");
            if (!int.TryParse(accountIdStr, out int accountId))
                return Unauthorized();

            var nhanVien = await _context.NhanViens
                .Include(nv => nv.TaiKhoan)
                .AsNoTracking()
                .FirstOrDefaultAsync(nv => nv.TaiKhoanID == accountId);

            if (nhanVien == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin hồ sơ của bạn. Vui lòng liên hệ Admin.";
                return View("NoProfile");
            }

            return View(nhanVien);
        }

        // ════════════════════════════════════════════════════════════════════
        //  ADMIN — quản lý toàn bộ hồ sơ nhân viên
        // ════════════════════════════════════════════════════════════════════

        // GET: /Profile/Index
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.NhanViens
                .Include(nv => nv.TaiKhoan)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(nv =>
                    nv.HoTen.Contains(search) ||
                    nv.NhanVienID.ToString().Contains(search));
            }

            ViewData["Search"] = search;
            var list = await query.OrderBy(nv => nv.HoTen).ToListAsync();
            return View(list);
        }

        // GET: /Profile/Details/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var nhanVien = await _context.NhanViens
                .Include(nv => nv.TaiKhoan)
                .AsNoTracking()
                .FirstOrDefaultAsync(nv => nv.NhanVienID == id);

            if (nhanVien == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên.";
                return RedirectToAction(nameof(Index));
            }

            return View(nhanVien);
        }

        // ─── CREATE ──────────────────────────────────────────────────────────

        // GET: /Profile/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Profile/Create
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            NhanVien nhanVien,
            string tenDangNhap,
            string matKhau)
        {
            // Kiểm tra tên đăng nhập đã tồn tại chưa
            bool trungTen = await _context.TaiKhoans
                .AnyAsync(t => t.TenDangNhap == tenDangNhap);

            if (trungTen)
                ModelState.AddModelError("tenDangNhap", "Tên đăng nhập đã tồn tại trong hệ thống.");

            if (string.IsNullOrWhiteSpace(tenDangNhap))
                ModelState.AddModelError("tenDangNhap", "Vui lòng nhập tên đăng nhập.");

            if (string.IsNullOrWhiteSpace(matKhau))
                ModelState.AddModelError("matKhau", "Vui lòng nhập mật khẩu.");

            if (!ModelState.IsValid)
                return View(nhanVien);

            // Tạo TaiKhoan trước
            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = tenDangNhap.Trim(),
                // Production: MatKhau = _passwordHasher.HashPassword(null, matKhau)
                MatKhau  = matKhau,
                VaiTro   = "NhanVien",
                NgayTao  = DateTime.UtcNow
            };

            _context.TaiKhoans.Add(taiKhoan);
            await _context.SaveChangesAsync(); // Lấy TaiKhoanID

            // Gán FK và tạo NhanVien
            nhanVien.TaiKhoanID = taiKhoan.TaiKhoanID;
            _context.NhanViens.Add(nhanVien);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm nhân viên \"{nhanVien.HoTen}\" thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ─── EDIT ─────────────────────────────────────────────────────────────

        // GET: /Profile/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var nhanVien = await _context.NhanViens
                .Include(nv => nv.TaiKhoan)
                .FirstOrDefaultAsync(nv => nv.NhanVienID == id);

            if (nhanVien == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên.";
                return RedirectToAction(nameof(Index));
            }

            return View(nhanVien);
        }

        // POST: /Profile/Edit/5
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NhanVien nhanVienForm)
        {
            if (id != nhanVienForm.NhanVienID)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(nhanVienForm);

            var nhanVien = await _context.NhanViens.FindAsync(id);
            if (nhanVien == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên.";
                return RedirectToAction(nameof(Index));
            }

            // Cập nhật các trường được phép chỉnh sửa
            nhanVien.HoTen            = nhanVienForm.HoTen;
            nhanVien.SoSoBHXH         = nhanVienForm.SoSoBHXH;
            nhanVien.SoTheBHYT        = nhanVienForm.SoTheBHYT;
            nhanVien.NoiDangKyKCB     = nhanVienForm.NoiDangKyKCB;
            nhanVien.LuongDongBaoHiem = nhanVienForm.LuongDongBaoHiem;
            nhanVien.MucHuongChinhSach = nhanVienForm.MucHuongChinhSach;
            nhanVien.NguoiThuHuong    = nhanVienForm.NguoiThuHuong;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật hồ sơ nhân viên \"{nhanVien.HoTen}\" thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ─── DELETE ───────────────────────────────────────────────────────────

        // GET: /Profile/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var nhanVien = await _context.NhanViens
                .Include(nv => nv.TaiKhoan)
                .AsNoTracking()
                .FirstOrDefaultAsync(nv => nv.NhanVienID == id);

            if (nhanVien == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên.";
                return RedirectToAction(nameof(Index));
            }

            return View(nhanVien);
        }

        // POST: /Profile/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nhanVien = await _context.NhanViens
                .Include(nv => nv.TaiKhoan)
                .FirstOrDefaultAsync(nv => nv.NhanVienID == id);

            if (nhanVien == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên.";
                return RedirectToAction(nameof(Index));
            }

            string hoTen = nhanVien.HoTen;

            // Xóa TaiKhoan sẽ cascade sang NhanVien (theo OnDelete Cascade trong DbContext)
            if (nhanVien.TaiKhoan != null)
                _context.TaiKhoans.Remove(nhanVien.TaiKhoan);
            else
                _context.NhanViens.Remove(nhanVien);

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa nhân viên \"{hoTen}\" khỏi hệ thống.";
            return RedirectToAction(nameof(Index));
        }
    }
}
