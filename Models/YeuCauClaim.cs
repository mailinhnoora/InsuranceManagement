using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceManagement.Models
{
    [Table("YeuCauClaim")]
    public class YeuCauClaim
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ClaimID { get; set; }

        [Required]
        [Display(Name = "Nhân viên")]
        public int NhanVienID { get; set; }

        /// <summary>
        /// Ví dụ: "Ốm đau", "Thai sản", "Tai nạn lao động", "Bệnh nghề nghiệp", ...
        /// </summary>
        [Required]
        [StringLength(100)]
        [Display(Name = "Loại chế độ")]
        public string LoaiCheDo { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Ngày yêu cầu")]
        public DateTime NgayYeuCau { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// QUAN TRỌNG: Dùng decimal, KHÔNG dùng float/double.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Số tiền yêu cầu (VNĐ)")]
        public decimal SoTienYeuCau { get; set; }

        [Required]
        [Display(Name = "Mô tả / Lý do")]
        public string MoTaLyDo { get; set; } = string.Empty;

        /// <summary>
        /// Đường dẫn file ảnh/PDF chứng từ đã upload lên server.
        /// </summary>
        [StringLength(255)]
        [Display(Name = "Chứng từ đính kèm")]
        public string? ChungTuDinhKem { get; set; }

        /// <summary>
        /// Nhận 1 trong 4 giá trị:
        ///   "ChoDuyet" | "DaDuyet" | "TuChoi" | "NhanVienHuy"
        /// </summary>
        [Required]
        [StringLength(50)]
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = TrangThaiClaimValues.ChoDuyet;

        /// <summary>
        /// Nullable — chỉ có giá trị khi Admin từ chối.
        /// Nếu để NOT NULL, hệ thống sẽ crash khi nhân viên tạo đơn mới.
        /// </summary>
        [Display(Name = "Lý do từ chối")]
        public string? LyDoTuChoi { get; set; }

        /// <summary>
        /// Nullable — chỉ có giá trị khi Admin đã xử lý (duyệt hoặc từ chối).
        /// </summary>
        [Display(Name = "Ngày xử lý")]
        public DateTime? NgayXuly { get; set; }

        // Navigation property
        [ForeignKey("NhanVienID")]
        public virtual NhanVien? NhanVien { get; set; }
    }

    /// <summary>
    /// Hằng số cho cột TrangThai — tránh lỗi magic string.
    /// </summary>
    public static class TrangThaiClaimValues
    {
        public const string ChoDuyet    = "ChoDuyet";
        public const string DaDuyet     = "DaDuyet";
        public const string TuChoi      = "TuChoi";
        public const string NhanVienHuy = "NhanVienHuy";
    }
}
