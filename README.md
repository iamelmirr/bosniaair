# BosniaAir 🇧🇦 - Simplified Air Quality Monitoring

**BosniaAir** is a modern web application for real-time air quality monitoring across cities in Bosnia and Herzegovina. The application provides users with up-to-date information about Air Quality Index (AQI), dominant pollutants, forecasts, and health recommendations.

[![Frontend](https://img.shields.io/badge/Frontend-Next.js%2014-blue)](https://nextjs.org/)
[![Backend](https://img.shields.io/badge/Backend-.NET%208-purple)](https://dotnet.microsoft.com/)
[![Database](https://img.shields.io/badge/Database-SQLite-green)](https://www.sqlite.org/)

Built with modern technologies including **.NET 8** for the backend and **Next.js (React)** for the frontend, designed to be fast, responsive, and easy to use.

## 🌐 Live Application

**BosniaAir** is deployed and accessible at: **[https://bosniaair.vercel.app](https://bosniaair.vercel.app)**

### Deployment Stack
- **Frontend:** Deployed on [Vercel](https://vercel.com/) for optimal Next.js performance
- **Backend API:** Deployed on [Railway](https://railway.app/) for reliable .NET hosting  
- **Database:** Hosted on [Supabase](https://supabase.com/) PostgreSQL for production data storage

---

## 📋 Table of Contents

- [Live Application](#-live-application)
- [Key Features](#-key-features)
- [Technologies](#️-technologies)
- [Architecture](#️-architecture)
- [Getting Started](#-getting-started)
- [Database](#️-database)
- [API Endpoints](#-api-endpoints)
- [Contributing](#-contributing)
- [License](#-license)

---

## ✨ Key Features

- **Real-Time Monitoring:** Live AQI values for selected cities
- **Weather Forecast:** Air quality forecast for upcoming days  
- **Detailed Pollutant Data:** Display concentrations of key pollutants (PM2.5, PM10, O3, NO2, CO, SO2)
- **Health Recommendations:** Advice for sensitive groups and general population based on current AQI
- **City Comparison:** Compare air quality between different cities
- **Responsive Design:** Optimized for both desktop and mobile devices
- **Customizable Settings:** Users can select their primary city for monitoring
- **Light and Dark Theme:** Automatic system theme detection with manual override option

---

## 🛠️ Technologies

| Component | Technology | Description |
| --- | --- | --- |
| **Backend** | **.NET 8** | Robust and scalable API for data processing and delivery |
| | **ASP.NET Core** | Framework for building web APIs |
| | **Entity Framework Core** | ORM for database interactions |
| | **SQLite** | Lightweight, serverless database for local data storage |
| | **Serilog** | Flexible logging for application monitoring |
| **Frontend** | **Next.js 14** | React framework for server-rendered and static web pages |
| | **React** | Library for building user interfaces |
| | **TypeScript** | Static typing for more robust and maintainable code |
| | **Tailwind CSS** | Utility-first CSS framework for rapid and responsive design |
| | **SWR** | React Hooks library for data fetching and caching |
| **Database** | **SQLite** | Storage for historical and forecast air quality data |
| **API Source** | **WAQI API** | World Air Quality Index project for air quality data |

---

## 🏗️ Architecture

BosniaAir follows a modern **three-tier architecture** with clean separation of concerns, ensuring maintainability, testability, and scalability.

### 🎨 System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                         BROWSER                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │         Next.js Frontend (React + TypeScript)            │   │
│  │                                                          │   │
│  │  Components → Hooks → API Client → Observable Pattern   │   │
│  │                         ↓                                │   │
│  │                    SWR Cache                             │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                            ↕ HTTP/REST (JSON)
┌─────────────────────────────────────────────────────────────────┐
│                  .NET 8 Backend API                             │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │         ASP.NET Core + Entity Framework                  │   │
│  │                                                          │   │
│  │  Controllers → Services → Repository → Database         │   │
│  │                    ↓                                     │   │
│  │         Background Scheduler (Hosted Service)           │   │
│  │              Refreshes every 10 minutes                 │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                            ↕ HTTP (External API)
┌─────────────────────────────────────────────────────────────────┐
│            WAQI API (World Air Quality Index)                   │
│         https://api.waqi.info/feed/{station}                    │
└─────────────────────────────────────────────────────────────────┘
```

---

### 🔧 Backend Architecture

The backend follows **clean architecture** with clear layer separation and dependency injection:

#### **1. API Layer (Controllers)**
- **Purpose:** HTTP endpoint handling and request/response mapping
- **Location:** `backend/src/BosniaAir.Api/Controllers/`
- **Key Files:**
  - `AirQualityController.cs` - REST API endpoints (`/live`, `/forecast`, `/complete`)
- **Responsibilities:**
  - Route HTTP requests to appropriate service methods
  - Validate request parameters (city enum, cancellation tokens)
  - Transform service responses to DTOs (Data Transfer Objects)
  - Handle HTTP status codes (200 OK, 503 Service Unavailable, 500 Internal Server Error)

#### **2. Service Layer (Business Logic)**
- **Purpose:** Core business logic, external API integration, data processing
- **Location:** `backend/src/BosniaAir.Api/Services/`
- **Key Files:**
  - `AirQualityService.cs` - Main business logic implementation
  - `AirQualityScheduler.cs` - Background service for periodic data refresh
- **Responsibilities:**
  - Fetch data from WAQI external API
  - Transform raw API data into domain models
  - Coordinate between repository and external APIs
  - Execute scheduled background jobs (every 10 minutes)
  - Error handling and logging

#### **3. Repository Layer (Data Access)**
- **Purpose:** Database abstraction and data persistence
- **Location:** `backend/src/BosniaAir.Api/Repositories/`
- **Key Files:**
  - `IAirQualityRepository.cs` - Repository interface
  - `AirQualityRepository.cs` - Entity Framework implementation
- **Responsibilities:**
  - Execute database queries via Entity Framework Core
  - Abstract database operations from business logic
  - Provide clean methods: `GetLive()`, `AddLive()`, `GetForecast()`, `UpdateForecast()`
  - Handle database transactions and caching

#### **4. Data Layer (Database Context)**
- **Purpose:** ORM configuration and database schema management
- **Location:** `backend/src/BosniaAir.Api/Data/`
- **Key Files:**
  - `AppDbContext.cs` - Entity Framework DbContext
  - Database: SQLite (development) or PostgreSQL (production)
- **Schema:** Single `AirQualityRecords` table with columns for live/forecast data

#### **5. Background Service**
- **Technology:** ASP.NET Core `IHostedService`
- **Execution:** Runs continuously in background, independent of HTTP requests
- **Schedule:** Refreshes all cities every 10 minutes
- **Parallel Processing:** All cities refresh simultaneously using `Task.WhenAll()`
- **Resilience:** City-level error isolation (one city failure doesn't affect others)

#### **6. Middleware & Configuration**
- **CORS Middleware:** Configured for frontend origins with credentials support
- **Exception Handling Middleware:** Global error catching and logging
- **Serilog:** Structured logging for debugging and monitoring
- **Health Checks:** `/health` endpoint for deployment monitoring
- **Swagger/OpenAPI:** Auto-generated API documentation at `/swagger`

---

### 🎨 Frontend Architecture

The frontend is a **modern Next.js 14 application** with React Server Components and client-side interactivity:

#### **1. Component Layer**
- **Purpose:** UI presentation and user interaction
- **Location:** `frontend/components/`
- **Key Components:**
  - `LiveAqiPanel.tsx` - Real-time AQI display with color-coded badges
  - `ForecastTimeline.tsx` - 5-day forecast visualization
  - `Pollutants.tsx` - Detailed pollutant concentrations
  - `CitiesComparison.tsx` - Multi-city comparison view
  - `SensitiveGroupsAdvice.tsx` - Health recommendations
- **Patterns:**
  - Functional components with React Hooks
  - Props-based communication
  - Conditional rendering based on data state

#### **2. Hooks Layer (Data Management)**
- **Purpose:** Data fetching, caching, and state synchronization
- **Location:** `frontend/lib/hooks.ts`
- **Key Hooks:**
  - `useLiveAqi(cityId)` - Fetch live AQI data with SWR caching
  - `useComplete(cityId)` - Fetch combined live + forecast data
  - `usePeriodicRefresh(interval)` - Setup auto-refresh timer
  - `useRefreshAll()` - Manual refresh trigger
- **Technology:** Built on top of **SWR** (stale-while-revalidate pattern)
- **Features:**
  - Automatic caching with unique keys per city
  - Background revalidation
  - Request deduplication
  - Observable integration for coordinated refresh

#### **3. API Client Layer**
- **Purpose:** HTTP communication with backend API
- **Location:** `frontend/lib/api-client.ts`
- **Key Methods:**
  - `getLive(city)` - GET `/api/v1/air-quality/{city}/live`
  - `getComplete(city)` - GET `/api/v1/air-quality/{city}/complete`
- **Features:**
  - Type-safe TypeScript interfaces
  - Automatic date string → Date object conversion
  - Error handling and retry logic
  - Environment-based API URL configuration

#### **4. Observable Pattern (Event Coordination)**
- **Purpose:** Coordinate data refresh across multiple components
- **Location:** `frontend/lib/observable.ts`
- **How It Works:**
  - Singleton `AirQualityObservable` instance
  - Components subscribe to refresh events
  - Timer triggers `notify()` every 60 seconds
  - All subscribers execute simultaneously
  - Automatic timer management (starts with first subscriber, stops when all unsubscribe)
- **Benefits:**
  - Single timer for entire app (efficient)
  - Coordinated refresh (all components update together)
  - Decoupled architecture (components don't know about each other)

#### **5. State Management**
- **Local State:** React `useState` for component-specific data
- **Persistent State:** `localStorage` for user preferences (selected city)
- **Server State:** SWR cache for API data
- **Global Events:** Observable pattern for cross-component coordination

---

### 🔄 Complete Data Flow

#### **Scenario 1: Background Data Refresh (Backend)**

```
Every 10 minutes:

AirQualityScheduler (Background Service)
    ↓
ExecuteAsync() triggers RunRefreshCycle()
    ↓
For each city (Sarajevo, Tuzla, Mostar, etc.):
    ↓
AirQualityService.RefreshCity(city)
    ↓
HTTP GET → https://api.waqi.info/feed/@{stationId}/?token=xxx
    ↓
Parse JSON response (AQI, pollutants, forecast)
    ↓
Transform to AirQualityRecord entity
    ↓
Repository.AddLive(record) → INSERT INTO database
    ↓
Repository.UpdateForecast(forecast) → UPDATE/INSERT forecast
    ↓
Data ready for frontend requests ✓
```

**Key Points:**
- Runs **independently** of frontend (even if no users online)
- **Parallel processing** - all cities refresh simultaneously
- **Error isolation** - one city failure doesn't stop others
- **Database caching** - fresh data ready for instant API responses

---

#### **Scenario 2: User Opens Application (Frontend)**

```
1. Browser loads page
    ↓
2. HomePage component mounts
    ↓
3. Read city from localStorage
    If empty → Show city selector modal
    If exists → Continue with saved city
    ↓
4. useLiveAqi('Sarajevo') executes
    ↓
5. SWR checks cache
    Cache MISS → Trigger fetch
    ↓
6. apiClient.getLive('Sarajevo')
    ↓
7. HTTP GET → http://localhost:5000/api/v1/air-quality/Sarajevo/live
    ↓
8. Backend: Controller → Service → Repository
    ↓
9. SQL: SELECT * FROM AirQualityRecords 
        WHERE City='Sarajevo' AND RecordType='LiveSnapshot'
        ORDER BY Timestamp DESC LIMIT 1
    ↓
10. Backend returns JSON response
    ↓
11. Frontend: Parse JSON, convert dates
    ↓
12. SWR updates cache
    ↓
13. React re-renders with fresh data
    ↓
14. UI displays AQI, pollutants, forecast ✓
```

**Key Points:**
- **SWR caching** prevents duplicate requests
- **Optimistic UI** - shows loading state while fetching
- **Background revalidation** - automatically refreshes stale data
- **Request deduplication** - multiple components share one request

---

#### **Scenario 3: Periodic Auto-Refresh (Frontend)**

```
Every 60 seconds:

Observable timer fires
    ↓
airQualityObservable.notify()
    ↓
EventTarget dispatches 'aqi-refresh' event
    ↓
All subscribed components receive event:
    - LiveAqiPanel → mutate()
    - ForecastTimeline → mutate()
    - CitiesComparison → mutate()
    ↓
Each component's SWR hook revalidates:
    - Check cache freshness
    - If stale → Fetch new data from backend
    - If fresh → Use cached data
    ↓
Components re-render with latest data ✓
```

**Key Points:**
- **Single timer** for entire app (60s interval)
- **Coordinated refresh** - all components update together
- **Smart caching** - SWR decides when to actually fetch
- **Automatic cleanup** - timer stops when no subscribers

---

### 🔐 Key Design Patterns

1. **Repository Pattern** - Abstracts data access, makes testing easier
2. **Dependency Injection** - All services injected via ASP.NET Core DI container
3. **Observer Pattern** - Frontend Observable for event-driven refresh
4. **Cache-Aside Pattern** - Backend caches WAQI data, frontend caches API responses
5. **Background Service Pattern** - Scheduled tasks independent of HTTP requests
6. **Singleton Pattern** - Single Observable instance shared across components

---

### 📊 Performance Optimizations

- **Backend:**
  - Database caching reduces external API calls
  - Parallel city refresh (all cities update simultaneously)
  - Scoped services prevent memory leaks in background service
  - Entity Framework query optimization with `AsNoTracking()`

- **Frontend:**
  - SWR automatic caching and deduplication
  - Observable pattern reduces timer overhead (single timer)
  - React Hooks prevent unnecessary re-renders
  - Code splitting and lazy loading with Next.js

---

## 🚀 Getting Started

To run the project locally, you need to set up and run both backend and frontend separately.

### Prerequisites

- **.NET 8 SDK:** [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js (v18+):** [Download here](https://nodejs.org/)
- **Git:** [Download here](https://git-scm.com/)
- **API Key:** Free API key required from [WAQI API](https://aqicn.org/data-platform/token/)

### Backend Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/iamelmirr/sarajevoairvibe.git
   cd sarajevoairvibe
   ```

2. **Set up API key:**
   - Create a `.env` file in the `backend/src/BosniaAir.Api/` directory
   - Add your WAQI API key using the configuration format:
     ```
     Aqicn__ApiToken=your_api_key_here
     ```
   - Alternatively, you can set it as an environment variable (overrides config):
     ```bash
     export WAQI_API_TOKEN=your_api_key_here
     ```

3. **Run the backend server:**
   ```bash
   cd backend/src/BosniaAir.Api
   dotnet run
   ```
   - Backend API will be available at `http://localhost:5000`
   - You can check the status at `http://localhost:5000/health`
   - API documentation available at `http://localhost:5000/swagger`

### Frontend Setup

1. **Install dependencies:**
   ```bash
   cd frontend
   npm install
   ```

2. **Run the frontend application:**
   ```bash
   npm run dev
   ```
   - Frontend application will be available at `http://localhost:3000`

Now you should have a fully functional application running locally!

---

## 🗃️ Database

The project uses **SQLite** for local data storage, making setup and usage easy without requiring an external database server. The database file is located at `bosniaair-aqi.db` in the `backend/src/BosniaAir.Api/` directory.

### Schema

The main table is `AirQualityRecords` and contains the following important columns:

| Column | Type | Description |
| --- | --- | --- |
| `Id` | INTEGER | Primary key |
| `City` | TEXT | City name (e.g., `Sarajevo`) |
| `RecordType` | TEXT | Record type (`LiveSnapshot` or `Forecast`) |
| `StationId` | TEXT | Station identifier |
| `Timestamp` | TEXT | Measurement time in UTC format |
| `AqiValue` | INTEGER | Overall AQI value |
| `DominantPollutant` | TEXT | Dominant pollutant |
| `Pm25`, `Pm10`, etc. | REAL | Individual pollutant values |
| `ForecastJson` | TEXT | JSON string containing forecast data |
| `CreatedAt` | TEXT | Record creation time |

### Useful Queries

You can use any SQLite browser or CLI to execute queries on the database.

**Get latest live measurements for all cities:**
```sql
SELECT City, AqiValue, DominantPollutant, MAX(Timestamp) as Timestamp 
FROM AirQualityRecords 
WHERE RecordType = 'LiveSnapshot' 
GROUP BY City 
ORDER BY City;
```

**Get latest forecast for all cities:**
```sql
SELECT City, ForecastJson, Timestamp 
FROM AirQualityRecords 
WHERE RecordType = 'Forecast' 
ORDER BY City, Timestamp DESC;
```

**Get 5 most recent live measurements:**
```sql
SELECT City, AqiValue, DominantPollutant, Timestamp 
FROM AirQualityRecords 
WHERE RecordType = 'LiveSnapshot' 
ORDER BY Timestamp DESC 
LIMIT 5;
```

---

## 🔌 API Endpoints

The backend provides the following REST API endpoints:

### Live Data
- `GET /api/v1/air-quality/{city}/live` - Get current live AQI data for a specific city
- `GET /health` - Health check endpoint

### Forecast Data  
- `GET /api/v1/air-quality/{city}/forecast` - Get 5-day forecast for a specific city

### Combined Data
- `GET /api/v1/air-quality/{city}/complete` - Get both live and forecast data for a city

### Available Cities
Current supported cities: `Sarajevo`, `Tuzla`, `Banja Luka`, `Mostar`, `Zenica`, `Bihac`, `Travnik`

### Example Usage
```bash
# Get live data for Sarajevo
curl http://localhost:5000/api/v1/air-quality/Sarajevo/live

# Get forecast for Tuzla
curl http://localhost:5000/api/v1/air-quality/Tuzla/forecast

# Check API health
curl http://localhost:5000/health
```

---

## 🧪 Testing

Run the backend tests:
```bash
cd backend
dotnet test
```

---

## 🤝 Contributing

Contributions are welcome! If you have suggestions for improvements or want to report a bug, feel free to open an issue or submit a pull request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the **MIT License**. See the `LICENSE` file for more details.

---

## 📚 Additional Documentation

For more detailed technical information, see:
- **[COMPLETE_FLOW_DOCUMENTATION.md](./COMPLETE_FLOW_DOCUMENTATION.md)** - In-depth flow documentation with timelines and scenarios
- **[APIREADME.md](./APIREADME.md)** - Detailed API endpoint documentation
- **[CSHARP_FOR_REACT_DEVS.md](./CSHARP_FOR_REACT_DEVS.md)** - C# guide for React developers

---

**Built with ❤️ for Bosnia and Herzegovina**

