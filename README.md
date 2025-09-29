# BosniaAir 🇧🇦 – Simplified Air Quality Monitoring

**BosniaAir** is a modern web application for real-time air quality monitoring across cities in Bosnia and Herzegovina. The application provides users with up-to-date information about Air Quality Index (AQI), dominant pollutants, forecasts, and health recommendations.

[![Frontend](https://img.shields.io/badge/Frontend-Next.js%2014-blue)](https://nextjs.org/)
[![Backend](https://img.shields.io/badge/Backend-.NET%208-purple)](https://dotnet.microsoft.com/)
[![Database](https://img.shields.io/badge/Database-SQLite-green)](https://www.sqlite.org/)

Built with modern technologies including **.NET 8** for the backend and **Next.js (React)** for the frontend, designed to be fast, responsive, and easy to use.

---

## 📋 Table of Contents

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

### Backend

The backend is built using clean architecture principles, separating logic into different layers:

- **API Layer (`BosniaAir.Api`):** Contains controllers, DTOs (Data Transfer Objects), and middleware. Responsible for receiving HTTP requests and sending responses.
- **Service Layer:** Contains business logic, such as fetching data from external APIs, data transformation, and caching.
- **Repository Layer:** Abstracts data access, enabling communication with the database (SQLite) via Entity Framework Core.
- **Entity Layer:** Contains domain models representing the basic data structures.

### Frontend

The frontend is a modern **Next.js** application that uses **React Hooks** for state management and data fetching.

- **Components:** UI is divided into reactive components (`LiveAqiPanel`, `ForecastTimeline`, `Pollutants`, etc.)
- **Data Fetching:** Uses **SWR** (`stale-while-revalidate`) for efficient caching and updating data from the backend API
- **Styling:** **Tailwind CSS** is used for fast and consistent design with dark theme support
- **State:** Local state is managed using `useState` and `useEffect`, while global settings (e.g., selected city) are stored in `localStorage`

### Data Flow

1. **Background Service:** Periodically (every 10 minutes) fetches fresh data from **WAQI API**
2. **Database Storage:** Fetched data (live and forecast) is processed and stored in **SQLite** database
3. **Frontend Request:** When user opens the application, frontend sends request to backend API
4. **API Response:** Backend retrieves latest data from its database and sends it to frontend in optimized format
5. **UI Display:** Frontend receives data and displays it to user through various components. **SWR** automatically updates data in the background

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
   - In the root directory of the project (`sarajevoairvibe`), create a `.env` file
   - Add your WAQI API key to the `.env` file:
     ```
     WAQI_API_TOKEN=your_api_key_here
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

The project uses **SQLite** for local data storage, making setup and usage easy without requiring an external database server. The database file is located at `bosniaair-aqi.db` in the root directory.

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

[![FSnapshots of Sarajevo's AQI are stored in `bosniaair-aqi.db` (auto-created). The hosted worker keeps that table up to date every 10 minutes.ontend](https://img.shields.io/badge/Frontend-Next.js%2014-blue)](https://nextjs.org/)
