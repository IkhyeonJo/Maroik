using Microsoft.EntityFrameworkCore;
using MyLaboratory.Site.Models;

namespace MyLaboratory.Site.Data
{
    public partial class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext()
        {
        }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            ChangeTracker.AutoDetectChangesEnabled = false;
        }

        #region (For Migration) DbSet<T>'s T is table type.
        public virtual DbSet<Account> Accounts { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<SubCategory> SubCategories { get; set; }
        #endregion
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasCharSet("utf8")
                            .UseCollation("utf8_general_ci");

            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(e => e.Email)
                    .HasName("PRIMARY");

                entity.ToTable("Account");

                entity.HasComment("MyLaboratory.Site 계정")
                    .HasCharSet("utf8mb4")
                    .UseCollation("utf8mb4_general_ci");

                entity.Property(e => e.Email).HasComment("계정 이메일 (ID)");

                entity.Property(e => e.AgreedServiceTerms).HasComment("약관 동의 여부");

                entity.Property(e => e.AvatarImagePath)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasDefaultValueSql("'/upload/Management/Profile/default-avatar.jpg'")
                    .HasComment("계정 아바타 이미지 경로");

                entity.Property(e => e.Created)
                    .HasMaxLength(6)
                    .HasDefaultValueSql("'1900-01-01 00:00:00.000000'")
                    .HasComment("계정 생성일");

                entity.Property(e => e.Deleted).HasComment("계정 삭제 여부");

                entity.Property(e => e.EmailConfirmed).HasComment("이메일 확인 여부");

                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("계정 성명");

                entity.Property(e => e.HashedPassword)
                    .IsRequired()
                    .HasComment("계정 암호화 된 비밀번호");

                entity.Property(e => e.Locked).HasComment("계정 잠금");

                entity.Property(e => e.LoginAttempt)
                    .HasColumnType("int(11)")
                    .HasComment("로그인 시도 횟수");

                entity.Property(e => e.Message).HasComment("계정 상태 메시지");

                entity.Property(e => e.RegistrationToken).HasComment("회원가입 인증 토큰");

                entity.Property(e => e.ResetPasswordToken).HasComment("비밀번호 찾기 인증 토큰");

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasDefaultValueSql("'User'")
                    .HasComment("계정 역할 (Admin 또는 User)");

                entity.Property(e => e.Updated)
                    .HasMaxLength(6)
                    .HasDefaultValueSql("'1900-01-01 00:00:00.000000'")
                    .HasComment("계정 업데이트일");
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Category");

                entity.HasComment("카테고리 /*MyLaboratory.Site의 로그인 후 접근 가능한 좌측 SideBar 설정 시 사용*/");

                entity.Property(e => e.Id)
                    .HasColumnType("int(11)")
                    .HasComment("ID");

                entity.Property(e => e.Action)
                    .HasMaxLength(256)
                    .HasComment("접근 MVC Action 명 /*이 값이 없으면 하위 카테고리 존재*/");

                entity.Property(e => e.Controller)
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasComment("접근 MVC Controller 명");

                entity.Property(e => e.DisplayName)
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasComment("표시이름");

                entity.Property(e => e.IconPath)
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasComment("표시 아이콘 경로 /*FontAwesome 사용*/");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasComment("이름");

                entity.Property(e => e.Order)
                    .HasColumnType("int(11)")
                    .HasComment("출력 순서");

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasDefaultValueSql("'Admin'")
                    .HasComment("접근 권한 설정");
            });

            modelBuilder.Entity<SubCategory>(entity =>
            {
                entity.ToTable("SubCategory");

                entity.HasComment("서브 카테고리 /*MyLaboratory.Site의 로그인 후 접근 가능한 좌측 SideBar 설정 시 사용*/");

                entity.HasIndex(e => e.CategoryId, "SubCategories_FK");

                entity.Property(e => e.Id)
                    .HasColumnType("int(11)")
                    .HasComment("ID");

                entity.Property(e => e.Action)
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasComment("접근 MVC Action 명 /*이 값이 없으면 하위 카테고리 존재*/");

                entity.Property(e => e.CategoryId)
                    .HasColumnType("int(11)")
                    .HasComment("부모 카테고리 ID");

                entity.Property(e => e.DisplayName)
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasComment("표시이름");

                entity.Property(e => e.IconPath)
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasComment("표시 아이콘 경로 /*FontAwesome 사용*/");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasComment("이름");

                entity.Property(e => e.Order)
                    .HasColumnType("int(11)")
                    .HasComment("출력 순서");

                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasDefaultValueSql("'Admin'")
                    .HasComment("접근 권한 설정");

                entity.HasOne(d => d.Category)
                    .WithMany(p => p.SubCategories)
                    .HasForeignKey(d => d.CategoryId)
                    .HasConstraintName("SubCategories_FK");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}