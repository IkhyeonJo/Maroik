using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MyLaboratory.Site.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateTable(
                name: "Account",
                columns: table => new
                {
                    Email = table.Column<string>(type: "varchar(255)", nullable: false, comment: "계정 이메일 (ID)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HashedPassword = table.Column<string>(type: "longtext", nullable: false, comment: "계정 암호화 된 비밀번호", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FullName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "계정 성명", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AvatarImagePath = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, defaultValueSql: "'/upload/Management/Profile/default-avatar.jpg'", comment: "계정 아바타 이미지 경로", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, defaultValueSql: "'User'", comment: "계정 역할 (Admin 또는 User)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Locked = table.Column<bool>(type: "tinyint(1)", nullable: false, comment: "계정 잠금"),
                    LoginAttempt = table.Column<int>(type: "int(11)", nullable: false, comment: "로그인 시도 횟수"),
                    EmailConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false, comment: "이메일 확인 여부"),
                    AgreedServiceTerms = table.Column<bool>(type: "tinyint(1)", nullable: false, comment: "약관 동의 여부"),
                    RegistrationToken = table.Column<string>(type: "longtext", nullable: true, comment: "회원가입 인증 토큰", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResetPasswordToken = table.Column<string>(type: "longtext", nullable: true, comment: "비밀번호 찾기 인증 토큰", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Created = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: false, defaultValueSql: "'1900-01-01 00:00:00.000000'", comment: "계정 생성일"),
                    Updated = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: false, defaultValueSql: "'1900-01-01 00:00:00.000000'", comment: "계정 업데이트일"),
                    Message = table.Column<string>(type: "longtext", nullable: true, comment: "계정 상태 메시지", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Deleted = table.Column<bool>(type: "tinyint(1)", nullable: false, comment: "계정 삭제 여부")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.Email);
                },
                comment: "MyLaboratory.Site 계정")
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int(11)", nullable: false, comment: "ID")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false, comment: "이름", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    DisplayName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false, comment: "표시이름", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    IconPath = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false, comment: "표시 아이콘 경로 /*FontAwesome 사용*/", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    Controller = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false, comment: "접근 MVC Controller 명", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    Action = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true, comment: "접근 MVC Action 명 /*이 값이 없으면 하위 카테고리 존재*/", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    Role = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false, defaultValueSql: "'Admin'", comment: "접근 권한 설정", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    Order = table.Column<int>(type: "int(11)", nullable: false, comment: "출력 순서")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.Id);
                },
                comment: "카테고리 /*MyLaboratory.Site의 로그인 후 접근 가능한 좌측 SideBar 설정 시 사용*/")
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "Asset",
                columns: table => new
                {
                    ProductName = table.Column<string>(type: "varchar(255)", nullable: false, comment: "상품명 (은행 계좌명, 증권 계좌명, 현금 등)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccountEmail = table.Column<string>(type: "varchar(255)", nullable: false, comment: "계정 이메일 (ID)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Item = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "항목 (자유입출금 자산, 신탁 자산, 현금 자산, 저축성 자산, 투자성 자산, 부동산, 동산, 기타 실물 자산, 보험 자산)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<long>(type: "bigint(255)", nullable: false, comment: "금액"),
                    MonetaryUnit = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false, comment: "화폐 단위 (KRW, USD, ETC)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Created = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: false, comment: "생성일"),
                    Updated = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: false, comment: "업데이트일"),
                    Note = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, comment: "비고", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Deleted = table.Column<bool>(type: "tinyint(1)", nullable: false, comment: "삭제여부"),
                    Reserved0 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved1 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved2 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved3 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved4 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.ProductName, x.AccountEmail })
                        .Annotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                    table.ForeignKey(
                        name: "Asset_FK",
                        column: x => x.AccountEmail,
                        principalTable: "Account",
                        principalColumn: "Email",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "MyLaboratory Site 자산")
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "SubCategory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int(11)", nullable: false, comment: "ID")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CategoryId = table.Column<int>(type: "int(11)", nullable: false, comment: "부모 카테고리 ID"),
                    Name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false, comment: "이름", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    DisplayName = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false, comment: "표시이름", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    IconPath = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false, comment: "표시 아이콘 경로 /*FontAwesome 사용*/", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    Action = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false, comment: "접근 MVC Action 명 /*이 값이 없으면 하위 카테고리 존재*/", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    Role = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false, defaultValueSql: "'Admin'", comment: "접근 권한 설정", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    Order = table.Column<int>(type: "int(11)", nullable: false, comment: "출력 순서")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubCategory", x => x.Id);
                    table.ForeignKey(
                        name: "SubCategories_FK",
                        column: x => x.CategoryId,
                        principalTable: "Category",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "서브 카테고리 /*MyLaboratory.Site의 로그인 후 접근 가능한 좌측 SideBar 설정 시 사용*/")
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "Expenditure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int(11)", nullable: false, comment: "PK")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AccountEmail = table.Column<string>(type: "varchar(255)", nullable: false, comment: "계정 이메일 (ID)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MainClass = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "대분류 (정기저축/비소비지출/소비지출)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubClass = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "소분류 (예적금/투자/(내계좌이체/인출) | 세금/공적연금/사회보험/가구간 이전지출/비영리단체 이전/부채상환 | (식비/외식비)/(주거/용품비)/교육비/의료비/교통비/통신비/(여가/문화)/(의류/신발)/용돈/보장성보험/기타기출/미파악지출)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Contents = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "내용 (하계할인마트/신한카드/남원추어탕/교보문고)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<long>(type: "bigint(255)", nullable: false, comment: "금액"),
                    MonetaryUnit = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false, comment: "화폐 단위 (KRW, USD, ETC)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PaymentMethod = table.Column<string>(type: "varchar(255)", nullable: false, comment: "결제수단 (자산 상품명/현금)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MyAsset = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "내자산 (지출 중 정기저축일 때 사용)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Created = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: false, comment: "생성일"),
                    Updated = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: false, comment: "업데이트일"),
                    Note = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, comment: "비고", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved0 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved1 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved2 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved3 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved4 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenditure", x => x.Id);
                    table.ForeignKey(
                        name: "Expenditure_FK",
                        columns: x => new { x.PaymentMethod, x.AccountEmail },
                        principalTable: "Asset",
                        principalColumns: new[] { "ProductName", "AccountEmail" },
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "MyLaboratory Site 지출")
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "FixedExpenditure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int(11)", nullable: false, comment: "PK")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AccountEmail = table.Column<string>(type: "varchar(255)", nullable: false, comment: "계정 이메일 (ID)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MainClass = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "대분류 (정기저축/비소비지출/소비지출)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubClass = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "소분류 (예적금/투자/(내계좌이체/인출) | 세금/공적연금/사회보험/가구간 이전지출/비영리단체 이전/부채상환 | (식비/외식비)/(주거/용품비)/교육비/의료비/교통비/통신비/(여가/문화)/(의류/신발)/용돈/보장성보험/기타기출/미파악지출)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Contents = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "내용 (하계할인마트/신한카드/남원추어탕/교보문고)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Creditor = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "금융사/채권인", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProductName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "상품명", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PaymentMethod = table.Column<string>(type: "varchar(255)", nullable: false, comment: "결제수단 (자산 상품명/현금)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<long>(type: "bigint(255)", nullable: false, comment: "금액"),
                    MonetaryUnit = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false, comment: "화폐 단위 (KRW, USD, ETC)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InterestRate = table.Column<float>(type: "float", nullable: false, comment: "이자율"),
                    WithdrawalDate = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: false, comment: "출금일 (매월 며칠)"),
                    MaturityDate = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: false, comment: "만기일 (만기일 없으면 9999-12-31 되도록 Checkbox 할 것)"),
                    Created = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: false, comment: "생성일"),
                    Updated = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: false, comment: "업데이트일"),
                    Note = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, comment: "비고", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Deleted = table.Column<bool>(type: "tinyint(1)", nullable: false, comment: "삭제여부"),
                    Reserved0 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved1 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved2 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved3 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved4 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedExpenditure", x => x.Id);
                    table.ForeignKey(
                        name: "FixedExpenditure_FK",
                        columns: x => new { x.PaymentMethod, x.AccountEmail },
                        principalTable: "Asset",
                        principalColumns: new[] { "ProductName", "AccountEmail" },
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "MyLaboratory Site 고정지출")
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "FixedIncome",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int(11)", nullable: false, comment: "PK")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AccountEmail = table.Column<string>(type: "varchar(255)", nullable: false, comment: "계정 이메일 (ID)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MainClass = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "대분류 (정기수입/비정기수입)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubClass = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "소분류 (근로수입/사업수입/연금수입/금융소득/임대수입/기타수입)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Contents = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "내용 (회사명/사업명)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Debtor = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "금융사/채무인", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProductName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "상품명", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DepositMethod = table.Column<string>(type: "varchar(255)", nullable: false, comment: "결제수단 (자산 상품명/현금)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<long>(type: "bigint(255)", nullable: false, comment: "금액"),
                    MonetaryUnit = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false, comment: "화폐 단위 (KRW, USD, ETC)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InterestRate = table.Column<float>(type: "float", nullable: false, comment: "이자율"),
                    DepositDate = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: false, comment: "입금일 (매월 며칠)"),
                    MaturityDate = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: false, comment: "만기일 (만기일 없으면 9999-12-31 되도록 Checkbox 할 것)"),
                    Created = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: false, comment: "생성일"),
                    Updated = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: false, comment: "업데이트일"),
                    Note = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, comment: "비고", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Deleted = table.Column<bool>(type: "tinyint(1)", nullable: false, comment: "삭제여부"),
                    Reserved0 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved1 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved2 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved3 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved4 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedIncome", x => x.Id);
                    table.ForeignKey(
                        name: "FixedIncome_FK",
                        columns: x => new { x.DepositMethod, x.AccountEmail },
                        principalTable: "Asset",
                        principalColumns: new[] { "ProductName", "AccountEmail" },
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "MyLaboratory Site 고정수입")
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "Income",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int(11)", nullable: false, comment: "PK")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AccountEmail = table.Column<string>(type: "varchar(255)", nullable: false, comment: "계정 이메일 (ID)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MainClass = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "대분류 (정기수입/비정기수입)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubClass = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "소분류 (근로수입/사업수입/연금수입/금융소득/임대수입/기타수입)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Contents = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, comment: "내용 (회사명/사업명)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<long>(type: "bigint(255)", nullable: false, comment: "금액"),
                    MonetaryUnit = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false, comment: "화폐 단위 (KRW, USD, ETC)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DepositAccount = table.Column<string>(type: "varchar(255)", nullable: false, comment: "입금계좌 (자산 상품명/현금)", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Created = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: false, comment: "생성일"),
                    Updated = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: false, comment: "업데이트일"),
                    Note = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, comment: "비고", collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved0 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved1 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved2 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved3 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reserved4 = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Income", x => x.Id);
                    table.ForeignKey(
                        name: "Income_FK",
                        columns: x => new { x.DepositAccount, x.AccountEmail },
                        principalTable: "Asset",
                        principalColumns: new[] { "ProductName", "AccountEmail" },
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "MyLaboratory Site 수입")
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateIndex(
                name: "Asset_FK",
                table: "Asset",
                column: "AccountEmail");

            migrationBuilder.CreateIndex(
                name: "Expenditure_FK",
                table: "Expenditure",
                columns: new[] { "PaymentMethod", "AccountEmail" });

            migrationBuilder.CreateIndex(
                name: "FixedExpenditure_FK",
                table: "FixedExpenditure",
                columns: new[] { "PaymentMethod", "AccountEmail" });

            migrationBuilder.CreateIndex(
                name: "FixedIncome_FK",
                table: "FixedIncome",
                columns: new[] { "DepositMethod", "AccountEmail" });

            migrationBuilder.CreateIndex(
                name: "Income_FK",
                table: "Income",
                columns: new[] { "DepositAccount", "AccountEmail" });

            migrationBuilder.CreateIndex(
                name: "SubCategories_FK",
                table: "SubCategory",
                column: "CategoryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Expenditure");

            migrationBuilder.DropTable(
                name: "FixedExpenditure");

            migrationBuilder.DropTable(
                name: "FixedIncome");

            migrationBuilder.DropTable(
                name: "Income");

            migrationBuilder.DropTable(
                name: "SubCategory");

            migrationBuilder.DropTable(
                name: "Asset");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "Account");
        }
    }
}
