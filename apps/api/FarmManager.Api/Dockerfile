# syntax=docker/dockerfile:1.6
# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy props first for better layer caching
COPY apps/api/Directory.Build.props apps/api/Directory.Packages.props apps/api/FarmManager.sln ./apps/api/
COPY apps/api/FarmManager.Domain/FarmManager.Domain.csproj         ./apps/api/FarmManager.Domain/
COPY apps/api/FarmManager.Application/FarmManager.Application.csproj ./apps/api/FarmManager.Application/
COPY apps/api/FarmManager.Infrastructure/FarmManager.Infrastructure.csproj ./apps/api/FarmManager.Infrastructure/
COPY apps/api/FarmManager.Api/FarmManager.Api.csproj               ./apps/api/FarmManager.Api/
COPY apps/api/FarmManager.Workers/FarmManager.Workers.csproj       ./apps/api/FarmManager.Workers/
COPY apps/api/FarmManager.Tests/FarmManager.Tests.csproj           ./apps/api/FarmManager.Tests/

RUN dotnet restore apps/api/FarmManager.Api/FarmManager.Api.csproj

COPY apps/api ./apps/api
RUN dotnet publish apps/api/FarmManager.Api/FarmManager.Api.csproj -c Release -o /out --no-restore

# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

RUN useradd --uid 10001 --no-create-home --shell /usr/sbin/nologin app && \
    chown -R app /app
USER 10001

COPY --from=build /out ./
EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl -fsS http://127.0.0.1:5000/healthz || exit 1

ENTRYPOINT ["dotnet", "FarmManager.Api.dll"]
