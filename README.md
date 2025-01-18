# Maroik
Welcome to Maroik!

I'm currently interested in make ASP.NET Core MVC WebApplication!

Docker, AdminLTE, MariaDB, Session Login, RESTful API, HTTPS etc.

# How to run this project (Windows OS / Mac OS)
## 0. Install Visual Studio 2023 or higher -> Execute [Maroik.sln] -> Visual Studio will ask you about to Install setup files. Install all required setup files to run this project 
## 1. Open [1.Web] folder in Solution Explorer -> right-click [docker-compose] in Solution Explorer -> click "Set as Startup Project" -> Press F5 Button
## 2. [Note] To completely debug Maroik.Website, you must set of /Maroik/Maroik.WebSite/[appsettings.DockerComposeLocalDebug.json & appsettings.Development]'s Mail Account Configuration.

# How to run this project (Ubuntu (Linux))
## 0. Open [Maroik] folder with Visual Studio Code
## 1. You will be asked to enter a password to run the sudo command in the TERMINAL of visual studio code. Enter the password to run the sudo command.
## 2. Wait till all process is done
## 3. Visual Studio Code may have recommended you to install the Workspace Recommended Extensions. Install all of them.
## 4. Wait till all process is done
## 5. Go to [Run and Debug] -> Choose the following configuration name. [Docker: Maroik.WebSite] -> Press F5
## 6. Answer "Yes" to all requests.
## 7. Open Web browser and access to [https://localhost] 
## 8. [Note] To completely debug Maroik.Website, you must set of /Maroik/Maroik.WebSite/[appsettings.DockerComposeLocalDebug.json & appsettings.Development]'s Mail Account Configuration.

# How to deploy to Ubuntu Server 22.04.3
## 0. You must set this value [Maroik -> Maroik.WebSite -> appsettings.Production.json -> DomainName (According to Server Domain [If you don't have server domain, just set your server Public IP address])] & Mail Account
## 1. To Backup your .sql & Upload file frequently, please set [Maroik -> Maroik.Crontab -> DBBackup.sh] [Maroik -> Maroik.Crontab -> UploadFileBackup.sh] file's email address & password
## 2. To Deploy Maroik service, Copy Maroik folder to your ubuntu server [path:(/home/peter-cho/)] (Default Path is '/home/peter-cho/')
## 3. Open Ubuntu Server 22.04.3's port 80, 443, 5001
## 4. Execute this command [chmod 777 /home/peter-cho/Maroik/Maroik.DeployOps/Deploy.sh && /home/peter-cho/Maroik/Maroik.DeployOps/Deploy.sh]
## 5. For more detail information, please read 홈서버 구축.pptx which is located /Maroik/Docs/HomeServer/홈서버 구축.pptx
## 6. [Note] If you set number 1, the Maroik.WebSite Upload File Backup and Maroik MariaDB SQL Backup delivered by mail are server backups, you can overwrite them in the \Maroik\Maroik.WebSite\wwwroot\upload, \Maroik\Maroik.FileStorage\upload and \Maroik\Maroik.MariaDB\SQL_Init_Script folders respectively.
                   
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
