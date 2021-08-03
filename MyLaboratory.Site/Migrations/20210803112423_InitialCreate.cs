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

            migrationBuilder.CreateIndex(
                name: "SubCategories_FK",
                table: "SubCategory",
                column: "CategoryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Account");

            migrationBuilder.DropTable(
                name: "SubCategory");

            migrationBuilder.DropTable(
                name: "Category");
        }
    }
}
