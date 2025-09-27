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
  const { data: primaryData } = useLiveAqi(primaryCity)
  const { data: selectedData } = useLiveAqi(selectedCity || 'Sarajevo')

  const availableCities = CITY_OPTIONS.filter(option => option.id !== primaryCity)

  const CityCard = ({ cityId, data, isPrimary = false }: {
    cityId: CityId
    data: any
    isPrimary?: boolean
  }) => {
    if (!data) {
      return (
        <div className="bg-gray-50 dark:bg-gray-800/30 rounded-xl p-6 border border-[rgb(var(--border))]">
          <div className="animate-pulse space-y-4">
            <div className="h-5 bg-gray-300 dark:bg-gray-600 rounded w-20 mx-auto"></div>
            <div className="h-10 bg-gray-300 dark:bg-gray-600 rounded w-16 mx-auto"></div>
          </div>
        </div>
      )
    }

    const textColorClass = getAqiColorClass(data.overallAqi, data.aqiCategory)

    return (
      <div className={`bg-gray-50 dark:bg-gray-800/30 rounded-xl p-6 border border-[rgb(var(--border))] ${isPrimary ? 'border-blue-300 dark:border-blue-600' : ''}`}>
        <div className="text-center space-y-4">
          <h3 className="text-lg font-semibold text-[rgb(var(--text))]">
            {cityIdToLabel(cityId)}
          </h3>
          
          <div className={`text-4xl font-bold ${textColorClass}`}>
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
        {availableCities.map(option => (
          <button
            key={option.id}
            onClick={() => setSelectedCity(selectedCity === option.id ? '' : option.id)}
            className={`px-3 py-2 sm:px-4 rounded-lg text-sm font-medium border transition-all duration-200 ${
              selectedCity === option.id
                ? 'bg-blue-600 text-white border-blue-600 shadow-lg'
                : 'bg-[rgb(var(--card))] text-[rgb(var(--text))] border-[rgb(var(--border))] hover:bg-gray-50 dark:hover:bg-gray-700/50'
            }`}
          >
            {option.name}
          </button>
        ))}
      </div>

      <div className="grid gap-4 sm:gap-6 sm:grid-cols-2">
        <CityCard cityId={primaryCity} data={primaryData} isPrimary />
        
        {selectedCity ? (
          <div key={selectedCity} className="animate-fade-in">
            <CityCard cityId={selectedCity} data={selectedData} />
          </div>
        ) : (
          <div className="flex items-center justify-center bg-gray-50 dark:bg-gray-800/30 rounded-xl p-8 sm:p-12 border-2 border-dashed border-gray-300 dark:border-gray-600">
            <div className="text-center">
              <div className="text-3xl sm:text-4xl opacity-50 mb-2">🏙️</div>
              <p className="text-sm text-gray-500 dark:text-gray-400">
                Odaberite grad
              </p>
            </div>
          </div>
        )}
      </div>

      {selectedCity && selectedData && primaryData && (
        <div key={`stats-${selectedCity}`} className="animate-fade-in text-center">
          <div className="inline-flex items-center space-x-2 bg-gray-50 dark:bg-gray-800 rounded-lg px-3 py-2">
            <span className="text-sm text-gray-600 dark:text-gray-400">
              {Math.abs(selectedData.overallAqi - primaryData.overallAqi) === 0 ? (
                'Isti AQI'
              ) : selectedData.overallAqi > primaryData.overallAqi ? (
                <span className="text-red-500">
                  +{selectedData.overallAqi - primaryData.overallAqi} gori
                </span>
              ) : (
                <span className="text-green-500">
                  {selectedData.overallAqi - primaryData.overallAqi} bolji
                </span>
              )}
            </span>
          </div>
        </div>
      )}
    </section>
  )
}
