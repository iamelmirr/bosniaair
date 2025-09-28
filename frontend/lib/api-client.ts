/**
 * API client module for BosniaAir application.
 * Contains TypeScript interfaces for API responses and a client class for making HTTP requests
 * to the backend API with automatic date conversion and error handling.
 */

/**
 * Represents a single air pollutant measurement from the API.
 */
export interface Measurement {
  id: string
  city: string
  locationName: string
  parameter: string
  value: number
  unit: string
  timestamp: Date
  coordinates?: {
    latitude: number
    longitude: number
  }
  sourceName: string
  averagingPeriod?: {
    value: number
    unit: string
  }
}

/**
 * Response structure for complete AQI data including live and forecast information.
 */
export interface CompleteAqiResponse {
  liveData: AqiResponse;
  forecastData: ForecastResponse;
  retrievedAt: Date;
}

/**
 * Response structure for live air quality data.
 */
export interface AqiResponse {
  city: string
  overallAqi: number
  aqiCategory: 'Good' | 'Moderate' | 'Unhealthy for Sensitive Groups' | 'Unhealthy' | 'Very Unhealthy' | 'Hazardous'
  color: string
  healthMessage: string
  timestamp: Date
  measurements: Measurement[]
  dominantPollutant?: string
}

/**
 * Represents forecast data for a specific day.
 */
export interface ForecastData {
  date: string
  aqi: number
  category: string
  color: string
  pollutants: {
    pm25?: { avg: number; min: number; max: number }
    pm10?: { avg: number; min: number; max: number }
    o3?: { avg: number; min: number; max: number } | null
  }
}

/**
 * Response structure for forecast data.
 */
export interface ForecastResponse {
  city: string
  forecast: ForecastData[]
  timestamp: string
}

/**
 * Raw response structure from the WAQI API (for reference).
 */
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

/**
 * API client class for making HTTP requests to the BosniaAir backend.
 * Handles authentication, caching, date conversion, and error handling.
 */
class ApiClient {
  private baseUrl: string

  constructor() {
    const baseApiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'
    this.baseUrl = `${baseApiUrl}/api/v1`
  }

  /**
   * Makes an HTTP request to the API with common configuration.
   * @param endpoint - API endpoint path
   * @param options - Fetch options
   * @returns Promise resolving to typed response data
   */
  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`

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

    if (!response.ok) {
      throw new Error(`API Error: ${response.status} ${response.statusText}`)
    }

    const data = await response.json()
    return this.convertDates(data)
  }

  /**
   * Recursively converts ISO date strings to Date objects in API response data.
   * @param obj - Object to convert dates in
   * @returns Object with dates converted
   */
  private convertDates(obj: any): any {
    if (obj === null || obj === undefined) return obj
    if (typeof obj === 'string' && this.isDateString(obj)) {
      return new Date(obj)
    }
    if (Array.isArray(obj)) {
      return obj.map(item => this.convertDates(item))
    }
    if (typeof obj === 'object') {
      const converted: any = {}
      for (const [key, value] of Object.entries(obj)) {
        converted[key] = this.convertDates(value)
      }
      return converted
    }
    return obj
  }

  /**
   * Checks if a string matches ISO date format.
   * @param str - String to test
   * @returns True if string is an ISO date
   */
  private isDateString(str: string): boolean {
    return /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/.test(str)
  }

  /**
   * Fetches live air quality data for a specific city.
   * @param cityId - City identifier
   * @returns Promise resolving to live AQI data
   */
  async getLive(cityId: string): Promise<AqiResponse> {
    const endpoint = `/air-quality/${encodeURIComponent(cityId)}/live`
    return this.request<AqiResponse>(endpoint)
  }

  /**
   * Fetches complete air quality data (live + forecast) for a specific city.
   * @param cityId - City identifier
   * @returns Promise resolving to complete AQI data
   */
  async getComplete(cityId: string): Promise<CompleteAqiResponse> {
    const endpoint = `/air-quality/${encodeURIComponent(cityId)}/complete`
    return this.request<CompleteAqiResponse>(endpoint)
  }
}

/**
 * Singleton instance of the API client.
 */
export const apiClient = new ApiClient()
export default apiClient