# Troubleshooting Guide

This document provides solutions to common issues you might encounter when setting up and running the BosniaAir application.

---

## Backend Issues

### Problem: Application won't start - "WAQI API token is not configured. Set Aqicn:ApiToken in configuration." error

**Solution:** You haven't configured the WAQI API token. The application supports two configuration methods:

1. **Method 1 - Configuration file (recommended for local development):**
   ```bash
   # Create .env file in backend/src/BosniaAir.Api/
   echo "Aqicn__ApiToken=your_actual_token_here" > backend/src/BosniaAir.Api/.env
   ```

2. **Method 2 - Environment variable (recommended for production):**
   ```bash
   # Set environment variable
   export WAQI_API_TOKEN=your_actual_token_here
   ```

   **Note:** The `WAQI_API_TOKEN` environment variable will override any `Aqicn__ApiToken` configuration.

3. **Get a WAQI API token:**
   - Visit [WAQI Data Platform](https://aqicn.org/data-platform/register/)
   - Register for a free account
   - Generate an API token in your dashboard
   - Replace `your_actual_token_here` with the actual token

### Problem: Getting 401/403 errors from WAQI API

**Symptoms:**
- Application starts but shows "No data available"
- Backend logs show "Invalid key" or "Forbidden" errors
- API responses return empty or error data

**Solutions:**
1. **Invalid API token:**
   ```bash
   # Test your token manually (replace YOUR_TOKEN with actual token)
   curl "https://api.waqi.info/feed/sarajevo/?token=YOUR_TOKEN"
   ```
   - If you get `{"status":"error","data":"Invalid key"}`, your token is wrong
   - Generate a new token at [WAQI Data Platform](https://aqicn.org/data-platform/token/)
   - Update your configuration with the correct token:
     ```bash
     # Either set in .env file
     Aqicn__ApiToken=your_new_token
     # Or as environment variable
     export WAQI_API_TOKEN=your_new_token
     ```

2. **Rate limit exceeded:**
   - WAQI free tier allows 1,000 requests/day
   - Check your usage in the WAQI dashboard
   - Consider upgrading for higher limits or wait for rate limit reset

3. **Token format issues:**
   ```bash
   # Correct formats (no quotes)
   
   # Option 1: Configuration format
   Aqicn__ApiToken=abc123def456
   
   # Option 2: Environment variable (overrides config)
   WAQI_API_TOKEN=abc123def456
   
   # Incorrect format
   WAQI_API_TOKEN="abc123def456"  # Remove quotes
   ```

### Problem: Database is empty or not updating

**Symptoms:**
- API returns 404 for all cities
- No data visible in frontend
- Database file exists but has no records

**Solutions:**
1. **Check database permissions:**
   ```bash
   # Ensure the application can write to the database directory
   chmod 755 backend/src/BosniaAir.Api/
   chmod 644 backend/src/BosniaAir.Api/bosniaair-aqi.db
   ```

2. **Force database recreation:**
   ```bash
   # Stop the application first
   rm backend/src/BosniaAir.Api/bosniaair-aqi.db*
   # Restart application - EF Core will recreate the database
   ```

3. **Check scheduler service:**
   ```bash
   # Look for scheduler logs
   grep -i "scheduler\|refresh" backend/src/BosniaAir.Api/logs/*.log
   ```

4. **Manual data refresh:**
   ```bash
   # Trigger refresh via API (if available)
   curl -X POST "http://localhost:5000/api/v1/air-quality/refresh"
   ```

### Problem: Application crashes with database connection errors

**Solutions:**
1. **SQLite file locked:**
   ```bash
   # Kill any processes using the database
   lsof backend/src/BosniaAir.Api/bosniaair-aqi.db
   # Or restart your machine
   ```

2. **Disk space full:**
   ```bash
   # Check disk space
   df -h
   # Clean up if needed
   ```

3. **Check database file integrity:**
   ```bash
   sqlite3 backend/src/BosniaAir.Api/bosniaair-aqi.db ".integrity_check"
   ```

---

## Frontend Issues

### Problem: "CORS error" in browser console

**Symptoms:**
- Frontend loads but shows "Error loading data"
- Browser console shows CORS policy errors
- Network tab shows failed API requests

**Solutions:**
1. **Check backend is running:**
   ```bash
   curl http://localhost:5000/health
   # Should return "Healthy"
   ```

2. **Verify CORS configuration:**
   - Backend must be configured to allow frontend origin
   - Check `appsettings.json` or environment variable `FRONTEND_ORIGIN`
   - Default should be `http://localhost:3000` for development

3. **Update CORS settings:**
   ```json
   // In appsettings.json
   {
     "AllowedOrigins": ["http://localhost:3000", "https://your-domain.com"]
   }
   ```

4. **Port conflicts:**
   - Ensure frontend runs on port 3000
   - Ensure backend runs on port 5000
   - If using different ports, update API URL in frontend

### Problem: "Error loading data" message in UI

**Solutions:**
1. **Check API endpoint:**
   ```bash
   # Test the API manually
   curl "http://localhost:5000/api/v1/air-quality/Sarajevo/complete"
   ```

2. **Verify API_URL configuration:**
   ```bash
   # In frontend/.env.local
   NEXT_PUBLIC_API_URL=http://localhost:5000
   ```

3. **Network connectivity:**
   - Check firewall settings
   - Verify no proxy blocking requests
   - Test from different browser/device

### Problem: `npm install` fails

**Common Issues:**
1. **Node.js version incompatibility:**
   ```bash
   # Check Node version (requires 18+)
   node --version
   
   # Update Node if needed (using nvm)
   nvm install 18
   nvm use 18
   ```

2. **Package conflicts:**
   ```bash
   # Clean installation
   rm -rf node_modules package-lock.json
   npm cache clean --force
   npm install
   ```

3. **Permission issues (macOS/Linux):**
   ```bash
   # Fix npm permissions
   sudo chown -R $(whoami) ~/.npm
   ```

4. **Network/proxy issues:**
   ```bash
   # Use different registry if needed
   npm install --registry https://registry.npmjs.org/
   ```

### Problem: TypeScript compilation errors

**Solutions:**
1. **Clear TypeScript cache:**
   ```bash
   rm -rf .next
   npm run build
   ```

2. **Update dependencies:**
   ```bash
   npm update
   npm audit fix
   ```

3. **Check TypeScript version:**
   ```bash
   # Ensure compatible version
   npm list typescript
   ```

---

## Development Environment Issues

### Problem: Hot reload not working

**Solutions:**
1. **Frontend hot reload:**
   ```bash
   # Restart development server
   npm run dev
   ```

2. **Backend hot reload:**
   ```bash
   # Use watch mode
   dotnet watch run
   ```

3. **Port conflicts:**
   ```bash
   # Check what's using your ports
   lsof -i :3000  # Frontend
   lsof -i :5000  # Backend
   
   # Kill processes if needed
   kill -9 <PID>
   ```

### Problem: Docker build fails

**Solutions:**
1. **Check Docker version:**
   ```bash
   docker --version
   # Ensure Docker is running
   ```

2. **Build context issues:**
   ```bash
   # Build from correct directory
   cd backend/src/BosniaAir.Api
   docker build -t bosniaair-api .
   ```

3. **Port mapping:**
   ```bash
   # Correct port mapping
   docker run -p 5000:80 -e WAQI_API_TOKEN=your_token bosniaair-api
   ```

---

## Production Deployment Issues

### Problem: API not accessible from frontend in production

**Solutions:**
1. **CORS configuration:**
   ```bash
   # Set production frontend URL
   export FRONTEND_ORIGIN=https://your-app.vercel.app
   ```

2. **HTTPS/HTTP mixed content:**
   - Ensure both frontend and backend use HTTPS in production
   - Or both use HTTP for local development

3. **Firewall/security groups:**
   - Ensure backend port is accessible
   - Check cloud provider security settings

### Problem: Database issues in production

**Solutions:**
1. **File permissions:**
   ```bash
   # Ensure write permissions for database directory
   chmod 755 /app/data
   ```

2. **Persistent storage:**
   - Ensure database file is stored in persistent volume
   - Not in ephemeral container storage

3. **Connection strings:**
   ```bash
   # Use environment-specific connection strings
   export DATABASE_URL=postgresql://user:pass@host:5432/db
   ```

---

## Performance Issues

### Problem: Slow API response times

**Solutions:**
1. **Check database performance:**
   ```sql
   -- Analyze slow queries
   EXPLAIN QUERY PLAN SELECT * FROM AirQualityRecords WHERE RecordType = 'LiveSnapshot';
   ```

2. **Monitor WAQI API response times:**
   ```bash
   # Test WAQI API directly
   time curl "https://api.waqi.info/feed/sarajevo/?token=YOUR_TOKEN"
   ```

3. **Enable caching:**
   - Verify data caching is working
   - Check cache expiration settings
   - Monitor memory usage

### Problem: High memory usage

**Solutions:**
1. **Monitor memory:**
   ```bash
   # Check application memory usage
   ps aux | grep -E "(dotnet|node)"
   ```

2. **Garbage collection:**
   ```bash
   # For .NET application
   export DOTNET_gcServer=1
   ```

3. **Database optimization:**
   ```sql
   -- Clean old records if needed
   DELETE FROM AirQualityRecords WHERE Timestamp < date('now', '-30 days');
   VACUUM;
   ```

---

## Getting Help

If you're still experiencing issues:

1. **Check application logs:**
   ```bash
   # Backend logs
   tail -f backend/src/BosniaAir.Api/logs/app.log
   
   # Frontend logs
   npm run dev  # Check console output
   ```

2. **Enable debug logging:**
   ```json
   // In appsettings.json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug"
       }
     }
   }
   ```

3. **Test individual components:**
   ```bash
   # Test WAQI API directly
   curl "https://api.waqi.info/feed/sarajevo/?token=YOUR_TOKEN"
   
   # Test backend API
   curl "http://localhost:5000/health"
   
   # Test database
   sqlite3 backend/src/BosniaAir.Api/bosniaair-aqi.db ".tables"
   ```

4. **Create a minimal reproduction:**
   - Isolate the issue to smallest possible case
   - Document exact steps to reproduce
   - Include error messages and logs

5. **Community support:**
   - Check existing GitHub issues
   - Create detailed bug report if needed
   - Include system information and logs