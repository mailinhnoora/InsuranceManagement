using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceManagement.Models
{
    [Table("NhanVien")]
    public class NhanVien
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NhanVienID { get; set; }

        [Required]
        [Display(Name = "Tài khoản")]
        public int TaiKhoanID { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string HoTen { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Số sổ BHXH")]
        public string? SoSoBHXH { get; set; }

        [StringLength(20)]
        [Display(Name = "Số thẻ BHYT")]
        public string? SoTheBHYT { get; set; }

        [StringLength(255)]
        [Display(Name = "Nơi đăng ký khám chữa bệnh ban đầu")]
        public string? NoiDangKyKCB { get; set; }

        /// <summary>
        /// Mức lương làm căn cứ trích đóng hàng tháng.
        /// QUAN TRỌNG: Dùng decimal, KHÔNG dùng float/double để tránh sai số làm tròn.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Lương đóng bảo hiểm")]
        public decimal LuongDongBaoHiem { get; set; }

        [Display(Name = "Mức hưởng chính sách")]
        public string? MucHuongChinhSach { get; set; }

        [Display(Name = "Người thụ hưởng")]
        public string? NguoiThuHuong { get; set; }

        // Navigation properties
        [ForeignKey("TaiKhoanID")]
        public virtual TaiKhoan? TaiKhoan { get; set; }

        public virtual ICollection<KyDongBaoHiem> KyDongBaoHiems { get; set; } = new List<KyDongBaoHiem>();
        public virtual ICollection<YeuCauClaim> YeuCauClaims { get; set; } = new List<YeuCauClaim>();
    }
}
