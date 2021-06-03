선행지식: docker, ASP.NET Core EF Core data migration

cmd나 terminal 창에서 docker-compose up -d 하면 알아서 다 설치됨. (Mariadb 10.2.38 사용) (docker 및 docker compose AWS에서 설치 방법은 docker-compose.yml 주석에 있음)

디버깅은 IIS Express로 잡고 로컬에 Mariadb 10.2.38 깔아서 디버깅할 것 (MariaDB 10.2.38 설치 파일은 Utility 폴더에 있음)

배포나 디버깅 결정 DB에 따라서만 달리지므로 은 appsettings.json에서 하면 됨

자세한 DB 자동 배포 및 설정, INSERT 등은 DOCKER-COMPOSE.yml과 ASP.NET.CORE EF Migration 문서 확인할 것.



아래 방법대로 설정하여 Init.sql과 asp.net core 5.0 다 맞추고 설정할 것



https://www.youtube.com/watch?v=N10QW_AIOnI
MySql Table의 모든 종류를 자동으로 C# 모든 Model로 변환하는 방법
Package Manager Console 열고
cd .\Site
dotnet ef dbcontext scaffold "server=localhost;port=3306;userid=root;password=root;database=ikhyeondb;" "Pomelo.EntityFrameworkCore.MySql" -o ./Models -f

이러면 DB 테이블이 알아서 다 C# 모델로 변형된다.



https://docs.microsoft.com/en-us/aspnet/core/data/ef-mvc/migrations?view=aspnetcore-5.0#pmc
Data Migration 방법: (테이블 자동 생성) (AppDbContext에 따름...)
dotnet tool install --global dotnet-ef
dotnet tool update --global dotnet-ef
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet ef migrations add InitialCreate
