using InsuranceManagement.Data;
using InsuranceManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InsuranceManagement.Controllers
{
    public class ClaimController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment  _env;

        // Danh mục loại chế độ — có thể chuyển ra config sau này
        public static readonly string[] DanhMucLoaiCheDo =
        {
            "Ốm đau",
            "Thai sản",
            "Tai nạn lao động",
            "Bệnh nghề nghiệp",
            "Hưu trí",
            "Tử tuất",
            "Khác"
        };

        public ClaimController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env     = env;
        }

        // ════════════════════════════════════════════════════════════════════
        //  NHÂN VIÊN — xem danh sách claim của mình
        // ════════════════════════════════════════════════════════════════════

        // GET: /Claim/MyClaims
        [Authorize(Roles = "NhanVien")]
        public async Task<IActionResult> MyClaims()
        {
            var nhanVien = await GetNhanVienHienTai();
            if (nhanVien == null)
            {
                TempData["Error"] = "Không tìm thấy hồ sơ nhân viên.";
                return RedirectToAction("MyProfile", "Profile");
            }

            var danhSach = await _context.YeuCauClaims
                .Where(c => c.NhanVienID == nhanVien.NhanVienID)
                .OrderByDescending(c => c.NgayYeuCau)
                .AsNoTracking()
                .ToListAsync();

            ViewData["NhanVien"] = nhanVien;
            return View(danhSach);
        }

        // ─── CREATE CLAIM ─────────────────────────────────────────────────────

        // GET: /Claim/CreateClaim
        [Authorize(Roles = "NhanVien")]
        public async Task<IActionResult> CreateClaim()
        {
            var nhanVien = await GetNhanVienHienTai();
            if (nhanVien == null)
            {
                TempData["Error"] = "Không tìm thấy hồ sơ nhân viên.";
                return RedirectToAction("MyProfile", "Profile");
            }

            ViewData["DanhMucLoaiCheDo"] = DanhMucLoaiCheDo;
            return View();
        }

        // POST: /Claim/CreateClaim
        [HttpPost]
        [Authorize(Roles = "NhanVien")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClaim(
            string loaiCheDo,
            decimal soTienYeuCau,
            string moTaLyDo,
            IFormFile? chungTu)
        {
            ViewData["DanhMucLoaiCheDo"] = DanhMucLoaiCheDo;

            var nhanVien = await GetNhanVienHienTai();
            if (nhanVien == null) return Unauthorized();

            // ── Validate thủ công ─────────────────────────────────────────
            bool coLoi = false;

            if (string.IsNullOrWhiteSpace(loaiCheDo))
            {
                ModelState.AddModelError("LoaiCheDo", "Vui lòng chọn loại chế độ.");
                coLoi = true;
            }

            if (soTienYeuCau <= 0)
            {
                ModelState.AddModelError("SoTienYeuCau", "Số tiền yêu cầu phải lớn hơn 0.");
                coLoi = true;
            }

            if (string.IsNullOrWhiteSpace(moTaLyDo))
            {
                ModelState.AddModelError("MoTaLyDo", "Vui lòng cung cấp đầy đủ thông tin và chứng từ kèm theo.");
                coLoi = true;
            }

            if (chungTu == null || chungTu.Length == 0)
            {
                ModelState.AddModelError("ChungTu", "Vui lòng cung cấp đầy đủ thông tin và chứng từ kèm theo.");
                coLoi = true;
            }

            if (coLoi) return View();

            // ── Lưu file chứng từ ─────────────────────────────────────────
            string? chungTuPath = null;

            if (chungTu != null && chungTu.Length > 0)
            {
                // Chỉ cho phép ảnh và PDF
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                var ext = Path.GetExtension(chungTu.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("ChungTu", "Chỉ chấp nhận file ảnh (JPG, PNG) hoặc PDF.");
                    return View();
                }

                // Giới hạn 10MB
                if (chungTu.Length > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError("ChungTu", "Dung lượng file tối đa là 10MB.");
                    return View();
                }

                string uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "claims");
                Directory.CreateDirectory(uploadFolder); // Tạo folder nếu chưa có

                string uniqueFileName = $"{Guid.NewGuid()}{ext}";
                string fullPath = Path.Combine(uploadFolder, uniqueFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                    await chungTu.CopyToAsync(stream);

                chungTuPath = $"uploads/claims/{uniqueFileName}";
            }

            // ── Tạo bản ghi Claim ─────────────────────────────────────────
            var claim = new YeuCauClaim
            {
                NhanVienID     = nhanVien.NhanVienID,
                LoaiCheDo      = loaiCheDo,
                NgayYeuCau     = DateTime.Now,
                SoTienYeuCau   = soTienYeuCau,
                MoTaLyDo       = moTaLyDo.Trim(),
                ChungTuDinhKem = chungTuPath,
                TrangThai      = TrangThaiClaimValues.ChoDuyet
            };

            _context.YeuCauClaims.Add(claim);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã gửi yêu cầu hưởng bảo hiểm thành công! Vui lòng chờ Admin xét duyệt.";
            return RedirectToAction(nameof(MyClaims));
        }

        // ─── CANCEL CLAIM (NHÂN VIÊN HỦY) ────────────────────────────────────

        // POST: /Claim/CancelClaim/5
        [HttpPost]
        [Authorize(Roles = "NhanVien")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelClaim(int claimId)
        {
            var nhanVien = await GetNhanVienHienTai();
            if (nhanVien == null) return Unauthorized();

            var claim = await _context.YeuCauClaims
                .FirstOrDefaultAsync(c => c.ClaimID == claimId &&
                                          c.NhanVienID == nhanVien.NhanVienID);

            if (claim == null)
            {
                TempData["Error"] = "Không tìm thấy yêu cầu hoặc bạn không có quyền hủy yêu cầu này.";
                return RedirectToAction(nameof(MyClaims));
            }

            // Chỉ được hủy khi đang ở trạng thái "ChoDuyet"
            if (claim.TrangThai != TrangThaiClaimValues.ChoDuyet)
            {
                TempData["Error"] = "Chỉ có thể hủy yêu cầu đang ở trạng thái \"Chờ duyệt\".";
                return RedirectToAction(nameof(MyClaims));
            }

            claim.TrangThai = TrangThaiClaimValues.NhanVienHuy;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã hủy yêu cầu giải quyết chế độ thành công!";
            return RedirectToAction(nameof(MyClaims));
        }

        // ════════════════════════════════════════════════════════════════════
        //  ADMIN — quản lý và phê duyệt toàn bộ claim
        // ════════════════════════════════════════════════════════════════════

        // GET: /Claim/AdminClaims
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminClaims(string? trangThai)
        {
            var query = _context.YeuCauClaims
                .Include(c => c.NhanVien)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(trangThai))
                query = query.Where(c => c.TrangThai == trangThai);

            var danhSach = await query
                .OrderByDescending(c => c.NgayYeuCau)
                .ToListAsync();

            ViewData["TrangThaiChon"] = trangThai;
            return View(danhSach);
        }

        // GET: /Claim/ClaimDetails/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ClaimDetails(int id)
        {
            var claim = await _context.YeuCauClaims
                .Include(c => c.NhanVien)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClaimID == id);

            if (claim == null)
            {
                TempData["Error"] = "Không tìm thấy yêu cầu.";
                return RedirectToAction(nameof(AdminClaims));
            }

            return View(claim);
        }

        // ─── PROCESS CLAIM (DUYỆT / TỪ CHỐI) ────────────────────────────────

        // POST: /Claim/ProcessClaim
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessClaim(int claimId, string action, string? lyDoTuChoi)
        {
            var claim = await _context.YeuCauClaims
                .FirstOrDefaultAsync(c => c.ClaimID == claimId);

            if (claim == null)
            {
                TempData["Error"] = "Không tìm thấy yêu cầu.";
                return RedirectToAction(nameof(AdminClaims));
            }

            // Chỉ xử lý được claim đang ở trạng thái "ChoDuyet"
            if (claim.TrangThai != TrangThaiClaimValues.ChoDuyet)
            {
                TempData["Error"] = "Yêu cầu này đã được xử lý trước đó.";
                return RedirectToAction(nameof(AdminClaims));
            }

            if (action == "Duyet")
            {
                claim.TrangThai = TrangThaiClaimValues.DaDuyet;
                claim.NgayXuly  = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã phê duyệt yêu cầu #{claimId} thành công!";
            }
            else if (action == "TuChoi")
            {
                if (string.IsNullOrWhiteSpace(lyDoTuChoi))
                {
                    TempData["Error"] = "Vui lòng nhập lý do từ chối trước khi xác nhận.";
                    return RedirectToAction(nameof(ClaimDetails), new { id = claimId });
                }

                claim.TrangThai  = TrangThaiClaimValues.TuChoi;
                claim.LyDoTuChoi = lyDoTuChoi.Trim();
                claim.NgayXuly   = DateTime.Now;

                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã từ chối yêu cầu #{claimId}.";
            }
            else
            {
                TempData["Error"] = "Hành động không hợp lệ.";
            }

            return RedirectToAction(nameof(AdminClaims));
        }

        // ─── XEM FILE CHỨNG TỪ ───────────────────────────────────────────────

        // GET: /Claim/ViewDocument/5
        [Authorize]
        public async Task<IActionResult> ViewDocument(int id)
        {
            YeuCauClaim? claim;

            if (User.IsInRole("Admin"))
            {
                // Admin được xem mọi claim
                claim = await _context.YeuCauClaims
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ClaimID == id);
            }
            else
            {
                // Nhân viên chỉ được xem claim của mình
                var nhanVien = await GetNhanVienHienTai();
                if (nhanVien == null) return Unauthorized();

                claim = await _context.YeuCauClaims
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ClaimID == id &&
                                              c.NhanVienID == nhanVien.NhanVienID);
            }

            if (claim == null || string.IsNullOrWhiteSpace(claim.ChungTuDinhKem))
            {
                TempData["Error"] = "Không tìm thấy chứng từ đính kèm.";
                return RedirectToAction(User.IsInRole("Admin") ? nameof(AdminClaims) : nameof(MyClaims));
            }

            string filePath = Path.Combine(_env.WebRootPath, claim.ChungTuDinhKem.Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "File chứng từ không tồn tại trên server.";
                return RedirectToAction(User.IsInRole("Admin") ? nameof(AdminClaims) : nameof(MyClaims));
            }

            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            string contentType = ext switch
            {
                ".pdf"  => "application/pdf",
                ".png"  => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _       => "application/octet-stream"
            };

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, contentType);
        }

        // ─── Helper: lấy NhanVien từ Claims đang đăng nhập ──────────────────
        private async Task<NhanVien?> GetNhanVienHienTai()
        {
            var accountIdStr = User.FindFirstValue("AccountId");
            if (!int.TryParse(accountIdStr, out int accountId))
                return null;

            return await _context.NhanViens
                .AsNoTracking()
                .FirstOrDefaultAsync(nv => nv.TaiKhoanID == accountId);
        }
    }
}
