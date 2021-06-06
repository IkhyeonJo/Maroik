선행지식: docker, ASP.NET Core EF Core data migration

cmd나 terminal 창에서 docker-compose up -d 하면 알아서 다 설치됨. (Mariadb 사용) (docker 및 docker compose AWS에서 설치 방법은 docker-compose.yml 주석에 있음)

디버깅은 IIS Express로 잡고 로컬에 Mariadb 깔아서 디버깅할 것 

배포나 디버깅 결정 DB에 따라서만 달리지므로 은 appsettings.json에서 하면 됨

자세한 DB 자동 배포 및 설정, INSERT 등은 DOCKER-COMPOSE.yml과 ASP.NET.CORE EF Migration 문서 확인할 것.





아래 방법대로 설정하여 Init.sql과 asp.net core 5.0(Migration Folder에 있는 내용) 다 맞추고 설정할 것



https://www.youtube.com/watch?v=N10QW_AIOnI
MySql Table의 모든 종류를 자동으로 C# 모든 Model로 변환하는 방법 (Init.sql을 MariaDB에 넣고... 이 방법을 쓰면 알아서 DB 테이블들이 다 C# 모델 클래스로 다 변환됨...)
Package Manager Console 열고
cd .\MyLaboratory.Site
dotnet ef dbcontext scaffold "server=localhost;port=3306;userid=root;password=root;database=ikhyeondb;" "Pomelo.EntityFrameworkCore.MySql" -o ./Models -f
이러면 DB 테이블이 알아서 다 C# 모델로 변형된다.

https://docs.microsoft.com/en-us/aspnet/core/data/ef-mvc/migrations?view=aspnetcore-5.0#pmc
Data Migration 방법: (테이블 자동 생성) (AppDbContext에 따름...) (C# Migration 내용으로 생성)
dotnet tool install --global dotnet-ef
dotnet tool update --global dotnet-ef
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet ef migrations add InitialCreate

(Migration 폴더 내용 다 지울 것)
(dotnet ef migrations add InitialCreate) 명령 실행 시, 카피할 DB string 맞추고 할 것... 나같은 경우는 로컬 MariaDB...


repository pattern & UnitWork pattern
https://enlabsoftware.com/development/how-to-implement-repository-unit-of-work-design-patterns-in-dot-net-core-practical-examples-part-one.html

https://www.programmingwithwolfgang.com/repository-pattern-net-core/
github참조.

Login

https://codinginfinite.com/creating-admin-panel-asp-net-core-mvc-tutorial/
https://developer.okta.com/blog/2019/08/02/build-a-simple-crud-app-with-aspnetcore-mysql-and-twilio
https://www.google.ca/search?q=asp.net+core+mvc+mysql+example&ei=8ra5YJnVB7HTmAX66KOIBg&oq=asp.net+core+mvc+mysql+example&gs_lcp=Cgdnd3Mtd2l6EAM6BwgAEEcQsAM6BggAEA0QHlD4FViUH2D0JGgBcAJ4AIABmAGIAYoHkgEDMS43mAEAoAEBqgEHZ3dzLXdpesgBCMABAQ&sclient=gws-wiz&ved=0ahUKEwiZlqC-nP3wAhWxKaYKHXr0CGEQ4dUDCA4&uact=5