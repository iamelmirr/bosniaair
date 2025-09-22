# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY ../../SarajevoAir.sln .

# Copy project files
COPY ../../src/SarajevoAir.Domain/SarajevoAir.Domain.csproj src/SarajevoAir.Domain/
COPY ../../src/SarajevoAir.Application/SarajevoAir.Application.csproj src/SarajevoAir.Application/
COPY ../../src/SarajevoAir.Infrastructure/SarajevoAir.Infrastructure.csproj src/SarajevoAir.Infrastructure/
COPY ../../src/SarajevoAir.Worker/SarajevoAir.Worker.csproj src/SarajevoAir.Worker/
COPY ./SarajevoAir.Api.csproj .

# Restore dependencies
RUN dotnet restore ./SarajevoAir.Api.csproj

# Copy source code
COPY ../../src/SarajevoAir.Domain/ src/SarajevoAir.Domain/
COPY ../../src/SarajevoAir.Application/ src/SarajevoAir.Application/
COPY ../../src/SarajevoAir.Infrastructure/ src/SarajevoAir.Infrastructure/
COPY ../../src/SarajevoAir.Worker/ src/SarajevoAir.Worker/
COPY . .

# Build and publish
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

EXPOSE 80

ENTRYPOINT ["dotnet", "SarajevoAir.Api.dll"]