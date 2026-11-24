# One image: the Angular build is served as static files by the API.

# 1) Front-end. angular.json writes the build to ../src/Inventory.Api/wwwroot.
FROM node:24-slim AS web
WORKDIR /repo/web
COPY web/package.json web/package-lock.json ./
RUN npm ci
COPY web/ ./
RUN npm run build

# 2) Back-end: publish the API.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/ src/
RUN dotnet publish src/Inventory.Api/Inventory.Api.csproj -c Release -o /app

# 3) Runtime: ASP.NET Core only, non-root user.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
COPY --from=web /repo/src/Inventory.Api/wwwroot ./wwwroot
RUN mkdir -p /app/data && chown "$APP_UID" /app/data
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__Inventory="Data Source=/app/data/inventory.db"
EXPOSE 8080
ENTRYPOINT ["dotnet", "Inventory.Api.dll"]
