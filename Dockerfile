# Multi-stage Dockerfile for ZEGU Maintenance Request System

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/Core/ZEGU.Core.csproj", "src/Core/"]
COPY ["src/Infrastructure/ZEGU.Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/WebApp/ZEGU.WebApp.csproj", "src/WebApp/"]
RUN dotnet restore "src/WebApp/ZEGU.WebApp.csproj"

# Copy everything else and build
COPY . .
WORKDIR /src/src/WebApp
RUN dotnet build "ZEGU.WebApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ZEGU.WebApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user
RUN groupadd -r zegu && useradd -r -g zegu zegu

# Copy published app
COPY --from=publish /app/publish .

# Create uploads directory with proper permissions
RUN mkdir -p /app/wwwroot/uploads && chown -R zegu:zegu /app

USER zegu

EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "ZEGU.WebApp.dll"]
