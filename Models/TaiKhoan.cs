using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsuranceManagement.Models
{
    [Table("TaiKhoan")]
    public class TaiKhoan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TaiKhoanID { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; } = string.Empty;

        /// <summary>
        /// Chỉ nhận 2 giá trị: "NhanVien" hoặc "Admin"
        /// </summary>
        [Required]
        [StringLength(20)]
        [Display(Name = "Vai trò")]
        public string VaiTro { get; set; } = "NhanVien";

        [Display(Name = "Ngày tạo")]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Navigation property
        public virtual NhanVien? NhanVien { get; set; }
    }
}
