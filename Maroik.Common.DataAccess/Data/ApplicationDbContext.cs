using Microsoft.EntityFrameworkCore;
using Maroik.Common.DataAccess.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace Maroik.Common.DataAccess.Data
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

        #region (For Migration) DbSet<T>'s T is Table.
        public virtual DbSet<Account> Accounts { get; set; }
        public virtual DbSet<Asset> Assets { get; set; }
        public virtual DbSet<AttachedFile> AttachedFiles { get; set; }
        public virtual DbSet<Board> Boards { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Comment> Comments { get; set; }
        public virtual DbSet<Expenditure> Expenditures { get; set; }
        public virtual DbSet<FixedExpenditure> FixedExpenditures { get; set; }
        public virtual DbSet<FixedIncome> FixedIncomes { get; set; }
        public virtual DbSet<Income> Incomes { get; set; }
        public virtual DbSet<SubCategory> SubCategories { get; set; }
        #endregion

        #region (For Migration) DbSet<T>'s T is Stored Procedure.
        public virtual DbSet<USP_Expenditure_MonthlyReport> USP_Expenditure_MonthlyReports { get; set; }
        public virtual DbSet<USP_Income_MonthlyReport> USP_Income_MonthlyReports { get; set; }
        #endregion

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region (For Migration) Entity<T>'s T is Stored Procedure.
            modelBuilder.Entity<USP_Expenditure_MonthlyReport>().HasNoKey();
            modelBuilder.Entity<USP_Income_MonthlyReport>().HasNoKey();
            #endregion

            #region (For Migration) Entity<T>'s T is Table.
            modelBuilder
                .UseCollation("utf8mb4_general_ci")
                .HasCharSet("utf8mb4");

            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(e => e.Email).HasName("PRIMARY");

                entity
                    .ToTable("Account", tb => tb.HasComment("Maroik.WebSite 계정"))
                    .UseCollation("utf8mb4_general_ci");

                entity.HasIndex(e => e.Nickname, "Nickname").IsUnique();

                entity.Property(e => e.Email).HasComment("이메일 (ID)");
                entity.Property(e => e.AgreedServiceTerms).HasComment("약관 동의 여부");
                entity.Property(e => e.AvatarImagePath)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasDefaultValueSql("'/upload/Management/Profile/default-avatar.jpg'")
                    .HasComment("아바타 이미지 경로");
                entity.Property(e => e.Created)
                    .HasMaxLength(6)
                    .HasDefaultValueSql("'1900-01-01 00:00:00.000000'")
                    .HasComment("생성일");
                entity.Property(e => e.Deleted).HasComment("삭제 여부");
                entity.Property(e => e.EmailConfirmed).HasComment("이메일 확인 여부");
                entity.Property(e => e.HashedPassword)
                    .IsRequired()
                    .HasComment("해시화 된 비밀번호");
                entity.Property(e => e.Locked).HasComment("잠금 여부");
                entity.Property(e => e.LoginAttempt)
                    .HasComment("로그인 시도 횟수")
                    .HasColumnType("int(11)");
                entity.Property(e => e.Message).HasComment("상태 메시지");
                entity.Property(e => e.Nickname)
                    .IsRequired()
                    .HasComment("닉네임");
                entity.Property(e => e.RegistrationToken).HasComment("회원가입 인증 토큰");
                entity.Property(e => e.ResetPasswordToken).HasComment("비밀번호 찾기 인증 토큰");
                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasDefaultValueSql("'User'")
                    .HasComment("역할 (Admin 또는 User)");
                entity.Property(e => e.Updated)
                    .HasMaxLength(6)
                    .HasDefaultValueSql("'1900-01-01 00:00:00.000000'")
                    .HasComment("업데이트일");
            });

            modelBuilder.Entity<Asset>(entity =>
            {
                entity.HasKey(e => new { e.ProductName, e.AccountEmail })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

                entity
                    .ToTable("Asset", tb => tb.HasComment("Maroik.WebSite 자산"))
                    .UseCollation("utf8mb4_general_ci");

                entity.HasIndex(e => e.AccountEmail, "Asset_FK");

                entity.Property(e => e.ProductName).HasComment("상품명 (은행 계좌명, 증권 계좌명, 현금 등)");
                entity.Property(e => e.AccountEmail).HasComment("계정 이메일 (ID)");
                entity.Property(e => e.Amount)
                    .HasComment("금액")
                    .HasColumnType("bigint(255)");
                entity.Property(e => e.Created)
                    .HasMaxLength(6)
                    .HasComment("생성일");
                entity.Property(e => e.Deleted).HasComment("삭제여부");
                entity.Property(e => e.Item)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("항목 (자유입출금 자산, 신탁 자산, 현금 자산, 저축성 자산, 투자성 자산, 부동산, 동산, 기타 실물 자산, 보험 자산)");
                entity.Property(e => e.MonetaryUnit)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasComment("화폐 단위 (KRW, USD, ETC)");
                entity.Property(e => e.Note)
                    .HasMaxLength(255)
                    .HasComment("비고");
                entity.Property(e => e.Updated)
                    .HasMaxLength(6)
                    .HasComment("업데이트일");

                entity.HasOne(d => d.AccountEmailNavigation).WithMany(p => p.Assets)
                    .HasForeignKey(d => d.AccountEmail)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("Asset_FK");
            });

            modelBuilder.Entity<AttachedFile>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PRIMARY");

                entity
                    .ToTable("AttachedFile", tb => tb.HasComment("Maroik.WebSite 첨부 파일"))
                    .HasCharSet("utf8mb3")
                    .UseCollation("utf8mb3_general_ci");

                entity.HasIndex(e => e.BoardId, "fk_Comment_Board_idx");

                entity.Property(e => e.Id)
                    .HasComment("PK")
                    .HasColumnType("int(11)");
                entity.Property(e => e.BoardId)
                    .HasComment("부모 게시판 Id")
                    .HasColumnType("int(11)");
                entity.Property(e => e.Extension)
                    .HasMaxLength(255)
                    .HasComment("확장자");
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("이름");
                entity.Property(e => e.Path)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("경로");
                entity.Property(e => e.Size)
                    .HasComment("크기 (Byte)")
                    .HasColumnType("int(11)");

                entity.HasOne(d => d.Board).WithMany(p => p.AttachedFiles)
                    .HasForeignKey(d => d.BoardId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_Comment_Board0");
            });

            modelBuilder.Entity<Board>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PRIMARY");

                entity
                    .ToTable("Board", tb => tb.HasComment("Maroik.WebSite 게시판"))
                    .HasCharSet("utf8mb3")
                    .UseCollation("utf8mb3_general_ci");

                entity.Property(e => e.Id)
                    .HasComment("PK")
                    .HasColumnType("int(11)");
                entity.Property(e => e.Content)
                    .IsRequired()
                    .HasComment("내용");
                entity.Property(e => e.Created)
                    .HasMaxLength(6)
                    .HasComment("생성일");
                entity.Property(e => e.Deleted).HasComment("삭제 여부");
                entity.Property(e => e.Locked).HasComment("잠금 여부");
                entity.Property(e => e.Noticed).HasComment("공지 여부");
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("제목");
                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("형태 (자유)");
                entity.Property(e => e.Updated)
                    .HasMaxLength(6)
                    .HasComment("업데이트일");
                entity.Property(e => e.Views)
                    .HasComment("조회수")
                    .HasColumnType("int(11)");
                entity.Property(e => e.Writer)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("작성자");
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PRIMARY");

                entity
                    .ToTable("Category", tb => tb.HasComment("카테고리 /*Maroik.WebSite의 로그인 후 접근 가능한 좌측 SideBar 설정 시 사용*/"))
                    .HasCharSet("utf8mb3")
                    .UseCollation("utf8mb3_general_ci");

                entity.Property(e => e.Id)
                    .HasComment("ID")
                    .HasColumnType("int(11)");
                entity.Property(e => e.Action)
                    .HasMaxLength(255)
                    .HasComment("접근 MVC Action 명 /*이 값이 없으면 하위 카테고리 존재*/");
                entity.Property(e => e.Controller)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("접근 MVC Controller 명");
                entity.Property(e => e.DisplayName)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("표시이름");
                entity.Property(e => e.IconPath)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("표시 아이콘 경로 /*FontAwesome 사용*/");
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("이름");
                entity.Property(e => e.Order)
                    .HasComment("출력 순서")
                    .HasColumnType("int(11)");
                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasDefaultValueSql("'Admin'")
                    .HasComment("접근 권한 설정");
            });

            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PRIMARY");

                entity
                    .ToTable("Comment", tb => tb.HasComment("Maroik.WebSite 댓글"))
                    .HasCharSet("utf8mb3")
                    .UseCollation("utf8mb3_general_ci");

                entity.HasIndex(e => e.BoardId, "fk_Comment_Board_idx");

                entity.Property(e => e.Id)
                    .HasComment("PK")
                    .HasColumnType("int(11)");
                entity.Property(e => e.AvatarImagePath)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("계정 아바타 이미지 경로");
                entity.Property(e => e.BoardId)
                    .HasComment("게시물 Id")
                    .HasColumnType("int(11)");
                entity.Property(e => e.Content)
                    .IsRequired()
                    .HasComment("내용");
                entity.Property(e => e.Created)
                    .HasMaxLength(6)
                    .HasComment("생성일");
                entity.Property(e => e.Deleted).HasComment("삭제 여부");
                entity.Property(e => e.Order)
                    .HasComment("순서")
                    .HasColumnType("int(11)");
                entity.Property(e => e.Writer)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("작성자");

                entity.HasOne(d => d.Board).WithMany(p => p.Comments)
                    .HasForeignKey(d => d.BoardId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_Comment_Board");
            });

            modelBuilder.Entity<Expenditure>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PRIMARY");

                entity
                    .ToTable("Expenditure", tb => tb.HasComment("Maroik.WebSite 지출"))
                    .UseCollation("utf8mb4_general_ci");

                entity.HasIndex(e => new { e.PaymentMethod, e.AccountEmail }, "Expenditure_FK");

                entity.Property(e => e.Id)
                    .HasComment("PK")
                    .HasColumnType("int(11)");
                entity.Property(e => e.AccountEmail)
                    .IsRequired()
                    .HasComment("계정 이메일 (ID)");
                entity.Property(e => e.Amount)
                    .HasComment("금액")
                    .HasColumnType("bigint(255)");
                entity.Property(e => e.Contents)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("내용 (A마트/B카드/C음식점/D도서관)");
                entity.Property(e => e.Created)
                    .HasMaxLength(6)
                    .HasComment("생성일");
                entity.Property(e => e.MainClass)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("대분류 (정기저축/비소비지출/소비지출)");
                entity.Property(e => e.MyDepositAsset)
                    .HasMaxLength(255)
                    .HasComment("내 입금 자산 (자산 상품명/현금) (지출 중 [예적금, 내자산이체, 투자, 공적연금, 부채상환]일 때 사용)");
                entity.Property(e => e.Note)
                    .HasMaxLength(255)
                    .HasComment("비고");
                entity.Property(e => e.PaymentMethod)
                    .IsRequired()
                    .HasComment("결제 수단 (자산 상품명/현금)");
                entity.Property(e => e.SubClass)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("소분류 (예적금/내자산이체/투자 | 공적연금/부채상환/세금/사회보험/가구간 이전지출/비영리단체 이전 | (식비/외식비)/(주거/용품비)/교육비/의료비/교통비/통신비/(여가/문화)/(의류/신발)/용돈/보장성보험/기타지출/미파악지출)");
                entity.Property(e => e.Updated)
                    .HasMaxLength(6)
                    .HasComment("업데이트일");

                entity.HasOne(d => d.Asset).WithMany(p => p.Expenditures)
                    .HasForeignKey(d => new { d.PaymentMethod, d.AccountEmail })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("Expenditure_FK");
            });

            modelBuilder.Entity<FixedExpenditure>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PRIMARY");

                entity
                    .ToTable("FixedExpenditure", tb => tb.HasComment("Maroik.WebSite 고정지출"))
                    .UseCollation("utf8mb4_general_ci");

                entity.HasIndex(e => new { e.PaymentMethod, e.AccountEmail }, "FixedExpenditure_FK");

                entity.Property(e => e.Id)
                    .HasComment("PK")
                    .HasColumnType("int(11)");
                entity.Property(e => e.AccountEmail)
                    .IsRequired()
                    .HasComment("계정 이메일 (ID)");
                entity.Property(e => e.Amount)
                    .HasComment("금액")
                    .HasColumnType("bigint(255)");
                entity.Property(e => e.Contents)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("내용 (A마트/B카드/C음식점/D도서관)");
                entity.Property(e => e.Created)
                    .HasMaxLength(6)
                    .HasComment("생성일");
                entity.Property(e => e.DepositDay)
                    .HasComment("입금일")
                    .HasColumnType("tinyint(2) unsigned");
                entity.Property(e => e.DepositMonth)
                    .HasComment("입금월")
                    .HasColumnType("tinyint(2) unsigned");
                entity.Property(e => e.MainClass)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("대분류 (정기저축/비소비지출/소비지출)");
                entity.Property(e => e.MaturityDate)
                    .HasMaxLength(6)
                    .HasComment("만기일");
                entity.Property(e => e.MyDepositAsset)
                    .HasMaxLength(255)
                    .HasComment("내 입금 자산 (자산 상품명/현금) (지출 중 [예적금, 내자산이체, 투자, 공적연금, 부채상환]일 때 사용)");
                entity.Property(e => e.Note)
                    .HasMaxLength(255)
                    .HasComment("비고");
                entity.Property(e => e.PaymentMethod)
                    .IsRequired()
                    .HasComment("결제 수단 (자산 상품명/현금)");
                entity.Property(e => e.SubClass)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("소분류 (예적금/내자산이체/투자 | 공적연금/부채상환/세금/사회보험/가구간 이전지출/비영리단체 이전 | (식비/외식비)/(주거/용품비)/교육비/의료비/교통비/통신비/(여가/문화)/(의류/신발)/용돈/보장성보험/기타지출/미파악지출)");
                entity.Property(e => e.Unpunctuality).HasComment("시간 미엄수 (고정 지출 시간 약속을 지키지 않았을 때 계속 알림에 표시하는 용도)");
                entity.Property(e => e.Updated)
                    .HasMaxLength(6)
                    .HasComment("업데이트일");

                entity.HasOne(d => d.Asset).WithMany(p => p.FixedExpenditures)
                    .HasForeignKey(d => new { d.PaymentMethod, d.AccountEmail })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FixedExpenditure_FK");
            });

            modelBuilder.Entity<FixedIncome>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PRIMARY");

                entity
                    .ToTable("FixedIncome", tb => tb.HasComment("Maroik.WebSite 고정수입"))
                    .UseCollation("utf8mb4_general_ci");

                entity.HasIndex(e => new { e.DepositMyAssetProductName, e.AccountEmail }, "FixedIncome_FK");

                entity.Property(e => e.Id)
                    .HasComment("PK")
                    .HasColumnType("int(11)");
                entity.Property(e => e.AccountEmail)
                    .IsRequired()
                    .HasComment("계정 이메일 (ID)");
                entity.Property(e => e.Amount)
                    .HasComment("금액")
                    .HasColumnType("bigint(255)");
                entity.Property(e => e.Contents)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("내용 (회사명/사업명)");
                entity.Property(e => e.Created)
                    .HasMaxLength(6)
                    .HasComment("생성일");
                entity.Property(e => e.DepositDay)
                    .HasComment("입금일")
                    .HasColumnType("tinyint(2) unsigned");
                entity.Property(e => e.DepositMonth)
                    .HasComment("입금월")
                    .HasColumnType("tinyint(2) unsigned");
                entity.Property(e => e.DepositMyAssetProductName)
                    .IsRequired()
                    .HasComment("입금 자산 (자산 상품명/현금)");
                entity.Property(e => e.MainClass)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("대분류 (정기수입/비정기수입)");
                entity.Property(e => e.MaturityDate)
                    .HasMaxLength(6)
                    .HasComment("만기일");
                entity.Property(e => e.Note)
                    .HasMaxLength(255)
                    .HasComment("비고");
                entity.Property(e => e.SubClass)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("소분류 (근로수입/사업수입/연금수입/금융소득/임대수입/기타수입)");
                entity.Property(e => e.Unpunctuality).HasComment("시간 미엄수 (고정 수입 시간 약속을 지키지 않았을 때 계속 알림에 표시하는 용도)");
                entity.Property(e => e.Updated)
                    .HasMaxLength(6)
                    .HasComment("업데이트일");

                entity.HasOne(d => d.Asset).WithMany(p => p.FixedIncomes)
                    .HasForeignKey(d => new { d.DepositMyAssetProductName, d.AccountEmail })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FixedIncome_FK");
            });

            modelBuilder.Entity<Income>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PRIMARY");

                entity
                    .ToTable("Income", tb => tb.HasComment("Maroik.WebSite 수입"))
                    .UseCollation("utf8mb4_general_ci");

                entity.HasIndex(e => new { e.DepositMyAssetProductName, e.AccountEmail }, "Income_FK");

                entity.Property(e => e.Id)
                    .HasComment("PK")
                    .HasColumnType("int(11)");
                entity.Property(e => e.AccountEmail)
                    .IsRequired()
                    .HasComment("계정 이메일 (ID)");
                entity.Property(e => e.Amount)
                    .HasComment("금액")
                    .HasColumnType("bigint(255)");
                entity.Property(e => e.Contents)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("내용 (회사명/사업명)");
                entity.Property(e => e.Created)
                    .HasMaxLength(6)
                    .HasComment("생성일");
                entity.Property(e => e.DepositMyAssetProductName)
                    .IsRequired()
                    .HasComment("입금 자산 (자산 상품명/현금)");
                entity.Property(e => e.MainClass)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("대분류 (정기수입/비정기수입)");
                entity.Property(e => e.Note)
                    .HasMaxLength(255)
                    .HasComment("비고");
                entity.Property(e => e.SubClass)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("소분류 (근로수입/사업수입/연금수입/금융소득/임대수입/기타수입)");
                entity.Property(e => e.Updated)
                    .HasMaxLength(6)
                    .HasComment("업데이트일");

                entity.HasOne(d => d.Asset).WithMany(p => p.Incomes)
                    .HasForeignKey(d => new { d.DepositMyAssetProductName, d.AccountEmail })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("Income_FK");
            });

            modelBuilder.Entity<SubCategory>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PRIMARY");

                entity
                    .ToTable("SubCategory", tb => tb.HasComment("서브 카테고리 /*Maroik.WebSite의 로그인 후 접근 가능한 좌측 SideBar 설정 시 사용*/"))
                    .HasCharSet("utf8mb3")
                    .UseCollation("utf8mb3_general_ci");

                entity.HasIndex(e => e.CategoryId, "SubCategory_FK");

                entity.Property(e => e.Id)
                    .HasComment("ID")
                    .HasColumnType("int(11)");
                entity.Property(e => e.Action)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("접근 MVC Action 명 /*이 값이 없으면 하위 카테고리 존재*/");
                entity.Property(e => e.CategoryId)
                    .HasComment("부모 카테고리 ID")
                    .HasColumnType("int(11)");
                entity.Property(e => e.DisplayName)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("표시이름");
                entity.Property(e => e.IconPath)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("표시 아이콘 경로 /*FontAwesome 사용*/");
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("이름");
                entity.Property(e => e.Order)
                    .HasComment("출력 순서")
                    .HasColumnType("int(11)");
                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasDefaultValueSql("'Admin'")
                    .HasComment("접근 권한 설정");

                entity.HasOne(d => d.Category).WithMany(p => p.SubCategories)
                    .HasForeignKey(d => d.CategoryId)
                    .HasConstraintName("SubCategory_FK");
            });
            #endregion

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}