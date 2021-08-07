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
        public virtual DbSet<Asset> Assets { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Expenditure> Expenditures { get; set; }
        public virtual DbSet<FixedExpenditure> FixedExpenditures { get; set; }
        public virtual DbSet<FixedIncome> FixedIncomes { get; set; }
        public virtual DbSet<Income> Incomes { get; set; }
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

            modelBuilder.Entity<Asset>(entity =>
            {
                entity.HasKey(e => new { e.ProductName, e.AccountEmail })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

                entity.ToTable("Asset");

                entity.HasComment("MyLaboratory Site 자산")
                    .HasCharSet("utf8mb4")
                    .UseCollation("utf8mb4_general_ci");

                entity.HasIndex(e => e.AccountEmail, "Asset_FK");

                entity.Property(e => e.ProductName).HasComment("상품명 (은행 계좌명, 증권 계좌명, 현금 등)");

                entity.Property(e => e.AccountEmail).HasComment("계정 이메일 (ID)");

                entity.Property(e => e.Amount)
                    .HasColumnType("bigint(255)")
                    .HasComment("금액");

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
                    .HasMaxLength(45)
                    .HasComment("비고");

                entity.Property(e => e.Reserved0).HasMaxLength(45);

                entity.Property(e => e.Reserved1).HasMaxLength(45);

                entity.Property(e => e.Reserved2).HasMaxLength(45);

                entity.Property(e => e.Reserved3).HasMaxLength(45);

                entity.Property(e => e.Reserved4).HasMaxLength(45);

                entity.Property(e => e.Updated)
                    .HasMaxLength(6)
                    .HasComment("업데이트일");

                entity.HasOne(d => d.AccountEmailNavigation)
                    .WithMany(p => p.Assets)
                    .HasForeignKey(d => d.AccountEmail)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("Asset_FK");
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

            modelBuilder.Entity<Expenditure>(entity =>
            {
                entity.ToTable("Expenditure");

                entity.HasComment("MyLaboratory Site 지출")
                    .HasCharSet("utf8mb4")
                    .UseCollation("utf8mb4_general_ci");

                entity.HasIndex(e => new { e.PaymentMethod, e.AccountEmail }, "Expenditure_FK");

                entity.Property(e => e.Id)
                    .HasColumnType("int(11)")
                    .HasComment("PK");

                entity.Property(e => e.AccountEmail)
                    .IsRequired()
                    .HasComment("계정 이메일 (ID)");

                entity.Property(e => e.Amount)
                    .HasColumnType("bigint(255)")
                    .HasComment("금액");

                entity.Property(e => e.Contents)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("내용 (하계할인마트/신한카드/남원추어탕/교보문고)");

                entity.Property(e => e.Created)
                    .HasMaxLength(6)
                    .HasComment("생성일");

                entity.Property(e => e.MainClass)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("대분류 (정기저축/비소비지출/소비지출)");

                entity.Property(e => e.MonetaryUnit)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasComment("화폐 단위 (KRW, USD, ETC)");

                entity.Property(e => e.MyAsset)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("내자산 (지출 중 정기저축일 때 사용)");

                entity.Property(e => e.Note)
                    .HasMaxLength(45)
                    .HasComment("비고");

                entity.Property(e => e.PaymentMethod)
                    .IsRequired()
                    .HasComment("결제수단 (자산 상품명/현금)");

                entity.Property(e => e.Reserved0).HasMaxLength(45);

                entity.Property(e => e.Reserved1).HasMaxLength(45);

                entity.Property(e => e.Reserved2).HasMaxLength(45);

                entity.Property(e => e.Reserved3).HasMaxLength(45);

                entity.Property(e => e.Reserved4).HasMaxLength(45);

                entity.Property(e => e.SubClass)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("소분류 (예적금/투자/(내계좌이체/인출) | 세금/공적연금/사회보험/가구간 이전지출/비영리단체 이전/부채상환 | (식비/외식비)/(주거/용품비)/교육비/의료비/교통비/통신비/(여가/문화)/(의류/신발)/용돈/보장성보험/기타기출/미파악지출)");

                entity.Property(e => e.Updated)
                    .HasMaxLength(6)
                    .HasComment("업데이트일");

                entity.HasOne(d => d.Asset)
                    .WithMany(p => p.Expenditures)
                    .HasForeignKey(d => new { d.PaymentMethod, d.AccountEmail })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("Expenditure_FK");
            });

            modelBuilder.Entity<FixedExpenditure>(entity =>
            {
                entity.ToTable("FixedExpenditure");

                entity.HasComment("MyLaboratory Site 고정지출")
                    .HasCharSet("utf8mb4")
                    .UseCollation("utf8mb4_general_ci");

                entity.HasIndex(e => new { e.PaymentMethod, e.AccountEmail }, "FixedExpenditure_FK");

                entity.Property(e => e.Id)
                    .HasColumnType("int(11)")
                    .HasComment("PK");

                entity.Property(e => e.AccountEmail)
                    .IsRequired()
                    .HasComment("계정 이메일 (ID)");

                entity.Property(e => e.Amount)
                    .HasColumnType("bigint(255)")
                    .HasComment("금액");

                entity.Property(e => e.Contents)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("내용 (하계할인마트/신한카드/남원추어탕/교보문고)");

                entity.Property(e => e.Created)
                    .HasMaxLength(6)
                    .HasComment("생성일");

                entity.Property(e => e.Creditor)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("금융사/채권인");

                entity.Property(e => e.Deleted).HasComment("삭제여부");

                entity.Property(e => e.InterestRate).HasComment("이자율");

                entity.Property(e => e.MainClass)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("대분류 (정기저축/비소비지출/소비지출)");

                entity.Property(e => e.MaturityDate)
                    .HasMaxLength(6)
                    .HasComment("만기일 (만기일 없으면 9999-12-31 되도록 Checkbox 할 것)");

                entity.Property(e => e.MonetaryUnit)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasComment("화폐 단위 (KRW, USD, ETC)");

                entity.Property(e => e.Note)
                    .HasMaxLength(45)
                    .HasComment("비고");

                entity.Property(e => e.PaymentMethod)
                    .IsRequired()
                    .HasComment("결제수단 (자산 상품명/현금)");

                entity.Property(e => e.ProductName)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("상품명");

                entity.Property(e => e.Reserved0).HasMaxLength(45);

                entity.Property(e => e.Reserved1).HasMaxLength(45);

                entity.Property(e => e.Reserved2).HasMaxLength(45);

                entity.Property(e => e.Reserved3).HasMaxLength(45);

                entity.Property(e => e.Reserved4).HasMaxLength(45);

                entity.Property(e => e.SubClass)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("소분류 (예적금/투자/(내계좌이체/인출) | 세금/공적연금/사회보험/가구간 이전지출/비영리단체 이전/부채상환 | (식비/외식비)/(주거/용품비)/교육비/의료비/교통비/통신비/(여가/문화)/(의류/신발)/용돈/보장성보험/기타기출/미파악지출)");

                entity.Property(e => e.Updated)
                    .HasMaxLength(6)
                    .HasComment("업데이트일");

                entity.Property(e => e.WithdrawalDate)
                    .HasMaxLength(6)
                    .HasComment("출금일 (매월 며칠)");

                entity.HasOne(d => d.Asset)
                    .WithMany(p => p.FixedExpenditures)
                    .HasForeignKey(d => new { d.PaymentMethod, d.AccountEmail })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FixedExpenditure_FK");
            });

            modelBuilder.Entity<FixedIncome>(entity =>
            {
                entity.ToTable("FixedIncome");

                entity.HasComment("MyLaboratory Site 고정수입")
                    .HasCharSet("utf8mb4")
                    .UseCollation("utf8mb4_general_ci");

                entity.HasIndex(e => new { e.DepositMethod, e.AccountEmail }, "FixedIncome_FK");

                entity.Property(e => e.Id)
                    .HasColumnType("int(11)")
                    .HasComment("PK");

                entity.Property(e => e.AccountEmail)
                    .IsRequired()
                    .HasComment("계정 이메일 (ID)");

                entity.Property(e => e.Amount)
                    .HasColumnType("bigint(255)")
                    .HasComment("금액");

                entity.Property(e => e.Contents)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("내용 (회사명/사업명)");

                entity.Property(e => e.Created)
                    .HasMaxLength(6)
                    .HasComment("생성일");

                entity.Property(e => e.Debtor)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("금융사/채무인");

                entity.Property(e => e.Deleted).HasComment("삭제여부");

                entity.Property(e => e.DepositDate)
                    .HasMaxLength(6)
                    .HasComment("입금일 (매월 며칠)");

                entity.Property(e => e.DepositMethod)
                    .IsRequired()
                    .HasComment("입금 방법 (자산 상품명/현금)");

                entity.Property(e => e.InterestRate).HasComment("이자율");

                entity.Property(e => e.MainClass)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("대분류 (정기수입/비정기수입)");

                entity.Property(e => e.MaturityDate)
                    .HasMaxLength(6)
                    .HasComment("만기일 (만기일 없으면 9999-12-31 되도록 Checkbox 할 것)");

                entity.Property(e => e.MonetaryUnit)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasComment("화폐 단위 (KRW, USD, ETC)");

                entity.Property(e => e.Note)
                    .HasMaxLength(45)
                    .HasComment("비고");

                entity.Property(e => e.ProductName)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("상품명");

                entity.Property(e => e.Reserved0).HasMaxLength(45);

                entity.Property(e => e.Reserved1).HasMaxLength(45);

                entity.Property(e => e.Reserved2).HasMaxLength(45);

                entity.Property(e => e.Reserved3).HasMaxLength(45);

                entity.Property(e => e.Reserved4).HasMaxLength(45);

                entity.Property(e => e.SubClass)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("소분류 (근로수입/사업수입/연금수입/금융소득/임대수입/기타수입)");

                entity.Property(e => e.Updated)
                    .HasMaxLength(6)
                    .HasComment("업데이트일");

                entity.HasOne(d => d.Asset)
                    .WithMany(p => p.FixedIncomes)
                    .HasForeignKey(d => new { d.DepositMethod, d.AccountEmail })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FixedIncome_FK");
            });

            modelBuilder.Entity<Income>(entity =>
            {
                entity.ToTable("Income");

                entity.HasComment("MyLaboratory Site 수입")
                    .HasCharSet("utf8mb4")
                    .UseCollation("utf8mb4_general_ci");

                entity.HasIndex(e => new { e.DepositAccount, e.AccountEmail }, "Income_FK");

                entity.Property(e => e.Id)
                    .HasColumnType("int(11)")
                    .HasComment("PK");

                entity.Property(e => e.AccountEmail)
                    .IsRequired()
                    .HasComment("계정 이메일 (ID)");

                entity.Property(e => e.Amount)
                    .HasColumnType("bigint(255)")
                    .HasComment("금액");

                entity.Property(e => e.Contents)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("내용 (회사명/사업명)");

                entity.Property(e => e.Created)
                    .HasMaxLength(6)
                    .HasComment("생성일");

                entity.Property(e => e.DepositAccount)
                    .IsRequired()
                    .HasComment("입금계좌 (자산 상품명/현금)");

                entity.Property(e => e.MainClass)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("대분류 (정기수입/비정기수입)");

                entity.Property(e => e.MonetaryUnit)
                    .IsRequired()
                    .HasMaxLength(45)
                    .HasComment("화폐 단위 (KRW, USD, ETC)");

                entity.Property(e => e.Note)
                    .HasMaxLength(45)
                    .HasComment("비고");

                entity.Property(e => e.Reserved0).HasMaxLength(45);

                entity.Property(e => e.Reserved1).HasMaxLength(45);

                entity.Property(e => e.Reserved2).HasMaxLength(45);

                entity.Property(e => e.Reserved3).HasMaxLength(45);

                entity.Property(e => e.Reserved4).HasMaxLength(45);

                entity.Property(e => e.SubClass)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasComment("소분류 (근로수입/사업수입/연금수입/금융소득/임대수입/기타수입)");

                entity.Property(e => e.Updated)
                    .HasMaxLength(6)
                    .HasComment("업데이트일");

                entity.HasOne(d => d.Asset)
                    .WithMany(p => p.Incomes)
                    .HasForeignKey(d => new { d.DepositAccount, d.AccountEmail })
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("Income_FK");
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