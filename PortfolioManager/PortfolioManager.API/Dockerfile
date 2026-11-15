# API image. Build context is the solution folder (PortfolioManager/), not this one: docker compose does that.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# The image's built-in unprivileged user; port 8080 needs no root.
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Project files first, so a change to code alone does not repeat the restore. The API's project references must be
# here too, or the restore cannot find them.
COPY PortfolioManager.API/PortfolioManager.API.csproj PortfolioManager.API/
COPY PortfolioManager.BLL/PortfolioManager.BLL.csproj PortfolioManager.BLL/
COPY PortfolioManager.DAL/PortfolioManager.DAL.csproj PortfolioManager.DAL/
COPY PortfolioManager.Common/PortfolioManager.Common.csproj PortfolioManager.Common/
COPY PortfolioManager.MathModule.Client/PortfolioManager.MathModule.Client.csproj PortfolioManager.MathModule.Client/
RUN dotnet restore PortfolioManager.API/PortfolioManager.API.csproj
COPY . .
RUN dotnet publish PortfolioManager.API/PortfolioManager.API.csproj -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PortfolioManager.API.dll"]
