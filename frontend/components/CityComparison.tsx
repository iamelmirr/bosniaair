'use client'

import React, { useState } from 'react'
import { useLiveAqi } from '../lib/hooks'
import { CITY_OPTIONS, CityId, cityIdToLabel } from '../lib/utils'

interface CityComparisonProps {
  primaryCity: CityId
}

const AQI_CATEGORY_TRANSLATIONS = {
  Good: 'Dobro',
  Moderate: 'Umjereno', 
  'Unhealthy for Sensitive Groups': 'Osjetljivo',
  Unhealthy: 'Nezdravo',
  'Very Unhealthy': 'Opasno',
  Hazardous: 'Fatalno'
} as const

const getAqiColorClass = (aqi: number, category: string) => {
  switch (category.toLowerCase()) {
    case 'good':
      return 'text-aqi-good'
    case 'moderate':
      return 'text-aqi-moderate'
    case 'unhealthy for sensitive groups':
      return 'text-aqi-usg'
    case 'unhealthy':
      return 'text-aqi-unhealthy'
    case 'very unhealthy':
      return 'text-aqi-very-unhealthy'
    case 'hazardous':
      return 'text-aqi-hazardous'
    default:
      return 'text-gray-600'
  }
}

const getAqiBackgroundClass = (category: string) => {
  switch (category.toLowerCase()) {
    case 'good':
      return 'bg-green-50 dark:bg-green-900/20 border-green-200 dark:border-green-800'
    case 'moderate':
      return 'bg-yellow-50 dark:bg-yellow-900/20 border-yellow-200 dark:border-yellow-800'
    case 'unhealthy for sensitive groups':
      return 'bg-orange-50 dark:bg-orange-900/20 border-orange-200 dark:border-orange-800'
    case 'unhealthy':
      return 'bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-800'
    case 'very unhealthy':
      return 'bg-purple-50 dark:bg-purple-900/20 border-purple-200 dark:border-purple-800'
    case 'hazardous':
      return 'bg-red-100 dark:bg-red-900/30 border-red-300 dark:border-red-700'
    default:
      return 'bg-gray-50 dark:bg-gray-800 border-gray-200 dark:border-gray-700'
  }
}

export default function CityComparison({ primaryCity }: CityComparisonProps) {
  const [selectedCity, setSelectedCity] = useState<CityId | ''>('')
  const [cachedData, setCachedData] = useState<any>(null)
  const { data: primaryData } = useLiveAqi(primaryCity)
  const { data: selectedData, isLoading: isSelectedLoading } = useLiveAqi(selectedCity || null)

  React.useEffect(() => {
    if (selectedData && selectedCity) {
      setCachedData(selectedData)
    } else if (!selectedCity) {
      setCachedData(null)
    }
  }, [selectedData, selectedCity])

  const displayData = selectedCity ? (isSelectedLoading && cachedData ? cachedData : selectedData) : null

  const availableCities = CITY_OPTIONS.filter(option => option.id !== primaryCity)

  const CityCard = ({ cityId, data, isPrimary = false }: {
    cityId: CityId
    data: any
    isPrimary?: boolean
  }) => {
    if (!data) {
      return (
        <div className="bg-gray-50 dark:bg-gray-800/30 rounded-xl p-4 sm:p-6 border border-[rgb(var(--border))] animate-pulse-subtle">
          <div className="animate-pulse space-y-3 sm:space-y-4">
            <div className="h-4 sm:h-5 bg-gray-300 dark:bg-gray-600 rounded w-20 mx-auto transition-all duration-200"></div>
            <div className="h-8 sm:h-10 bg-gray-300 dark:bg-gray-600 rounded w-12 sm:w-16 mx-auto transition-all duration-200"></div>
          </div>
        </div>
      )
    }

    const textColorClass = getAqiColorClass(data.overallAqi, data.aqiCategory)

    return (
      <div className={`
        bg-gray-50 dark:bg-gray-800/30 rounded-xl p-4 sm:p-6 border 
        transition-all duration-300 ease-in-out
        md:hover:shadow-lg md:hover:bg-white dark:md:hover:bg-gray-800/60
        ${isPrimary 
          ? 'border-blue-300 dark:border-blue-600 ring-2 ring-blue-100 dark:ring-blue-900/30' 
          : 'border-[rgb(var(--border))] md:hover:border-blue-200 dark:md:hover:border-blue-700'
        }
        md:hover:-translate-y-1 md:hover:scale-[1.02] 
        mobile-simple-hover
      `}>
        <div className="text-center space-y-3 sm:space-y-4">
          <h3 className="text-base sm:text-lg font-semibold text-[rgb(var(--text))] transition-colors duration-200">
            {cityIdToLabel(cityId)}
          </h3>
          
          <div className={`text-3xl sm:text-4xl font-bold transition-all duration-300 ${textColorClass}`}>
            {data.overallAqi}
          </div>
        </div>
      </div>
    )
  }

  return (
    <section className="bg-[rgb(var(--card))] rounded-xl border border-[rgb(var(--border))] p-4 sm:p-6 shadow-card space-y-4 sm:space-y-6">
      <div className="text-center">
        <h2 className="text-lg sm:text-xl font-semibold text-[rgb(var(--text))]">
          Poređenje gradova
        </h2>
      </div>

      <div className="flex flex-wrap justify-center gap-2 sm:gap-3">
        {availableCities.map((option, index) => (
          <button
            key={option.id}
            onClick={() => setSelectedCity(selectedCity === option.id ? '' : option.id)}
            className={`
              px-3 py-2 sm:px-4 rounded-lg text-sm font-medium border 
              mobile-minimal-animation mobile-simple-hover
              transform active:scale-95 
              md:hover:scale-105 md:hover:-translate-y-0.5
              ${selectedCity === option.id
                ? 'bg-blue-600 text-white border-blue-600 shadow-lg animate-slide-in-right'
                : 'bg-[rgb(var(--card))] text-[rgb(var(--text))] border-[rgb(var(--border))] hover:bg-gray-50 dark:hover:bg-gray-700/50 hover:shadow-md'
              }
            `}
            style={{ animationDelay: `${index * 50}ms` }}
          >
            {option.name}
          </button>
        ))}
      </div>

      <div className="grid gap-4 sm:gap-6 sm:grid-cols-2">
        <CityCard cityId={primaryCity} data={primaryData} isPrimary />
        
        {selectedCity ? (
          <div className={`
            transition-all duration-300 ease-in-out
            ${displayData 
              ? 'opacity-100 translate-y-0 scale-100' 
              : 'opacity-70 translate-y-1 scale-98'
            }
          `}>
            <CityCard cityId={selectedCity} data={displayData} />
          </div>
        ) : (
          <div className="flex items-center justify-center bg-gray-50 dark:bg-gray-800/30 rounded-xl p-6 sm:p-8 md:p-12 border-2 border-dashed border-gray-300 dark:border-gray-600 transition-all duration-300 md:hover:border-gray-400 dark:md:hover:border-gray-500 md:hover:bg-gray-100 dark:md:hover:bg-gray-800/50 group cursor-pointer animate-float">
            <div className="text-center">
              <div className="text-2xl sm:text-3xl md:text-4xl opacity-50 mb-2 transition-all duration-300 md:group-hover:scale-110 md:group-hover:opacity-70 animate-float">
                🏙️
              </div>
              <p className="text-xs sm:text-sm text-gray-500 dark:text-gray-400 transition-colors duration-200 font-medium">
                ✨ Odaberite grad za poređenje
              </p>
              <p className="text-xs text-gray-400 dark:text-gray-500 mt-1 opacity-0 md:group-hover:opacity-100 transition-opacity duration-300 hidden sm:block">
                Kliknite na dugme iznad
              </p>
            </div>
          </div>
        )}
      </div>

      <div className="text-center h-12 flex items-center justify-center">
        <div className={`
          transition-all duration-300 ease-in-out
          ${selectedCity && displayData && primaryData 
            ? 'opacity-100 translate-y-0 scale-100' 
            : 'opacity-0 translate-y-2 scale-95 pointer-events-none'
          }
        `}>
          <div className="inline-flex items-center space-x-2 bg-gray-50 dark:bg-gray-800 rounded-lg px-4 py-2 shadow-sm transition-all duration-300 hover:shadow-md hover:bg-gray-100 dark:hover:bg-gray-700">
            <span className="text-sm text-gray-600 dark:text-gray-400 font-medium">
              {selectedCity && displayData && primaryData ? (
                Math.abs(displayData.overallAqi - primaryData.overallAqi) === 0 ? (
                  <span className="text-blue-600 dark:text-blue-400">📊 Identičan AQI</span>
                ) : displayData.overallAqi > primaryData.overallAqi ? (
                  <span className="text-red-500 flex items-center gap-1">
                    📈 +{displayData.overallAqi - primaryData.overallAqi} gori
                  </span>
                ) : (
                  <span className="text-green-500 flex items-center gap-1">
                    📉 {displayData.overallAqi - primaryData.overallAqi} bolji
                  </span>
                )
              ) : (
                <span className="text-transparent">Placeholder</span>
              )}
            </span>
          </div>
        </div>
      </div>
    </section>
  )
}
