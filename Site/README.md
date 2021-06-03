선행지식: docker, ASP.NET Core EF Core data migration

cmd나 terminal 창에서 docker-compose up -d 하면 알아서 다 설치됨. (Mariadb 10.2.38 사용) (docker 및 docker compose AWS에서 설치 방법은 docker-compose.yml 주석에 있음)

디버깅은 IIS Express로 잡고 로컬에 Mariadb 10.2.38 깔아서 디버깅할 것 (MariaDB 10.2.38 설치 파일은 Utility 폴더에 있음)

배포나 디버깅 결정 DB에 따라서만 달리지므로 은 appsettings.json에서 하면 됨

DB 자동 배포 및 설정, INSERT 등은 DOCKER-COMPOSE.yml과 ASP.NET.CORE EF Migration 문서 확인할 것.

[VS 2019 - Tools - NuGet Package Manager - Package Manager Console - run command "Add-Migration DBInit" - once done, run another command "Update-Database".]