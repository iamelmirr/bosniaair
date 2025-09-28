/*
===========================================================================================
                                FRONTEND API CLIENT LIBRARY
===========================================================================================

PURPOSE & FRONTEND-BACKEND INTEGRATION:
TypeScript API client za kommunikaciju sa SarajevoAir backend REST API.
Provides type-safe HTTP calls sa structured response handling i error management.

FRONTEND ARCHITECTURE ROLE:
┌─────────────────────┐    ┌──────────────────────────┐    ┌─────────────────────┐
│   REACT COMPONENTS  │────│     API CLIENT           │────│   BACKEND API       │
│   (UI Layer)        │    │   (This Library)         │    │  (ASP.NET Core)     │
│                     │    │                          │    │                     │
│ • LiveAqiCard       │────│ • getLive()              │────│ • /api/v1/air-quality/{city}/live │
│ • DailyTimeline     │────│ • getComplete()          │────│ • /api/v1/air-quality/{city}/complete │
└─────────────────────┘    └──────────────────────────┘    └─────────────────────┘

TYPE SAFETY STRATEGY:
- Interfaces mirror backend DTO structures za type consistency
- Runtime type validation za API response safety
- Error handling patterns za graceful degradation
- Async/await patterns za modern React integration

FRONTEND PATTERNS:
- React Query integration za caching i state management
- Custom hooks za component-level API state  
- Error boundaries za API failure isolation
- Loading states za improved user experience
*/

// API client za SarajevoAir backend integration

/*
=== AIR QUALITY MEASUREMENT INTERFACE ===

INDIVIDUAL POLLUTANT DATA:
Represents single pollutant measurement od monitoring station
Maps to backend MeasurementDto structure za type consistency
*/

/// <summary>
/// Individual air quality measurement data structure
/// Mirrors backend MeasurementDto za type-safe API responses
/// </summary>
export interface Measurement {
  /// Unique measurement identifier od WAQI system
  id: string
  /// City name where measurement was taken
  city: string
  /// Specific monitoring station location/name  
  locationName: string
  /// Pollutant type (PM2.5, PM10, O3, NO2, SO2, CO)
  parameter: string
  /// Measured concentration value
  value: number
  /// Unit od measurement (μg/m³, mg/m³, ppm)
  unit: string
  /// UTC timestamp kada je measurement recorded
  timestamp: Date
  /// Optional geographic coordinates od monitoring station
  coordinates?: {
    latitude: number
    longitude: number
  }
  /// Data source organization (typically WAQI)
  sourceName: string
  /// Optional time period over which measurement was averaged
  averagingPeriod?: {
    value: number
    unit: string
  }
}

/*
=== COMPLETE RESPONSE INTERFACE ===

Kombinovani payload koristi unified backend /complete endpoint
Eliminiše potrebu za multiple API pozive
*/

/// <summary>
/// Kombinovani response za Sarajevo - live + forecast data u jednom pozivu
/// Maps to backend SarajevoCompleteDto za performance optimization
/// </summary>
export interface CompleteAqiResponse {
  liveData: AqiResponse;
  forecastData: ForecastResponse;
  retrievedAt: Date;
}

/*
=== MAIN AQI API RESPONSE ===

LIVE AIR QUALITY DATA:
Primary interface za live AQI endpoint responses
Contains complete air quality information za specified city
Mirrors backend AqiDto za type safety
*/

/// <summary>
/// Complete live air quality response structure
/// Maps to backend AqiDto za type consistency
/// </summary>
export interface AqiResponse {
  /// Target city name
  city: string
  /// EPA AQI index value (0-500 scale)  
  overallAqi: number
  /// EPA category sa strict typing za UI consistency
  aqiCategory: 'Good' | 'Moderate' | 'Unhealthy for Sensitive Groups' | 'Unhealthy' | 'Very Unhealthy' | 'Hazardous'
  /// Hex color code za visual indicators
  color: string
  /// User-friendly health recommendation message
  healthMessage: string
  /// UTC timestamp od data collection
  timestamp: Date
  /// Detailed pollutant measurements array
  measurements: Measurement[]
  /// Primary pollutant driving AQI value
  dominantPollutant?: string
}

/*
=== FORECAST PREDICTION DATA ===

FUTURE AQI ESTIMATES:
Interfaces za air quality prediction i forecast visualization
Supports planning i decision-making based on predicted conditions
*/

/// <summary>
/// Single day forecast prediction data
/// Contains AQI estimates sa pollutant-specific ranges
/// </summary>
export interface ForecastData {
  /// Date za forecast u ISO format (YYYY-MM-DD)
  date: string
  /// Predicted overall AQI value (required - backend always provides this)
  aqi: number
  /// EPA AQI category za predicted value
  category: string
  /// Hex color code za visual representation
  color: string
  /// Individual pollutant prediction ranges
  pollutants: {
    pm25?: { avg: number; min: number; max: number }
    pm10?: { avg: number; min: number; max: number }
    o3?: { avg: number; min: number; max: number } | null
  }
}

/// <summary>
/// Frontend forecast response structure
/// Maps to backend ForecastResponse
/// </summary>
export interface ForecastResponse {
  city: string
  forecast: ForecastData[]
  timestamp: string
}

export interface AqicnForecastResponse {
  status: string
  data: {
    aqi: number
    city: {
      name: string
      geo: number[]
    }
    forecast?: {
      daily?: {
        pm25?: Array<{ avg: number; day: string; min: number; max: number }>
        pm10?: Array<{ avg: number; day: string; min: number; max: number }>
        o3?: Array<{ avg: number; day: string; min: number; max: number }>
      }
    }
  }
}

// ShareResponse interface removed - using Web Share API in utils instead

/*
===========================================================================================
                                    API CLIENT CLASS
===========================================================================================

CENTRALIZED HTTP COMMUNICATION:
Main class za all backend API communication sa type safety i error handling
Provides consistent interface za all REST endpoint interactions

CONFIGURATION STRATEGY:
Environment-based URL configuration za development i production deployments
Automatic fallback to localhost za local development workflow
*/

/// <summary>
/// Centralized API client za SarajevoAir backend communication
/// Handles HTTP requests, response processing, i type conversion
/// </summary>
class ApiClient {
  private baseUrl: string

  constructor() {
    /*
    === ENVIRONMENT-BASED CONFIGURATION ===
    
    DEPLOYMENT FLEXIBILITY:
    NEXT_PUBLIC_API_URL environment variable za production backend URL
    Localhost fallback enables seamless local development
    Docker i Kubernetes deployments can override URL easily
    */
    // Use environment variable za production, fallback to localhost:5000 za development
    const baseApiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'
    this.baseUrl = `${baseApiUrl}/api/v1`
  }

  /*
  === GENERIC HTTP REQUEST HANDLER ===
  
  TYPE-SAFE API COMMUNICATION:
  Generic method za all HTTP requests sa TypeScript type safety
  Handles common HTTP patterns: headers, error handling, response processing
  */
  
  /// <summary>
  /// Generic HTTP request method sa type safety i error handling
  /// Handles JSON serialization, headers, i response conversion
  /// </summary>
  private async request<T>(
    endpoint: string, 
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`
    
    /*
    === HTTP REQUEST CONFIGURATION ===
    
    STANDARD HEADERS:
    Content-Type: application/json za consistent API communication
    Extensible headers via options.headers za custom requirements
    */
    const response = await fetch(url, {
      credentials: 'include',
      cache: 'no-cache',
      headers: {
        'Content-Type': 'application/json',
        'Cache-Control': 'no-cache, no-store, must-revalidate',
        ...options.headers,
      },
      ...options,
    })

    /*
    === ERROR HANDLING ===
    
    HTTP ERROR DETECTION:
    response.ok checks za 200-299 status codes
    Throws descriptive errors za HTTP failures
    Enables proper error boundaries u React components
    */
    if (!response.ok) {
      throw new Error(`API Error: ${response.status} ${response.statusText}`)
    }

    const data = await response.json()
    
    /*
    === AUTOMATIC DATE CONVERSION ===
    
    JSON DESERIALIZATION ENHANCEMENT:
    Converts ISO date strings back to JavaScript Date objects
    Enables proper date handling u React components
    Maintains type safety across API boundaries
    */
    return this.convertDates(data)
  }

  /*
  === RECURSIVE DATE CONVERSION ===
  
  DEEP OBJECT PROCESSING:
  Recursively processes nested objects i arrays za date string detection
  Converts ISO date strings to JavaScript Date objects
  Preserves original structure while enhancing date handling
  */
  
  /// <summary>
  /// Recursively converts ISO date strings to Date objects u API responses
  /// Handles nested objects i arrays za complete date transformation
  /// </summary>
  private convertDates(obj: any): any {
    if (obj === null || obj === undefined) return obj
    
    // Convert ISO date strings to Date objects
    if (typeof obj === 'string' && this.isDateString(obj)) {
      return new Date(obj)
    }
    
    // Process arrays recursively
    if (Array.isArray(obj)) {
      return obj.map(item => this.convertDates(item))
    }
    
    // Process objects recursively
    if (typeof obj === 'object') {
      const converted: any = {}
      for (const [key, value] of Object.entries(obj)) {
        converted[key] = this.convertDates(value)
      }
      return converted
    }
    
    return obj
  }

  /*
  === DATE STRING DETECTION ===
  
  ISO DATE FORMAT MATCHING:
  Regex pattern matches ISO 8601 date format (YYYY-MM-DDTHH:mm:ss)
  Enables automatic detection od date strings u API responses
  */
  
  /// <summary>
  /// Detects ISO 8601 date string format za automatic conversion
  /// </summary>
  private isDateString(str: string): boolean {
    return /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/.test(str)
  }

  /*
  ===========================================================================================
                                    API ENDPOINT METHODS
  ===========================================================================================
  
  TYPE-SAFE BACKEND COMMUNICATION:
  Methods za each backend REST endpoint sa proper TypeScript typing
  Handles URL encoding, query parameters, i response type conversion
  */

  /*
  === UNIFIED AIR QUALITY ENDPOINTS ===

  SINGLE CONTROLLER ARCHITECTURE:
  Sve operacije dostupne preko /api/v1/air-quality/{city}
  Omogućava isti kod put za sve gradove i eliminiše Sarajevo-specifične funkcije
  */

  /// <summary>
  /// Retrieves latest cached live AQI podatke
  /// </summary>
  async getLive(cityId: string): Promise<AqiResponse> {
    const endpoint = `/air-quality/${encodeURIComponent(cityId)}/live`
    return this.request<AqiResponse>(endpoint)
  }

  /// <summary>
  /// Retrieves combined live + forecast payload u jednom pozivu iz cache-a
  /// </summary>
  async getComplete(cityId: string): Promise<CompleteAqiResponse> {
    const endpoint = `/air-quality/${encodeURIComponent(cityId)}/complete`
    return this.request<CompleteAqiResponse>(endpoint)
  }

  /*
  === LEGACY ENDPOINTS (DEPRECATED) ===
  
  Kept for backward compatibility during transition
  Will be removed after frontend migration is complete
  */

  /*
  === REMOVED ENDPOINTS ===
  
  These methods are no longer needed due to architecture simplification:
  - getForecastData() - replaced with getSarajevoForecast()
  - getGroups() - moved to frontend health-advice.ts
  - getDailyData() - not used in current frontend
  - comparison endpoints - simplified in components
  */

  // Share endpoints removed - using Web Share API in utils instead
}

// Export singleton instance
export const apiClient = new ApiClient()
export default apiClient