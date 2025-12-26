#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
#https://medium.com/@maheshwar.ramkrushna/best-practices-for-secure-docker-containerization-non-root-user-read-only-volumes-and-resource-d34ed09b1bd3

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Maroik.WebSite/Maroik.WebSite.csproj", "Maroik.WebSite/"]
COPY ["Maroik.Core.Common/Maroik.Core.Common.csproj", "Maroik.Core.Common/"]
COPY ["Maroik.Core.PostgreSQL/Maroik.Core.PostgreSQL.csproj", "Maroik.Core.PostgreSQL/"]
COPY ["Maroik.Core.Repository/Maroik.Core.Repository.csproj", "Maroik.Core.Repository/"]
COPY ["Maroik.Core.Client/Maroik.Core.Client.csproj", "Maroik.Core.Client/"]
COPY ["Maroik.Core.Service/Maroik.Core.Service.csproj", "Maroik.Core.Service/"]
RUN dotnet restore "Maroik.WebSite/Maroik.WebSite.csproj" --disable-parallel
COPY . .
WORKDIR "/src/Maroik.WebSite"
RUN dotnet build "Maroik.WebSite.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Maroik.WebSite.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Maroik.WebSite.dll"]
