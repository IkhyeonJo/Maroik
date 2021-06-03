이 프로젝트에서 cmd키고

dotnet ef

해서 되는 지 체크... 안 된다면 되도록

dotnet tool install --global dotnet-ef
dotnet tool update --global dotnet-ef
dotnet add package Microsoft.EntityFrameworkCore.Design

할 것


그리고 로컬DB 테이블을 다 C# 모델로 바꾸는 명령어는

dotnet ef dbcontext scaffold "server=localhost;port=3306;userid=root;password=root;database=ikhyeondb;" "Pomelo.EntityFrameworkCore.Mysql" -o .\Models -f