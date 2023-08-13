# Maroik
Welcome to Maroik!

I'm currently interested in make ASP.NET Core MVC WebApplication!

Docker, AdminLTE, MariaDB, Session Login, RESTful API, HTTPS etc.

# How to run this project
## 0. To completely debug Maroik.Website, you must set of /Maroik/Maroik.WebSite/[appsettings.DockerComposeLocalDebug.json & appsettings.Development]'s Mail Account Configuration.
## 1. Execute [Maroik.sln] -> Open [1.Web] folder -> right-click [docker-compose] -> click "Set as Startup Project" -> Press F5 Button

# How to deploy to Ubuntu Server 22.04.3
## 0. You must set this value [Maroik -> Maroik.WebSite -> appsettings.Production.json -> DomainName (According to Server Domain [If you don't have server domain, just set your server Public IP address])] & Mail Account
## 1. Depending on whether or not the domain exists, you must look at the comments (When you have server domain, uncomment, When you don't have server domain, uncomment) in [Maroik -> Maroik.WebSite -> Program.cs] and set it according to the environment. 
## 2. If you have server domain, you must register your specific domain!! [www.[domain] & [domain]] in DNS Server!
## 3. To Backup your .sql & Upload file frequently, please set [Maroik -> Maroik.Crontab -> DBBackup.sh] [Maroik -> Maroik.Crontab -> UploadFileBackup.sh] file's email address & password
## 4. To Deploy Maroik service, Copy Maroik folder to your ubuntu server [path:(~)]
## 5. Open Ubuntu Server 22.04.3 port 80, 443, 5001
## 6. Execute this command [chmod 777 ~/Maroik/Maroik.DeployOps/Deploy.sh && ~/Maroik/Maroik.DeployOps/Deploy.sh]
## 7. For more detail information, please read 홈서버 구축.pptx which is located /Maroik/Docs/HomeServer/홈서버 구축.pptx
## [Note] If you set number 3, the Maroik.WebSite Upload File Backup and Maroik MariaDB SQL Backup delivered by mail are server backups, you can overwrite them in the \Maroik\Maroik.WebSite\wwwroot\upload and \Maroik\Maroik.MariaDB\SQL_Init_Script folders respectively.

# For more detail information, please see each project's README.md file

☞ 프로젝트명 : 스마트 홈 IoT & 가계부

☞ OS 및 사용 기술 : AWS, Ubuntu Server 22.04.3, C#, .NET 7, Docker, Docker-Compose, HTTPS, HTML, CSS (Bootstrap), JS (JQeury), AdminLTE, ASP.NET Core MVC Pattern, MariaDB, MySQL WorkBrench(ERD), nonfactor-grid, EntityFrameWork Core, REST API, GitHub Desktop, GitHub, Visual Studio, Visual Studio Code
                   
# Login Account

## Admin Account ID: ***REMOVED***m
## Admin Account Password: rhdiddl12!

## User Account ID: ***REMOVED***
## User Account Password: rhdiddl12!

# Login
![Login](https://user-images.githubusercontent.com/20404991/132020270-488a1ab7-448c-44d9-938a-40ce32d6d364.jpg)

# User-DashBoard
![User-DashBoard](https://user-images.githubusercontent.com/20404991/132020299-e5adb366-9041-44f9-ad56-f2bb606028d5.jpg)

# NonfactorGrid
![NonfactorGrid](https://user-images.githubusercontent.com/20404991/132020455-e66897ef-ece8-4e71-b323-6ebb72f6b110.jpg)

# UserProfile
![UserProfile](https://user-images.githubusercontent.com/20404991/132020484-4b633287-a1b1-48b0-8340-ae3ead83235a.jpg)

# WebAPI
![WebAPI](https://user-images.githubusercontent.com/20404991/132020514-13951172-3bcd-48a5-bfe0-a8328cdb766a.jpg)
