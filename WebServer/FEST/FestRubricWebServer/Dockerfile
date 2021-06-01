#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#just auto execute linux commands to run webserver  

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["FestRubricWebServer/FESTRubricWebServer.csproj", "FestRubricWebServer/"]
RUN dotnet restore "FestRubricWebServer/FESTRubricWebServer.csproj"
COPY . .
WORKDIR "/src/FestRubricWebServer"
RUN dotnet build "FESTRubricWebServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FESTRubricWebServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FESTRubricWebServer.dll"]