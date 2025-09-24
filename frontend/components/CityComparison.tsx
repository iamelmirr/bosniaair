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
}

interface CityComparisonProps {
  defaultCity?: string
}

const fetcher = (url: string) => fetch(url).then(res => res.json())

// Available cities
const AVAILABLE_CITIES = [
  { value: 'Sarajevo', label: 'Sarajevo' },
  { value: 'Tuzla', label: 'Tuzla' }, 
  { value: 'Mostar', label: 'Mostar' },
  { value: 'Banja Luka', label: 'Banja Luka' },
  { value: 'Zenica', label: 'Zenica' },
  { value: 'Bihac', label: 'Bihać' }
]

// Bosnian AQI category translations
const AQI_CATEGORY_TRANSLATIONS = {
  'Good': 'Dobro',
  'Moderate': 'Umjereno',
  'Unhealthy for Sensitive Groups': 'Osjetljivo',
  'Unhealthy': 'Nezdravo',
  'Very Unhealthy': 'Opasno',
  'Hazardous': 'Fatalno'
} as const

export default function CityComparison({ defaultCity = 'Sarajevo' }: CityComparisonProps) {
  const [selectedCity, setSelectedCity] = useState(() => {
    const available = AVAILABLE_CITIES.find(city => city.value !== defaultCity)
    return available?.value || 'Tuzla'
  })

  // Fetch data for both cities
  const { data: defaultCityData, error: defaultCityError, isLoading: defaultCityLoading } = useSWR<LiveAirQualityData>(
    `${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001/api/v1'}/live?city=${defaultCity}`,
    fetcher,
    {
      refreshInterval: 15 * 60 * 1000,
      revalidateOnFocus: true,
      revalidateOnReconnect: true
    }
  )

  const { data: selectedCityData, error: selectedCityError, isLoading: selectedCityLoading, mutate: mutateSelectedCity } = useSWR<LiveAirQualityData>(
    `${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001/api/v1'}/live?city=${selectedCity}`,
    fetcher,
    {
      refreshInterval: 15 * 60 * 1000,
      revalidateOnFocus: true,
      revalidateOnReconnect: true
    }
  )

  // Get AQI colors that match app theme
  const getAqiBackgroundColor = (aqi: number) => {
    if (aqi <= 50) return 'bg-green-50 dark:bg-green-900/20 border-green-200 dark:border-green-800'
    if (aqi <= 100) return 'bg-yellow-50 dark:bg-yellow-900/20 border-yellow-200 dark:border-yellow-800'
    if (aqi <= 150) return 'bg-orange-50 dark:bg-orange-900/20 border-orange-200 dark:border-orange-800'
    if (aqi <= 200) return 'bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-800'
    if (aqi <= 300) return 'bg-red-100 dark:bg-red-900/30 border-red-300 dark:border-red-700'
    return 'bg-red-200 dark:bg-red-900/40 border-red-400 dark:border-red-600'
  }

  const getAqiTextColor = (aqi: number) => {
    if (aqi <= 50) return 'text-green-700 dark:text-green-400'
    if (aqi <= 100) return 'text-yellow-700 dark:text-yellow-400'
    if (aqi <= 150) return 'text-orange-700 dark:text-orange-400'
    if (aqi <= 200) return 'text-red-700 dark:text-red-400'
    if (aqi <= 300) return 'text-red-800 dark:text-red-300'
    return 'text-red-900 dark:text-red-200'
  }

  const renderCityCard = (
    data: LiveAirQualityData | undefined,
    error: any,
    isLoading: boolean,
    cityName: string,
    isMain: boolean = false
  ) => {
    const cityInfo = AVAILABLE_CITIES.find(c => c.value === cityName)

    if (isLoading) {
      return (
        <div className={`rounded-xl border border-gray-200 dark:border-gray-700 ${isMain ? 'bg-blue-50 dark:bg-blue-900/10' : 'bg-[rgb(var(--card))]'} p-6 shadow-card`}>
          <div className="animate-pulse space-y-4">
            <div className="flex items-center gap-2">
              <div className="w-2 h-2 bg-gray-300 dark:bg-gray-600 rounded-full"></div>
              <div className="h-4 bg-gray-300 dark:bg-gray-600 rounded w-20"></div>
            </div>
            <div className="text-center space-y-2">
              <div className="h-12 bg-gray-300 dark:bg-gray-600 rounded w-16 mx-auto"></div>
              <div className="h-3 bg-gray-300 dark:bg-gray-600 rounded w-12 mx-auto"></div>
            </div>
            <div className="h-6 bg-gray-300 dark:bg-gray-600 rounded-full w-20 mx-auto"></div>
            <div className="h-3 bg-gray-300 dark:bg-gray-600 rounded w-24 mx-auto"></div>
          </div>
        </div>
      )
    }

    if (error || !data) {
      return (
        <div className={`rounded-xl border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/10 p-6 shadow-card`}>
          <div className="text-center space-y-3">
            <div className="flex items-center justify-center gap-2">
              <div className="w-2 h-2 bg-red-500 rounded-full"></div>
              <div className="text-sm font-semibold text-[rgb(var(--text))]">
                {cityInfo?.label || cityName}
              </div>
            </div>
            <div className="text-xs text-red-600 dark:text-red-400">Nema podataka</div>
          </div>
        </div>
      )
    }

    const translatedCategory = AQI_CATEGORY_TRANSLATIONS[data.aqiCategory as keyof typeof AQI_CATEGORY_TRANSLATIONS] || data.aqiCategory

    return (
      <div className={`rounded-xl border ${getAqiBackgroundColor(data.overallAqi)} p-6 shadow-card ${isMain ? 'ring-2 ring-blue-200 dark:ring-blue-800' : ''}`}>
        {/* City Header */}
        <div className="flex items-center justify-between mb-6">
          <div className="flex items-center gap-2">
            <div className={`w-2 h-2 rounded-full ${isMain ? 'bg-blue-500' : 'bg-gray-400'}`}></div>
            <div className="text-sm font-semibold text-[rgb(var(--text))]">
              {cityInfo?.label || cityName}
            </div>
          </div>
          {isMain && (
            <span className="px-2 py-1 bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300 rounded-full text-xs font-medium">
              Glavni
            </span>
          )}
        </div>

        {/* AQI Display */}
        <div className="text-center mb-6">
          <div className={`text-4xl font-bold ${getAqiTextColor(data.overallAqi)} mb-2`}>
            {data.overallAqi}
          </div>
          <div className="text-xs text-gray-500 dark:text-gray-400 uppercase tracking-wide font-medium">
            AQI Indeks
          </div>
        </div>

        {/* Category */}
        <div className="text-center mb-4">
          <span className={`inline-block px-3 py-1 rounded-full text-xs font-semibold ${getAqiTextColor(data.overallAqi)} ${getAqiBackgroundColor(data.overallAqi)} border`}>
            {translatedCategory}
          </span>
        </div>

        {/* Timestamp */}
        <div className="text-center">
          <div className="text-xs text-gray-500 dark:text-gray-400">
            Ažurirano {new Date(data.timestamp).toLocaleString('bs-BA', {
              hour: '2-digit',
              minute: '2-digit'
            })}
          </div>
        </div>
      </div>
    )
  }

  const selectedCityInfo = AVAILABLE_CITIES.find(city => city.value === selectedCity)

  return (
    <section className="bg-[rgb(var(--card))] rounded-xl border border-[rgb(var(--border))] p-6 shadow-card">
      {/* Header */}
      <div className="text-center mb-6">
        <div className="flex items-center justify-center gap-2 mb-4">
          <div className="w-6 h-6 bg-blue-100 dark:bg-blue-900 rounded-full flex items-center justify-center">
            <span className="text-blue-600 dark:text-blue-400 text-sm">⚖️</span>
          </div>
          <h2 className="text-xl font-semibold text-[rgb(var(--text))]">
            Poređenje gradova
          </h2>
        </div>
        
        {/* City Selector */}
        <div className="flex flex-wrap justify-center gap-2">
          {AVAILABLE_CITIES
            .filter(city => city.value !== defaultCity)
            .map(city => (
              <button
                key={city.value}
                onClick={() => {
                  setSelectedCity(city.value)
                  mutateSelectedCity()
                }}
                className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                  city.value === selectedCity
                    ? 'bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300 border border-blue-200 dark:border-blue-800'
                    : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600 border border-gray-200 dark:border-gray-600'
                }`}
              >
                {city.label}
              </button>
            ))
          }
        </div>
      </div>

      {/* Cards */}
      <div className="space-y-4 md:space-y-0 md:flex md:items-start md:gap-6">
        {/* Main City */}
        <div className="flex-1">
          {renderCityCard(defaultCityData, defaultCityError, defaultCityLoading, defaultCity, true)}
        </div>

        {/* VS Divider */}
        <div className="flex justify-center md:flex-col md:items-center md:mt-8">
          <div className="w-12 h-12 bg-gray-100 dark:bg-gray-700 rounded-full flex items-center justify-center border border-gray-200 dark:border-gray-600">
            <span className="text-gray-600 dark:text-gray-400 font-semibold text-sm">VS</span>
          </div>
        </div>

        {/* Selected City */}
        <div className="flex-1">
          {renderCityCard(selectedCityData, selectedCityError, selectedCityLoading, selectedCity, false)}
        </div>
      </div>

      {/* Results Panel */}
      {defaultCityData && selectedCityData && (
        <div className="mt-6 p-4 bg-gray-50 dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700">
          <div className="text-center">
            <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">Rezultat poređenja</h3>
            <p className="text-sm text-gray-600 dark:text-gray-400">
              {defaultCityData.overallAqi > selectedCityData.overallAqi 
                ? `${selectedCityInfo?.label} ima bolji kvalitet vazduha za ${Math.abs(defaultCityData.overallAqi - selectedCityData.overallAqi)} AQI bodova`
                : defaultCityData.overallAqi < selectedCityData.overallAqi
                ? `${AVAILABLE_CITIES.find(c => c.value === defaultCity)?.label} ima bolji kvalitet vazduha za ${Math.abs(defaultCityData.overallAqi - selectedCityData.overallAqi)} AQI bodova`
                : 'Oba grada imaju sličan kvalitet vazduha'
              }
            </p>
          </div>
        </div>
      )}
    </section>
  )
}
