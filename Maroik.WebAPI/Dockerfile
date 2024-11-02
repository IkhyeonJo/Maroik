#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
#https://medium.com/@maheshwar.ramkrushna/best-practices-for-secure-docker-containerization-non-root-user-read-only-volumes-and-resource-d34ed09b1bd3

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Package update and install procps
RUN apt-get update && apt-get install -y procps
RUN apt-get install sysstat -y

EXPOSE 5000
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Maroik.WebAPI/Maroik.WebAPI.csproj", "Maroik.WebAPI/"]
COPY ["Maroik.Common.DataAccess/Maroik.Common.DataAccess.csproj", "Maroik.Common.DataAccess/"]
COPY ["Maroik.Common.Miscellaneous/Maroik.Common.Miscellaneous.csproj", "Maroik.Common.Miscellaneous/"]

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