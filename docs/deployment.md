# 🚀 Deployment Guide - Besplatno Hostovanje

Ovaj vodič će te provesti kroz proces deployovanja **BosniaAir** aplikacije na besplatne servise.

---

## 📋 Deploy Plan

| Komponenta | Servis | Cijena | Limitacije |
|------------|--------|--------|------------|
| **Database** | Supabase | Besplatno | 500MB storage, 2 CPU hours |
| **Backend API** | Railway | Besplatno | $5 mjesečno, 512MB RAM |
| **Frontend** | Vercel | Besplatno | 100GB bandwidth, 6000 build minutes |

---

## Korak 1: Database Setup (Supabase)

1. **Idi na [supabase.com](https://supabase.com)**
2. **Login with GitHub**
3. **"New Project":**
   - Name: `bosniaair-db`
   - Database Password: (generiraj jak password)
   - Region: `Central EU (Frankfurt)`
4. **Čekaj 2-3 minuta da se kreira**
5. **Settings → Database → Connection String:**
   - Kopiraj `URI` connection string
   - Format: `postgresql://postgres:[YOUR-PASSWORD]@db.[ID].supabase.co:5432/postgres`

---

## Korak 2: Backend Deploy (Railway)

1. **Idi na [railway.app](https://railway.app)**
2. **Login with GitHub**
3. **"New Project" → "Deploy from GitHub repo"**
4. **Odaberi `sarajevoairvibe` repo**
5. **U "Variables" dodaj:**
   ```
   DATABASE_URL=postgresql://postgres:[PASSWORD]@db.[ID].supabase.co:5432/postgres
   WAQI_API_TOKEN=your_waqi_token_here
   ASPNETCORE_ENVIRONMENT=Production
   ASPNETCORE_URLS=http://0.0.0.0:8080
   ```
6. **Deploy će biti na:** `https://sarajevoairvibe-production.up.railway.app`

---

## Korak 3: Frontend Deploy (Vercel)

1. **Push kod na GitHub** (ako nisi već)
2. **Idi na [vercel.com](https://vercel.com)**
3. **Login with GitHub**
4. **"New Project" → Import `sarajevoairvibe`**
5. **Framework Preset:** Next.js
6. **Root Directory:** `frontend`
7. **Environment Variables:**
   ```
   NEXT_PUBLIC_API_URL=https://sarajevoairvibe-production.up.railway.app
   ```
8. **Deploy!**

---

## Korak 4: Database Migration

Ako imaš postojeće SQLite podatke:

1. **Export podatke:**
   ```bash
   ./export_data.sh
   ```

2. **Import u Supabase:**
   - Idi u Supabase Dashboard
   - SQL Editor → New Query
   - Pokreni migration:
   ```sql
   -- Kreiraj tabelu (automatski će Railway kreirati)
   -- Zatim import podatke:
   -- Upload CSV kroz Supabase UI ili koristi psql
   ```

---

## ✅ Provjera Deploya

1. **Backend Health Check:**
   ```
   https://your-railway-app.railway.app/health
   ```
   Trebao bi vratiti "Healthy"

2. **Frontend Test:**
   ```
   https://your-app.vercel.app
   ```
   Trebao bi se učitati i prikazati podatke

3. **API Test:**
   ```
   https://your-railway-app.railway.app/api/v1/air-quality/Sarajevo/live
   ```

---

## 🔧 Environment Variables Summary

### Railway (Backend)
```
DATABASE_URL=postgresql://postgres:[PASSWORD]@db.[ID].supabase.co:5432/postgres
WAQI_API_TOKEN=your_waqi_token
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8080
FRONTEND_ORIGIN=https://your-app.vercel.app
```

### Vercel (Frontend)
```
NEXT_PUBLIC_API_URL=https://your-railway-app.railway.app
```

---

## 💰 Troškovi

- **Supabase:** $0/mjesec (do 500MB)
- **Railway:** $0-5/mjesec (ovisno o korištenju)
- **Vercel:** $0/mjesec (do 100GB bandwidth)

**Ukupno: $0-5/mjesec** 🎉

---

## 🚨 Napomene

1. **Railway Trial:** 500 sati besplatno, zatim $5/mjesec
2. **Supabase Limit:** Ako prekoračiš 500MB, prebaci na Neon (također besplatno)
3. **Vercel Bandwidth:** 100GB mjesečno je prilično za malu aplikaciju

---

## 📞 Troubleshooting

**Problem: Railway ne builduje**
- Provjeri da li je `railway.json` u root direktoriju
- Provjeri Docker syntax u `Dockerfile.railway`

**Problem: CORS greška**
- Ažuriraj `FRONTEND_ORIGIN` na Railway sa tačnim Vercel URL-om

**Problem: Database connection**
- Provjeri da li je `DATABASE_URL` tačan u Railway varijablama
- Testuj connection string lokalno prvo

## Backend (.NET API)

Backend se može deployati kao samostalna aplikacija na bilo kojem hostingu koji podržava **.NET 8**.

### Preporučene opcije:

1.  **Kontejnerizacija (Docker):**
    - Projekt sadrži `Dockerfile` koji olakšava kreiranje Docker image-a.
    - **Koraci:**
      1.  Buildajte Docker image: `docker build -t bosniaair-api .`
      2.  Pokrenite kontejner: `docker run -p 8080:80 -e WAQI_API_TOKEN=vas_token bosniaair-api`
    - Ovaj image se može deployati na platforme kao što su **Azure Container Apps**, **AWS Fargate**, ili **DigitalOcean App Platform**.

2.  **PaaS (Platform as a Service):**
    - **Azure App Service:** Direktno objavljivanje iz Visual Studija ili putem CI/CD pipeline-a.
    - **Heroku:** Iako nije primarno za .NET, moguće je deployati koristeći Docker.

### Konfiguracija

Ključna varijabla okruženja koju trebate postaviti je `WAQI_API_TOKEN`. Također, `FRONTEND_ORIGIN` treba biti postavljen na URL vaše frontend aplikacije kako bi CORS ispravno radio.

## Frontend (Next.js)

Frontend je optimiziran za deployment na platformama koje podržavaju **Next.js**.

### Preporučene opcije:

1.  **Vercel:**
    - Kao kreatori Next.js-a, Vercel nudi najbolju podršku i performanse.
    - **Koraci:**
      1.  Povežite svoj Git repozitorij (GitHub, GitLab, Bitbucket) s Vercelom.
      2.  Vercel će automatski prepoznati Next.js projekt i konfigurirati build postavke.
      3.  Postavite varijablu okruženja `NEXT_PUBLIC_API_URL` na URL vašeg deployanog backend API-ja.
    - Deployment se automatski pokreće nakon svakog `git push`-a na glavnu granu.

2.  **Netlify:**
    - Slično Vercelu, nudi jednostavan deployment s Git-a.

3.  **Samostalno Hostiranje (Node.js server):**
    - Buildajte aplikaciju: `npm run build`
    - Pokrenite server: `npm start`
    - Ovo zahtijeva da sami upravljate Node.js serverom.

---

## CI/CD (Kontinuirana Integracija i Isporuka)

Preporučuje se postavljanje CI/CD pipeline-a (npr. pomoću **GitHub Actions**) za automatizaciju testiranja, buildanja i deploymenta obje aplikacije.
