# SarajevoAir Deployment Guide

This guide covers deployment options for the SarajevoAir application across different environments.

## 🌍 Deployment Architecture

```mermaid
graph TB
    subgraph "Production Environment"
        FE[Frontend - Vercel]
        BE[Backend - Render/Railway]
        DB[PostgreSQL - Supabase]
        CDN[Static Assets - CDN]
    end
    
    subgraph "Development"
        DEV[Docker Compose]
        DEVDB[PostgreSQL Container]
    end
    
    subgraph "CI/CD"
        GH[GitHub Actions]
        REG[GitHub Container Registry]
        SEC[Security Scanning]
    end
    
    FE --> BE
    BE --> DB
    GH --> REG
    GH --> FE
    GH --> BE
```

## 🚀 Quick Deployment Options

### Option 1: Docker Compose (Recommended for Local/Staging)

```bash
# Clone repository
git clone https://github.com/yourusername/sarajevoair.git
cd sarajevoair

# Start all services
docker-compose up -d

# Check status
docker-compose ps
```

**Endpoints:**
- Frontend: http://localhost:3000
- Backend API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger
- PostgreSQL: localhost:5432

### Option 2: Cloud Deployment (Production)

#### Frontend → Vercel
1. **Connect Repository**
   ```bash
   # Install Vercel CLI
   npm i -g vercel
   
   # Deploy from frontend directory
   cd frontend
   vercel --prod
   ```

2. **Environment Variables (Vercel)**
   ```env
   NEXT_PUBLIC_API_URL=https://your-backend.herokuapp.com/api
   NEXT_PUBLIC_BASE_URL=https://your-domain.vercel.app
   NEXT_PUBLIC_APP_NAME=SarajevoAir
   ```

#### Backend → Render/Railway

**Render Deployment:**
1. Connect GitHub repository
2. Set build command: `dotnet publish -c Release -o out`
3. Set start command: `dotnet out/SarajevoAir.Api.dll`
4. Add environment variables

**Railway Deployment:**
```bash
# Install Railway CLI
npm install -g @railway/cli

# Login and deploy
railway login
railway link
railway up
```

#### Database → Supabase/PlanetScale

**Supabase Setup:**
1. Create new project at [supabase.com](https://supabase.com)
2. Get connection string
3. Run migrations:
   ```bash
   dotnet ef database update --connection "YourConnectionString"
   ```

## ⚙️ Environment Configuration

### Backend (.env)
```env
# Database
ConnectionStrings__DefaultConnection=Host=localhost;Database=sarajevoair;Username=postgres;Password=yourpassword

# OpenAQ API
OpenAQ__BaseUrl=https://api.openaq.org/v3
OpenAQ__ApiKey=your_openaq_key
OpenAQ__RateLimitPerHour=10000

# Application
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000

# Logging
Serilog__MinimumLevel__Default=Information
Serilog__WriteTo__0__Name=Console

# CORS
CORS__AllowedOrigins__0=https://yourdomain.vercel.app
CORS__AllowedOrigins__1=http://localhost:3000
```

### Frontend (.env.local)
```env
# API Configuration
NEXT_PUBLIC_API_URL=https://your-backend-url.herokuapp.com/api
NEXT_PUBLIC_BASE_URL=https://yourdomain.vercel.app

# Application
NEXT_PUBLIC_APP_NAME=SarajevoAir
NEXT_PUBLIC_APP_VERSION=1.0.0

# Development
NODE_ENV=production
NEXT_TELEMETRY_DISABLED=1

# Analytics (Optional)
NEXT_PUBLIC_GA_ID=G-XXXXXXXXXX
```

## 🔒 Security & Performance

### SSL/TLS
- **Frontend**: Automatic with Vercel
- **Backend**: Use service SSL certificates
- **Database**: Enable SSL in production

### Performance Optimizations
```yaml
# docker-compose.prod.yml
version: '3.8'
services:
  backend:
    build:
      context: ./backend
      dockerfile: Dockerfile.prod
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    deploy:
      replicas: 2
      resources:
        limits:
          memory: 512M
        reservations:
          memory: 256M

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile.prod
    deploy:
      replicas: 2
      resources:
        limits:
          memory: 256M
        reservations:
          memory: 128M
```

## 🏗️ CI/CD Pipeline

### GitHub Actions Workflow

The included `.github/workflows/ci-cd.yml` provides:

1. **Automated Testing**
   - Backend: .NET unit tests with PostgreSQL
   - Frontend: TypeScript compilation, linting, build

2. **Security Scanning**
   - Dependency vulnerability scanning
   - Container image scanning with Trivy

3. **Docker Build & Push**
   - Multi-platform builds (AMD64, ARM64)
   - GitHub Container Registry
   - Automated versioning

4. **Deployment Stages**
   - Staging: Automatic on main branch
   - Production: Manual approval required

### Required GitHub Secrets

```yaml
# Repository Secrets
GITHUB_TOKEN: automatic # GitHub provides this
DATABASE_URL: postgresql://user:pass@host:5432/db
OPENAQ_API_KEY: your_openaq_api_key

# Repository Variables  
NEXT_PUBLIC_API_URL: https://api.yourdomain.com/api
PRODUCTION_URL: https://yourdomain.com
```

## 🐳 Docker Production Images

### Backend Dockerfile.prod
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY out/ .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

# Security: Run as non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser
USER appuser

EXPOSE 5000
ENTRYPOINT ["dotnet", "SarajevoAir.Api.dll"]
```

### Frontend Dockerfile.prod
```dockerfile
FROM node:18-alpine AS builder
WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production

COPY . .
RUN npm run build

FROM node:18-alpine AS runner
RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs
USER nextjs

COPY --from=builder --chown=nextjs:nodejs /app/.next ./.next
COPY --from=builder /app/node_modules ./node_modules
COPY --from=builder /app/package.json ./package.json

EXPOSE 3000
CMD ["npm", "start"]
```

## 📊 Monitoring & Logging

### Application Monitoring
```csharp
// Program.cs - Add monitoring
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString)
    .AddUrlGroup(new Uri("https://api.openaq.org/v3/locations"), "OpenAQ API");

// Add Application Insights (optional)
builder.Services.AddApplicationInsightsTelemetry();
```

### Log Aggregation
```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": "Information",
    "WriteTo": [
      {"Name": "Console"},
      {
        "Name": "File",
        "Args": {"path": "logs/sarajevoair-.txt", "rollingInterval": "Day"}
      }
    ]
  }
}
```

## 🧪 Testing Deployments

### Health Checks
```bash
# Backend health
curl -f https://your-api.herokuapp.com/health

# Frontend availability  
curl -f https://your-app.vercel.app/api/health

# Database connectivity
curl -f https://your-api.herokuapp.com/health/database
```

### Load Testing
```bash
# Install artillery
npm install -g artillery

# Run load test
artillery run load-test.yml
```

### Performance Monitoring
- **Frontend**: Vercel Analytics, Web Vitals
- **Backend**: Application Insights, Custom metrics
- **Database**: Connection pooling, query performance

## 🚨 Troubleshooting

### Common Issues

1. **CORS Errors**
   ```csharp
   // Ensure correct CORS origins in appsettings.json
   "CORS": {
     "AllowedOrigins": ["https://yourdomain.vercel.app"]
   }
   ```

2. **Database Connection Issues**
   ```bash
   # Test connection string
   dotnet ef database update --connection "YourConnectionString" --verbose
   ```

3. **Build Failures**
   ```bash
   # Clear Next.js cache
   rm -rf .next
   npm run build
   
   # Clear Docker cache
   docker builder prune
   ```

### Rollback Strategy

```bash
# Rollback with Docker tags
docker-compose down
docker-compose up -d --scale backend=0
docker pull ghcr.io/user/sarajevoair-backend:previous-version
docker-compose up -d
```

## 📈 Scaling Considerations

### Horizontal Scaling
- **Frontend**: Automatic with Vercel Edge Network
- **Backend**: Load balancer + multiple instances
- **Database**: Read replicas, connection pooling

### Caching Strategy
```csharp
// Redis for caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

// Memory cache for development
builder.Services.AddMemoryCache();
```

### Database Optimization
```sql
-- Add indexes for performance
CREATE INDEX CONCURRENTLY idx_measurements_city_timestamp 
ON measurements(city, timestamp DESC);

CREATE INDEX CONCURRENTLY idx_measurements_parameter 
ON measurements(parameter, timestamp DESC);
```

---

## 🎯 Quick Start Checklist

- [ ] Clone repository
- [ ] Set up environment variables
- [ ] Choose deployment method
- [ ] Configure CI/CD pipeline
- [ ] Set up monitoring
- [ ] Test all endpoints
- [ ] Configure custom domain
- [ ] Enable SSL certificates
- [ ] Set up backup strategy
- [ ] Document runbooks

For additional help, check the [troubleshooting guide](../docs/troubleshooting.md) or open an issue on GitHub.