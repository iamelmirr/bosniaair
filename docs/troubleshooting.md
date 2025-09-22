# 🔧 SarajevoAir Troubleshooting Guide

This guide helps you diagnose and fix common issues when developing, deploying, or using the SarajevoAir application.

## 🚨 Quick Diagnostics

### Health Check Script
```bash
#!/bin/bash
# health-check.sh - Quick system diagnostics

echo "🔍 SarajevoAir Health Check"
echo "=========================="

# Check if services are running
echo "📊 Service Status:"
curl -f http://localhost:5000/health 2>/dev/null && echo "✅ Backend: Healthy" || echo "❌ Backend: Down"
curl -f http://localhost:3000 2>/dev/null && echo "✅ Frontend: Healthy" || echo "❌ Frontend: Down"

# Check database connection
echo -e "\n🗄️  Database Status:"
docker exec sarajevoair_postgres_1 pg_isready 2>/dev/null && echo "✅ PostgreSQL: Connected" || echo "❌ PostgreSQL: Disconnected"

# Check external APIs
echo -e "\n🌐 External Services:"
curl -f https://api.openaq.org/v3/locations?limit=1 2>/dev/null && echo "✅ OpenAQ API: Accessible" || echo "❌ OpenAQ API: Unreachable"

echo -e "\n🐳 Docker Status:"
docker-compose ps
```

---

## 🛠️ Development Issues

### 1. Docker Compose Won't Start

**Problem**: `docker-compose up` fails or services crash immediately.

**Symptoms:**
- Services exit with code 1
- "Port already in use" errors
- Database connection failures

**Solutions:**

#### Check Port Conflicts
```bash
# Check if ports are in use
lsof -i :3000  # Frontend
lsof -i :5000  # Backend
lsof -i :5432  # PostgreSQL

# Kill processes if needed
sudo lsof -ti:5432 | xargs kill -9
```

#### Clean Docker State
```bash
# Stop all containers and remove volumes
docker-compose down -v

# Remove orphaned containers
docker-compose down --remove-orphans

# Clean up Docker system
docker system prune -f

# Rebuild without cache
docker-compose build --no-cache
docker-compose up -d
```

#### Fix Environment Variables
```bash
# Check if .env file exists in root
ls -la .env

# Verify required variables
cat .env | grep -E "(OPENAQ_API_KEY|CONNECTION_STRING)"
```

### 2. Backend Build Failures

**Problem**: .NET application fails to build or run.

**Symptoms:**
- NuGet restore errors
- Compilation errors
- Missing dependencies

**Solutions:**

#### Clear NuGet Cache
```bash
cd backend
dotnet clean
dotnet nuget locals all --clear
dotnet restore
dotnet build
```

#### Check .NET Version
```bash
# Verify .NET 8.0 is installed
dotnet --version

# Should output 8.0.x
# If not, install .NET 8.0 SDK
```

#### Database Migration Issues
```bash
# Reset database
cd backend
dotnet ef database drop --force
dotnet ef database update

# Or recreate Docker volume
docker-compose down -v
docker volume rm sarajevoair_postgres_data
docker-compose up -d
```

### 3. Frontend Build Failures

**Problem**: Next.js application won't build or start.

**Symptoms:**
- TypeScript compilation errors
- Module not found errors
- Build process hangs

**Solutions:**

#### Clear Node.js Cache
```bash
cd frontend

# Clear npm cache
rm -rf node_modules package-lock.json
npm cache clean --force
npm install

# Clear Next.js cache
rm -rf .next
npm run build
```

#### Fix TypeScript Errors
```bash
# Check TypeScript configuration
npx tsc --noEmit

# Common fixes:
# 1. Update type definitions
npm install --save-dev @types/node @types/react @types/react-dom

# 2. Check tsconfig.json paths
cat tsconfig.json | jq '.compilerOptions.paths'
```

#### Fix Import Errors
```bash
# Check if all dependencies are installed
npm ls

# Install missing dependencies
npm install chart.js react-chartjs-2 leaflet react-leaflet
```

---

## 🌐 API Issues

### 1. OpenAQ API Integration

**Problem**: External API requests failing or returning empty data.

**Symptoms:**
- 403 Forbidden errors
- Rate limit exceeded messages
- No data returned

**Solutions:**

#### Verify API Key
```bash
# Test API key manually
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.openaq.org/v3/locations?limit=1"

# Check environment variable
echo $OPENAQ_API_KEY
```

#### Check Rate Limits
```bash
# View current rate limit status
curl -I -H "X-API-Key: YOUR_API_KEY" \
  "https://api.openaq.org/v3/locations"

# Look for headers:
# X-RateLimit-Limit
# X-RateLimit-Remaining
# X-RateLimit-Reset
```

#### Debug Background Worker
```bash
# Check worker logs
docker-compose logs -f backend | grep "OpenAQ\|Worker"

# Manually trigger data fetch (if endpoint exists)
curl -X POST http://localhost:5000/api/data-sources/openaq/sync
```

### 2. CORS Issues

**Problem**: Frontend can't connect to backend API.

**Symptoms:**
- "CORS policy" errors in browser console
- Network requests blocked
- 403 Forbidden from browser

**Solutions:**

#### Check CORS Configuration
```csharp
// In Program.cs or Startup.cs
app.UseCors(policy => policy
    .WithOrigins("http://localhost:3000", "https://yourdomain.com")
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());
```

#### Verify Environment Variables
```bash
# Check backend CORS settings
grep -r "CORS\|Origin" backend/src/

# Check if frontend URL is correct
echo $NEXT_PUBLIC_API_BASE_URL
```

#### Development Workaround
```bash
# Temporarily disable CORS in development
# Add to appsettings.Development.json:
{
  "CORS": {
    "AllowedOrigins": ["*"]
  }
}
```

### 3. Database Connection Issues

**Problem**: Backend cannot connect to PostgreSQL.

**Symptoms:**
- "Connection string" errors
- Database timeout errors
- Migration failures

**Solutions:**

#### Test Database Connection
```bash
# Connect to database manually
docker exec -it sarajevoair_postgres_1 psql -U dev -d sarajevoair

# Test connection string
dotnet ef database update --connection "Host=localhost;Database=sarajevoair;Username=dev;Password=dev" --verbose
```

#### Reset Database
```bash
# Drop and recreate database
docker-compose exec postgres dropdb -U dev sarajevoair
docker-compose exec postgres createdb -U dev sarajevoair

# Run migrations again
cd backend
dotnet ef database update
```

#### Check Docker Network
```bash
# Verify containers can communicate
docker-compose exec backend ping postgres
docker network ls
docker network inspect sarajevoair_default
```

---

## 🚀 Deployment Issues

### 1. Docker Production Build Failures

**Problem**: Production builds fail or containers crash.

**Symptoms:**
- Out of memory errors
- Build timeouts
- Missing files in production images

**Solutions:**

#### Optimize Docker Build
```dockerfile
# Multi-stage build for smaller images
FROM node:18-alpine AS builder
WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production && npm cache clean --force
COPY . .
RUN npm run build

FROM node:18-alpine AS runner
RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs
WORKDIR /app
COPY --from=builder --chown=nextjs:nodejs /app/.next ./.next
COPY --from=builder --chown=nextjs:nodejs /app/node_modules ./node_modules
USER nextjs
EXPOSE 3000
CMD ["npm", "start"]
```

#### Increase Build Resources
```yaml
# docker-compose.prod.yml
version: '3.8'
services:
  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile.prod
    deploy:
      resources:
        limits:
          memory: 1G
        reservations:
          memory: 512M
```

### 2. Environment Variable Issues

**Problem**: Application behavior differs between environments.

**Solutions:**

#### Verify All Variables
```bash
# Create environment check script
#!/bin/bash
echo "Frontend Variables:"
echo "NEXT_PUBLIC_API_BASE_URL: $NEXT_PUBLIC_API_BASE_URL"
echo "NODE_ENV: $NODE_ENV"

echo "Backend Variables:"
echo "ASPNETCORE_ENVIRONMENT: $ASPNETCORE_ENVIRONMENT"
echo "ConnectionStrings__DefaultConnection: [REDACTED]"
echo "OpenAQ__ApiKey: [REDACTED]"
```

#### Production Environment Template
```env
# .env.production
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
ConnectionStrings__DefaultConnection=Host=prod-db;Database=sarajevoair;Username=prod_user;Password=secure_password
OpenAQ__ApiKey=production_api_key
CORS__AllowedOrigins__0=https://yourdomain.com
Serilog__MinimumLevel__Default=Warning
```

### 3. SSL/TLS Certificate Issues

**Problem**: HTTPS errors in production.

**Solutions:**

#### Let's Encrypt with Docker
```yaml
# docker-compose.ssl.yml
version: '3.8'
services:
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/nginx/ssl
  
  certbot:
    image: certbot/certbot
    volumes:
      - ./ssl:/etc/letsencrypt
    command: certonly --webroot -w /var/www/html -d yourdomain.com
```

---

## 🔍 Performance Issues

### 1. Slow API Responses

**Problem**: API endpoints responding slowly.

**Diagnostic Steps:**

#### Check Database Performance
```sql
-- Check slow queries
SELECT query, mean_time, calls 
FROM pg_stat_statements 
ORDER BY mean_time DESC 
LIMIT 10;

-- Check index usage
SELECT schemaname, tablename, indexname, idx_scan 
FROM pg_stat_user_indexes 
WHERE idx_scan = 0;
```

#### Profile API Endpoints
```bash
# Use curl to measure response times
curl -w "@curl-format.txt" -o /dev/null -s "http://localhost:5000/api/air-quality/sarajevo/current"

# curl-format.txt:
#      time_namelookup:  %{time_namelookup}\n
#         time_connect:  %{time_connect}\n
#      time_appconnect:  %{time_appconnect}\n
#     time_pretransfer:  %{time_pretransfer}\n
#        time_redirect:  %{time_redirect}\n
#   time_starttransfer:  %{time_starttransfer}\n
#                      ----------\n
#           time_total:  %{time_total}\n
```

#### Solutions:

1. **Add Database Indexes**
```sql
-- Create indexes for common queries
CREATE INDEX CONCURRENTLY idx_measurements_city_timestamp 
ON measurements(city, timestamp DESC);

CREATE INDEX CONCURRENTLY idx_measurements_parameter 
ON measurements(parameter, value DESC);
```

2. **Enable Response Caching**
```csharp
// In Program.cs
builder.Services.AddResponseCaching();
builder.Services.AddMemoryCache();

app.UseResponseCaching();
```

3. **Optimize Queries**
```csharp
// Use projection to select only needed fields
var measurements = await _context.Measurements
    .Where(m => m.City == city && m.Timestamp >= startDate)
    .Select(m => new { m.Value, m.Timestamp, m.Parameter })
    .ToListAsync();
```

### 2. High Memory Usage

**Problem**: Application consuming too much memory.

**Diagnostic Steps:**
```bash
# Monitor container memory usage
docker stats

# Check for memory leaks in .NET
dotnet-dump collect -p $(pgrep dotnet)
dotnet-dump analyze core_dump.dmp
```

**Solutions:**

1. **Configure Garbage Collection**
```csharp
// In Program.cs
builder.Services.Configure<GCSettings>(options =>
{
    options.GCSettings.ServerGC = true;
    options.GCSettings.ConcurrentGC = true;
});
```

2. **Optimize Entity Framework**
```csharp
// Use AsNoTracking for read-only queries
var data = await _context.Measurements
    .AsNoTracking()
    .Where(m => m.City == city)
    .ToListAsync();

// Configure DbContext pooling
builder.Services.AddDbContextPool<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
```

---

## 🧪 Testing Issues

### 1. Unit Test Failures

**Problem**: Tests fail randomly or in CI/CD.

**Solutions:**

#### Isolate Database Tests
```csharp
public class TestFixture : IDisposable
{
    public DbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"test_db_{Guid.NewGuid()}")
            .Options;
        
        return new AppDbContext(options);
    }
}
```

#### Mock External Dependencies
```csharp
[Fact]
public async Task GetCurrentAirQuality_ReturnsData()
{
    // Arrange
    var mockHttpClient = new Mock<IHttpClientFactory>();
    var mockLogger = new Mock<ILogger<AirQualityService>>();
    
    // Mock OpenAQ API response
    var mockResponse = new HttpResponseMessage
    {
        StatusCode = HttpStatusCode.OK,
        Content = new StringContent(mockApiResponse)
    };
    
    var service = new AirQualityService(mockHttpClient.Object, mockLogger.Object);
    
    // Act & Assert
    var result = await service.GetCurrentAirQuality("sarajevo");
    Assert.NotNull(result);
}
```

### 2. Integration Test Issues

**Problem**: End-to-end tests failing.

**Setup Test Database:**
```bash
# Use test database for integration tests
export ConnectionStrings__DefaultConnection="Host=localhost;Database=sarajevoair_test;Username=test;Password=test"

# Run tests with test database
dotnet test --settings test.runsettings
```

---

## 📊 Monitoring & Logging

### 1. Enable Debug Logging

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### 2. Application Metrics

```csharp
// Add custom metrics
builder.Services.AddSingleton<IMetricsLogger, MetricsLogger>();

public class MetricsLogger
{
    public void LogApiCall(string endpoint, TimeSpan duration)
    {
        _logger.LogInformation("API Call: {Endpoint} completed in {Duration}ms", 
            endpoint, duration.TotalMilliseconds);
    }
}
```

### 3. Health Check Dashboard

```csharp
// Enhanced health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString)
    .AddUrlGroup(new Uri("https://api.openaq.org/v3/locations"), "OpenAQ API")
    .AddCheck<CustomHealthCheck>("custom-check");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

---

## 🆘 Getting Help

### 1. Enable Verbose Logging
```bash
# Backend
export ASPNETCORE_ENVIRONMENT=Development
export Serilog__MinimumLevel__Default=Debug

# Frontend  
export NODE_ENV=development
export NEXT_PUBLIC_DEBUG=true
```

### 2. Collect Diagnostic Information
```bash
#!/bin/bash
# collect-diagnostics.sh

echo "System Information:" > diagnostic-report.txt
date >> diagnostic-report.txt
uname -a >> diagnostic-report.txt
docker --version >> diagnostic-report.txt
docker-compose --version >> diagnostic-report.txt

echo -e "\nDocker Containers:" >> diagnostic-report.txt
docker-compose ps >> diagnostic-report.txt

echo -e "\nDocker Logs:" >> diagnostic-report.txt
docker-compose logs --tail=50 >> diagnostic-report.txt

echo -e "\nNetwork Configuration:" >> diagnostic-report.txt
docker network ls >> diagnostic-report.txt

echo -e "\nEnvironment Variables:" >> diagnostic-report.txt
env | grep -E "(OPENAQ|CONNECTION|NEXT_PUBLIC)" >> diagnostic-report.txt
```

### 3. Community Support

- **GitHub Issues**: Report bugs and feature requests
- **Discussions**: Ask questions and share experiences
- **Documentation**: Check API docs and deployment guides
- **Stack Overflow**: Use `sarajevoair` tag for questions

### 4. Professional Support

For production deployments and custom development:
- Performance optimization consulting
- Custom deployment configurations
- Extended monitoring setup
- SLA-backed support contracts

---

## 📚 Additional Resources

- **Docker Troubleshooting**: [Docker Documentation](https://docs.docker.com/config/troubleshooting/)
- **ASP.NET Core Diagnostics**: [Microsoft Docs](https://docs.microsoft.com/en-us/aspnet/core/test/troubleshoot)
- **Next.js Debugging**: [Next.js Documentation](https://nextjs.org/docs/advanced-features/debugging)
- **PostgreSQL Performance**: [PostgreSQL Documentation](https://www.postgresql.org/docs/current/performance-tips.html)

Remember: Most issues can be resolved by carefully checking logs, verifying environment variables, and ensuring all services are properly configured and running.