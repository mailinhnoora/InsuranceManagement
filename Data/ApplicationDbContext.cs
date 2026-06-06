using InsuranceManagement.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
namespace InsuranceManagement.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ─── DbSets ────────────────────────────────────────────────────────────
        public DbSet<TaiKhoan>      TaiKhoans      { get; set; }
        public DbSet<NhanVien>      NhanViens      { get; set; }
        public DbSet<KyDongBaoHiem> KyDongBaoHiems { get; set; }
        public DbSet<YeuCauClaim>   YeuCauClaims   { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ─── TaiKhoan ────────────────────────────────────────────────────
            modelBuilder.Entity<TaiKhoan>(entity =>
            {
                entity.HasIndex(t => t.TenDangNhap).IsUnique();

                entity.Property(t => t.VaiTro)
                      .HasDefaultValue("NhanVien");

                entity.Property(t => t.NgayTao)
                      .HasDefaultValueSql("NOW()");
            });

            // ─── NhanVien ────────────────────────────────────────────────────
            modelBuilder.Entity<NhanVien>(entity =>
            {
                // 1-1: mỗi TaiKhoan chỉ có đúng 1 NhanVien
                entity.HasOne(nv => nv.TaiKhoan)
                      .WithOne(tk => tk.NhanVien)
                      .HasForeignKey<NhanVien>(nv => nv.TaiKhoanID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ─── KyDongBaoHiem ────────────────────────────────────────────────
            modelBuilder.Entity<KyDongBaoHiem>(entity =>
            {
                entity.HasOne(k => k.NhanVien)
                      .WithMany(nv => nv.KyDongBaoHiems)
                      .HasForeignKey(k => k.NhanVienID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(k => k.TrangThaiThanhToan)
                      .HasDefaultValue(TrangThaiThanhToanValues.ChuaThanhToan);
            });

            // ─── YeuCauClaim ──────────────────────────────────────────────────
            modelBuilder.Entity<YeuCauClaim>(entity =>
            {
                entity.HasOne(c => c.NhanVien)
                      .WithMany(nv => nv.YeuCauClaims)
                      .HasForeignKey(c => c.NhanVienID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(c => c.TrangThai)
                      .HasDefaultValue(TrangThaiClaimValues.ChoDuyet);

                // LyDoTuChoi và NgayXuly phải là nullable
                entity.Property(c => c.LyDoTuChoi).IsRequired(false);
                entity.Property(c => c.NgayXuly).IsRequired(false);
            });
        }
    }
}
