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
export interface CompleteAqiResponse {
  liveData: AqiResponse;
  forecastData: ForecastResponse;
  retrievedAt: Date;
}
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

class ApiClient {
  private baseUrl: string

  constructor() {
    const baseApiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'
    this.baseUrl = `${baseApiUrl}/api/v1`
  }

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
  private isDateString(str: string): boolean {
    return /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/.test(str)
  }
  async getLive(cityId: string): Promise<AqiResponse> {
    const endpoint = `/air-quality/${encodeURIComponent(cityId)}/live`
    return this.request<AqiResponse>(endpoint)
  }

  async getComplete(cityId: string): Promise<CompleteAqiResponse> {
    const endpoint = `/air-quality/${encodeURIComponent(cityId)}/complete`
    return this.request<CompleteAqiResponse>(endpoint)
  }
}

export const apiClient = new ApiClient()
export default apiClient