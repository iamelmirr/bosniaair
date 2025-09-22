# SarajevoAir - Air Quality Monitoring System

[![CI](https://github.com/yourusername/sarajevoair/actions/workflows/ci.yml/badge.svg)](https://github.com/yourusername/sarajevoair/actions/workflows/ci.yml)
[![Frontend](https://img.shields.io/badge/Frontend-Next.js%2014-blue)](https://nextjs.org/)
[![Backend](https://img.shields.io/badge/Backend-.NET%208-purple)](https://dotnet.microsoft.com/)
[![Database](https://img.shields.io/badge/Database-PostgreSQL-blue)](https://postgresql.org/)

A comprehensive full-stack application for real-time air quality monitoring in Sarajevo and Bosnia & Herzegovina, featuring data from OpenAQ with a modern web interface and robust backend API.

## 🌟 Overview

SarajevoAir provides real-time air quality monitoring with:
- **Live AQI data** from OpenAQ API v3  
- **Clean, modern interface** with dark/light themes
- **Health recommendations** for different user groups
- **Historical data analysis** with charts and trends
- **Multi-city comparison** functionality
- **Mobile-first responsive design**
- **Production-ready deployment** with Docker
- Health dashboard (Sportisti, Djeca, Stariji, Astmatičari)
- Compare mode (Sarajevo vs Tuzla/Mostar/Banja Luka/Zenica)
- Share (Web Share API + clipboard fallback)
- Map with sensor markers (Leaflet)
- Offline snapshot fallback (localStorage)
- Accessible, responsive, dark/light

## 📸 Screenshots

### 🖥️ Desktop Interface

<div align="center">

| Main Dashboard | History Charts |
|:---:|:---:|
| ![Desktop Dashboard](docs/images/desktop-dashboard.png) | ![History Charts](docs/images/history-charts.png) |
| *Live AQI monitoring with pollutant breakdown* | *Interactive time-series charts with EPA color coding* |

| City Comparison | Health Groups |
|:---:|:---:|
| ![City Comparison](docs/images/city-comparison.png) | ![Health Groups](docs/images/health-groups.png) |
| *Multi-city AQI comparison interface* | *Health-based air quality recommendations* |

</div>

### 📱 Mobile Interface

<div align="center">

| Mobile Dashboard | Mobile Dark Theme |
|:---:|:---:|
| ![Mobile Dashboard](docs/images/mobile-dashboard.png) | ![Mobile Dark](docs/images/mobile-dark.png) |
| *Mobile-first responsive design* | *Dark theme with accessibility features* |

</div>

### ✨ Key Features Showcase

- **🎨 Modern UI**: Clean, professional interface with EPA-compliant color coding
- **🌙 Theme Support**: Automatic dark/light mode based on system preferences
- **📊 Interactive Charts**: Time-series visualization with Chart.js integration
- **🏢 Multi-City**: Compare air quality across different cities
- **💡 Health Guidance**: Personalized recommendations for sensitive groups
- **📱 Mobile-First**: Fully responsive design optimized for all devices
- **⚡ Real-time**: Live data updates with efficient caching
- **🔍 Accessibility**: WCAG compliant with semantic HTML and ARIA labels

> **Note**: Screenshots will be added once the application is deployed. The interface follows EPA air quality standards and modern web design principles.

## Tech Stack
- **Frontend**: Next.js (App Router), TypeScript, Tailwind CSS, SWR, Chart.js, Leaflet, Framer Motion
- **Backend**: ASP.NET Core (.NET 8), EF Core (PostgreSQL), IHostedService worker, Serilog, Swagger, HealthChecks, Polly  
- **Database**: PostgreSQL
- **Deployment**: Vercel (frontend), Render (backend)

## Architecture

```
Frontend (Next.js TS) ⟷ REST API (ASP.NET Core) ⟷ Background Worker (IHostedService) → OpenAQ (X-API-Key)
Backend ↔ PostgreSQL (measurements + daily aggregates)
```

Frontend uses SWR/React Query for AJAX, Web Share API for sharing, Leaflet for map/heatmap, Chart.js for graphs.

## Quickstart (Local)

### Prerequisites
- Docker & Docker Compose
- Node.js 20+ with pnpm
- .NET 8 SDK
- OpenAQ API Key (https://explore.openaq.org)

### 1. Environment Setup
```bash
git clone https://github.com/yourusername/sarajevoair.git
cd sarajevoair
cp .env.example .env
# Edit .env and set your OPENAQ_API_KEY
```

### 2. Start Database + API
```bash
docker-compose up --build
```

### 3. Start Frontend (separate terminal)
```bash
cd frontend
pnpm install
pnpm dev
```

### 4. Access Applications
- **Frontend**: http://localhost:3000
- **API Documentation**: http://localhost:5000/swagger
- **Health Check**: http://localhost:5000/health

## Environment Variables

### Root `.env`
```bash
OPENAQ_API_KEY=your_api_key_here
FRONTEND_ORIGIN=http://localhost:3000
CONNECTION_STRING=Host=localhost;Database=sarajevoair;Username=dev;Password=dev
```

### Frontend `.env.local`
```bash
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000
```

## Data Model

### Database Schema
- **locations**: sensor locations (id, name, lat, lon, source, external_id)
- **measurements**: time-series data (location_id, timestamp_utc, pm25, pm10, o3, no2, so2, co, computed_aqi, aqi_category)
- **daily_aggregates**: daily summaries (location_id, date, avg/max values per pollutant)

All timestamps stored in UTC. Optimized indices for fast time-series queries.

## AQI Calculation (EPA Method)

Calculates Air Quality Index using EPA breakpoint tables:
- **Subindex** per pollutant using breakpoint interpolation
- **Final AQI** = max(subindices)
- **Categories**: Good (0-50), Moderate (51-100), USG (101-150), Unhealthy (151-200), Very Unhealthy (201-300), Hazardous (301-500)

Configurable breakpoints in `backend/src/SarajevoAir.Domain/Aqi/Breakpoints.json`. Unit tests cover edge cases.

## OpenAQ Integration

Background worker fetches data every 10 minutes:
- Uses OpenAQ API v3 with `X-API-Key` header
- Finds locations near Sarajevo (coordinates + radius or city filter)
- Fetches latest measurements from sensors
- Handles pagination, deduplication, and rate limiting
- Transforms and stores in PostgreSQL

Key endpoints:
- `GET /v3/locations?coordinates=43.8563,18.4131&radius=50000`
- `GET /v3/sensors/{id}/measurements`

## API Endpoints

Base URL: `/api/v1`

- `GET /live?city=Sarajevo` - Current AQI and pollutant levels
- `GET /history?city=Sarajevo&days=7&resolution=hour|day` - Historical data  
- `GET /compare?cities=Sarajevo,Tuzla,Mostar` - Multi-city comparison
- `GET /locations?city=Sarajevo` - Sensor locations for map
- `GET /groups` - Health recommendations by user group
- `POST /share` - Generate shareable content

## Security & Performance

- **CORS**: Restricted to frontend origin
- **Rate Limiting**: Per-endpoint limits
- **Caching**: In-memory cache for live data (1-5 min), history data (10-30 min)
- **Input Validation**: Query parameter validation and sanitization
- **Health Checks**: Database connectivity monitoring

## Testing

### Backend
```bash
cd backend
dotnet test
```

### Frontend  
```bash
cd frontend
pnpm test
```

Unit tests cover:
- AQI calculation breakpoints and edge cases
- OpenAQ API response parsing
- Measurement deduplication logic
- API endpoint contracts

## Deployment

### Frontend (Vercel)
1. Import repository to Vercel
2. Set environment variable: `NEXT_PUBLIC_API_BASE_URL=https://your-api.render.com`
3. Deploy

### Backend (Render)
1. Create new Web Service from repository
2. Set build source: `backend/src/SarajevoAir.Api/Dockerfile`  
3. Set environment variables:
   - `DATABASE_URL` (managed PostgreSQL)
   - `OPENAQ_API_KEY`
   - `FRONTEND_ORIGIN=https://your-app.vercel.app`
4. Set health check path: `/health`

### Database
Use managed PostgreSQL (Render PostgreSQL, ElephantSQL, or similar).

## Development

### Project Structure
```
/
├── README.md, LICENSE, .gitignore
├── docker-compose.yml, .env.example
├── .github/workflows/ci.yml
├── frontend/                 # Next.js app
│   ├── app/                 # App Router pages
│   ├── components/          # React components  
│   ├── lib/                 # Utilities, hooks, API client
│   └── styles/              # Theme CSS
└── backend/                 # ASP.NET Core solution
    ├── src/
    │   ├── SarajevoAir.Api/           # Web API project
    │   ├── SarajevoAir.Application/   # Business logic
    │   ├── SarajevoAir.Domain/        # Domain models
    │   ├── SarajevoAir.Infrastructure/# Data access
    │   └── SarajevoAir.Worker/        # Background services
    └── tests/
        └── SarajevoAir.Tests/         # Unit tests
```

### Code Quality
- **Backend**: SOLID principles, clean architecture, centralized error handling (ProblemDetails), structured logging (Serilog)
- **Frontend**: TypeScript strict mode, ESLint + Prettier, mobile-first responsive design
- **Accessibility**: ARIA labels, keyboard navigation, WCAG 2.1 AA compliance

## Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)  
5. Open Pull Request

## License

MIT License - see [LICENSE](LICENSE) file for details.

## Acknowledgments

- [OpenAQ](https://openaq.org) for air quality data
- [EPA](https://airnow.gov) for AQI calculation standards
- Community contributors and testers

---

**Live Demo**: [https://sarajevoair.vercel.app](https://sarajevoair.vercel.app) *(coming soon)*