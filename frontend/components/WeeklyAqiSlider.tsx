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
    fetcher
  )

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
      <div className="bg-white dark:bg-gray-800 rounded-xl shadow-lg p-6">
        <div className="animate-pulse">
          <div className="h-6 bg-gray-200 dark:bg-gray-700 rounded-md mb-4 w-48"></div>
          <div className="flex space-x-4 overflow-hidden">
            {Array.from({ length: 7 }).map((_, i) => (
              <div key={i} className="flex-shrink-0 w-24 h-32 bg-gray-200 dark:bg-gray-700 rounded-lg"></div>
            ))}
          </div>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="bg-white dark:bg-gray-800 rounded-xl shadow-lg p-6">
        <div className="text-center">
          <div className="text-red-500 dark:text-red-400 text-sm mb-2">
            Failed to load weekly AQI data
          </div>
          <button 
            onClick={() => window.location.reload()} 
            className="text-blue-500 hover:text-blue-600 dark:text-blue-400 dark:hover:text-blue-300 text-sm underline"
          >
            Retry
          </button>
        </div>
      </div>
    )
  }

  if (!data) return null

  return (
    <div className="bg-white dark:bg-gray-800 rounded-xl shadow-lg p-6">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-xl font-semibold text-gray-800 dark:text-gray-100">
          Weekly AQI Trend
        </h2>
        <span className="text-sm text-gray-500 dark:text-gray-400">
          {data.period}
        </span>
      </div>

      {/* Slider Container */}
      <div className="relative overflow-hidden py-2">
        {/* Left Scroll Button */}
        {showLeftButton && (
          <button
            onClick={scrollLeft}
            className="absolute left-0 top-1/2 transform -translate-y-1/2 z-10 bg-white dark:bg-gray-700 rounded-full shadow-lg border border-gray-200 dark:border-gray-600 p-2 hover:bg-gray-50 dark:hover:bg-gray-600 transition-colors"
            aria-label="Scroll left"
          >
            <svg className="w-4 h-4 text-gray-600 dark:text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
            </svg>
          </button>
        )}

        {/* Right Scroll Button */}
        {showRightButton && (
          <button
            onClick={scrollRight}
            className="absolute right-0 top-1/2 transform -translate-y-1/2 z-10 bg-white dark:bg-gray-700 rounded-full shadow-lg border border-gray-200 dark:border-gray-600 p-2 hover:bg-gray-50 dark:hover:bg-gray-600 transition-colors"
            aria-label="Scroll right"
          >
            <svg className="w-4 h-4 text-gray-600 dark:text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
            </svg>
          </button>
        )}

        {/* Cards Container - Responsive Layout */}
        <div
          ref={scrollRef}
          className="flex gap-3 md:gap-4 overflow-x-auto scrollbar-hide px-8 cards-container"
          style={{ scrollbarWidth: 'none', msOverflowStyle: 'none' }}
          onScroll={checkScrollButtons}
        >
          {data.data.map((day, index) => {
            const isToday = index === data.data.length - 1
            const textColor = getTextColor(day.color)
            
            return (
              <div
                key={day.date}
                className={`flex-shrink-0 p-4 rounded-lg transition-all duration-200 hover:scale-105 hover:shadow-md weekly-card ${
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
                    {day.shortDay}
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

                {/* Today Indicator */}
                {isToday && (
                  <div className="text-center">
                    <div 
                      className="text-xs font-medium px-2 py-1 rounded-full bg-white bg-opacity-20"
                      style={{ color: textColor }}
                    >
                      Today
                    </div>
                  </div>
                )}
              </div>
            )
          })}
        </div>
      </div>

      {/* Category Legend */}
      <div className="mt-6 pt-4 border-t border-gray-200 dark:border-gray-700">
        <div className="text-center text-sm text-gray-500 dark:text-gray-400 mb-3">
          Air Quality Categories
        </div>
        <div className="flex flex-wrap justify-center gap-2 text-xs">
          <div className="flex items-center gap-1">
            <div className="w-3 h-3 rounded-full bg-green-400"></div>
            <span className="text-gray-600 dark:text-gray-300">Good (0-50)</span>
          </div>
          <div className="flex items-center gap-1">
            <div className="w-3 h-3 rounded-full bg-yellow-400"></div>
            <span className="text-gray-600 dark:text-gray-300">Moderate (51-100)</span>
          </div>
          <div className="flex items-center gap-1">
            <div className="w-3 h-3 rounded-full bg-orange-400"></div>
            <span className="text-gray-600 dark:text-gray-300">Unhealthy (101-150)</span>
          </div>
          <div className="flex items-center gap-1">
            <div className="w-3 h-3 rounded-full bg-red-400"></div>
            <span className="text-gray-600 dark:text-gray-300">Very Unhealthy (151+)</span>
          </div>
        </div>
      </div>

      {/* Summary */}
      <div className="mt-4 text-center text-sm text-gray-500 dark:text-gray-400">
        Data for <span className="font-medium text-gray-700 dark:text-gray-300">{data.city}</span>
      </div>
    </div>
  )
}