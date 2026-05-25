# syntax=docker/dockerfile:1.6
# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY apps/api/Directory.Build.props apps/api/Directory.Packages.props apps/api/FarmManager.sln ./apps/api/
COPY apps/api/FarmManager.Domain/FarmManager.Domain.csproj         ./apps/api/FarmManager.Domain/
COPY apps/api/FarmManager.Application/FarmManager.Application.csproj ./apps/api/FarmManager.Application/
COPY apps/api/FarmManager.Infrastructure/FarmManager.Infrastructure.csproj ./apps/api/FarmManager.Infrastructure/
COPY apps/api/FarmManager.Api/FarmManager.Api.csproj               ./apps/api/FarmManager.Api/
COPY apps/api/FarmManager.Workers/FarmManager.Workers.csproj       ./apps/api/FarmManager.Workers/
COPY apps/api/FarmManager.Tests/FarmManager.Tests.csproj           ./apps/api/FarmManager.Tests/

RUN dotnet restore apps/api/FarmManager.Workers/FarmManager.Workers.csproj

COPY apps/api ./apps/api
RUN dotnet publish apps/api/FarmManager.Workers/FarmManager.Workers.csproj -c Release -o /out --no-restore

# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV DOTNET_RUNNING_IN_CONTAINER=true

RUN useradd --uid 10002 --no-create-home --shell /usr/sbin/nologin app && \
    chown -R app /app
USER 10002

COPY --from=build /out ./
ENTRYPOINT ["dotnet", "FarmManager.Workers.dll"]
