# Render.com Deployment Guide

## Why Render.com?

- ✅ **Free tier** - 750 hours/month (enough for 1 app 24/7)
- ✅ **Auto-deploy** from GitHub
- ✅ **PostgreSQL** included (90 days free)
- ✅ **Similar to Railway** - easy migration
- ✅ **Automatic HTTPS**
- ✅ **Docker support**

---

## 🚀 Quick Start (5 minutes)

### Step 1: Create Render Account

1. Go to https://render.com
2. Sign up with GitHub
3. Authorize Render to access your repositories

### Step 2: Deploy from GitHub

**Option A: Using Blueprint (Recommended)**

1. Push `render.yaml` to your GitHub repo
2. In Render dashboard, click **"New +"** → **"Blueprint"**
3. Select your `sarajevoairvibe` repository
4. Render will detect `render.yaml` automatically
5. Click **"Apply"**
6. Enter your `WAQI_API_TOKEN` when prompted
7. Wait 5-10 minutes for deployment

**Option B: Manual Setup**

1. In Render dashboard, click **"New +"** → **"Web Service"**
2. Connect your GitHub repository
3. Configure:
   - **Name:** `bosniaair-api`
   - **Environment:** `Docker`
   - **Dockerfile Path:** `./backend/src/BosniaAir.Api/Dockerfile.railway`
   - **Docker Context:** `./backend/src/BosniaAir.Api`
4. Add environment variables (see below)
5. Click **"Create Web Service"**

### Step 3: Configure Environment Variables

Add these in the **Environment** tab:

```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:5000
WAQI_API_TOKEN=your_waqi_token_here
FRONTEND_ORIGIN=https://bosniaair.vercel.app
```

### Step 4: Create PostgreSQL Database (Optional)

1. Click **"New +"** → **"PostgreSQL"**
2. Name: `bosniaair-db`
3. Free plan (90 days)
4. Copy the **Internal Connection String**
5. Add to your web service as `DATABASE_URL`

### Step 5: Update Frontend API URL

In your Vercel environment variables:

```bash
NEXT_PUBLIC_API_URL=https://bosniaair-api.onrender.com
```

---

## 📋 Checklist After Deployment

- [ ] Service is deployed and running
- [ ] Health check passes: `https://your-app.onrender.com/health`
- [ ] CORS is working (check browser console)
- [ ] Database is connected
- [ ] Frontend can fetch data

---

## 🆚 Render vs Railway

| Feature | Render | Railway |
|---------|--------|---------|
| Free Tier | ✅ 750h/month | ✅ $5 credit |
| PostgreSQL | ✅ Free 90 days | ✅ Free |
| Auto-deploy | ✅ Yes | ✅ Yes |
| Docker | ✅ Yes | ✅ Yes |
| Speed | ⚡ Good | ⚡ Great |
| UI | 😊 Simple | 😍 Beautiful |
| Uptime | 99.9% | 99.9% |

---

## 🐛 Troubleshooting

### Build Fails

Check Render logs for errors. Common issues:
- Dockerfile path incorrect
- Missing environment variables

### CORS Errors

Make sure `FRONTEND_ORIGIN` matches your Vercel URL exactly:
```bash
# ✅ Correct
FRONTEND_ORIGIN=https://bosniaair.vercel.app

# ❌ Wrong
FRONTEND_ORIGIN=https://bosniaair.vercel.app/
FRONTEND_ORIGIN=http://bosniaair.vercel.app  # Missing HTTPS
```

### Database Connection Issues

- Use the **Internal Connection String** from Render
- Format: `postgresql://user:password@host:5432/dbname`

### App Sleeps After 15 Minutes

Free tier apps sleep after inactivity. Solutions:
1. Upgrade to paid ($7/month for always-on)
2. Use UptimeRobot to ping every 10 minutes
3. Accept 30-second cold start delay

---

## 💰 Pricing

**Free Tier:**
- 750 hours/month (enough for 1 app running 24/7)
- 400 build minutes/month
- PostgreSQL free for 90 days

**After 90 days:**
- PostgreSQL: $7/month (1GB storage)
- Or switch to SQLite (free, included in your app)

---

## 🔄 Migration Steps

1. ✅ Push `render.yaml` to GitHub
2. ✅ Deploy on Render
3. ✅ Test API endpoints
4. ✅ Update Vercel `NEXT_PUBLIC_API_URL`
5. ✅ Verify frontend works
6. ✅ Delete Railway service

---

## 🌐 Alternative: Fly.io

If Render doesn't work, try Fly.io:

```bash
# Install
brew install flyctl

# Login
flyctl auth login

# Deploy
flyctl launch
```

Fly.io is faster but slightly more complex.

---

## Need Help?

- Render Docs: https://render.com/docs
- Render Discord: https://render.com/discord
- Or ping me here! 😊
