'use client'

import { useState } from 'react'
import { useCompare } from '../lib/hooks'
import { CITIES, getAqiCategoryClass, formatDateRelative, classNames, type City } from '../lib/utils'

interface ComparePanelProps {
  currentCity: City
}

export default function ComparePanel({ currentCity }: ComparePanelProps) {
  const [selectedCities, setSelectedCities] = useState<City[]>([currentCity])
  const [isExpanded, setIsExpanded] = useState(false)
  
  const { data, error, isLoading } = useCompare(selectedCities)

  const handleCityToggle = (city: City) => {
    if (selectedCities.includes(city)) {
      if (selectedCities.length > 1) { // Keep at least one city
        setSelectedCities(prev => prev.filter(c => c !== city))
      }
    } else {
      if (selectedCities.length < 4) { // Max 4 cities for comparison
        setSelectedCities(prev => [...prev, city])
      }
    }
  }

  return (
    <section className="bg-[rgb(var(--card))] rounded-xl p-6 border border-[rgb(var(--border))] shadow-card">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-xl font-semibold text-[rgb(var(--text))]">
            City Comparison
          </h2>
          <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
            Compare air quality across Bosnia & Herzegovina
          </p>
        </div>
        
        {/* Expand/Collapse Button */}
        <button
          onClick={() => setIsExpanded(!isExpanded)}
          className="p-2 rounded-lg bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
          title={isExpanded ? 'Collapse' : 'Expand'}
        >
          <svg 
            className={`w-5 h-5 transition-transform ${isExpanded ? 'rotate-180' : ''}`} 
            fill="none" 
            stroke="currentColor" 
            viewBox="0 0 24 24"
          >
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
          </svg>
        </button>
      </div>

      {/* City Selection */}
      <div className="mb-6">
        <label className="block text-sm font-medium text-[rgb(var(--text))] mb-3">
          Select Cities to Compare (max 4)
        </label>
        <div className="flex flex-wrap gap-2">
          {CITIES.map((city) => (
            <button
              key={city}
              onClick={() => handleCityToggle(city)}
              disabled={!selectedCities.includes(city) && selectedCities.length >= 4}
              className={classNames(
                'px-3 py-2 rounded-lg text-sm font-medium transition-all duration-200',
                'focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                'disabled:opacity-50 disabled:cursor-not-allowed',
                selectedCities.includes(city)
                  ? 'bg-blue-600 text-white shadow-md'
                  : 'bg-gray-100 dark:bg-gray-700 text-[rgb(var(--text))] hover:bg-gray-200 dark:hover:bg-gray-600'
              )}
            >
              {city}
              {selectedCities.includes(city) && (
                <span className="ml-1 font-bold">×</span>
              )}
            </button>
          ))}
        </div>
        <div className="mt-2 text-xs text-gray-500">
          {selectedCities.length}/4 cities selected
        </div>
      </div>

      {/* Loading State */}
      {isLoading && (
        <div className="animate-pulse space-y-4">
          {[1, 2, 3].map((i) => (
            <div key={i} className="border border-gray-200 dark:border-gray-700 rounded-lg p-4">
              <div className="flex items-center justify-between">
                <div className="h-4 bg-gray-300 dark:bg-gray-600 rounded w-20"></div>
                <div className="h-8 bg-gray-300 dark:bg-gray-600 rounded w-16"></div>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Error State */}
      {error && (
        <div className="text-center py-8 text-red-600 dark:text-red-400">
          <div className="text-2xl mb-2">⚠️</div>
          <p className="text-sm">{error.message || 'Failed to load comparison data'}</p>
        </div>
      )}

      {/* Comparison Results */}
      {data && data.data.length > 0 && (
        <div className="space-y-4">
          {/* Summary Stats */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 p-4 bg-gray-50 dark:bg-gray-800 rounded-lg">
            <div className="text-center">
              <div className="text-lg font-semibold text-[rgb(var(--text))]">
                {Math.max(...data.data.map(d => d.currentAqi))}
              </div>
              <div className="text-xs text-gray-500">Highest AQI</div>
            </div>
            <div className="text-center">
              <div className="text-lg font-semibold text-[rgb(var(--text))]">
                {Math.min(...data.data.map(d => d.currentAqi))}
              </div>
              <div className="text-xs text-gray-500">Lowest AQI</div>
            </div>
            <div className="text-center">
              <div className="text-lg font-semibold text-[rgb(var(--text))]">
                {Math.round(data.data.reduce((sum, d) => sum + d.currentAqi, 0) / data.data.length)}
              </div>
              <div className="text-xs text-gray-500">Average</div>
            </div>
            <div className="text-center">
              <div className="text-lg font-semibold text-[rgb(var(--text))]">
                {data.data.length}
              </div>
              <div className="text-xs text-gray-500">Cities</div>
            </div>
          </div>

          {/* City Comparison Cards */}
          <div className="space-y-3">
            {data.data
              .sort((a, b) => a.currentAqi - b.currentAqi) // Sort by AQI, best first
              .map((cityData, index) => (
              <div
                key={cityData.city}
                className={classNames(
                  'border rounded-lg p-4 transition-all duration-200',
                  isExpanded ? 'border-gray-200 dark:border-gray-700' : 'border-transparent',
                  index === 0 
                    ? 'bg-green-50 dark:bg-green-900/20 border-green-200 dark:border-green-800' 
                    : 'bg-white dark:bg-gray-800'
                )}
              >
                <div className="flex items-center justify-between">
                  {/* City Info */}
                  <div className="flex items-center gap-3">
                    <div>
                      <h3 className="font-semibold text-[rgb(var(--text))] flex items-center gap-2">
                        {cityData.city}
                        {index === 0 && <span className="text-green-600" title="Best air quality">🏆</span>}
                      </h3>
                      <p className="text-sm text-gray-600 dark:text-gray-400">
                        {formatDateRelative(cityData.timestamp)}
                      </p>
                    </div>
                  </div>
                  
                  {/* AQI Display */}
                  <div className="text-right">
                    <div className={`text-2xl font-bold ${getAqiCategoryClass(cityData.currentAqi)}`}>
                      {cityData.currentAqi}
                    </div>
                    <div className="text-sm text-gray-600 dark:text-gray-400">
                      {cityData.aqiCategory}
                    </div>
                  </div>
                </div>

                {/* Expanded Details */}
                {isExpanded && cityData.measurements && (
                  <div className="mt-4 pt-4 border-t border-gray-200 dark:border-gray-700">
                    <div className="grid grid-cols-2 md:grid-cols-3 gap-3 text-sm">
                      {cityData.measurements.slice(0, 6).map((measurement) => (
                        <div key={measurement.parameter} className="flex justify-between">
                          <span className="text-gray-600 dark:text-gray-400">
                            {measurement.parameter.toUpperCase()}:
                          </span>
                          <span className="font-medium">
                            {measurement.value.toFixed(1)} {measurement.unit}
                          </span>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            ))}
          </div>

          {/* Information Note */}
          <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-3 border border-blue-200 dark:border-blue-800">
            <div className="flex items-start gap-2">
              <svg className="w-4 h-4 text-blue-600 dark:text-blue-400 mt-0.5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <p className="text-sm text-blue-800 dark:text-blue-200">
                Cities are ranked by AQI from best to worst. Air quality can vary significantly within cities based on location and weather conditions.
              </p>
            </div>
          </div>
        </div>
      )}

      {/* No Data State */}
      {data && data.data.length === 0 && (
        <div className="text-center py-8">
          <div className="text-4xl mb-4">🏙️</div>
          <h3 className="text-lg font-semibold text-[rgb(var(--text))] mb-2">
            No Comparison Data
          </h3>
          <p className="text-gray-600 dark:text-gray-400">
            No data available for the selected cities
          </p>
        </div>
      )}
    </section>
  )
}