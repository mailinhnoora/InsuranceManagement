using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceManagement.Models
{
    [Table("KyDongBaoHiem")]
    public class KyDongBaoHiem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int KyDongID { get; set; }

        [Required]
        [Display(Name = "Nhân viên")]
        public int NhanVienID { get; set; }

        /// <summary>
        /// Định dạng MM/YYYY. Ví dụ: "05/2026"
        /// </summary>
        [Required]
        [StringLength(7)]
        [Display(Name = "Tháng/Năm")]
        public string ThangNam { get; set; } = string.Empty;

        /// <summary>
        /// Mức lương tính đóng tại thời điểm tháng đó (snapshot tại thời điểm tính).
        /// QUAN TRỌNG: Dùng decimal, KHÔNG dùng float/double.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Lương gốc")]
        public decimal LuongGoc { get; set; }

        /// <summary>
        /// Tiền BHXH nhân viên tự đóng = LuongGoc × 8.0%
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Tiền BHXH (NV đóng)")]
        public decimal TienBHXH_NV { get; set; }

        /// <summary>
        /// Tiền BHYT nhân viên tự đóng = LuongGoc × 1.5%
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Tiền BHYT (NV đóng)")]
        public decimal TienBHYT_NV { get; set; }

        /// <summary>
        /// Tiền BHTN nhân viên tự đóng = LuongGoc × 1.0%
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Tiền BHTN (NV đóng)")]
        public decimal TienBHTN_NV { get; set; }

        /// <summary>
        /// Tổng khấu trừ vào lương = TienBHXH_NV + TienBHYT_NV + TienBHTN_NV (= 10.5%)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Tổng NV đóng")]
        public decimal TongTienNhanVien { get; set; }

        /// <summary>
        /// Tổng doanh nghiệp đóng thêm = LuongGoc × 22.0%
        /// (BHXH 17.5% + BHYT 3.0% + BHTN 1.0% + BHTNLĐ-BNN 0.5%)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Tổng công ty đóng")]
        public decimal TongTienCongTy { get; set; }

        /// <summary>
        /// "DaThanhToan" hoặc "ChuaThanhToan"
        /// </summary>
        [Required]
        [StringLength(50)]
        [Display(Name = "Trạng thái thanh toán")]
        public string TrangThaiThanhToan { get; set; } = TrangThaiThanhToanValues.ChuaThanhToan;

        // Navigation property
        [ForeignKey("NhanVienID")]
        public virtual NhanVien? NhanVien { get; set; }
    }

    /// <summary>
    /// Hằng số cho cột TrangThaiThanhToan — tránh lỗi magic string.
    /// </summary>
    public static class TrangThaiThanhToanValues
    {
        public const string DaThanhToan   = "DaThanhToan";
        public const string ChuaThanhToan = "ChuaThanhToan";
    }
}
