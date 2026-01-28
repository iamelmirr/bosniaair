# Railway Deployment Setup

## Required Environment Variables

Set these environment variables in your Railway project settings:

### Backend Service

```bash
# WAQI API Token (required)
WAQI_API_TOKEN=your_waqi_api_token_here

# Frontend Origin for CORS (required for production)
FRONTEND_ORIGIN=https://your-frontend-domain.vercel.app

# Database (optional - Railway provides PostgreSQL automatically)
DATABASE_URL=<automatically set by Railway if using PostgreSQL>

# Logging Level (optional)
LOGGING__LOGLEVEL__DEFAULT=Information
```

## How to Set Environment Variables on Railway

1. Go to your Railway project: https://railway.app/project/your-project-id
2. Click on your backend service
3. Go to the **Variables** tab
4. Click **+ New Variable**
5. Add each variable with its value
6. Click **Deploy** to apply changes

## CORS Configuration

The backend now accepts requests from:
- `http://localhost:3000` (local development)
- `http://localhost:3001` (local development)
- `http://localhost:3002` (local development)
- `http://localhost:5000` (local development)
- `http://localhost:5001` (local development)
- **Value from `FRONTEND_ORIGIN` environment variable** (production)

### Example Production Setup

If your frontend is deployed at `https://bosniaair.vercel.app`, set:

```bash
FRONTEND_ORIGIN=https://bosniaair.vercel.app
```

**Important:** Do NOT include a trailing slash!

## Verifying Deployment

After setting environment variables and redeploying:

1. Check Railway logs for: `Added FRONTEND_ORIGIN to CORS: https://your-domain`
2. Test the health endpoint: `https://your-backend.railway.app/health`
3. Test CORS with a browser request from your frontend

## Troubleshooting

### 404 on Preflight Requests

- ✅ **Fixed:** CORS middleware now runs before routing
- ✅ **Fixed:** Preflight responses cached for 10 minutes
- Verify `FRONTEND_ORIGIN` is set correctly in Railway

### CORS Still Blocked

1. Check Railway logs for CORS configuration
2. Verify frontend URL matches `FRONTEND_ORIGIN` exactly (including protocol)
3. Make sure there's no trailing slash in `FRONTEND_ORIGIN`
4. Check that the deployed backend is actually running the latest code

### Database Issues

- Railway automatically provides `DATABASE_URL` for PostgreSQL
- SQLite is used as fallback for local development
- Check logs for: `Database ready`
