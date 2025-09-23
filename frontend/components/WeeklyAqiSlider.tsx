'use client'

import React, { useRef, useState } from 'react'
import useSWR from 'swr'

interface DailyData {
  date: string
  dayName: string
  shortDay: string
  aqi: number
  category: string
  color: string
}

interface DailyAqiResponse {
  city: string
  period: string
  data: DailyData[]
  timestamp: string
}

interface WeeklyAqiSliderProps {
  city: string
}

const fetcher = (url: string) => fetch(url).then(res => res.json())

// Function to determine if we should use light or dark text based on background color
const getTextColor = (backgroundColor: string): string => {
  // Remove # if present
  const hex = backgroundColor.replace('#', '')
  
  // Convert to RGB
  const r = parseInt(hex.substr(0, 2), 16)
  const g = parseInt(hex.substr(2, 2), 16)
  const b = parseInt(hex.substr(4, 2), 16)
  
  // Calculate luminance using the relative luminance formula
  // https://www.w3.org/WAI/GL/wiki/Relative_luminance
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255
  
  // If luminance is high (light background), use dark text
  // If luminance is low (dark background), use light text
  return luminance > 0.6 ? '#1f2937' : '#ffffff' // gray-800 or white
}

export default function WeeklyAqiSlider({ city }: WeeklyAqiSliderProps) {
  const scrollRef = useRef<HTMLDivElement>(null)
  const [showLeftButton, setShowLeftButton] = useState(false)
  const [showRightButton, setShowRightButton] = useState(true)

  const { data, error, isLoading } = useSWR<DailyAqiResponse>(
    `${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080/api/v1'}/daily?city=${encodeURIComponent(city)}`,
    fetcher,
    {
      refreshInterval: 15 * 60 * 1000, // Refresh every 15 minutes
      revalidateOnFocus: true,
      revalidateOnReconnect: true
    }
  )

  const translateCategory = (category: string) => {
    switch (category.toLowerCase()) {
      case 'good':
        return 'Dobro'
      case 'moderate':
        return 'Umjereno'
      case 'unhealthy for sensitive groups':
        return 'Osjetljivo'
      case 'unhealthy':
        return 'Nezdravо'
      case 'very unhealthy':
        return 'Opasno'
      case 'hazardous':
        return 'Fatalno'
      default:
        return category
    }
  }

  const translateDay = (dayName: string) => {
    const days: Record<string, string> = {
      'Monday': 'Pon',
      'Tuesday': 'Uto', 
      'Wednesday': 'Sri',
      'Thursday': 'Čet',
      'Friday': 'Pet',
      'Saturday': 'Sub',
      'Sunday': 'Ned'
    }
    return days[dayName] || dayName
  }

  const scrollLeft = () => {
    if (scrollRef.current) {
      scrollRef.current.scrollBy({ left: -200, behavior: 'smooth' })
      setTimeout(checkScrollButtons, 300)
    }
  }

  const scrollRight = () => {
    if (scrollRef.current) {
      scrollRef.current.scrollBy({ left: 200, behavior: 'smooth' })
      setTimeout(checkScrollButtons, 300)
    }
  }

  const checkScrollButtons = () => {
    if (scrollRef.current) {
      const { scrollLeft, scrollWidth, clientWidth } = scrollRef.current
      setShowLeftButton(scrollLeft > 0)
      setShowRightButton(scrollLeft < scrollWidth - clientWidth - 1)
    }
  }

  React.useEffect(() => {
    checkScrollButtons()
    const handleResize = () => checkScrollButtons()
    window.addEventListener('resize', handleResize)
    return () => window.removeEventListener('resize', handleResize)
  }, [data])

  if (isLoading) {
    return (
      <section className="bg-[rgb(var(--card))] rounded-xl p-8 border border-[rgb(var(--border))] shadow-card">
        <div className="animate-pulse">
          <div className="h-6 bg-gray-300 dark:bg-gray-600 rounded-md mb-6 w-48"></div>
          <div className="flex space-x-4 overflow-hidden">
            {Array.from({ length: 7 }).map((_, i) => (
              <div key={i} className="flex-shrink-0 w-20 h-24 bg-gray-300 dark:bg-gray-600 rounded-lg"></div>
            ))}
          </div>
        </div>
      </section>
    )
  }

  if (error) {
    return (
      <section className="bg-[rgb(var(--card))] rounded-xl p-8 border border-red-300 dark:border-red-600 shadow-card">
        <div className="text-center">
          <div className="text-4xl mb-4">⚠️</div>
          <h2 className="text-xl font-semibold text-red-600 dark:text-red-400 mb-2">
            Greška pri učitavanju sedmičnih podataka
          </h2>
          <button 
            onClick={() => window.location.reload()} 
            className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors"
          >
            Pokušaj ponovo
          </button>
        </div>
      </section>
    )
  }

  if (!data) return null

  return (
    <section className="bg-[rgb(var(--card))] rounded-xl p-8 border border-[rgb(var(--border))] shadow-card hover:shadow-card-hover transition-all">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-xl font-semibold text-[rgb(var(--text))]">
          Sedmični AQI trend — {city}
        </h2>
        <span className="text-sm text-gray-500">
          {data.period}
        </span>
      </div>

      {/* Slider Container */}
      <div className="relative overflow-hidden py-2 mb-6">
        {/* Left Scroll Button */}
        {showLeftButton && (
          <button
            onClick={scrollLeft}
            className="absolute left-0 top-1/2 transform -translate-y-1/2 z-10 bg-[rgb(var(--card))] rounded-full shadow-lg border border-[rgb(var(--border))] p-2 hover:shadow-card-hover transition-all"
            aria-label="Pomjeri lijevo"
          >
            <svg className="w-4 h-4 text-[rgb(var(--text))]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
            </svg>
          </button>
        )}

        {/* Right Scroll Button */}
        {showRightButton && (
          <button
            onClick={scrollRight}
            className="absolute right-0 top-1/2 transform -translate-y-1/2 z-10 bg-[rgb(var(--card))] rounded-full shadow-lg border border-[rgb(var(--border))] p-2 hover:shadow-card-hover transition-all"
            aria-label="Pomjeri desno"
          >
            <svg className="w-4 h-4 text-[rgb(var(--text))]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
            </svg>
          </button>
        )}

        {/* Cards Container */}
        <div
          ref={scrollRef}
          className="flex gap-3 md:gap-4 overflow-x-auto scrollbar-hide px-8"
          style={{ scrollbarWidth: 'none', msOverflowStyle: 'none' }}
          onScroll={checkScrollButtons}
        >
          {data.data.map((day, index) => {
            const isToday = index === data.data.length - 1
            const textColor = getTextColor(day.color)
            
            return (
              <div
                key={day.date}
                className={`flex-shrink-0 p-4 rounded-lg transition-all duration-200 hover:scale-105 hover:shadow-md min-w-[80px] ${
                  isToday 
                    ? 'ring-2 ring-blue-400 ring-opacity-50' 
                    : ''
                }`}
                style={{ backgroundColor: day.color }}
              >
                {/* Day Info */}
                <div className="text-center mb-3">
                  <div 
                    className="text-sm font-medium"
                    style={{ color: textColor }}
                  >
                    {translateDay(day.dayName)}
                  </div>
                  <div 
                    className="text-xs opacity-80"
                    style={{ color: textColor }}
                  >
                    {new Date(day.date).getDate()}
                  </div>
                </div>

                {/* AQI Value */}
                <div className="text-center mb-3">
                  <div 
                    className="text-2xl font-bold"
                    style={{ color: textColor }}
                  >
                    {day.aqi}
                  </div>
                  <div 
                    className="text-xs opacity-80"
                    style={{ color: textColor }}
                  >
                    AQI
                  </div>
                </div>

                {/* Category */}
                <div className="text-center">
                  <div 
                    className="text-xs font-medium"
                    style={{ color: textColor }}
                  >
                    {translateCategory(day.category)}
                  </div>
                </div>

                {/* Today Indicator */}
                {isToday && (
                  <div className="text-center mt-2">
                    <div 
                      className="text-xs font-medium px-2 py-1 rounded-full bg-white bg-opacity-20"
                      style={{ color: textColor }}
                    >
                      Danas
                    </div>
                  </div>
                )}
              </div>
            )
          })}
        </div>
      </div>

      {/* AQI Scale Reference */}
      <div className="hidden md:block mt-6 pt-4 border-t border-gray-200 dark:border-gray-700">
        <div className="grid grid-cols-6 gap-1 text-[10px] leading-tight">
          <div className="text-center">
            <div className="h-2 bg-aqi-good rounded mb-1"></div>
            <span className="block">Dobro<br />0-50</span>
          </div>
          <div className="text-center">
            <div className="h-2 bg-aqi-moderate rounded mb-1"></div>
            <span className="block">Umjereno<br />51-100</span>
          </div>
          <div className="text-center">
            <div className="h-2 bg-aqi-usg rounded mb-1"></div>
            <span className="block">Osjetljivo<br />101-150</span>
          </div>
          <div className="text-center">
            <div className="h-2 bg-aqi-unhealthy rounded mb-1"></div>
            <span className="block">Nezdravо<br />151-200</span>
          </div>
          <div className="text-center">
            <div className="h-2 bg-aqi-very rounded mb-1"></div>
            <span className="block">Opasno<br />201-300</span>
          </div>
          <div className="text-center">
            <div className="h-2 bg-aqi-hazardous rounded mb-1"></div>
            <span className="block">Fatalno<br />301+</span>
          </div>
        </div>
      </div>

      {/* Summary */}
      <div className="mt-4 text-center text-sm text-gray-500 dark:text-gray-400">
        Sedmični pregled kvaliteta zraka u gradu <span className="font-medium text-[rgb(var(--text))]">{data.city}</span>
      </div>
    </section>
  )
}