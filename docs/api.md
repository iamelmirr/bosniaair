# 🔌 SarajevoAir API Documentation

The SarajevoAir API provides comprehensive access to air quality data with RESTful endpoints, real-time monitoring capabilities, and standardized AQI calculations following EPA guidelines.

## 🌐 Base URLs

- **Development**: `http://localhost:5000/api`
- **Production**: `https://sarajevoair-api.onrender.com/api`

## 🔐 Authentication

Currently, the API is open and does not require authentication. Rate limiting may apply based on usage patterns.

## 📋 API Overview

### Core Endpoints
- **Live Data**: Current air quality readings
- **Historical Data**: Time-series data with aggregations
- **Locations**: Supported cities and monitoring stations
- **Health**: System status and health checks

---

## 🏙️ Locations

### Get Supported Cities
```http
GET /api/locations/cities
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "sarajevo",
      "name": "Sarajevo",
      "country": "BA",
      "coordinates": {
        "latitude": 43.8563,
        "longitude": 18.4131
      },
      "timezone": "Europe/Sarajevo",
      "lastUpdate": "2024-01-15T10:30:00Z"
    }
  ],
  "metadata": {
    "total": 1,
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
```

### Get City Details
```http
GET /api/locations/{cityId}
```

**Parameters:**
- `cityId` (required): City identifier (e.g., "sarajevo")

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "sarajevo",
    "name": "Sarajevo",
    "country": "BA",
    "coordinates": {
      "latitude": 43.8563,
      "longitude": 18.4131
    },
    "timezone": "Europe/Sarajevo",
    "stations": [
      {
        "id": "station_001",
        "name": "Center",
        "coordinates": {
          "latitude": 43.8563,
          "longitude": 18.4131
        },
        "parameters": ["pm25", "pm10", "no2", "so2", "o3", "co"]
      }
    ],
    "lastUpdate": "2024-01-15T10:30:00Z"
  }
}
```

---

## 📊 Air Quality Data

### Get Current Air Quality
```http
GET /api/air-quality/{cityId}/current
```

**Parameters:**
- `cityId` (required): City identifier

**Response:**
```json
{
  "success": true,
  "data": {
    "city": "Sarajevo",
    "timestamp": "2024-01-15T10:30:00Z",
    "overall": {
      "aqi": 78,
      "level": "Moderate",
      "color": "#FFFF00",
      "dominantPollutant": "pm25"
    },
    "pollutants": {
      "pm25": {
        "value": 25.4,
        "unit": "µg/m³",
        "aqi": 78,
        "level": "Moderate",
        "color": "#FFFF00"
      },
      "pm10": {
        "value": 45.2,
        "unit": "µg/m³",
        "aqi": 62,
        "level": "Moderate",
        "color": "#FFFF00"
      },
      "no2": {
        "value": 38.7,
        "unit": "µg/m³",
        "aqi": 45,
        "level": "Good",
        "color": "#00E400"
      },
      "so2": {
        "value": 12.3,
        "unit": "µg/m³",
        "aqi": 15,
        "level": "Good",
        "color": "#00E400"
      },
      "o3": {
        "value": 89.1,
        "unit": "µg/m³",
        "aqi": 52,
        "level": "Moderate",
        "color": "#FFFF00"
      },
      "co": {
        "value": 1.8,
        "unit": "mg/m³",
        "aqi": 18,
        "level": "Good",
        "color": "#00E400"
      }
    },
    "healthAdvice": {
      "general": "Air quality is acceptable for most people.",
      "sensitiveGroups": "Consider reducing outdoor activities if experiencing symptoms."
    }
  }
}
```

### Get Historical Data
```http
GET /api/air-quality/{cityId}/history?period={period}&parameter={parameter}
```

**Parameters:**
- `cityId` (required): City identifier
- `period` (optional): Time period (`24h`, `7d`, `30d`, `90d`) - default: `24h`
- `parameter` (optional): Specific pollutant (`pm25`, `pm10`, `no2`, `so2`, `o3`, `co`) - default: all

**Response:**
```json
{
  "success": true,
  "data": {
    "city": "Sarajevo",
    "period": "24h",
    "parameter": "pm25",
    "measurements": [
      {
        "timestamp": "2024-01-15T09:00:00Z",
        "value": 23.7,
        "unit": "µg/m³",
        "aqi": 72
      },
      {
        "timestamp": "2024-01-15T10:00:00Z",
        "value": 25.4,
        "unit": "µg/m³",
        "aqi": 78
      }
    ],
    "statistics": {
      "average": 24.6,
      "minimum": 18.2,
      "maximum": 31.8,
      "count": 24
    }
  },
  "metadata": {
    "total": 24,
    "timestamp": "2024-01-15T10:30:00Z"
  }
}
```

### Get Daily Aggregates
```http
GET /api/air-quality/{cityId}/daily?days={days}
```

**Parameters:**
- `cityId` (required): City identifier
- `days` (optional): Number of days (1-365) - default: 7

**Response:**
```json
{
  "success": true,
  "data": {
    "city": "Sarajevo",
    "days": 7,
    "aggregates": [
      {
        "date": "2024-01-15",
        "overall": {
          "averageAqi": 68,
          "maxAqi": 89,
          "dominantPollutant": "pm25"
        },
        "pollutants": {
          "pm25": {
            "average": 22.8,
            "minimum": 15.2,
            "maximum": 35.6,
            "unit": "µg/m³",
            "averageAqi": 68
          }
        }
      }
    ]
  }
}
```

---

## 🏥 Health Recommendations

### Get Health Advice
```http
GET /api/health/{cityId}/advice?group={group}
```

**Parameters:**
- `cityId` (required): City identifier
- `group` (optional): Health group (`general`, `sensitive`, `elderly`, `children`, `heart`, `lung`) - default: `general`

**Response:**
```json
{
  "success": true,
  "data": {
    "city": "Sarajevo",
    "currentAqi": 78,
    "level": "Moderate",
    "group": "sensitive",
    "recommendations": {
      "outdoor": {
        "level": "caution",
        "message": "Consider reducing prolonged outdoor activities",
        "activities": {
          "exercise": "Limit intense outdoor exercise",
          "walking": "Short walks are generally acceptable",
          "sports": "Move activities indoors if possible"
        }
      },
      "indoor": {
        "level": "normal",
        "message": "Indoor air quality should be acceptable",
        "recommendations": [
          "Keep windows closed during high pollution periods",
          "Use air purifiers if available",
          "Avoid indoor smoking"
        ]
      },
      "protection": {
        "mask": "Consider N95 masks for extended outdoor exposure",
        "timing": "Best air quality typically in early morning"
      }
    },
    "symptoms": {
      "watch": [
        "Coughing",
        "Throat irritation",
        "Shortness of breath"
      ],
      "action": "If symptoms persist, consult healthcare provider"
    }
  }
}
```

### Get Health Groups Info
```http
GET /api/health/groups
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": "general",
      "name": "General Population",
      "description": "Healthy adults and children",
      "sensitivity": "low"
    },
    {
      "id": "sensitive",
      "name": "Sensitive Groups",
      "description": "People with heart/lung conditions, older adults, children",
      "sensitivity": "high"
    },
    {
      "id": "elderly",
      "name": "Older Adults",
      "description": "Adults aged 65 and older",
      "sensitivity": "medium"
    },
    {
      "id": "children",
      "name": "Children",
      "description": "Children and teenagers",
      "sensitivity": "medium"
    },
    {
      "id": "heart",
      "name": "Heart Disease",
      "description": "People with cardiovascular conditions",
      "sensitivity": "high"
    },
    {
      "id": "lung",
      "name": "Lung Disease",
      "description": "People with asthma, COPD, or other lung conditions",
      "sensitivity": "high"
    }
  ]
}
```

---

## 🔄 System Status

### Health Check
```http
GET /api/health
```

**Response:**
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "version": "1.0.0",
  "checks": {
    "database": {
      "status": "Healthy",
      "responseTime": "12ms"
    },
    "openaq": {
      "status": "Healthy",
      "responseTime": "245ms",
      "lastSync": "2024-01-15T10:25:00Z"
    }
  },
  "uptime": "2d 14h 32m"
}
```

### Database Health
```http
GET /api/health/database
```

**Response:**
```json
{
  "status": "Healthy",
  "connectionString": "Server=***;Database=sarajevoair",
  "responseTime": "8ms",
  "recordCount": {
    "locations": 1,
    "measurements": 15428,
    "dailyAggregates": 124
  },
  "lastMeasurement": "2024-01-15T10:30:00Z"
}
```

---

## 📡 Data Sources

### OpenAQ Integration Status
```http
GET /api/data-sources/openaq/status
```

**Response:**
```json
{
  "success": true,
  "data": {
    "status": "active",
    "lastSync": "2024-01-15T10:25:00Z",
    "nextSync": "2024-01-15T10:55:00Z",
    "syncInterval": "30 minutes",
    "statistics": {
      "totalRequests": 2847,
      "successfulRequests": 2834,
      "failedRequests": 13,
      "successRate": 99.54,
      "avgResponseTime": "1.2s"
    },
    "rateLimits": {
      "hourly": {
        "limit": 10000,
        "used": 127,
        "remaining": 9873,
        "resetTime": "2024-01-15T11:00:00Z"
      },
      "daily": {
        "limit": 100000,
        "used": 2847,
        "remaining": 97153
      }
    }
  }
}
```

---

## 📈 Analytics & Statistics

### Get City Statistics
```http
GET /api/analytics/{cityId}/stats?period={period}
```

**Parameters:**
- `cityId` (required): City identifier
- `period` (optional): Time period (`7d`, `30d`, `90d`, `1y`) - default: `30d`

**Response:**
```json
{
  "success": true,
  "data": {
    "city": "Sarajevo",
    "period": "30d",
    "airQuality": {
      "averageAqi": 65,
      "distribution": {
        "Good": 8,
        "Moderate": 18,
        "UnhealthyForSensitive": 4,
        "Unhealthy": 0,
        "VeryUnhealthy": 0,
        "Hazardous": 0
      },
      "trends": {
        "pm25": {
          "trend": "improving",
          "change": -12.3,
          "unit": "%"
        },
        "pm10": {
          "trend": "stable",
          "change": 2.1,
          "unit": "%"
        }
      }
    },
    "pollution": {
      "dominantPollutant": "pm25",
      "frequency": {
        "pm25": 18,
        "pm10": 8,
        "no2": 3,
        "so2": 1
      }
    }
  }
}
```

---

## ⚠️ Error Handling

### Error Response Format
```json
{
  "success": false,
  "error": {
    "code": "CITY_NOT_FOUND",
    "message": "The specified city was not found",
    "details": {
      "cityId": "invalid-city",
      "availableCities": ["sarajevo"]
    }
  },
  "timestamp": "2024-01-15T10:30:00Z",
  "requestId": "req_abc123"
}
```

### HTTP Status Codes
- `200 OK`: Request successful
- `400 Bad Request`: Invalid parameters
- `404 Not Found`: Resource not found
- `429 Too Many Requests`: Rate limit exceeded
- `500 Internal Server Error`: Server error
- `503 Service Unavailable`: External service unavailable

### Common Error Codes
- `CITY_NOT_FOUND`: Requested city is not supported
- `INVALID_PARAMETER`: Parameter validation failed
- `INVALID_TIME_PERIOD`: Time period out of range
- `DATA_NOT_AVAILABLE`: No data available for specified period
- `EXTERNAL_SERVICE_ERROR`: Error from OpenAQ API
- `RATE_LIMIT_EXCEEDED`: Too many requests

---

## 🔄 Rate Limiting

### Limits
- **Anonymous**: 1000 requests/hour
- **Per IP**: 100 requests/minute
- **Burst**: 10 requests/second

### Headers
```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1642248000
X-RateLimit-RetryAfter: 3600
```

---

## 📋 Data Standards

### AQI Calculation
Following EPA Air Quality Index standards:

| AQI Range | Level | Color | Description |
|-----------|-------|-------|-------------|
| 0-50 | Good | Green (#00E400) | Air quality is satisfactory |
| 51-100 | Moderate | Yellow (#FFFF00) | Acceptable for most people |
| 101-150 | Unhealthy for Sensitive | Orange (#FF7E00) | Sensitive groups may experience issues |
| 151-200 | Unhealthy | Red (#FF0000) | Everyone may experience issues |
| 201-300 | Very Unhealthy | Purple (#8F3F97) | Health warnings of emergency conditions |
| 301+ | Hazardous | Maroon (#7E0023) | Emergency conditions |

### Pollutant Units
- **PM2.5, PM10, NO2, SO2, O3**: µg/m³ (micrograms per cubic meter)
- **CO**: mg/m³ (milligrams per cubic meter)

### Time Zones
All timestamps are in UTC (ISO 8601 format). Local time conversions available in city details.

---

## 🛠️ SDKs & Libraries

### JavaScript/TypeScript
```typescript
// Frontend API client example
import { ApiClient } from './lib/api-client';

const api = new ApiClient('http://localhost:5000/api');

// Get current air quality
const aqiData = await api.getCurrentAirQuality('sarajevo');
console.log(`Current AQI: ${aqiData.overall.aqi}`);

// Get historical data
const history = await api.getHistoricalData('sarajevo', '7d', 'pm25');
console.log(`7-day PM2.5 average: ${history.statistics.average}`);
```

### cURL Examples
```bash
# Get current air quality
curl "http://localhost:5000/api/air-quality/sarajevo/current"

# Get 24h history for PM2.5
curl "http://localhost:5000/api/air-quality/sarajevo/history?period=24h&parameter=pm25"

# Get health advice for sensitive groups
curl "http://localhost:5000/api/health/sarajevo/advice?group=sensitive"
```

---

## 🔗 Related Resources

- **OpenAPI/Swagger**: `/swagger` (interactive documentation)
- **Health Endpoint**: `/health` (system status)
- **Frontend Repository**: [GitHub Link](#)
- **Data Sources**: [OpenAQ API v3](https://docs.openaq.org/)

For additional API support, please open an issue on our [GitHub repository](https://github.com/yourusername/sarajevoair).