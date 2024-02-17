#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Maroik.WebAPI/Maroik.WebAPI.csproj", "Maroik.WebAPI/"]
COPY ["Maroik.Common.DataAccess/Maroik.Common.DataAccess.csproj", "Maroik.Common.DataAccess/"]
RUN dotnet restore "Maroik.WebAPI/Maroik.WebAPI.csproj"
COPY . .
WORKDIR "/src/Maroik.WebAPI"
RUN dotnet build "Maroik.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Maroik.WebAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Maroik.WebAPI.dll"]