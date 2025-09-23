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

// List of available cities with display names and icons
const AVAILABLE_CITIES = [
  { value: 'Sarajevo', label: 'Sarajevo', icon: '🏛️' },
  { value: 'Tuzla', label: 'Tuzla', icon: '🏭' }, 
  { value: 'Mostar', label: 'Mostar', icon: '🌉' },
  { value: 'Banja Luka', label: 'Banja Luka', icon: '🌲' },
  { value: 'Zenica', label: 'Zenica', icon: '⚙️' },
  { value: 'Bihac', label: 'Bihać', icon: '🏞️' }
]

// Bosnian AQI category translations
const AQI_CATEGORY_TRANSLATIONS = {
  'Good': 'Dobro',
  'Moderate': 'Umjereno',
  'Unhealthy for Sensitive Groups': 'Osjetljivo',
  'Unhealthy': 'Nezdravо',
  'Very Unhealthy': 'Opasno',
  'Hazardous': 'Fatalno'
} as const

// Function to determine text color based on background
const getTextColor = (backgroundColor: string | undefined): string => {
  if (!backgroundColor || typeof backgroundColor !== 'string') {
    return '#1f2937'
  }
  
  const hex = backgroundColor.replace('#', '')
  const r = parseInt(hex.substr(0, 2), 16)
  const g = parseInt(hex.substr(2, 2), 16)
  const b = parseInt(hex.substr(4, 2), 16)
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255
  return luminance > 0.6 ? '#1f2937' : '#ffffff'
}

export default function CityComparison({ defaultCity = 'Sarajevo' }: CityComparisonProps) {
  const [selectedCity, setSelectedCity] = useState(() => {
    const available = AVAILABLE_CITIES.find(city => city.value !== defaultCity)
    return available?.value || 'Tuzla'
  })
  const [isDropdownOpen, setIsDropdownOpen] = useState(false)

  // Fetch data for both cities
  const { data: defaultCityData, error: defaultCityError, isLoading: defaultCityLoading } = useSWR<LiveAirQualityData>(
    `${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080/api/v1'}/live?city=${defaultCity}`,
    fetcher,
    {
      refreshInterval: 15 * 60 * 1000,
      revalidateOnFocus: true,
      revalidateOnReconnect: true
    }
  )

  const { data: selectedCityData, error: selectedCityError, isLoading: selectedCityLoading } = useSWR<LiveAirQualityData>(
    `${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080/api/v1'}/live?city=${selectedCity}`,
    fetcher,
    {
      refreshInterval: 15 * 60 * 1000,
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
    const cityInfo = AVAILABLE_CITIES.find(c => c.value === cityName)

    if (isLoading) {
      return (
        <div className="flex-1 bg-[rgb(var(--card))] rounded-xl border border-[rgb(var(--border))] p-6 shadow-card">
          <div className="animate-pulse">
            <div className="flex items-center gap-2 mb-4">
              <div className="w-6 h-6 bg-gray-300 dark:bg-gray-600 rounded"></div>
              <div className="h-6 bg-gray-300 dark:bg-gray-600 rounded w-20"></div>
            </div>
            <div className="h-20 bg-gray-300 dark:bg-gray-600 rounded-lg mb-4"></div>
            <div className="h-4 bg-gray-300 dark:bg-gray-600 rounded mb-2"></div>
            <div className="h-3 bg-gray-300 dark:bg-gray-600 rounded w-3/4"></div>
          </div>
        </div>
      )
    }

    if (error || !data) {
      return (
        <div className="flex-1 bg-[rgb(var(--card))] rounded-xl border border-red-300 dark:border-red-600 p-6 shadow-card">
          <div className="text-center">
            <div className="text-4xl mb-3">😞</div>
            <h3 className="text-lg font-semibold text-[rgb(var(--text))] mb-2 flex items-center justify-center gap-2">
              {cityInfo?.icon} {cityInfo?.label || cityName}
            </h3>
            <div className="text-red-500 dark:text-red-400 text-sm mb-3">
              Greška pri učitavanju podataka
            </div>
            <button 
              onClick={() => window.location.reload()} 
              className="text-blue-500 hover:text-blue-600 dark:text-blue-400 dark:hover:text-blue-300 text-sm underline transition-colors"
            >
              Pokušaj ponovo
            </button>
          </div>
        </div>
      )
    }

    const textColor = getTextColor(data.color)
    const translatedCategory = AQI_CATEGORY_TRANSLATIONS[data.aqiCategory as keyof typeof AQI_CATEGORY_TRANSLATIONS] || data.aqiCategory

    return (
      <div className="flex-1 bg-[rgb(var(--card))] rounded-xl border border-[rgb(var(--border))] p-6 shadow-card hover:shadow-card-hover transition-all">
        {/* City Header */}
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-semibold text-[rgb(var(--text))] flex items-center gap-2">
            <span className="text-xl">{cityInfo?.icon}</span>
            {cityInfo?.label || cityName}
          </h3>
          {isDefault && (
            <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-800 dark:bg-blue-800 dark:text-blue-200">
              Glavni
            </span>
          )}
        </div>

        {/* AQI Display with modern gradient */}
        <div 
          className="rounded-xl p-6 mb-4 text-center transition-all duration-300 hover:scale-105 shadow-lg"
          style={{ 
            background: data.color 
              ? `linear-gradient(135deg, ${data.color}dd, ${data.color})`
              : 'linear-gradient(135deg, #6b7280dd, #6b7280)'
          }}
        >
          <div 
            className="text-4xl font-bold mb-2"
            style={{ color: textColor }}
          >
            {data.overallAqi}
          </div>
          <div 
            className="text-sm opacity-90 uppercase tracking-wider font-medium"
            style={{ color: textColor }}
          >
            AQI
          </div>
        </div>

        {/* Category and Details */}
        <div className="space-y-3">
          <div className="text-center">
            <div className="font-semibold text-[rgb(var(--text))] text-lg">
              {translatedCategory}
            </div>
          </div>
          
          {data.dominantPollutant && (
            <div className="text-center">
              <span className="inline-flex items-center px-3 py-1 rounded-full text-sm bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300">
                Glavni zagađivač: {data.dominantPollutant}
              </span>
            </div>
          )}

          <div className="text-center text-xs text-gray-500 dark:text-gray-400 pt-2 border-t border-gray-200 dark:border-gray-600">
            Ažurirano: {new Date(data.timestamp).toLocaleString('bs-BA', {
              hour: '2-digit',
              minute: '2-digit',
              day: '2-digit',
              month: '2-digit'
            })}
          </div>
        </div>
      </div>
    )
  }

  const selectedCityInfo = AVAILABLE_CITIES.find(city => city.value === selectedCity)

  return (
    <section className="bg-[rgb(var(--card))] rounded-xl shadow-card p-6 border border-[rgb(var(--border))]">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
        <h2 className="text-xl font-semibold text-[rgb(var(--text))]">
          Poređenje gradova
        </h2>
        
        {/* Custom Dropdown */}
        <div className="relative">
          <label className="block text-sm text-gray-600 dark:text-gray-400 mb-2">
            Poredi sa:
          </label>
          <div className="relative">
            <button
              onClick={() => setIsDropdownOpen(!isDropdownOpen)}
              className="flex items-center gap-2 px-4 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-[rgb(var(--card))] hover:bg-gray-50 dark:hover:bg-gray-700 text-[rgb(var(--text))] transition-colors min-w-40"
            >
              <span className="text-lg">{selectedCityInfo?.icon}</span>
              <span className="font-medium">{selectedCityInfo?.label}</span>
              <svg
                className={`w-4 h-4 ml-auto transition-transform ${isDropdownOpen ? 'rotate-180' : ''}`}
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="m19 9-7 7-7-7" />
              </svg>
            </button>

            {isDropdownOpen && (
              <div className="absolute top-full left-0 right-0 mt-2 bg-[rgb(var(--card))] border border-gray-300 dark:border-gray-600 rounded-lg shadow-lg z-10 max-h-60 overflow-auto">
                {AVAILABLE_CITIES
                  .filter(city => city.value !== defaultCity)
                  .map(city => (
                    <button
                      key={city.value}
                      onClick={() => {
                        setSelectedCity(city.value)
                        setIsDropdownOpen(false)
                      }}
                      className={`flex items-center gap-3 w-full px-4 py-3 text-left hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors ${
                        city.value === selectedCity ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300' : 'text-[rgb(var(--text))]'
                      }`}
                    >
                      <span className="text-lg">{city.icon}</span>
                      <span className="font-medium">{city.label}</span>
                      {city.value === selectedCity && (
                        <svg className="w-4 h-4 ml-auto" fill="currentColor" viewBox="0 0 20 20">
                          <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                        </svg>
                      )}
                    </button>
                  ))
                }
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Comparison Cards */}
      <div className="flex flex-col lg:flex-row gap-6">
        {/* Default City (Left) */}
        {renderCityCard(defaultCityData, defaultCityError, defaultCityLoading, defaultCity, true)}

        {/* VS Divider with modern styling */}
        <div className="flex items-center justify-center lg:flex-col">
          <div className="bg-gradient-to-br from-blue-500 to-purple-600 rounded-full w-12 h-12 flex items-center justify-center shadow-lg">
            <span className="text-sm font-bold text-white">VS</span>
          </div>
        </div>

        {/* Selected City (Right) */}
        {renderCityCard(selectedCityData, selectedCityError, selectedCityLoading, selectedCity, false)}
      </div>

      {/* Comparison Summary with better styling */}
      {defaultCityData && selectedCityData && (
        <div className="mt-6 pt-6 border-t border-gray-200 dark:border-gray-700">
          <div className="bg-gradient-to-r from-blue-50 to-purple-50 dark:from-blue-900/20 dark:to-purple-900/20 rounded-lg p-4">
            <div className="text-center">
              <div className="text-2xl mb-2">
                {defaultCityData.overallAqi === selectedCityData.overallAqi ? '🤝' :
                 defaultCityData.overallAqi > selectedCityData.overallAqi ? '👆' : '👇'}
              </div>
              <p className="text-sm text-gray-700 dark:text-gray-300 font-medium">
                {defaultCityData.overallAqi > selectedCityData.overallAqi 
                  ? `${selectedCityInfo?.label} ima bolji kvalitet vazduha (${Math.abs(defaultCityData.overallAqi - selectedCityData.overallAqi)} AQI bodova niži)`
                  : defaultCityData.overallAqi < selectedCityData.overallAqi
                  ? `${AVAILABLE_CITIES.find(c => c.value === defaultCity)?.label} ima bolji kvalitet vazduha (${Math.abs(defaultCityData.overallAqi - selectedCityData.overallAqi)} AQI bodova niži)`
                  : 'Oba grada imaju sličan kvalitet vazduha'
                }
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Click outside to close dropdown */}
      {isDropdownOpen && (
        <div 
          className="fixed inset-0 z-5" 
          onClick={() => setIsDropdownOpen(false)}
        />
      )}
    </section>
  )
}
