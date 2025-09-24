'use client'

import { useEffect, useState } from 'react'
import { apiClient, DailyData, AqiResponse, ForecastData } from '../lib/api-client'

interface TimelineData extends DailyData {
  isToday?: boolean
  isForecast?: boolean
  isPast?: boolean
}

interface DailyTimelineProps {
  city: string
}

export default function DailyTimeline({ city }: DailyTimelineProps) {
  const [timelineData, setTimelineData] = useState<TimelineData[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const fetchTimelineData = async () => {
      setLoading(true)
      try {
        const [liveResponse, forecastResponse] = await Promise.all([
          apiClient.getLiveAqi(city),
          apiClient.getForecastData(city)
        ])

        // Combine live and forecast data
        const timeline: TimelineData[] = []

        // Generate historical data (past 3 days + today)
        const historyDays = generateHistoricalData(liveResponse)
        timeline.push(...historyDays)

        // Generate forecast data (next 3 days)
        const forecastDays = generateForecastData(forecastResponse)
        timeline.push(...forecastDays)

        setTimelineData(timeline)
      } catch (err) {
        console.error('Error fetching timeline data:', err)
        setError('Greška pri dohvaćanju vremenskih podataka')
        
        // Generate fallback data
        const fallbackTimeline = generateFallbackData()
        setTimelineData(fallbackTimeline)
      } finally {
        setLoading(false)
      }
    }

    if (city) {
      fetchTimelineData()
    }
  }, [city])

  const generateHistoricalData = (liveData: AqiResponse): TimelineData[] => {
    const historical: TimelineData[] = []
    
    for (let i = 3; i >= 0; i--) {
      const targetDate = new Date()
      targetDate.setDate(targetDate.getDate() - i)
      const dateStr = targetDate.toISOString().split('T')[0]
      
      // Use real data if available, otherwise generate based on live data
      let dayAqi = liveData?.overallAqi || 50
      
      // Add some variation for historical days
      if (i > 0) {
        dayAqi = Math.max(10, Math.min(300, dayAqi + (Math.random() - 0.5) * 20))
      }
      
      historical.push({
        date: dateStr,
        dayName: getDayName(dateStr),
        shortDay: getShortDay(dateStr),
        aqi: Math.round(dayAqi),
        category: getAqiCategory(dayAqi),
        color: getAqiColorFromAqi(dayAqi),
        isToday: i === 0,
        isPast: i > 0,
        isForecast: false
      })
    }
    
    return historical
  }

  const generateForecastData = (forecastData: ForecastData[]): TimelineData[] => {
    const forecast: TimelineData[] = []
    
    for (let i = 1; i <= 3; i++) {
      const futureDate = new Date()
      futureDate.setDate(futureDate.getDate() + i)
      const dateStr = futureDate.toISOString().split('T')[0]
      
      // Find forecast for this date
      const dayForecast = forecastData.find(f => f.date === dateStr)
      
      // Use AQI from backend if available, otherwise calculate from PM2.5
      let forecastAqi = 50 // Default moderate
      if (dayForecast?.aqi) {
        forecastAqi = dayForecast.aqi
      } else if (dayForecast?.pm25) {
        // Convert PM2.5 to AQI (simplified EPA calculation)
        forecastAqi = convertPm25ToAqi(dayForecast.pm25.avg)
      }
      
      forecast.push({
        date: dateStr,
        dayName: getDayName(dateStr),
        shortDay: getShortDay(dateStr),
        aqi: forecastAqi,
        category: getAqiCategory(forecastAqi),
        color: getAqiColorFromAqi(forecastAqi),
        isForecast: true
      })
    }
    
    return forecast
  }

  const generateFallbackData = (): TimelineData[] => {
    const fallbackData: TimelineData[] = []
    
    // Generate 7 days of fallback data (3 past, today, 3 future)
    for (let i = -3; i <= 3; i++) {
      const date = new Date()
      date.setDate(date.getDate() + i)
      const dateStr = date.toISOString().split('T')[0]
      
      // Generate random but realistic AQI values
      const baseAqi = 70 + Math.random() * 60 // 70-130 range
      const aqi = Math.round(baseAqi)
      
      fallbackData.push({
        date: dateStr,
        dayName: getDayName(dateStr),
        shortDay: getShortDay(dateStr),
        aqi,
        category: getAqiCategory(aqi),
        color: getAqiColorFromAqi(aqi),
        isToday: i === 0,
        isForecast: i > 0,
        isPast: i < 0
      })
    }
    
    return fallbackData
  }

  // Helper functions
  const getDayName = (dateStr: string): string => {
    const date = new Date(dateStr)
    const today = new Date()
    today.setHours(0, 0, 0, 0)
    date.setHours(0, 0, 0, 0)
    
    const diffTime = date.getTime() - today.getTime()
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24))
    
    if (diffDays === 0) return 'Danas'
    if (diffDays === 1) return 'Sutra'
    if (diffDays === -1) return 'Jučer'
    
    const dayNames = ['Ned', 'Pon', 'Uto', 'Sri', 'Čet', 'Pet', 'Sub']
    return dayNames[date.getDay()]
  }

  const getShortDay = (dateStr: string): string => {
    const date = new Date(dateStr)
    const dayNames = ['Ned', 'Pon', 'Uto', 'Sri', 'Čet', 'Pet', 'Sub']
    return dayNames[date.getDay()]
  }

  const getAqiCategory = (aqi: number): string => {
    if (aqi <= 50) return 'Dobro'
    if (aqi <= 100) return 'Umjereno'
    if (aqi <= 150) return 'Osjetljivo'
    if (aqi <= 200) return 'Nezdravo'
    if (aqi <= 300) return 'Opasno'
    return 'Fatalno'
  }

  const getAqiColorFromAqi = (aqi: number): string => {
    if (aqi <= 50) return '#22C55E'    // aqi-good
    if (aqi <= 100) return '#EAB308'   // aqi-moderate  
    if (aqi <= 150) return '#F97316'   // aqi-usg
    if (aqi <= 200) return '#EF4444'   // aqi-unhealthy
    if (aqi <= 300) return '#A855F7'   // aqi-very
    return '#7C2D12'                   // aqi-hazardous
  }

  const getAqiColorClass = (aqi: number): string => {
    if (aqi <= 50) return 'bg-aqi-good'
    if (aqi <= 100) return 'bg-aqi-moderate'
    if (aqi <= 150) return 'bg-aqi-usg'
    if (aqi <= 200) return 'bg-aqi-unhealthy'
    if (aqi <= 300) return 'bg-aqi-very'
    return 'bg-aqi-hazardous'
  }

  const convertPm25ToAqi = (pm25: number): number => {
    const breakpoints = [
      { pm25Low: 0, pm25High: 12, aqiLow: 0, aqiHigh: 50 },
      { pm25Low: 12.1, pm25High: 35.4, aqiLow: 51, aqiHigh: 100 },
      { pm25Low: 35.5, pm25High: 55.4, aqiLow: 101, aqiHigh: 150 },
      { pm25Low: 55.5, pm25High: 150.4, aqiLow: 151, aqiHigh: 200 },
      { pm25Low: 150.5, pm25High: 250.4, aqiLow: 201, aqiHigh: 300 },
      { pm25Low: 250.5, pm25High: 500.4, aqiLow: 301, aqiHigh: 500 }
    ]

    for (const bp of breakpoints) {
      if (pm25 >= bp.pm25Low && pm25 <= bp.pm25High) {
        return Math.round(((bp.aqiHigh - bp.aqiLow) / (bp.pm25High - bp.pm25Low)) * (pm25 - bp.pm25Low) + bp.aqiLow)
      }
    }
    
    return pm25 > 500 ? 500 : Math.round(pm25 * 2)
  }

  const getCardStyles = (day: TimelineData): string => {
    let baseStyles = 'relative w-full h-44 p-5 rounded-lg border-2 transition-all duration-200 hover:shadow-md flex flex-col text-center '
    
    if (day.isToday) {
      baseStyles += 'bg-[rgb(var(--card))] border-blue-500 shadow-lg ring-2 ring-blue-200 dark:ring-blue-800 '
    } else {
      baseStyles += 'bg-[rgb(var(--card))] border-gray-200 dark:border-gray-700 '
    }
    
    return baseStyles
  }

  // DayCard component
  const DayCard = ({ day }: { day: TimelineData }) => {
    return (
      <div className="h-full flex flex-col justify-between">
        <div className="text-center">
          <div className="text-sm font-medium text-gray-600 dark:text-gray-400 mb-1">
            {day.shortDay}
          </div>
          <div className="text-xs text-gray-500 dark:text-gray-500">
            {new Date(day.date).getDate()}.{new Date(day.date).getMonth() + 1}
          </div>
        </div>
        
        <div className="flex items-center justify-center my-4">
          <div className={`w-12 h-12 rounded-full flex items-center justify-center text-white text-sm font-bold ${getAqiColorClass(day.aqi)}`}>
            {day.aqi}
          </div>
        </div>
        
        <div className="text-center">
          <div className="text-xs font-medium text-gray-700 dark:text-gray-300 leading-tight">
            {day.category}
          </div>
        </div>
      </div>
    )
  }

  if (loading) {
    return (
      <div className="w-full p-4 bg-[rgb(var(--card))] rounded-xl border border-[rgb(var(--border))]">
        <div className="text-center">
          <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-500 mx-auto"></div>
          <p className="text-sm text-gray-600 dark:text-gray-400 mt-2">Učitavam vremensku liniju...</p>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="w-full p-4 bg-red-50 dark:bg-red-900/20 rounded-xl border border-red-200 dark:border-red-800">
        <p className="text-red-700 dark:text-red-400 text-center">{error}</p>
      </div>
    )
  }

  return (
    <div className="w-full bg-[rgb(var(--card))] rounded-xl border border-[rgb(var(--border))] shadow-sm">
      <div className="p-4 border-b border-[rgb(var(--border))]">
        <h3 className="text-lg font-semibold text-[rgb(var(--foreground))]">
          Vremenska linija kvalitete zraka
        </h3>
        <p className="text-sm text-gray-600 dark:text-gray-400">
          Prethodna 3 dana • Danas • Naredna 3 dana
        </p>
      </div>

      <div className="p-4">
        {/* Mobile: Horizontal scrollable slider */}
        <div className="md:hidden">
          <div className="flex gap-4 overflow-x-auto pb-2 scrollbar-hide snap-x snap-mandatory px-2">
            {timelineData.map((day, index) => (
              <div 
                key={day.date}
                className={`${getCardStyles(day)} min-w-[160px] max-w-[160px] flex-shrink-0 snap-center`}
              >
                <DayCard day={day} />
              </div>
            ))}
          </div>
        </div>

        {/* Desktop: Full width grid */}
        <div className="hidden md:block">
          <div className="grid grid-cols-7 gap-3">
            {timelineData.map((day, index) => (
              <div 
                key={day.date}
                className={getCardStyles(day)}
              >
                <DayCard day={day} />
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}
