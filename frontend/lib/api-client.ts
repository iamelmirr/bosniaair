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

export interface ShareResponse {
  shareUrl: string
  shareText: string
  imageUrl?: string
  qrCodeUrl?: string
}

class ApiClient {
  private baseUrl: string

  constructor() {
    // Use environment variable for production, fallback to localhost for development
    this.baseUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api'
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
    return this.request<AqiResponse>(`/live/aqi?city=${encodeURIComponent(city)}`)
  }

  async getLiveMeasurements(city: string): Promise<Measurement[]> {
    return this.request<Measurement[]>(`/live/measurements?city=${encodeURIComponent(city)}`)
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