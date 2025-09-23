'use client'

import React, { useState } from 'react'
import useSWR from 'swr'

interface LiveAirQualityData {
  city: string
  overallAqi: number
  aqiCategory: string
  color: string
  timestamp: string
  dominantPollutant?: string
  healthMessage?: string
  coordinates?: {
    latitude: number
    longitude: number
  }
}

interface CityComparisonProps {
  defaultCity?: string
}

const fetcher = (url: string) => fetch(url).then(res => res.json())

// List of available cities for comparison
const AVAILABLE_CITIES = [
  'Sarajevo',
  'Tuzla', 
  'Mostar',
  'Banja Luka',
  'Zenica',
  'Bihac'
]

// Function to determine text color based on background
const getTextColor = (backgroundColor: string | undefined): string => {
  if (!backgroundColor || typeof backgroundColor !== 'string') {
    return '#1f2937' // Default to dark text
  }
  
  const hex = backgroundColor.replace('#', '')
  const r = parseInt(hex.substr(0, 2), 16)
  const g = parseInt(hex.substr(2, 2), 16)
  const b = parseInt(hex.substr(4, 2), 16)
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255
  return luminance > 0.6 ? '#1f2937' : '#ffffff'
}

export default function CityComparison({ defaultCity = 'Sarajevo' }: CityComparisonProps) {
  const [selectedCity, setSelectedCity] = useState(AVAILABLE_CITIES.find(city => city !== defaultCity) || 'Tuzla')

  // Fetch data for both cities
  const { data: defaultCityData, error: defaultCityError, isLoading: defaultCityLoading } = useSWR<LiveAirQualityData>(
    `${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080/api/v1'}/live?city=${defaultCity}`,
    fetcher,
    {
      refreshInterval: 15 * 60 * 1000, // Refresh every 15 minutes
      revalidateOnFocus: true,
      revalidateOnReconnect: true
    }
  )

  const { data: selectedCityData, error: selectedCityError, isLoading: selectedCityLoading } = useSWR<LiveAirQualityData>(
    `${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080/api/v1'}/live?city=${selectedCity}`,
    fetcher,
    {
      refreshInterval: 15 * 60 * 1000, // Refresh every 15 minutes
      revalidateOnFocus: true,
      revalidateOnReconnect: true
    }
  )

  const renderCityCard = (
    data: LiveAirQualityData | undefined,
    error: any,
    isLoading: boolean,
    cityName: string,
    isDefault: boolean = false
  ) => {
    if (isLoading) {
      return (
        <div className="flex-1 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
          <div className="animate-pulse">
            <div className="h-6 bg-gray-200 dark:bg-gray-700 rounded mb-4 w-24"></div>
            <div className="h-16 bg-gray-200 dark:bg-gray-700 rounded mb-4"></div>
            <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded mb-2"></div>
            <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-3/4"></div>
          </div>
        </div>
      )
    }

    if (error || !data) {
      return (
        <div className="flex-1 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
          <div className="text-center">
            <h3 className="text-lg font-semibold text-gray-800 dark:text-gray-100 mb-2">
              {cityName}
            </h3>
            <div className="text-red-500 dark:text-red-400 text-sm mb-2">
              Failed to load data
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

    const textColor = getTextColor(data.color)

    return (
      <div className="flex-1 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-6">
        {/* City Header */}
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-semibold text-gray-800 dark:text-gray-100">
            {cityName}
          </h3>
          {isDefault && (
            <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-800 dark:bg-blue-800 dark:text-blue-200">
              Default
            </span>
          )}
        </div>

        {/* AQI Display */}
        <div 
          className="rounded-lg p-6 mb-4 text-center transition-all duration-200"
          style={{ backgroundColor: data.color || '#6b7280' }}
        >
          <div 
            className="text-3xl font-bold mb-1"
            style={{ color: textColor }}
          >
            {data.overallAqi}
          </div>
          <div 
            className="text-sm opacity-90"
            style={{ color: textColor }}
          >
            AQI
          </div>
        </div>

        {/* Category and Details */}
        <div className="space-y-2">
          <div className="text-center">
            <div className="font-medium text-gray-800 dark:text-gray-100">
              {data.aqiCategory}
            </div>
          </div>
          
          {data.dominantPollutant && (
            <div className="text-center text-sm text-gray-600 dark:text-gray-400">
              Primary: {data.dominantPollutant}
            </div>
          )}

          <div className="text-center text-xs text-gray-500 dark:text-gray-500 mt-4">
            Updated: {new Date(data.timestamp).toLocaleTimeString()}
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="bg-white dark:bg-gray-800 rounded-xl shadow-lg p-6">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-xl font-semibold text-gray-800 dark:text-gray-100">
          City Comparison
        </h2>
        
        {/* City Selector for right side */}
        <div className="flex items-center gap-2">
          <label className="text-sm text-gray-600 dark:text-gray-400">
            Compare with:
          </label>
          <select
            value={selectedCity}
            onChange={(e) => setSelectedCity(e.target.value)}
            className="px-3 py-1 rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-800 dark:text-gray-100 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            {AVAILABLE_CITIES
              .filter(city => city !== defaultCity)
              .map(city => (
                <option key={city} value={city}>
                  {city}
                </option>
              ))
            }
          </select>
        </div>
      </div>

      {/* Comparison Cards */}
      <div className="flex flex-col md:flex-row gap-6">
        {/* Default City (Left) */}
        {renderCityCard(defaultCityData, defaultCityError, defaultCityLoading, defaultCity, true)}

        {/* VS Divider */}
        <div className="flex items-center justify-center">
          <div className="bg-gray-200 dark:bg-gray-600 rounded-full w-10 h-10 flex items-center justify-center">
            <span className="text-sm font-medium text-gray-600 dark:text-gray-300">VS</span>
          </div>
        </div>

        {/* Selected City (Right) */}
        {renderCityCard(selectedCityData, selectedCityError, selectedCityLoading, selectedCity, false)}
      </div>

      {/* Comparison Summary */}
      {defaultCityData && selectedCityData && (
        <div className="mt-6 pt-4 border-t border-gray-200 dark:border-gray-700">
          <div className="text-center text-sm text-gray-600 dark:text-gray-400">
            <span className="font-medium text-gray-800 dark:text-gray-100">
              {defaultCityData.overallAqi > selectedCityData.overallAqi 
                ? `${selectedCity} has better air quality (${Math.abs(defaultCityData.overallAqi - selectedCityData.overallAqi)} AQI points lower)`
                : defaultCityData.overallAqi < selectedCityData.overallAqi
                ? `${defaultCity} has better air quality (${Math.abs(defaultCityData.overallAqi - selectedCityData.overallAqi)} AQI points lower)`
                : 'Both cities have similar air quality levels'
              }
            </span>
          </div>
        </div>
      )}
    </div>
  )
}