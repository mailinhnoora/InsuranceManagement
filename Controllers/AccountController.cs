using InsuranceManagement.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InsuranceManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ─── GET: /Account/Login ─────────────────────────────────────────────
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Nếu đã đăng nhập thì redirect về trang chủ phù hợp
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectByRole();
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // ─── POST: /Account/Login ────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string tenDangNhap, string matKhau, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrWhiteSpace(tenDangNhap) || string.IsNullOrWhiteSpace(matKhau))
            {
                TempData["Error"] = "Vui lòng nhập tên đăng nhập và mật khẩu.";
                return View();
            }

            // Tìm tài khoản theo tên đăng nhập
            var taiKhoan = await _context.TaiKhoans
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);

            if (taiKhoan == null)
            {
                TempData["Error"] = "Tên đăng nhập không tồn tại.";
                return View();
            }

            // ── Kiểm tra mật khẩu ────────────────────────────────────────────
            // Nếu dùng BCrypt: BCrypt.Net.BCrypt.Verify(matKhau, taiKhoan.MatKhau)
            // Hiện tại so sánh trực tiếp (plain-text) — thay bằng hash ở production
            bool matKhauHopLe = taiKhoan.MatKhau == matKhau;

            if (!matKhauHopLe)
            {
                TempData["Error"] = "Mật khẩu không chính xác.";
                return View();
            }

            // ── Tạo Claims và phát Cookie ─────────────────────────────────────
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name,           taiKhoan.TenDangNhap),
                new Claim(ClaimTypes.Role,           taiKhoan.VaiTro),
                new Claim("AccountId",               taiKhoan.TaiKhoanID.ToString()),
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc   = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // ── Redirect theo vai trò ─────────────────────────────────────────
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectByRole(taiKhoan.VaiTro);
        }

        // ─── POST: /Account/Logout ───────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "Đăng xuất thành công!";
            return RedirectToAction(nameof(Login));
        }

        // ─── GET: /Account/AccessDenied ──────────────────────────────────────
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ─── Helper ──────────────────────────────────────────────────────────
        private IActionResult RedirectByRole(string? vaiTro = null)
        {
            vaiTro ??= User.FindFirstValue(ClaimTypes.Role);

            return vaiTro == "Admin"
                ? RedirectToAction("Index", "AdminDashboard")
                : RedirectToAction("MyProfile", "Profile");
        }
    }
}
