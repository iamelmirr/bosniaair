# API Documentation

This document describes the key endpoints available through the BosniaAir API and provides information about the underlying WAQI (World Air Quality Index) data source.

**Base URL:** `http://localhost:5000/api/v1`

---

## BosniaAir API Endpoints

### 1. Get Complete AQI Data

Retrieves both live and forecast data for a specific city in a single request. This is the primary endpoint used by the frontend application.

- **URL:** `/air-quality/{city}/complete`
- **Method:** `GET`
- **Parameters:**
  - `city` (string, required): City identifier (e.g., `Sarajevo`, `Tuzla`, `Zenica`, `Mostar`, `BanjaLuka`).
- **Success Response (200 OK):**
  ```json
  {
    "liveData": {
      "city": "Sarajevo",
      "overallAqi": 70,
      "aqiCategory": "Moderate",
      "color": "#FFFF00",
      "healthMessage": "Air quality is acceptable for most people. However, for some pollutants there may be a moderate health concern for a very small number of people who are unusually sensitive to air pollution.",
      "timestamp": "2025-09-29T09:00:00.000000Z",
      "measurements": [
        {
          "id": "196_pm2.5",
          "city": "Sarajevo",
          "locationName": "Sarajevo",
          "parameter": "PM2.5",
          "value": 70,
          "unit": "μg/m³",
          "timestamp": "2025-09-29T09:00:00.000000Z",
          "sourceName": "WAQI",
          "coordinates": null,
          "averagingPeriod": null
        }
      ],
      "dominantPollutant": "PM2.5"
    },
    "forecastData": {
      "city": "Sarajevo",
      "forecast": [
        {
          "date": "2025-09-29",
          "aqi": 97,
          "category": "Moderate",
          "color": "#FFFF00",
          "pollutants": {
            "pm25": {
              "avg": 97,
              "min": 55,
              "max": 152
            },
            "pm10": {
              "avg": 34,
              "min": 15,
              "max": 54
            },
            "o3": {
              "avg": 8,
              "min": 2,
              "max": 17
            }
          }
        }
      ],
      "timestamp": "2025-09-29T09:00:00.000000Z"
    },
    "retrievedAt": "2025-09-29T10:57:10.124531Z"
  }
  ```
- **Error (404 Not Found):** When data for the requested city is not available.

### 2. Get Live AQI Data

Retrieves only real-time air quality data.

- **URL:** `/air-quality/{city}/live`
- **Method:** `GET`
- **Success Response (200 OK):** Returns the `liveData` object as shown above.

### 3. Get Forecast Data

Retrieves only air quality forecast data.

- **URL:** `/air-quality/{city}/forecast`
- **Method:** `GET`
- **Success Response (200 OK):** Returns the `forecastData` object as shown above.

### 4. Health Check

Checks the API status and availability.

- **URL:** `/health`
- **Method:** `GET`
- **Success Response (200 OK):**
  ```
  Healthy
  ```

---

## WAQI (World Air Quality Index) API Integration

### About WAQI

The World Air Quality Index (WAQI) project is a global air quality monitoring platform that provides real-time air pollution data from over 12,000 monitoring stations worldwide. Our application integrates with WAQI to fetch authoritative air quality data for Bosnian cities.

**WAQI Website:** https://waqi.info/  
**API Documentation:** https://aqicn.org/json-api/doc/

### How WAQI API Works

The WAQI API provides structured air quality data including:
- **Real-time measurements** for various pollutants (PM2.5, PM10, O3, NO2, SO2, CO)
- **AQI calculations** based on EPA standards
- **Forecast data** for up to 6 days
- **Geographic information** for monitoring stations

### WAQI API Response Structure

When we query WAQI for a city (e.g., `https://api.waqi.info/feed/sarajevo/?token=YOUR_TOKEN`), the API returns:

```json
{
  "status": "ok",
  "data": {
    "aqi": 70,
    "idx": 196,
    "city": {
      "geo": [43.8476, 18.3564],
      "name": "Sarajevo, Federation of Bosnia and Herzegovina, Bosnia and Herzegovina",
      "url": "https://aqicn.org/city/bosnia-and-herzegovina/sarajevo"
    },
    "dominentpol": "pm25",
    "iaqi": {
      "co": { "v": 1.5 },
      "no2": { "v": 11.9 },
      "o3": { "v": 11.4 },
      "pm10": { "v": 20 },
      "pm25": { "v": 70 },
      "so2": { "v": 14.2 }
    },
    "time": {
      "s": "2025-09-29 09:00:00",
      "tz": "+02:00",
      "v": 1727596800,
      "iso": "2025-09-29T09:00:00+02:00"
    },
    "forecast": {
      "daily": {
        "pm25": [
          {
            "avg": 97,
            "day": "2025-09-29",
            "max": 152,
            "min": 55
          }
        ],
        "pm10": [
          {
            "avg": 34,
            "day": "2025-09-29",
            "max": 54,
            "min": 15
          }
        ],
        "o3": [
          {
            "avg": 8,
            "day": "2025-09-29",
            "max": 17,
            "min": 2
          }
        ]
      }
    }
  }
}
```

### Key WAQI Response Fields

| Field | Description |
|-------|-------------|
| `status` | API call status (`"ok"` or `"error"`) |
| `data.aqi` | Overall Air Quality Index (0-500+) |
| `data.dominentpol` | Primary pollutant driving the AQI |
| `data.iaqi` | Individual Air Quality Index for each pollutant |
| `data.time` | Measurement timestamp in multiple formats |
| `data.forecast.daily` | 6-day forecast data by pollutant type |
| `data.city.geo` | Station coordinates [latitude, longitude] |

### Supported Cities and Station IDs

| City | WAQI Station ID | Population |
|------|----------------|------------|
| Sarajevo | 196 | ~400,000 |
| Tuzla | 197 | ~120,000 |
| Zenica | 198 | ~115,000 |
| Mostar | 199 | ~105,000 |
| Banja Luka | 200 | ~185,000 |

---

## Data Models

### `LiveAqiResponse`

| Field | Type | Description |
|-------|------|-------------|
| `city` | string | City name |
| `overallAqi` | number | Primary AQI value (0-500+) |
| `aqiCategory` | string | Air quality category (`Good`, `Moderate`, `Unhealthy for Sensitive Groups`, `Unhealthy`, `Very Unhealthy`, `Hazardous`) |
| `color` | string | Hex color code for UI visualization |
| `healthMessage` | string | Health recommendation based on AQI level |
| `timestamp` | DateTime | Measurement timestamp (ISO 8601) |
| `measurements` | `Measurement[]` | Array of individual pollutant measurements |
| `dominantPollutant` | string | Primary pollutant affecting AQI |

### `Measurement`

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique measurement identifier |
| `parameter` | string | Pollutant type (`PM2.5`, `PM10`, `O3`, `NO2`, `SO2`, `CO`) |
| `value` | number | Measured concentration |
| `unit` | string | Measurement unit (`μg/m³` or `mg/m³`) |
| `timestamp` | DateTime | When measurement was taken |
| `sourceName` | string | Data source (`WAQI`) |

### `ForecastResponse`

| Field | Type | Description |
|-------|------|-------------|
| `city` | string | City name |
| `forecast` | `ForecastDay[]` | Array of daily forecasts (up to 6 days) |
| `timestamp` | DateTime | When forecast was generated |

### `ForecastDay`

| Field | Type | Description |
|-------|------|-------------|
| `date` | string | Forecast date (YYYY-MM-DD) |
| `aqi` | number | Predicted AQI value |
| `category` | string | Predicted air quality category |
| `color` | string | Hex color for visualization |
| `pollutants` | object | Forecast details by pollutant type |

---

## AQI Scale and Health Messages

| AQI Range | Category | Color | Health Implications |
|-----------|----------|-------|-------------------|
| 0-50 | Good | Green (#00E400) | Air quality is satisfactory |
| 51-100 | Moderate | Yellow (#FFFF00) | Acceptable for most people |
| 101-150 | Unhealthy for Sensitive Groups | Orange (#FF7E00) | Sensitive individuals may experience problems |
| 151-200 | Unhealthy | Red (#FF0000) | Everyone may experience health effects |
| 201-300 | Very Unhealthy | Purple (#8F3F97) | Health alert, everyone may experience serious effects |
| 301+ | Hazardous | Maroon (#7E0023) | Emergency conditions, entire population affected |

---

## Error Handling

### Common Error Responses

**404 Not Found - City Data Unavailable:**
```json
{
  "error": "No data available for the requested city",
  "city": "InvalidCity",
  "timestamp": "2025-09-29T10:57:10Z"
}
```

**500 Internal Server Error - WAQI API Issues:**
```json
{
  "error": "Failed to fetch data from WAQI API",
  "details": "Invalid API token or rate limit exceeded"
}
```

**503 Service Unavailable - Database Issues:**
```json
{
  "error": "Temporary service unavailability",
  "message": "Database connection failed, please try again later"
}
```

---

## Rate Limiting and Caching

- **WAQI API Rate Limit:** 1,000 requests per API key per day
- **Internal Caching:** Data is refreshed every hour to minimize API calls
- **Database Storage:** Historical data is stored locally for trend analysis
- **Frontend Caching:** Client-side caching reduces server load

---

## Configuration

### Environment Variables and Settings

The application uses a hierarchical configuration system that supports both configuration files and environment variables:

| Configuration Method | Format | Use Case | Priority |
|---------------------|---------|-----------|----------|
| Configuration file | `Aqicn__ApiToken=token` | Local development | Low |
| Environment variable | `WAQI_API_TOKEN=token` | Production deployment | High |
| appsettings.json | `"Aqicn": {"ApiToken": "token"}` | Not recommended | Lowest |

**Configuration file example (.env):**
```bash
# WAQI API Configuration
Aqicn__ApiToken=4017a1c616179160829bd7e3abb9cc9c8449958e

# Frontend Configuration  
FRONTEND_ORIGIN=http://localhost:3000

# Application Settings
ASPNETCORE_ENVIRONMENT=Development
```

**Environment variables (production):**
```bash
export WAQI_API_TOKEN=4017a1c616179160829bd7e3abb9cc9c8449958e
export FRONTEND_ORIGIN=https://your-app.vercel.app
export ASPNETCORE_ENVIRONMENT=Production
```

---

## Authentication

### WAQI API Token

To use this API, you need a valid WAQI API token:

1. Register at [WAQI Data Platform](https://aqicn.org/data-platform/register/)
2. Generate an API token
3. Configure the token using one of these methods:

**Method 1: Configuration file (local development)**
```bash
# Create or update .env file in backend/src/BosniaAir.Api/
Aqicn__ApiToken=your_actual_token_here
```

**Method 2: Environment variable (production)**
```bash
# Set environment variable (overrides configuration file)
export WAQI_API_TOKEN=your_actual_token_here
```

**Important:** Keep your API token secure and never commit it to version control.