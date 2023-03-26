#https://docs.microsoft.com/en-us/visualstudio/containers/docker-compose-properties?view=vs-2019

### ------------------------------------------README !IMPORTANT---------------------------------------------------------
#Before start this project, Set Server settings in appsettings.json (for registration & password recovery & Login) [Especially DomainName]
### ------------------------------------------README !IMPORTANT---------------------------------------------------------

##AWS step
#step 1: Create AWS instance (in my case, AWS Linux)
#step 2: open PORT
#80, 443, 5000, 5001 (DevelopmentMode)
#80, 443, 5001 (Deploy)

#AWS Command (docker and docker-compose install)
#go to docker-compose.yml file and execute below this command

#-----------------------------------------------------------------------------------
#sudo yum update -y && sudo amazon-linux-extras install docker -y && sudo service docker start && sudo curl -L https://github.com/docker/compose/releases/download/1.21.0/docker-compose-`uname -s`-`uname -m` | sudo tee /usr/local/bin/docker-compose > /dev/null && sudo chmod +x /usr/local/bin/docker-compose && sudo ln -s /usr/local/bin/docker-compose /usr/bin/docker-compose && sudo docker-compose up -d
#-----------------------------------------------------------------------------------

###stop docker-compose
#docker-compose down

###check docker images
#docker images

###remove docker images
#docker rmi [Image ID]

#when you should go debugging mode , change DB location (appsettings.json, DatabaseConfig.cs) and install (mariadb 10.6.5) and use IIS Express debugging mode

#----------------------------------------------------------------------------------------------------
### Tip: To see [CONTAINER ID], execute below this command
#docker ps

###Access docker container
#docker exec -it <containerID> /bin/bash/

###In order to copy a file from a container to the host, you can use the command
#docker cp <containerId>:/file/path/within/container /host/path/target

###Backup and restore a mysql database from a running Docker mysql container

## Backup
#docker exec [CONTAINER ID] /usr/bin/mysqldump -u root --password=***REMOVED*** [DATABASE] > backup.sql

## Restore
#cat backup.sql | docker exec -i [CONTAINER ID] /usr/bin/mysql -u root --password=***REMOVED*** [DATABASE]
#----------------------------------------------------------------------------------------------------

#How To Use ASPNETCORE Entity Framework Core
https://www.entityframeworktutorial.net/efcore/querying-in-ef-core.aspx
https://www.learnentityframeworkcore.com/raw-sql

https://www.learnentityframeworkcore5.com/lazy-loading-in-ef-core // Lazy Loading Proxy
https://www.youtube.com/c/ThumbIKR/videos // useful videos for coding
https://softwareengineering.stackexchange.com/questions/392512/async-why-have-an-async-for-a-login // async
https://code-maze.com/asynchronous-programming-with-async-and-await-in-asp-net-core/ // async
OutputModel: Frontend에 쓰는 모델
InputModel: Backend에 쓰는 모델

ModelState.IsValid (받는 모델의 Property들의 Attribute를 조사해 전달 받은 값이 유효한 값인지 판단.)
ModelState.Remove (ModelState.IsValid에서 Property 조사할 때, 조사 내용에서 제외함.)
https://www.freecodecamp.org/news/how-to-use-html-to-open-link-in-new-tab/ // tabnapping
#--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
https://whitekeyboard.tistory.com/630
(디버깅 & Data Migration 용) MariaDB Windows 로컬에 설치 시, 

환경 MariaDB 10.6.5  ( x64, windows )

Windows에서 mysqldum를 하고 linux에 DB를 넣으면 테이블명이 소문자로 들어가는 문제를 종종 본 적이 있을 것이다.

윈도우 자체의 문제인 줄 알았는데 MariaDB의 문제였다. 왜 이런식으로 defalut값을 해놓은지는 이해는 가지 않지만.

linux에 보통 많이 서비스 제품을 올리기 때문에 linux에 맞추는 설정을 정리하고자 한다.

lower_case_table_names = 0  :  테이블 생성 및 조회 시 대·소문자 구분한다.
lower_case_table_names = 1  :  입력 값이 대·소문자든 소문자로 인식 소문자 인식 파일 생성
lower_case_table_names = 2  :  윈도우에서 대·소문자를 구분해서 테이블생성

기본값
리눅스, 유닉스 : 0
윈도우 : 1
맥키토시 : 2

C:\Program Files\MariaDB 10.6\data\my.ini에서
[mysqld] 항목의 [Port] 아래에 lower_case_table_names = 2로 넣어주고

3) 작업관리자 - 서비스를 열어 MariaDB를 재시작 해준다. 

(MariaDB3306은 install 당시 이름명을 저렇게 넣은 것, default값은 아마 "Mysql" 이렇게 되어 있을 것이다.)

4) 이렇게 설정이되면 mysqldump 할 때 대소문자 구분하여 백업 받을 수 있고, 백업 파일을 만들 수 있게 된다.

# [Code First Migration]

먼저 DB 깨끗이 다 지우고 [Package Manager Console] 창 띄우고 Default project를 [1.Common\Maroik.Common.DataAccess]로 맞추고 

[cd .\Maroik.Common.DataAccess] 명령어를 통해 Maroik.Common.DataAccess로 이동

밑 코드 실행

dotnet tool install --global dotnet-ef

밑 코드를 ApplicationDbContext.cs file에 복사 & 붙여넣기 할 것

protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
            => optionsBuilder.UseMySql("server=localhost;port=3306;userid=root;password=***REMOVED***;database=Maroik", Microsoft.EntityFrameworkCore.ServerVersion.Parse("10.9.2-mariadb"));


밑 코드 실행

dotnet ef migrations add InitialCreate

Migration 폴더 만들어진 거 확인하고 밑 코드 실행

dotnet ef database update

그런 다음 DB 확인하면 테이블들이 형성되어 있는 것을 볼 수 있음

# Docker DB 설정 자동화 작업 [DB First]

먼저 DB 깨끗이 다 지우고 [Maroik.Common.DataAccess]에 있는 새롭게 짜여진 DB Script (_MariaDBSql_Init_Script)로 다 복원 후 [로컬 DB에 할 것!!] [그 appsettings.json도 로컬로 맞추고]

[Package Manager Console] 창 띄우고 Default project를 [1.Common\Maroik.Common.DataAccess]로 맞추고

[cd .\Maroik.Common.DataAccess] 명령어를 통해 Maroik.Common.DataAccess로 이동

dotnet ef dbcontext scaffold "server=localhost;port=3306;userid=root;password=***REMOVED***;database=maroik;" "Pomelo.EntityFrameworkCore.MySql" -o ./Models11 -f

이 명령어를 통해 기존 모델, ApplicationDbContext것과 새롭게 생성된 모델, ApplicationDbContext 것을 비교하여 [Maroik.Common.DataAccess]에 있는 모델, ApplicationDbContext 다시 새롭게 덮어씌움

안 되면 
https://docs.microsoft.com/en-us/aspnet/core/data/ef-mvc/migrations?view=aspnetcore-5.0#pmc
(테이블 자동 생성) (AppDbContext에 따름...)
dotnet tool install --global dotnet-ef
dotnet tool update --global dotnet-ef
dotnet add package Microsoft.EntityFrameworkCore.Design

(dotnet ef migrations add InitialCreate) 명령 실행 시, 카피할 DB string 맞추고 할 것... 나같은 경우는 로컬 MariaDB...

이런 거 해서 어떻게든 되도록 할 것
#--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------



#--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
선행지식: docker, ASP.NET Core EF Core data migration

cmd나 terminal 창에서 docker-compose up -d 하면 알아서 다 설치됨. (Mariadb 사용) (docker 및 docker compose AWS에서 설치 방법은 docker-compose.yml 주석에 있음)

디버깅은 IIS Express로 잡고 로컬에 Mariadb 깔아서 디버깅할 것 

배포나 디버깅 결정 DB에 따라서만 달리지므로 은 appsettings.json에서 하면 됨

자세한 DB 자동 배포 및 설정, INSERT 등은 DOCKER-COMPOSE.yml과 ASP.NET.CORE EF Migration 문서 확인할 것.
#--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------



아래 방법대로 설정하여 Init.sql과 asp.net core 5.0(Migration Folder에 있는 내용) 다 맞추고 설정할 것



https://www.youtube.com/watch?v=N10QW_AIOnI
MySql Table의 모든 종류를 자동으로 C# 모든 Model로 변환하는 방법 (Init.sql을 MariaDB에 넣고... 이 방법을 쓰면 알아서 DB 테이블들이 다 C# 모델 클래스로 다 변환됨...)
Package Manager Console 열고
cd .\Maroik.WebSite
dotnet ef dbcontext scaffold "server=localhost;port=3306;userid=root;password=***REMOVED***;database=maroik;" "Pomelo.EntityFrameworkCore.MySql" -o ./Models11 -f
이러면 DB 테이블이 알아서 다 C# 모델로 변형된다.

https://docs.microsoft.com/en-us/aspnet/core/data/ef-mvc/migrations?view=aspnetcore-5.0#pmc
(테이블 자동 생성) (AppDbContext에 따름...) (C# Migration 내용으로 생성)
dotnet tool install --global dotnet-ef
dotnet tool update --global dotnet-ef
dotnet add package Microsoft.EntityFrameworkCore.Design

repository pattern & UnitWork pattern
https://enlabsoftware.com/development/how-to-implement-repository-unit-of-work-design-patterns-in-dot-net-core-practical-examples-part-one.html

https://www.programmingwithwolfgang.com/repository-pattern-net-core/
github참조.

Login

https://codinginfinite.com/creating-admin-panel-asp-net-core-mvc-tutorial/
https://developer.okta.com/blog/2019/08/02/build-a-simple-crud-app-with-aspnetcore-mysql-and-twilio

https://www.youtube.com/watch?v=1u8-ufTxIcQ
https://mobiletonster.com/area/code

https://jhyeok.com/aspnetcore-claim/
https://www.youtube.com/watch?v=BWa7Mu-oMHk&t=3202s
Cookies, when used correctly, are a safe option. The key is to 
1. use https,
2. make sure the cookie is bound to a specific domain
3. set it to httponly so it can't be read on the client side in javascript
4.  control the security level to be strict or same site, not lax. 

Using these and other techniques such as CSRF tokens to prevent cross site scripting attacks will further improve the security of your site while using cookies.
Alternatives to using cookies include using JWT tokens added to the Authorization header, but they are really not any safer thank a cookie. Like a cookie, the JWT token is passed in the header, but the JWT token is usually readable by the client side, whereas a cookie can be encrypted so that it is not readable by the client side or browser (httponly). 
If you need store JWT tokens in the browser, don't store them in localstorage (which is a common practice, but not a good idea). Store tokens in sessionstorage or in memory (like in a react state object, or Angular state object) or .... in a cookie (lol).
https://wonpaper.tistory.com/239
PBKDF2 


Page Header나 MainSideBar, MainTopBar 등을 컨트롤러, 액션에 따라 다이나믹하게 바꿀필요가 있을 때 및 코드 참조... DB랑도 연동 생각하고!!
System.Net.WebUtility.UrlEncode(returnUrl)



AntiForgeryToken 을 이용하여 폼 전송 경로의 유효성을 체크하자
https://m.blog.naver.com/PostView.naver?isHttpsRedirect=true&blogId=happywow&logNo=80134498163


Run ASP.NET Core 3.1 Apps in Docker with HTTPS.
https://www.youtube.com/watch?v=lcaDDxJv260

.NET Core 3.1 MVC REST API - Full Course
https://www.youtube.com/watch?v=fmvcAzHpsk8



#-------------------------------------------------------------------------------
#인증서 생성 (powerShell창) C:\Users\HOME\.aspnet\https에 있음
dotnet dev-certs https -ep $env:USERPROFILE/.aspnet/https/haha.pfx -p Passw0rd

#인증서 이 프로젝트에 바로 놓을 것

도커 컴포즈에서
- ASPNETCORE_Kestrel__Certificates__Default__Password=Passw0rd 와 인증서 패스워드가 같아야함
#-------------------------------------------------------------------------------

### MYSQL DB 백업하기 (procedure, function, trigger 포함) (Windows에서는 관리자 권한으로 C:\Program Files\MariaDB 10.6\bin cmd 가서 할 것)
mysqldump --routines --triggers --events -u [사용자 계정] -p [원본 데이터베이스명] > [생성할 백업 DB명].sql
또는
DBeaver에서 DB 클릭하고 Dump 할 것

#백업 시... 트리거 부분 조심해서 볼 것;;


### MYSQL DB 복원하기 (Windows에서는 관리자 권한으로 C:\Program Files\MariaDB 10.6\bin cmd 가서 할 것)
mysql -u [사용자 계정] -p [복원할 DB] < [백업된 DB].sql
또는
DBeaver에서 복원할 것

#ASP.net Core - Entity Framework Core - Stored Procedure Call / EF Core에서 프로시저 호출하기

https://glorymind.tistory.com/m/entry/ASPnet-Core-Entity-Framework-Core-Stored-Procedure-Call-EF-Core%EC%97%90%EC%84%9C-%ED%94%84%EB%A1%9C%EC%8B%9C%EC%A0%80-%ED%98%B8%EC%B6%9C%ED%95%98%EA%B8%B0?category=893375

Install-Package StoredProcedureEFCore

using 문 넣어주시고

List<Model> rows = null;
 
ctx.LoadStoredProc("dbo.ListAll")
   .AddParam("limit", 300L)
   .AddParam("limitOut", out IOutParam<long> limitOut)
   .Exec(r => rows = r.ToList<Model>());


#MariaDB Stored Program - Stored Procedure (MySQL)
https://reddb.tistory.com/126

DELIMITER $$
CREATE PROCEDURE LockAccount(IN Email VARCHAR(255))
BEGIN
UPDATE maroik.Account
SET IsLocked=1
WHERE Email=Email;
END $$
DELIMITER







https://stackoverflow.com/questions/53985013/how-do-i-get-all-dbcontext-classes-and-all-dbset-under-each-dbcontext


https://stackoverflow.com/questions/3169559/mysql-categories-and-subcategories-table-structure
CREATE TABLE test.Categories (
	Id INT auto_increment NOT null primary key,
	Main varchar(1000) NULL,
	Sub varchar(1000) NULL
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8
COLLATE=utf8_general_ci;

CREATE TABLE test.SubCategories (
	Id INT auto_increment NOT null primary key,
	CategoryId int,
	Name varchar(1000),
	FOREIGN KEY (CategoryId) REFERENCES test.Categories (Id)
)
ENGINE=InnoDB
DEFAULT CHARSET=utf8
COLLATE=utf8_general_ci;


#_MainLeftSideBar.cshtml DB 구조
#-----------------------------------------------------------------------------------------------------------------------------------------------
Category
Id: PK(not null, Auto_Increment, Int) # 그냥 식별용
Name: String (not null) # 이름
DisplayName: String (not null) # 보이는 이름
Icon: String (not null) # 보이는 아이콘
Controller: String (not null) # Active or InActive 결정
Action: String # (# or 진짜 Action) [이것에 따라 SubCategory 여부 및 화살표 없어짐 ]
Role: String # Admin or User (역할에 따른 보여주기)
Order: int # 출력 순서

CREATE TABLE `Category` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Name` varchar(256) NOT NULL,
  `DisplayName` varchar(256) NOT NULL,
  `IconPath` varchar(256) NOT NULL,
  `Controller` varchar(256) NOT NULL,
  `Action` varchar(256) DEFAULT NULL,
  `Role` varchar(256) NOT NULL DEFAULT 'Admin',
  `Order` int(11) NOT NULL,
  PRIMARY KEY (`Id`),
  CONSTRAINT `Categories_check` CHECK (`Role` = 'Admin' or `Role` = 'User')
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

SubCategory
Id: PK(not null, Auto_Increment, Int) # 그냥 식별용
CategoryId: FK(int) (Categories의 Id 참조) # 외래키
Name: String (not null) # 이름
DisplayName: String (not null) # 보이는 이름
Icon: String (not null) # 보이는 아이콘
Action: String # (이것이 View랑 일치하면 Active가 됨)
Role: String # Admin or User (역할에 따른 보여주기)
Order: int # 출력 순서

CREATE TABLE `SubCategory` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `CategoryId` int(11) NOT NULL,
  `Name` varchar(256) NOT NULL,
  `DisplayName` varchar(256) NOT NULL,
  `IconPath` varchar(256) NOT NULL,
  `Action` varchar(256) NOT NULL,
  `Role` varchar(256) NOT NULL DEFAULT 'Admin',
  `Order` int(11) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `SubCategories_FK` (`CategoryId`),
  CONSTRAINT `SubCategories_FK` FOREIGN KEY (`CategoryId`) REFERENCES `Category` (`Id`),
  CONSTRAINT `SubCategories_check` CHECK (`Role` = 'Admin' or `Role` = 'User')
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
#-----------------------------------------------------------------------------------------------------------------------------------------------



#_MainLeftSideBar.cshtml 알고리즘
#-----------------------------------------------------------------------------------------------------------------------------------------------
# 로그인 후 DB 초기 (Cache)
A-0: 카테고리 Role이 현재 사용자 Role과 같은 거 다 꺼내기
A-1: 꺼낸 카테고리를 Order에 따라 다 정렬

A-2: 서브 카테고리 Role이 현재 사용자 Role과 같은 거 다 꺼내기


# View Load 시 Category 생성 Algorithm
B-0: 정리한 카테고리 처음부터 마지막까지 하나씩 꺼냄 (다 꺼내면 종료)
B-1: 꺼낸 카테고리의 Controller 값이 현 View의 Controller와 같다면 Active
      Active 된 카테고리에 Action 값이 없으면 TreeOpen 그리고 B-3로 넘어감 // SubCategory 있음
      Active 된 카테고리에 Action 값이 있으면 그냥 HyperLink 만들고 다음 카테고리로 넘어감 // SubCategory 없음

B-2: 꺼낸 카테고리의 Controller 값이 현 View의 Controller와 같지 않다면 InActive
      InActive 된 카테고리에 Action 값이 없으면 TreeClose 그리고 B-3로 넘어감 // SubCategory 있음
      InActive 된 카테고리에 Action 값이 있으면 그냥 HyperLink 만들고 다음 카테고리로 넘어감 // SubCategory 없음

# View Load 시 SubCategory 생성 Algorithm
B-3: 현 카테고리의 Id와 일치하는 모든 SubCategory를 그 SubCategory 속성인 CategoryId를 통해 다 모음. 모은 SubCategory들을 Order에 맞게 배치.
B-4: SubCategory들을 처음부터 마지막까지 하나씩 거냄
B-5: 꺼낸 SubCategory의 Action 값이 현 View의 Action과 같다면 Active
      hyperLink는 Category의 Controller와 SubCategory의 Action을 통해 생성.
B-6: 꺼낸 SubCategory의 Action 값이 현 View의 Action과 같지 않다면 InActive
      hyperLink는 Category의 Controller와 SubCategory의 Action을 통해 생성.
B-7: B-0로 돌아감
#-----------------------------------------------------------------------------------------------------------------------------------------------




https://holjjack.tistory.com/105?category=831597 // [Core 3.1] ASP.NET Core ActionFilter를 이용한 Session 체크


https://odetocode.com/blogs/scott/archive/2015/10/06/authorization-policies-and-middleware-in-asp-net-5.aspx // Authorization Policies and Middleware in ASP.NET 5

https://stackoverflow.com/questions/36775942/how-do-i-serve-static-files-only-to-authorized-users //wwwroot 로그인 전 파일 보호



https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/filters?view=aspnetcore-5.0 // aspnetcore 5.0 Filter
OnActionExecuting - 컨트롤러 액션이 실행되기 전에 호출됩니다.
OnActionExecuted - 컨트롤러 액션이 실행된 후에 호출됩니다.
OnResultExecuting - 컨트롤러의 액션결과가 실행되기 전에 호출됩니다.
OnResultExecuted - 컨트롤러의 액션결과가 실행된 후에 호출됩니다.
      var controllerName = routeData.Values["controller"];
      var actionName = routeData.Values["action"];
      var message = String.Format("{0} controller:{1} action:{2}", methodName, controllerName, actionName);
      Debug.WriteLine(message, "Action Filter Log");












# 세션 관리법
var tempAccount = accountRepository.GetAccount(loginInputViewModel.Email);


HttpContext.Session.Set("AccountSession", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(tempAccount))); // Set Session

var isExist = HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);
var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session









file:///C:/Github/Maroik/Maroik.WebSite/wwwroot/pages/UI/buttons.html
Category Menu View 생성 시 Button 볼 것

file:///C:/Github/Maroik/Maroik.WebSite/wwwroot/pages/UI/modals.html
Category Menu View 생성 시 Modal 볼 것

file:///C:/Github/Maroik/Maroik.WebSite/wwwroot/pages/forms/general.html
File Upload

file:///C:/Github/Maroik/Maroik.WebSite/wwwroot/pages/forms/advanced.html
Date picker
Dropzone.js jQuery File Upload like look

file:///C:/Github/Maroik/Maroik.WebSite/wwwroot/pages/forms/editors.html
Text Editors

file:///C:/Github/Maroik/Maroik.WebSite/wwwroot/pages/tables/simple.html
Category Menu View 생성 시 Ordered Lists 볼 것 Expandable Table Tree [카테고리 표시에 할 것]

file:///C:/Github/Maroik/Maroik.WebSite/wwwroot/pages/tables/data.html
DataTables

file:///C:/Github/Maroik/Maroik.WebSite/wwwroot/pages/tables/jsgrid.html
jsGrid

file:///C:/Github/Maroik/Maroik.WebSite/wwwroot/pages/mailbox/compose.html
메일 보내기인데 fileUpload로도 쓸 수 있을 듯

file:///C:/Github/Maroik/Maroik.WebSite/wwwroot/pages/examples/profile.html#
Pages/Profile -- 프로파일 할 때

file:///C:/Github/Maroik/Maroik.WebSite/wwwroot/pages/examples/projects.html#
그 Category View할 때 버튼 참조하면 좋을 껄?

file:///C:/Github/Maroik/Maroik.WebSite/wwwroot/pages/examples/contact-us.html
Contact us


AdminLTE 그 ... 로그인 화면 엄청 많이 고려할 것!! 유용한 것 많음

file:///C:/Github/Maroik/Maroik.WebSite/wwwroot/pages/examples/404.html
404 not found
file:///C:/Github/Maroik/Maroik.WebSite/wwwroot/pages/examples/500.html
500 Error Page


https://www.youtube.com/watch?v=H0jeLTMLD5c&list=PL2UwEEhB4YafVIdKCBRjTyryET2EHgryp&index=8
Build Completed E-Commerce with ASP.NET Core MVC - Part 8 - Category Management in Admin

https://www.youtube.com/watch?v=lEjzIlliYJA&list=PL2UwEEhB4YafVIdKCBRjTyryET2EHgryp&index=12
Build Completed E-Commerce with ASP.NET Core MVC - Part 12 - Banner Sliders Management in Admin [Image 업로드 같은데... 좋은 코드인듯]

http://js-grid.com/ // CRUD Grid


https://aspnet-core-grid.azurewebsites.net/ //Grid

https://www.youtube.com/watch?v=OSveDDVdtOM //jQuery datatables export to pdf

https://exceptionnotfound.net/using-anti-forgery-tokens-in-asp-net-core-razor-pages/ // antiforgerykey in ajax

Bind(Include="ID,Title,ReleaseDate,Genre,Price")]Movie movie

Post 파라미터 매개변수 옆에..

Bind 어트리뷰트는 해커들이 오버포스팅(over-posting) 하지 못하도록 지켜주는 중요한 보안 메커니즘입니다. 
(역주: User라는 모델에 IsAdmin 속성이 있다고 할때, 우리가 비록 입력폼에 이 속성을 노출하지 않았다 하더라도 해커가 이 속성을 유추하여 포스팅 데이터에 추가하는 것을 오버포스팅이라 한다.)

웹개발 짜증유발자! CORS가 뭔가요? // POSTMAN 같은 건 다 되는데 Ajax 실제 할 때는 안 됨.

https://stackoverflow.com/questions/21583278/getting-all-controllers-and-actions-names-in-c-sharp/30969888

https://docs.microsoft.com/en-us/aspnet/core/tutorials/first-mvc-app/validation?view=aspnetcore-5.0 // Validation Model value



https://getbootstrap.com/docs/3.4/javascript/
$('#myModal').modal()                      // initialized with defaults
$('#myModal').modal({ keyboard: false })   // initialized with no keyboard
$('#myModal').modal('show')                // initializes and invokes show immediately
data-toggle="modal" data-target="#createMenuDialogModal" data-backdrop="static" data-keyboard="false"
https://stackoverflow.com/questions/13183630/how-to-open-a-bootstrap-modal-window-using-jquery


https://craftpip.github.io/jquery-confirm/
wwwroot 아이콘 오른쪽 클릭 후 Add -> Client-Side Library -> 원하는 라이브러리 검색 후 다운로드 받으면 libman.json과 함께 해당 Resource가 자동으로 다운받아집니다.


https://www.youtube.com/watch?v=Hy9G30nncMM // Localization and Globalization in ASP.NET Core MVC (.NET 5)

https://www.fitrianingrum.me/post/account-confirmation-aspnet-core-sendgrid/ // sendgrid

https://www.youtube.com/watch?v=4I1rYUbjAhw // sendgrid



https://www.youtube.com/watch?v=UQ2GdLzMmbE // User Registration With Email and Email Confirmation or Validation | ASP.NET Core Web API

https://www.youtube.com/watch?v=C4O8vqg295o // Mail Send

https://jinseongsoft.tistory.com/294 // Setting GMail for MailKit

http://www.binaryintellect.net/articles/e30d07c6-6f57-43e7-a2ce-6d2d67ebf403.aspx // Send Emails In ASP.NET Core In 5 Easy Steps

https://m.blog.naver.com/PostView.naver?isHttpsRedirect=true&blogId=eternalklaus&logNo=221601195622 // C# dotnet mailkit

using MimeKit;
using MailKit.Net.Smtp;

var message = new MimeMessage();
message.From.Add(new MailboxAddress("Test Project", "***REMOVED***"));  // ***REMOVED*** // "보안 수준이 낮은 앱 허용: 사용 안함" 설정을 On 시켜줌 (메일 계정에 들어가서)  
message.To.Add(new MailboxAddress("naren", "***REMOVED***")); 
message.Subject = "TEST FOR ASPNETCORE";
message.Body = new TextPart("plain")
{
    Text = "Hello World mail"
};

using (var client = new SmtpClient())
{
    client.Connect("smtp.gmail.com", 587, false); // SSL을 사용하지 않아서 약점 존재
    client.Authenticate("***REMOVED***", "***REMOVED***");

    client.Send(message);
    client.Disconnect(true);
}

https://stackoverflow.com/questions/60634480/user-email-confirmation-optionsjwt // email validation

https://stackoverflow.com/questions/15188829/how-to-use-a-guid-to-verify-user-email-in-asp-net-c-sharp // email validation

https://www.youtube.com/watch?v=yRP6C7fhAuE // ASP NET Core email confirmation

https://jasonwatmore.com/post/2019/10/11/aspnet-core-3-jwt-authentication-tutorial-with-example-api // JWT




remember me logic과

회원가입, 비밀번호 복구 등 이메일 인증과 (GUID를 통한) 다 구현할 것


[Email Confirmation Industry Standard]
{
a typical way of building an email validation thing, with a one time code, would be to use a GUID. In the email you sent, add a link with this unique GUID attached at the end something like :

www.something.com/email-verify/877ebd19-6852-4de9-b228-4d796a4cd7c7

you create a table in your db where you store these generated tokens with the a user id so you know which user it is when they click the link, add a used flag so you know you only accept it once and you're pretty much done.
}






SignUpWithEmailConfirmation 프로젝트 버그 수정 및 테스트 완료한 거 소스코드 전부 분석하여서 이 프로젝트에 넣을 것.

http://ajaxload.info/ // loading gif

RSA // https://github.com/IkhyeonJo/DotnetCore.RSA

메일 전송 시, Login Token Problem (+ 문자가 사라진다...) 해결법: var encodedToken = Uri.EscapeDataString(token); 메일 전송 전, 다 할 것 
https://stackoverflow.com/questions/38781295/asp-net-core-identity-invalid-token-on-confirmation-email

해독법

Uri.UnescapeDataString()



###파일 업로드
#파일 업로드는
#로그인 아닐 시: [anonymous] folder만 이용 (wwwroot)
#User 로그인일 시: [user] folder만 이용 (wwwroot)
#Admin 로그인일 시: [admin] folder만 이용 (wwwroot)
#이므로 이외의 폴더에서 생성하면 Upload만 되고 접근 불가능해진다. (Anonymous, User, Admin 등은 자기 폴더에만 갇혀있기 때문 (StartUp.cs에서 설정함))
#그 이외의 폴더 만들 때는 README.md 파일 하나 붙여서 폴더가 git push할 때 성공적으로 들어갈 수 있도록 할 것!!
#https://stackoverflow.com/questions/35379309/how-to-upload-files-in-asp-net-core
#https://askforyou.tistory.com/10
#FEST Project File (File Upload 볼 것)
#docker라 파일 권한 Linux 설정 필요 없음




###Docker Compose File Contents


#https://docs.microsoft.com/en-us/visualstudio/containers/docker-compose-properties?view=vs-2019

### ------------------------------------------README !IMPORTANT---------------------------------------------------------
#Before start this project, Set Server settings in appsettings.json (for registration & password recovery)
### ------------------------------------------README !IMPORTANT---------------------------------------------------------

##AWS step
#step 1: Create AWS instance (in my case, AWS Linux)
#step 2: open PORT
#80, 443, 5000, 5001 (DevelopmentMode)
#80, 443, 5001 (Deploy)

#AWS Command (docker and docker-compose install)
#go to docker-compose.yml file and execute below this command

#-----------------------------------------------------------------------------------
#sudo yum update -y && sudo amazon-linux-extras install docker -y && sudo service docker start && sudo curl -L https://github.com/docker/compose/releases/download/1.21.0/docker-compose-`uname -s`-`uname -m` | sudo tee /usr/local/bin/docker-compose > /dev/null && sudo chmod +x /usr/local/bin/docker-compose && sudo ln -s /usr/local/bin/docker-compose /usr/bin/docker-compose && sudo docker-compose up -d
#-----------------------------------------------------------------------------------

#when you should go debugging mode , change DB location (appsettings.json, DatabaseConfig.cs) and install (mariadb 10.6.5) and use IIS Express debugging mode

#----------------------------------------------------------------------------------------------------
### Tip: To see [CONTAINER ID], execute below this command
#docker ps

###Access docker container
#docker exec -it <containerID> /bin/bash/

###In order to copy a file from a container to the host, you can use the command
#docker cp <containerId>:/file/path/within/container /host/path/target

###Backup and restore a mysql database from a running Docker mysql container

## Backup
#docker exec [CONTAINER ID] /usr/bin/mysqldump -u root --password=***REMOVED*** [DATABASE] > backup.sql

## Restore
#cat backup.sql | docker exec -i [CONTAINER ID] /usr/bin/mysql -u root --password=***REMOVED*** [DATABASE]
#----------------------------------------------------------------------------------------------------


다국어 처리 (Multi-Language)
https://www.youtube.com/watch?v=Hy9G30nncMM

Repository Pattern
https://codewithmukesh.com/blog/repository-pattern-in-aspnet-core/

ASP.net Core 강좌
https://le-piee.tistory.com/6

웹사이트 제작 [게시판 종류와 명칭]
https://m.blog.naver.com/PostView.naver?isHttpsRedirect=true&blogId=kjhjun&logNo=220868708278

11.ASP.NET - 자료실 게시판(4) : Biz
https://godffs.tistory.com/m/484?category=126912

# 게시판 글 !!!
ASP.NET Core 5.0 게시판 소스 다운로드 및 실행 데모
https://www.youtube.com/watch?v=7lC6sYRh1fg

# PHP 게시판
https://blog.naver.com/bgpoilkj/220708456525

#Custom Attribute
CustomAttribute 만들어서 POST할 시 그 자동으로 로그인 권한 체크하기...!!

Authorization Filter 할 때 Custom Attribute도 포함 되게 할 것... 없으면 POST 안 되도록

Attribute Name: RequiredHttpPostAccess (Custom)

https://qzqz.tistory.com/246

#SecurityCodeScan.VS2019
https://security-code-scan.github.io/
Error List Hotkey: Ctrl+\, E
(Check Warnings)

#시큐어코딩-쿠키-및-세션관리
https://coyagi.tistory.com/entry/%EC%8B%9C%ED%81%90%EC%96%B4%EC%BD%94%EB%94%A9-%EC%BF%A0%ED%82%A4-%EB%B0%8F-%EC%84%B8%EC%85%98%EA%B4%80%EB%A6%AC


# MariaDB AutoCommit On/Off
[MariaDB] autocommit 설정을 변경하는 방법
 Posted on 2017-03-08 19:00:18

Session 레벨에서 autocommit 설정을 변경할 때는, 아래의 구문을 사용한다.

• autocommit 설정값 확인

show variables like 'autocommit%';

• autocommit 설정 또는 해제

SET AUTOCOMMIT = TRUE;  --> 설정
SET AUTOCOMMIT = FALSE; --> 해제

• 트랜잭션 commit, rollback

COMMIT;
ROLLBACK;

그리고 DB의 기본 autocommit 설정을 변경하기 원한다면, 아래의 설정을 추가해주면 된다.

• /etc/my.cnf.d/server.cnf

[mysqld]
autocommit=0   --> autocommit 해제

# MariaDB Table Column Comment
https://jhnyang.tistory.com/309
https://arjunphp.com/how-to-get-mysql-table-comments/

# MariaDB Table Comment Example
CREATE TABLE `Account` (
  `Email` varchar(255) NOT NULL COMMENT '계정 이메일 (ID)',
  `HashedPassword` longtext NOT NULL COMMENT '계정 암호화 된 비밀번호',
  `Nickname` varchar(255) NOT NULL COMMENT '계정 닉네임',
  `AvatarImagePath` varchar(255) NOT NULL DEFAULT '/upload/Management/Profile/default-avatar.jpg' COMMENT '계정 아바타 이미지 경로',
  `Role` varchar(255) NOT NULL DEFAULT 'User' COMMENT '계정 역할 (Admin 또는 User)',
  `Locked` tinyint(1) NOT NULL COMMENT '계정 잠금',
  `LoginAttempt` int(11) NOT NULL COMMENT '로그인 시도 횟수',
  `EmailConfirmed` tinyint(1) NOT NULL COMMENT '이메일 확인 여부',
  `AgreedServiceTerms` tinyint(1) NOT NULL COMMENT '약관 동의 여부',
  `RegistrationToken` longtext DEFAULT NULL COMMENT '회원가입 인증 토큰',
  `ResetPasswordToken` longtext DEFAULT NULL COMMENT '비밀번호 찾기 인증 토큰',
  `Created` datetime(6) NOT NULL DEFAULT '1900-01-01 00:00:00.000000' COMMENT '계정 생성일',
  `Updated` datetime(6) NOT NULL DEFAULT '1900-01-01 00:00:00.000000' COMMENT '계정 업데이트일',
  `Message` longtext DEFAULT NULL COMMENT '계정 상태 메시지',
  `Deleted` tinyint(1) NOT NULL COMMENT '계정 삭제 여부',
  PRIMARY KEY (`Email`),
  CONSTRAINT `Accounts_check` CHECK (`Role` = 'Admin' or `Role` = 'User')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT '계정';

SHOW FULL COLUMNS FROM Account; // Account 테이블의 컬럼 COMMENT 전부 출력

SELECT table_comment
FROM INFORMATION_SCHEMA.TABLES
WHERE table_name='Account'; // Account 테이블의 COMMENT 출력

#MySQL Trigger의 활용 [그러나 사용 금지 (디버깅 난해)]
https://blog.naver.com/PostView.nhn?blogId=alcmskfl17&logNo=221859839012


Maroik.WebSite 가계부

##사용 그리드:nonfactor grid
#Select Multiple Row by shilt + LeftClick nonfactor grid... rowclick event 랑 색칠 되어 있는 지 확인해서 논리로 구현할 수 있다.

# DateTime.utc 에서 한국 시간 설정
https://k-garage.tistory.com/entry/C-%EC%84%B8%EA%B3%84-%EC%8B%9C%EA%B0%84-%EB%B3%84-%EA%B3%A0%EC%A0%95-%EA%B0%92-%EA%B0%80%EC%A0%B8%EC%98%A4%EA%B8%B0

# Windows & linux Timezone setting
https://dejanstojanovic.net/aspnet/2018/july/differences-in-time-zones-in-net-core-on-windows-and-linux-host-os/

# C# Get Time By Time Zone
https://www.eugenechiang.com/2021/05/25/c-get-time-by-time-zone/

# Jquery DatePicker Korean
https://zetawiki.com/wiki/JQuery_UI_datepicker_%ED%95%9C%EA%B5%AD%EC%96%B4_%EC%A0%81%EC%9A%A9

#[Docker] [Mysql] Docker에서 .sql파일을 마이그레이션 할때 한글 깨지는 현상 막기
https://ink1234.tistory.com/14

##How to Do a Clean Restart of a Docker Instance (https://docs.tibco.com/pub/mash-local/4.3.0/doc/html/docker/GUID-BD850566-5B79-4915-987E-430FC38DAAE4.html)
#If you are using Docker-Machine, make sure your are talking to the right one. Execute the command docker-machine ls to find which one is currently active. It is also recommended to always redo the command:
#eval "$(docker-machine env <docker machine name>)"
#Note: Deleting volumes will wipe out their data. Back up any data that you need before deleting a container.
#Procedure
#Stop the container(s) using the following command:
#1. docker-compose down
#Delete all containers using the following command:
#2. docker rm -f $(docker ps -a -q)
#Delete all volumes using the following command:
#3. docker volume rm $(docker volume ls -q)
#Restart the containers using the following command:
#4. docker-compose up -d

DashBoard Donut Chart

https://www.chartjs.org/
https://stackoverflow.com/questions/20966817/how-to-add-text-inside-the-doughnut-chart-using-chart-js
https://quickchart.io/documentation/chart-js/custom-pie-doughnut-chart-labels/

How to control the width of select tag?
https://stackoverflow.com/questions/10671614/how-to-control-the-width-of-select-tag/10671666#10671666


Entity Framework Core - FromSqlRaw Vs FromSqlInterpolated
https://www.c-sharpcorner.com/blogs/fromsqlraw-vs-fromsqlinterpolated

How to pass a dynamic/variable list of values as SqlParameters with FromSqlRaw in EF Core
https://erikej.github.io/efcore/sqlserver/2020/04/20/use-dynamic-sqlparameters-with-fromsql.html
Executing Dynamically Generated SQL in EF Core
https://www.pmichaels.net/2020/10/10/executing-dynamically-generated-sql-in-ef-core/
How to handle dynamic sql parameters
https://stackoverflow.com/questions/8482028/how-to-handle-dynamic-sql-parameters
C# / SqlServer : Dynamic SqlParameter creation
https://www.techpowerup.com/forums/threads/c-sqlserver-dynamic-sqlparameter-creation.101663/

SQL Operators
https://www.w3schools.com/mysql/mysql_operators.asp

Pie / Donut
https://apexcharts.com/docs/chart-types/pie-donut/

리눅스 크론탭(Linux Crontab) 사용법
https://jdm.kr/blog/2

[Linux crontab 스케쥴러 실행 안 될 때 / 크론 탭 실행 안 됨]
https://bigphu.tistory.com/92

Docker Mysql Server 주기적으로 백업하기(Crontab 활용하기)
https://backkom-blog.tistory.com/entry/Docker-Mysql-Server-%EC%A3%BC%EA%B8%B0%EC%A0%81%EC%9C%BC%EB%A1%9C-%EB%B0%B1%EC%97%85%ED%95%98%EA%B8%B0crontab-%ED%99%9C%EC%9A%A9%ED%95%98%EA%B8%B0?category=294180

Step by Step: Backup MySQL Databases Daily in Docker Container Using Docker Compose
https://thedotenv.com/t/step-by-step-backup-mysql-data-daily-in-docker-container-using-docker-compose/1

[Docker]Mysql.InitSql : Mysql동작시 sql읽고 db초기화하기
https://kamang-it.tistory.com/entry/DockerMysqlInitSql

How to cron job setup in Amazon ec2
https://stackoverflow.com/questions/30806318/how-to-cron-job-setup-in-amazon-ec2

How to create a cron job using Bash automatically without the interactive editor?
https://stackoverflow.com/questions/878600/how-to-create-a-cron-job-using-bash-automatically-without-the-interactive-editor?rq=1

crontab automation problem in amazon linux
https://stackoverflow.com/questions/73045218/crontab-automation-problem-in-amazon-linux
https://stackoverflow.com/questions/39527571/are-shell-scripts-sensitive-to-encoding-and-line-endings

* * * * * docker exec Maroik.MariaDB /usr/bin/mysqldump -u root --password=***REMOVED*** Maroik > backup.sql > /home/ec2-user/SQLBackup.sql

MS SQL Server 2019 및 SSMS 설치 방법 - 윈도우 11
https://blueshare.tistory.com/42

게시판 HTML, CSS 코드
https://blog.naver.com/code-ing/222272856716
https://namu.wiki/w/%EC%9B%8C%EB%93%9C%ED%94%84%EB%A0%88%EC%8A%A4
https://github.com/summernote/summernote
https://github.com/mganss/HtmlSanitizer

[2022/10/19 작성]
도메인은 AWS Route 53을 통하여 www.maroik.com으로 등록하였으며 (1년 갱신비 2만원) (자동 갱신 상태)
인증서는 https://www.sslforfree.com을 통해 발급 후 pfx로 변환하였음

https://kenfavors.com/code/mounting-an-external-drive-on-ubuntu-server/ // Mounting An External Drive On Ubuntu Server
https://askubuntu.com/questions/164926/how-to-make-partitions-mount-at-startup
https://unix.stackexchange.com/questions/349118/how-to-mount-by-uuid-without-using-etc-fstab
https://tragramming.tistory.com/86 // Linux log 명령어

[HTML 코드를 작성하면 <form> 태그를 많이 쓰게 되는데

이 때  <form> 태그 내 <input text>가 1개인 경우

엔터키를 누르면 submit 된다고 하네요 (input  text 2개 이상이면 X)]