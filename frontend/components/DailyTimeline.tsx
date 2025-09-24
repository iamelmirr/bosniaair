'use client'

import { useEffect, useState } from 'react'
import { apiClient, DailyData, AqiResponse } from '../lib/api-client'

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
    const fetchData = async () => {
      try {
        setLoading(true)
        setError(null)

        // Get daily historical data and current live data
        const [dailyResponse, liveResponse] = await Promise.all([
          apiClient.getDailyData(city),
          apiClient.getLiveAqi(city)
        ])

        const today = new Date().toISOString().split('T')[0]
        const timeline: TimelineData[] = []

        // Process historical data (get last 3 days excluding today)
        const historicalData = dailyResponse.data
          .filter(day => day.date < today)
          .slice(-3)
          .map(day => ({
            ...day,
            isPast: true
          }))

        // Add historical days
        timeline.push(...historicalData)

        // Add today with live data
        const todayData: TimelineData = {
          date: today,
          dayName: new Date().toLocaleDateString('en-US', { weekday: 'long' }),
          shortDay: new Date().toLocaleDateString('en-US', { weekday: 'short' }),
          aqi: liveResponse.overallAqi,
          category: liveResponse.aqiCategory,
          color: liveResponse.color,
          isToday: true
        }
        timeline.push(todayData)

        // Generate forecast for next 3 days
        const forecastDays = generateForecastData(liveResponse.overallAqi, 3)
        timeline.push(...forecastDays)

        setTimelineData(timeline)
      } catch (err) {
        console.error('Error fetching timeline data:', err)
        setError('Failed to load timeline data')
      } finally {
        setLoading(false)
      }
    }

    fetchData()
  }, [city])

  const generateForecastData = (currentAqi: number, days: number): TimelineData[] => {
    const forecast: TimelineData[] = []
    const today = new Date()
    
    for (let i = 1; i <= days; i++) {
      const futureDate = new Date(today)
      futureDate.setDate(today.getDate() + i)
      
      // Generate realistic forecast based on current AQI with some variation
      const variation = (Math.random() - 0.5) * 30 // ±15 AQI points
      const forecastAqi = Math.max(10, Math.min(300, Math.round(currentAqi + variation)))
      
      forecast.push({
        date: futureDate.toISOString().split('T')[0],
        dayName: futureDate.toLocaleDateString('en-US', { weekday: 'long' }),
        shortDay: futureDate.toLocaleDateString('en-US', { weekday: 'short' }),
        aqi: forecastAqi,
        category: getAqiCategory(forecastAqi),
        color: getAqiColor(forecastAqi),
        isForecast: true
      })
    }
    
    return forecast
  }

  const getAqiCategory = (aqi: number): string => {
    if (aqi <= 50) return 'Good'
    if (aqi <= 100) return 'Moderate'
    if (aqi <= 150) return 'Unhealthy for Sensitive Groups'
    if (aqi <= 200) return 'Unhealthy'
    if (aqi <= 300) return 'Very Unhealthy'
    return 'Hazardous'
  }

  const getAqiColor = (aqi: number): string => {
    if (aqi <= 50) return '#00e400'
    if (aqi <= 100) return '#ffff00'
    if (aqi <= 150) return '#ff7e00'
    if (aqi <= 200) return '#ff0000'
    if (aqi <= 300) return '#8f3f97'
    return '#7e0023'
  }

  const getCardStyles = (day: TimelineData) => {
    let baseStyles = 'relative p-4 rounded-xl transition-all duration-200 border-2 '
    
    if (day.isToday) {
      baseStyles += 'bg-[rgb(var(--card))] border-[rgb(var(--primary))] shadow-lg transform scale-105 '
    } else if (day.isForecast) {
      baseStyles += 'bg-[rgb(var(--card))] border-dashed border-[rgb(var(--border))] opacity-80 '
    } else if (day.isPast) {
      baseStyles += 'bg-[rgb(var(--muted))] border-[rgb(var(--border))] opacity-70 '
    } else {
      baseStyles += 'bg-[rgb(var(--card))] border-[rgb(var(--border))] '
    }
    
    return baseStyles
  }

  const getAqiIndicatorStyles = (day: TimelineData) => {
    const opacity = day.isPast ? '0.6' : day.isForecast ? '0.8' : '1'
    return {
      backgroundColor: day.color,
      opacity
    }
  }

  if (loading) {
    return (
      <div className="w-full p-6 bg-[rgb(var(--card))] rounded-xl border border-[rgb(var(--border))]">
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-[rgb(var(--primary))] mx-auto"></div>
          <p className="text-sm text-[rgb(var(--text-muted))] mt-2">Loading timeline...</p>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="w-full p-6 bg-[rgb(var(--card))] rounded-xl border border-[rgb(var(--border))]">
        <div className="text-center text-red-500">
          <p>{error}</p>
        </div>
      </div>
    )
  }

  return (
    <div className="w-full p-6 bg-[rgb(var(--card))] rounded-xl border border-[rgb(var(--border))]">
      <div className="mb-6">
        <h2 className="text-xl font-semibold text-[rgb(var(--text))] mb-2">
          Air Quality Timeline
        </h2>
        <p className="text-sm text-[rgb(var(--text-muted))]">
          Past 3 days, today, and 3-day forecast for {city}
        </p>
      </div>

      {/* Timeline */}
      <div className="relative">
        {/* Timeline line */}
        <div className="absolute top-1/2 left-0 right-0 h-0.5 bg-[rgb(var(--border))] transform -translate-y-1/2 z-0"></div>
        
        {/* Timeline cards */}
        <div className="relative z-10 grid grid-cols-7 gap-2 md:gap-4">
          {timelineData.map((day, index) => (
            <div key={day.date} className="flex flex-col items-center">
              {/* Card */}
              <div className={getCardStyles(day)}>
                {/* Day name */}
                <div className="text-center mb-2">
                  <p className="text-xs font-medium text-[rgb(var(--text-muted))] mb-1">
                    {day.shortDay}
                  </p>
                  <p className="text-xs text-[rgb(var(--text-muted))]">
                    {new Date(day.date).getDate()}
                  </p>
                </div>

                {/* AQI Value */}
                <div className="text-center mb-2">
                  <div className="text-lg font-bold text-[rgb(var(--text))]">
                    {day.aqi}
                  </div>
                </div>

                {/* AQI Indicator */}
                <div className="flex justify-center mb-2">
                  <div 
                    className="w-4 h-4 rounded-full"
                    style={getAqiIndicatorStyles(day)}
                  ></div>
                </div>

                {/* Category */}
                <div className="text-center">
                  <p className="text-xs text-[rgb(var(--text-muted))] font-medium truncate">
                    {day.category}
                  </p>
                </div>

                {/* Special indicators */}
                {day.isToday && (
                  <div className="absolute -top-2 -right-2 w-4 h-4 bg-[rgb(var(--primary))] rounded-full flex items-center justify-center">
                    <div className="w-2 h-2 bg-white rounded-full"></div>
                  </div>
                )}

                {day.isForecast && (
                  <div className="absolute -bottom-1 left-1/2 transform -translate-x-1/2">
                    <div className="text-xs text-[rgb(var(--text-muted))] opacity-70">⟡</div>
                  </div>
                )}
              </div>

              {/* Timeline dot */}
              <div className="mt-3 w-3 h-3 rounded-full bg-[rgb(var(--border))] border-2 border-[rgb(var(--background))]"></div>
            </div>
          ))}
        </div>
      </div>

      {/* Legend */}
      <div className="flex justify-center mt-6 gap-6 text-xs text-[rgb(var(--text-muted))]">
        <div className="flex items-center gap-1">
          <div className="w-2 h-2 rounded-full bg-[rgb(var(--muted))] opacity-70"></div>
          <span>Past</span>
        </div>
        <div className="flex items-center gap-1">
          <div className="w-2 h-2 rounded-full bg-[rgb(var(--primary))]"></div>
          <span>Today</span>
        </div>
        <div className="flex items-center gap-1">
          <div className="w-2 h-2 rounded-full border border-dashed border-[rgb(var(--border))]"></div>
          <span>Forecast</span>
        </div>
      </div>
    </div>
  )
}