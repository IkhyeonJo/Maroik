#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["MyLaboratory.Site.csproj", "."]
RUN dotnet restore "./MyLaboratory.Site.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "MyLaboratory.Site.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR /app
COPY MyLaboratory.Site.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o dist

FROM base AS final
WORKDIR /dist
COPY --from=publish /app/dist .
ENTRYPOINT ["dotnet", "MyLaboratory.Site.dll"]