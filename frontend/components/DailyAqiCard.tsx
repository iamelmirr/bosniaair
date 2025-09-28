'use client'

import React from 'react'
import useSWR from 'swr'

/**
 * Represents a single day's air quality data in the weekly trend.
 */
interface DailyData {
  date: string
  dayName: string
  shortDay: string
  aqi: number
  category: string
  color: string
}

/**
 * Response structure for the daily AQI API endpoint.
 * Contains weekly air quality data for a specific city.
 */
interface DailyAqiResponse {
  city: string
  period: string
  data: DailyData[]
  timestamp: string
}

/**
 * Props for the DailyAqiCard component.
 */
interface DailyAqiCardProps {
  city: string
}

/**
 * SWR fetcher function for making API requests.
 */
const fetcher = (url: string) => fetch(url).then(res => res.json())

/**
 * DailyAqiCard component displays a weekly air quality index trend for a specific city.
 * Shows 7 days of AQI data with color-coded categories and highlights today's data.
 * Automatically refreshes every 15 minutes and supports real-time updates.
 *
 * @param city - The city name to display AQI data for
 */
export default function DailyAqiCard({ city }: DailyAqiCardProps) {
  const { data, error, isLoading } = useSWR<DailyAqiResponse>(
    `${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'}/api/v1/daily?city=${encodeURIComponent(city)}`,
    fetcher,
    {
      refreshInterval: 15 * 60 * 1000, // Refresh every 15 minutes
      revalidateOnFocus: true,
      revalidateOnReconnect: true
    }
  )

  if (isLoading) {
    return (
      <div className="bg-white dark:bg-gray-800 rounded-xl shadow-lg p-6">
        <div className="animate-pulse">
          <div className="h-6 bg-gray-200 dark:bg-gray-700 rounded-md mb-4"></div>
          <div className="space-y-3">
            {Array.from({ length: 7 }).map((_, i) => (
              <div key={i} className="h-12 bg-gray-200 dark:bg-gray-700 rounded-lg"></div>
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
            Failed to load daily AQI data
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

      <div className="space-y-3">
        {data.data.map((day, index) => (
          <div 
            key={day.date} 
            className={`flex items-center justify-between p-4 rounded-lg border transition-all duration-200 hover:shadow-md ${
              index === data.data.length - 1 
                ? 'border-blue-200 bg-blue-50 dark:border-blue-800 dark:bg-blue-900/20' 
                : 'border-gray-200 dark:border-gray-700'
            }`}
          >
            {/* Day Info */}
            <div className="flex items-center space-x-4">
              <div className="text-center min-w-[3rem]">
                <div className="text-sm font-medium text-gray-700 dark:text-gray-300">
                  {day.shortDay}
                </div>
                <div className="text-xs text-gray-500 dark:text-gray-400">
                  {new Date(day.date).getDate()}
                </div>
              </div>
              
              {/* AQI Badge */}
              <div 
                className="px-3 py-1 rounded-full text-white text-sm font-medium min-w-[3rem] text-center"
                style={{ backgroundColor: day.color }}
              >
                {day.aqi}
              </div>
            </div>

            {/* Category */}
            <div className="flex-1 text-right">
              <div className={`text-sm font-medium ${
                index === data.data.length - 1 
                  ? 'text-gray-800 dark:text-gray-100' 
                  : 'text-gray-600 dark:text-gray-300'
              }`}>
                {day.category}
              </div>
            </div>

            {/* Today indicator */}
            {index === data.data.length - 1 && (
              <div className="ml-3">
                <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-800 dark:bg-blue-800 dark:text-blue-200">
                  Today
                </span>
              </div>
            )}
          </div>
        ))}
      </div>

      {/* Summary */}
      <div className="mt-6 pt-4 border-t border-gray-200 dark:border-gray-700">
        <div className="text-center text-sm text-gray-500 dark:text-gray-400">
          Data for <span className="font-medium text-gray-700 dark:text-gray-300">{data.city}</span>
        </div>
      </div>
    </div>
  )
}