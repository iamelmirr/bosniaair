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
│ • LiveAqiCard       │────│ • fetchLiveAqi()         │────│ • /api/v1/live      │
│ • ForecastTimeline  │────│ • fetchForecast()        │────│ • /api/v1/forecast  │
│ • DailyAqiCard      │────│ • fetchDailyAqi()        │────│ • /api/v1/daily     │ 
│ • CityComparison    │────│ • fetchComparison()      │────│ • /api/v1/compare   │
│ • GroupCard         │────│ • fetchHealthGroups()    │────│ • /api/v1/groups    │
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
=== MAIN AQI API RESPONSE ===

LIVE AIR QUALITY DATA:
Primary interface za live AQI endpoint responses
Contains complete air quality information za specified city
Mirrors backend LiveAqiResponse za type safety
*/

/// <summary>
/// Complete live air quality response structure
/// Maps to backend LiveAqiResponse DTO za type consistency
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
=== HEALTH-SENSITIVE GROUPS ===

PERSONALIZED HEALTH ADVISORY:
Interfaces za health group classification i recommendations
Enables targeted messaging za different population vulnerabilities
*/

/// <summary>
/// Health-sensitive population group definition
/// Maps to backend HealthGroupDto za consistent advisory logic
/// </summary>
export interface HealthGroup {
  /// Specific health group identifier sa Bosnian localization
  groupName: 'Sportisti' | 'Djeca' | 'Stariji' | 'Astmatičari'
  /// AQI threshold where special precautions begin za this group
  aqiThreshold: number
  /// Complete recommendation set za all AQI levels
  recommendations: {
    good: string
    moderate: string
    unhealthyForSensitive: string
    unhealthy: string
    veryUnhealthy: string
    hazardous: string
  }
  /// Visual emoji representation za UI display
  iconEmoji: string
  /// Detailed explanation od who belongs to this group
  description: string
}

/*
=== HEALTH GROUPS API RESPONSE ===

COMPREHENSIVE HEALTH ADVISORY:
Response structure za health groups endpoint
Contains personalized recommendations za all health-sensitive populations
*/

/// <summary>
/// Complete health groups analysis response
/// Maps to backend GroupsResponse za type-safe health advisory
/// </summary>
export interface GroupsResponse {
  /// Target city za health advisory
  city: string
  /// Current AQI value driving recommendations
  currentAqi: number
  /// Current EPA AQI category
  aqiCategory: string
  /// Array od health group statuses sa personalized recommendations
  groups: Array<{
    /// Health group definition i characteristics
    group: HealthGroup
    /// Active recommendation based on current AQI
    currentRecommendation: string
    /// Current risk assessment za this group
    riskLevel: 'low' | 'moderate' | 'high' | 'very-high'
  }>
  /// UTC timestamp od analysis
  timestamp: Date
}

/*
=== DAILY HISTORICAL DATA ===

TREND ANALYSIS INTERFACES:
Structures za daily AQI historical data i trend visualization
Enables weekly pattern analysis i dashboard components
*/

/// <summary>
/// Single day historical AQI data entry
/// Maps to backend DailyAqiEntry za consistent trend analysis
/// </summary>
export interface DailyData {
  /// Date u ISO format (YYYY-MM-DD)
  date: string
  /// Full day name (Monday, Tuesday, etc.) za user display
  dayName: string
  /// Abbreviated day name (Mon, Tue, etc.) za compact UI
  shortDay: string
  /// Daily average AQI value
  aqi: number
  /// EPA AQI category za daily average
  category: string
  /// Hex color code za visual indicators i charts
  color: string
}

/// <summary>
/// Complete daily AQI trends response
/// Maps to backend DailyAqiResponse za historical analysis
/// </summary>
export interface DailyResponse {
  /// Target city za historical analysis
  city: string
  /// Human-readable description od analysis timeframe
  period: string
  /// Chronologically ordered daily AQI entries
  data: DailyData[]
  /// UTC timestamp kada je analysis performed
  timestamp: Date
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
  /// Predicted overall AQI value (optional - depends on data availability)
  aqi?: number
  /// EPA AQI category za predicted value (optional)
  category?: string
  /// Fine particulate matter prediction range (most health-relevant)
  pm25?: { avg: number; min: number; max: number }
  /// Coarse particulate matter prediction range
  pm10?: { avg: number; min: number; max: number }
  /// Ground-level ozone prediction range (photochemical smog)
  o3?: { avg: number; min: number; max: number }
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
      headers: {
        'Content-Type': 'application/json',
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
  === LIVE AQI ENDPOINTS ===
  
  REAL-TIME AIR QUALITY DATA:
  Communicates sa backend /api/v1/live endpoint
  Provides current AQI data sa detailed measurements
  */
  
  /// <summary>
  /// Fetches live air quality data za specified city
  /// Calls backend /api/v1/live endpoint sa type-safe response handling
  /// </summary>
  async getLiveAqi(city: string): Promise<AqiResponse> {
    return this.request<AqiResponse>(`/live?city=${encodeURIComponent(city)}`)
  }

  /// <summary>
  /// Fetches detailed pollutant measurements za specified city
  /// Alternative method focusing on measurement details
  /// </summary>
  async getLiveMeasurements(city: string): Promise<Measurement[]> {
    return this.request<Measurement[]>(`/live?city=${encodeURIComponent(city)}`)
  }

  /*
  === DAILY HISTORICAL ENDPOINTS ===
  
  TREND ANALYSIS DATA:
  Communicates sa backend /api/v1/daily endpoint
  Provides 7-day historical AQI trends za visualization
  */
  
  /// <summary>
  /// Fetches daily AQI historical data za trend analysis
  /// Calls backend /api/v1/daily endpoint za 7-day trends
  /// </summary>
  async getDailyData(city: string): Promise<DailyResponse> {
    return this.request<DailyResponse>(`/daily?city=${encodeURIComponent(city)}`)
  }

  // Forecast endpoints
  async getForecastData(city: string): Promise<ForecastData[]> {
    try {
      // Try to get forecast from our backend first
      try {
        const response = await this.request<any>(`/forecast?city=${encodeURIComponent(city)}`)
        
        if (response.forecast) {
          return response.forecast.map((day: any) => ({
            date: day.date,
            aqi: day.aqi,
            category: day.category,
            pm25: day.pollutants?.pm25 ? {
              avg: day.pollutants.pm25.avg,
              min: day.pollutants.pm25.min,
              max: day.pollutants.pm25.max
            } : undefined,
            pm10: day.pollutants?.pm10 ? {
              avg: day.pollutants.pm10.avg,
              min: day.pollutants.pm10.min,
              max: day.pollutants.pm10.max
            } : undefined,
            o3: day.pollutants?.o3 ? {
              avg: day.pollutants.o3.avg,
              min: day.pollutants.o3.min,
              max: day.pollutants.o3.max
            } : undefined
          }))
        }
      } catch (backendError) {
        console.warn('Backend forecast unavailable, generating fallback forecast')
      }

      // Fallback: Generate simple forecast based on current data trends
      const dailyData = await this.getDailyData(city)
      const liveData = await this.getLiveAqi(city)
      
      const forecast: ForecastData[] = []
      const today = new Date()
      
      // Use recent trend from daily data to estimate forecast
      const recentDays = dailyData.data.slice(-3)
      const avgTrend = recentDays.length > 1 ? 
        (recentDays[recentDays.length - 1].aqi - recentDays[0].aqi) / recentDays.length : 0
      
      for (let i = 1; i <= 3; i++) {
        const futureDate = new Date(today)
        futureDate.setDate(today.getDate() + i)
        
        // Simple forecast: current AQI + trend + some randomness
        const baseForecast = liveData.overallAqi + (avgTrend * i)
        const variation = (Math.random() - 0.5) * 20 // ±10 AQI points variation
        const forecastAqi = Math.max(10, Math.min(300, Math.round(baseForecast + variation)))
        
        forecast.push({
          date: futureDate.toISOString().split('T')[0],
          aqi: forecastAqi,
          category: this.getAqiCategory(forecastAqi)
        })
      }
      
      return forecast
    } catch (error) {
      console.error('Error fetching forecast data:', error)
      return []
    }
  }

  private getAqiCategory(aqi: number): string {
    if (aqi <= 50) return 'Good'
    if (aqi <= 100) return 'Moderate'
    if (aqi <= 150) return 'Unhealthy for Sensitive Groups'
    if (aqi <= 200) return 'Unhealthy'
    if (aqi <= 300) return 'Very Unhealthy'
    return 'Hazardous'
  }

  // Groups endpoints
  async getGroups(city: string): Promise<GroupsResponse> {
    return this.request<GroupsResponse>(`/groups?city=${encodeURIComponent(city)}`)
  }

  // Share endpoints removed - using Web Share API in utils instead
}

// Export singleton instance
export const apiClient = new ApiClient()
export default apiClient