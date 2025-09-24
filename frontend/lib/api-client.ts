// API client for SarajevoAir backend

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

export interface HistoryResponse {
  city: string
  parameter: string
  data: Array<{
    timestamp: Date
    value: number
    aqi?: number
    category?: string
  }>
  aggregation: 'hourly' | 'daily'
  startDate: Date
  endDate: Date
}

export interface CompareResponse {
  data: Array<{
    city: string
    currentAqi: number
    aqiCategory: string
    color: string
    timestamp: Date
    measurements: Measurement[]
  }>
}

export interface LocationResponse {
  locations: Array<{
    id: string
    name: string
    city: string
    country: string
    coordinates: {
      latitude: number
      longitude: number
    }
    parameters: string[]
    lastMeasurement?: Date
  }>
}

export interface HealthGroup {
  groupName: 'Sportisti' | 'Djeca' | 'Stariji' | 'Astmatičari'
  aqiThreshold: number
  recommendations: {
    good: string
    moderate: string
    unhealthyForSensitive: string
    unhealthy: string
    veryUnhealthy: string
    hazardous: string
  }
  iconEmoji: string
  description: string
}

export interface GroupsResponse {
  city: string
  currentAqi: number
  aqiCategory: string
  groups: Array<{
    group: HealthGroup
    currentRecommendation: string
    riskLevel: 'low' | 'moderate' | 'high' | 'very-high'
  }>
  timestamp: Date
}

export interface DailyData {
  date: string
  dayName: string
  shortDay: string
  aqi: number
  category: string
  color: string
}

export interface DailyResponse {
  city: string
  period: string
  data: DailyData[]
  timestamp: Date
}

export interface ForecastData {
  date: string
  aqi?: number
  category?: string
  pm25?: { avg: number; min: number; max: number }
  pm10?: { avg: number; min: number; max: number }
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

export interface ShareResponse {
  shareUrl: string
  shareText: string
  imageUrl?: string
  qrCodeUrl?: string
}

class ApiClient {
  private baseUrl: string

  constructor() {
    // Use environment variable for production, fallback to localhost:5001 for development
    this.baseUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001/api/v1'
  }

  private async request<T>(
    endpoint: string, 
    options: RequestInit = {}
  ): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`
    
    const response = await fetch(url, {
      headers: {
        'Content-Type': 'application/json',
        ...options.headers,
      },
      ...options,
    })

    if (!response.ok) {
      throw new Error(`API Error: ${response.status} ${response.statusText}`)
    }

    const data = await response.json()
    
    // Convert date strings back to Date objects
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

  // Live AQI endpoints
  async getLiveAqi(city: string): Promise<AqiResponse> {
    return this.request<AqiResponse>(`/live?city=${encodeURIComponent(city)}`)
  }

  async getLiveMeasurements(city: string): Promise<Measurement[]> {
    return this.request<Measurement[]>(`/live?city=${encodeURIComponent(city)}`)
  }

  // History endpoints
  async getHistory(
    city: string,
    parameter: string,
    startDate: Date,
    endDate: Date,
    aggregation: 'hourly' | 'daily' = 'hourly'
  ): Promise<HistoryResponse> {
    const params = new URLSearchParams({
      city,
      parameter,
      startDate: startDate.toISOString(),
      endDate: endDate.toISOString(),
      aggregation,
    })
    
    return this.request<HistoryResponse>(`/history?${params}`)
  }

  // Compare endpoints
  async compareCities(cities: string[]): Promise<CompareResponse> {
    const params = new URLSearchParams()
    cities.forEach(city => params.append('cities', city))
    
    return this.request<CompareResponse>(`/compare/cities?${params}`)
  }

  async compareTimeframes(
    city: string,
    startDate1: Date,
    endDate1: Date,
    startDate2: Date,
    endDate2: Date
  ): Promise<any> {
    const params = new URLSearchParams({
      city,
      startDate1: startDate1.toISOString(),
      endDate1: endDate1.toISOString(),
      startDate2: startDate2.toISOString(),
      endDate2: endDate2.toISOString(),
    })
    
    return this.request(`/compare/timeframes?${params}`)
  }

  // Location endpoints
  async getLocations(city?: string, country?: string): Promise<LocationResponse> {
    const params = new URLSearchParams()
    if (city) params.append('city', city)
    if (country) params.append('country', country)
    
    return this.request<LocationResponse>(`/locations?${params}`)
  }

  // Daily endpoints
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

  // Share endpoints
  async generateShareLink(
    city: string,
    includeChart: boolean = false,
    includeMap: boolean = false
  ): Promise<ShareResponse> {
    const params = new URLSearchParams({
      city,
      includeChart: includeChart.toString(),
      includeMap: includeMap.toString(),
    })
    
    return this.request<ShareResponse>(`/share/link?${params}`)
  }
}

// Export singleton instance
export const apiClient = new ApiClient()
export default apiClient