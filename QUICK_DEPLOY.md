# Alternative Hosting Quick Commands

## 🚀 Render.com (Easiest - 5 minutes)

```bash
# 1. Push to GitHub
git add .
git commit -m "Add Render config"
git push

# 2. Go to Render Dashboard
open https://dashboard.render.com

# 3. Click "New +" → "Blueprint"
# 4. Select your repository
# 5. Enter WAQI_API_TOKEN when prompted
# 6. Deploy!

# Your API will be at: https://bosniaair-api.onrender.com
```

---

## ⚡ Fly.io (Fastest - 10 minutes)

```bash
# Install CLI
brew install flyctl

# Login
flyctl auth login

# Launch (from project root)
cd /Users/elmirbesirovic/Desktop/projects/sarajevoairvibe
flyctl launch --name bosniaair-api --region ams --no-deploy

# Set secrets
flyctl secrets set WAQI_API_TOKEN="your_token_here"
flyctl secrets set FRONTEND_ORIGIN="https://bosniaair.vercel.app"

# Deploy
flyctl deploy

# Your API will be at: https://bosniaair-api.fly.dev
```

---

## ☁️ Azure (Best for .NET - 15 minutes)

```bash
# Install CLI
brew install azure-cli

# Login
az login

# Deploy (one command!)
cd backend/src/BosniaAir.Api
az webapp up --name bosniaair-api --runtime "DOTNET:8.0" --location westeurope

# Set environment variables
az webapp config appsettings set --name bosniaair-api --resource-group bosniaair-api-rg --settings \
  WAQI_API_TOKEN="your_token" \
  FRONTEND_ORIGIN="https://bosniaair.vercel.app"

# Your API will be at: https://bosniaair-api.azurewebsites.net
```

---

## 🌊 DigitalOcean (Simplest UI - 10 minutes)

```bash
# Install CLI (optional - web UI is easier)
brew install doctl

# Or just use the web UI:
open https://cloud.digitalocean.com/apps/new

# Steps in UI:
# 1. Connect GitHub
# 2. Select repository
# 3. Choose Docker deployment
# 4. Set Dockerfile path: backend/src/BosniaAir.Api/Dockerfile.railway
# 5. Add environment variables
# 6. Deploy!

# Your API will be at: https://bosniaair-api-xxxxx.ondigitalocean.app
```

---

## 🎯 Quick Comparison

| Platform | Command Count | Time | Free Tier | Difficulty |
|----------|--------------|------|-----------|------------|
| **Render** | 0 (UI only) | 5 min | ✅ 750h | ⭐ |
| **Fly.io** | 5 commands | 10 min | ✅ 3 VMs | ⭐⭐ |
| **Azure** | 3 commands | 15 min | 💰 $200 | ⭐⭐⭐ |
| **DigitalOcean** | 0 (UI) | 10 min | 💰 $200 | ⭐ |

---

## 📝 After Deployment

Update Vercel environment variable:

```bash
# In Vercel dashboard, set:
NEXT_PUBLIC_API_URL=https://your-new-api-url.com

# Then redeploy frontend:
vercel --prod
```

---

## ✅ Test Your Deployment

```bash
# Health check
curl https://your-api-url.com/health

# Get live AQI for Sarajevo
curl https://your-api-url.com/api/v1/air-quality/Sarajevo/live

# Should return JSON with AQI data
```

---

## 🆘 Troubleshooting

### CORS Issues
Make sure `FRONTEND_ORIGIN` is set correctly:
```bash
# ✅ Correct
FRONTEND_ORIGIN=https://bosniaair.vercel.app

# ❌ Wrong (trailing slash)
FRONTEND_ORIGIN=https://bosniaair.vercel.app/
```

### Build Fails
Check the platform logs:
```bash
# Render: Dashboard → Logs
# Fly.io: flyctl logs
# Azure: az webapp log tail --name bosniaair-api --resource-group bosniaair-api-rg
# DO: Dashboard → Logs
```

### Database Issues
For SQLite (default), no setup needed!
For PostgreSQL:
- Render: Create database in dashboard
- Fly.io: `flyctl postgres create`
- Azure: Create SQL Database resource
- DO: Add PostgreSQL in dashboard

---

## 💰 Cost After Free Tier

| Platform | After Free | Notes |
|----------|-----------|-------|
| Render | $7/month | For always-on |
| Fly.io | $1.94/month | For 1 VM always-on |
| Azure | Pay-as-you-go | Can be expensive |
| DigitalOcean | $5/month | Basic app |

---

## 🎬 My Recommendation

1. **Try Render first** (easiest, best free tier)
2. **If you need speed** → Fly.io
3. **If you're a .NET dev** → Azure
4. **If you want simplicity** → DigitalOcean

---

Choose one and run the commands above! 🚀
