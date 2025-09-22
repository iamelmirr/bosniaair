'use client'

import { useLiveAqi } from '../lib/hooks'

interface LiveAqiCardProps {
  city: string
}

export default function LiveAqiCard({ city }: LiveAqiCardProps) {
  const { data: aqiData, error, isLoading } = useLiveAqi(city)

  if (isLoading) {
    return (
      <section className="bg-[rgb(var(--card))] rounded-xl p-8 border border-[rgb(var(--border))] shadow-card">
        <div className="animate-pulse">
          <div className="flex items-baseline justify-between mb-4">
            <div className="h-6 bg-gray-300 dark:bg-gray-600 rounded w-48"></div>
            <div className="h-4 bg-gray-300 dark:bg-gray-600 rounded w-16"></div>
          </div>
          
          <div className="flex items-end gap-6 mb-4">
            <div className="h-20 bg-gray-300 dark:bg-gray-600 rounded w-32"></div>
            <div className="h-6 bg-gray-300 dark:bg-gray-600 rounded w-20"></div>
          </div>
          
          <div className="h-4 bg-gray-300 dark:bg-gray-600 rounded w-full mb-4"></div>
          <div className="h-3 bg-gray-300 dark:bg-gray-600 rounded w-48"></div>
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
            Unable to load air quality data
          </h2>
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            {error.message || 'Please check your internet connection and try again.'}
          </p>
          <button
            onClick={() => window.location.reload()}
            className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors"
          >
            Retry
          </button>
        </div>
      </section>
    )
  }

  if (!aqiData) {
    return (
      <section className="bg-[rgb(var(--card))] rounded-xl p-8 border border-[rgb(var(--border))] shadow-card">
        <div className="text-center text-gray-600 dark:text-gray-400">
          No data available for {city}
        </div>
      </section>
    )
  }

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

  const getHealthAdvice = (category: string) => {
    switch (category.toLowerCase()) {
      case 'good':
        return 'Air quality is satisfactory. Enjoy outdoor activities!'
      case 'moderate':
        return 'Acceptable for most people. Unusually sensitive people should consider reducing prolonged outdoor exertion.'
      case 'unhealthy for sensitive groups':
        return 'Members of sensitive groups may experience health effects. The general public is not likely to be affected.'
      case 'unhealthy':
        return 'Everyone may begin to experience health effects. Members of sensitive groups may experience more serious health effects.'
      case 'very unhealthy':
        return 'Health warnings of emergency conditions. The entire population is more likely to be affected.'
      case 'hazardous':
        return 'Health alert: everyone may experience more serious health effects.'
      default:
        return 'Monitor air quality conditions.'
    }
  }

  const formatTimestamp = (timestamp: Date) => {
    return new Intl.DateTimeFormat('bs-BA', {
      dateStyle: 'short',
      timeStyle: 'short',
    }).format(timestamp)
  }

  return (
    <section className="bg-[rgb(var(--card))] rounded-xl p-8 border border-[rgb(var(--border))] shadow-card hover:shadow-card-hover transition-all">
      {/* Header */}
      <div className="flex items-baseline justify-between mb-6">
        <h2 className="text-xl font-semibold text-[rgb(var(--text))]">
          Current AQI — {aqiData.city}
        </h2>
        <div className="flex items-center gap-2">
          <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse"></div>
          <span className="text-sm text-gray-500">Live Data</span>
        </div>
      </div>
      
      {/* Main AQI Display */}
      <div className="flex items-end gap-6 mb-6">
        <div className={`text-6xl font-bold ${getAqiColorClass(aqiData.overallAqi, aqiData.aqiCategory)}`}>
          {aqiData.overallAqi}
        </div>
        <div className="flex flex-col">
          <div className={`text-2xl font-medium ${getAqiColorClass(aqiData.overallAqi, aqiData.aqiCategory)}`}>
            {aqiData.aqiCategory}
          </div>
          {aqiData.dominantPollutant && (
            <div className="text-sm text-gray-500">
              Primary: {aqiData.dominantPollutant}
            </div>
          )}
        </div>
      </div>
      
      {/* Health Message */}
      <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-4 mb-4">
        <p className="text-sm text-gray-700 dark:text-gray-300">
          {aqiData.healthMessage || getHealthAdvice(aqiData.aqiCategory)}
        </p>
      </div>
      
      {/* Timestamp */}
      <div className="flex items-center justify-between text-xs text-gray-500">
        <span>Last updated: {formatTimestamp(aqiData.timestamp)}</span>
        <span>{aqiData.measurements?.length || 0} measurements</span>
      </div>
      
      {/* AQI Scale Reference (hidden on mobile, shown on larger screens) */}
      <div className="hidden md:block mt-6 pt-4 border-t border-gray-200 dark:border-gray-700">
        <div className="grid grid-cols-6 gap-1 text-xs">
          <div className="text-center">
            <div className="h-2 bg-aqi-good rounded mb-1"></div>
            <span>Good<br />0-50</span>
          </div>
          <div className="text-center">
            <div className="h-2 bg-aqi-moderate rounded mb-1"></div>
            <span>Moderate<br />51-100</span>
          </div>
          <div className="text-center">
            <div className="h-2 bg-aqi-usg rounded mb-1"></div>
            <span>USG<br />101-150</span>
          </div>
          <div className="text-center">
            <div className="h-2 bg-aqi-unhealthy rounded mb-1"></div>
            <span>Unhealthy<br />151-200</span>
          </div>
          <div className="text-center">
            <div className="h-2 bg-aqi-very-unhealthy rounded mb-1"></div>
            <span>Very Unhealthy<br />201-300</span>
          </div>
          <div className="text-center">
            <div className="h-2 bg-aqi-hazardous rounded mb-1"></div>
            <span>Hazardous<br />301+</span>
          </div>
        </div>
      </div>
    </section>
  )
}