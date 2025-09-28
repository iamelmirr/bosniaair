'use client'

import { useMemo } from 'react'
import { useComplete } from '../lib/hooks'
import { CityId } from '../lib/utils'

interface DailyData {
  date: string
  dayName: string
  shortDay: string
  aqi: number
  category: string
  color: string
}

interface TimelineData extends DailyData {
  isToday?: boolean
  isForecast?: boolean
  isPast?: boolean
}

interface DailyTimelineProps {
  city: CityId
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
  const { data: completeData, error, isLoading } = useComplete(city)

  const timelineData = useMemo(() => {
    if (!completeData) {
      return [] as TimelineData[]
    }

    const timeline: TimelineData[] = []
    const forecastPayload = completeData.forecastData?.forecast ?? []

    // Use only backend forecast data (includes today from backend timezone)
    forecastPayload.forEach((day, index) => {
      const aqiValue = day.aqi || 0
      const isFirstDay = index === 0 // First forecast day is "today" from backend
      
      timeline.push({
        date: day.date,
        dayName: getDayName(day.date),
        shortDay: getShortDay(day.date),
        aqi: aqiValue,
        category: getAqiCategory(aqiValue),
        color: getAqiColorFromAqi(aqiValue),
        isToday: isFirstDay,
        isPast: false,
        isForecast: !isFirstDay
      })
    })

    return timeline
  }, [completeData])

  const getAqiColorClass = (aqi: number): string => {
    if (aqi <= 50) return 'bg-aqi-good'
    if (aqi <= 100) return 'bg-aqi-moderate'
    if (aqi <= 150) return 'bg-aqi-usg'
    if (aqi <= 200) return 'bg-aqi-unhealthy'
    if (aqi <= 300) return 'bg-aqi-very'
    return 'bg-aqi-hazardous'
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

  if (isLoading) {
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
        <p className="text-red-700 dark:text-red-400 text-center">{error.message || 'Greška pri dohvaćanju vremenske linije'}</p>
      </div>
    )
  }

  if (timelineData.length === 0) {
    return (
      <div className="w-full p-4 bg-[rgb(var(--card))] rounded-xl border border-[rgb(var(--border))]">
        <p className="text-sm text-gray-600 dark:text-gray-400 text-center">Trenutno nema dovoljno podataka za vremensku liniju.</p>
      </div>
    )
  }

  const forecastDaysCount = Math.max(timelineData.length - 1, 0)

  return (
    <div className="w-full bg-[rgb(var(--card))] rounded-xl border border-[rgb(var(--border))] shadow-sm">
      <div className="p-4 border-b border-[rgb(var(--border))]">
        <h3 className="text-lg font-semibold text-[rgb(var(--foreground))]">
          Vremenska linija kvalitete zraka
        </h3>
        <p className="text-sm text-gray-600 dark:text-gray-400">
          Danas • Forecast narednih {forecastDaysCount} dana
        </p>
      </div>

      <div className="p-4">
        {/* Mobile: Horizontal scrollable slider */}
        <div className="md:hidden">
          <div className="flex gap-4 overflow-x-auto pb-2 scrollbar-hide snap-x snap-mandatory px-2" style={{ scrollBehavior: 'smooth' }}>
            {timelineData.map((day, index) => (
              <div 
                key={`${day.date}-${day.isToday ? 'today' : 'forecast'}-${index}`}
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
                key={`${day.date}-${day.isToday ? 'today' : 'forecast'}-${index}`}
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
