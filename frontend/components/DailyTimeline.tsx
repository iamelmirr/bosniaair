'use client'

import { useMemo } from 'react'
import { DailyData, AqiResponse, ForecastData } from '../lib/api-client'
import { useLiveAqi, useForecast } from '../lib/hooks'

interface TimelineData extends DailyData {
  isToday?: boolean
  isForecast?: boolean
  isPast?: boolean
}

interface DailyTimelineProps {
  city: string
}

// Helper functions - moved outside component to prevent re-creation
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

export default function DailyTimeline({ city }: DailyTimelineProps) {
  // Use SWR hooks for synchronized 10-minute updates
  const { data: liveData, error: liveError, isLoading: liveLoading } = useLiveAqi(city)
  const { data: forecastData, error: forecastError, isLoading: forecastLoading } = useForecast(city)

  // Combine data using useMemo for performance
  const { timelineData, error, loading } = useMemo(() => {
    const isLoading = liveLoading || forecastLoading
    const hasError = liveError || forecastError
    
    if (isLoading) {
      return { timelineData: [], error: null, loading: true }
    }
    
    if (hasError) {
      const errorMessage = liveError?.message || forecastError?.message || 'Greška pri dohvaćanju vremenskih podataka'
      
      // Generate static fallback data (no random values)
      const fallbackData: TimelineData[] = []
      for (let i = 0; i <= 5; i++) {
        const date = new Date()
        date.setDate(date.getDate() + i)
        const dateStr = date.toISOString().split('T')[0]
        
        fallbackData.push({
          date: dateStr,
          dayName: getDayName(dateStr),
          shortDay: getShortDay(dateStr),
          aqi: 50, // Static moderate AQI
          category: 'Umjereno',
          color: getAqiColorFromAqi(50),
          isToday: i === 0,
          isPast: false,
          isForecast: i > 0
        })
      }
      
      return { 
        timelineData: fallbackData, 
        error: errorMessage, 
        loading: false 
      }
    }

    if (!liveData || !forecastData) {
      return { timelineData: [], error: null, loading: false }
    }

    // Combine today + forecast data (no mock historical data)
    const timeline: TimelineData[] = []

    // Add today (live data)
    const today = new Date()
    const dateStr = today.toISOString().split('T')[0]
    
    const todayData: TimelineData = {
      date: dateStr,
      dayName: getDayName(dateStr),
      shortDay: getShortDay(dateStr),
      aqi: liveData?.overallAqi || 50,
      category: getAqiCategory(liveData?.overallAqi || 50),
      color: getAqiColorFromAqi(liveData?.overallAqi || 50),
      isToday: true,
      isPast: false,
      isForecast: false
    }
    timeline.push(todayData)

    // Add forecast data (skip today's data which is index 0)
    const realForecastData = forecastData.filter((f, index) => {
      return index > 0 && f.aqi && f.aqi > 0
    })
    
    realForecastData.forEach(dayForecast => {
      const aqiValue = dayForecast.aqi!
      timeline.push({
        date: dayForecast.date,
        dayName: getDayName(dayForecast.date),
        shortDay: getShortDay(dayForecast.date),
        aqi: aqiValue,
        category: getAqiCategory(aqiValue),
        color: getAqiColorFromAqi(aqiValue),
        isForecast: true,
        isPast: false,
        isToday: false
      })
    })

    return { timelineData: timeline, error: null, loading: false }
  }, [liveData, forecastData, liveError, forecastError, liveLoading, forecastLoading])

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
    let baseStyles = 'relative w-full h-44 p-5 rounded-lg border-2 transition-all duration-300 hover:shadow-card-hover hover:-translate-y-2 hover:scale-105 cursor-pointer flex flex-col text-center '
    
    if (day.isToday) {
      baseStyles += 'bg-[rgb(var(--card))] border-blue-500 shadow-lg ring-2 ring-blue-200 dark:ring-blue-800 '
    } else {
      baseStyles += 'bg-[rgb(var(--card))] border-gray-200 dark:border-gray-700 hover:border-blue-300 dark:hover:border-blue-600 '
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
          Danas • Forecast narednih {timelineData.length - 1} dana
        </p>
      </div>

      <div className="p-4">
        {/* Mobile: Horizontal scrollable slider */}
        <div className="md:hidden">
          <div className="flex gap-4 overflow-x-auto pb-2 scrollbar-hide snap-x snap-mandatory px-2" style={{ scrollBehavior: 'smooth' }}>
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

        {/* Desktop: Full width adaptive grid */}
        <div className="hidden md:block">
          <div className={`grid gap-3 ${timelineData.length === 6 ? 'grid-cols-6' : timelineData.length === 7 ? 'grid-cols-7' : 'grid-cols-5'}`}>
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
