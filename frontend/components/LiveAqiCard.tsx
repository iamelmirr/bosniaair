'use client'

import { useLiveAqi } from '../lib/hooks'
import { cityIdToLabel, CityId } from '../lib/utils'

/// <summary>
/// Component that displays the current live air quality index (AQI) for a preferred city.
/// Shows the overall AQI value, category, dominant pollutant, health advice, and AQI scale.
/// Includes loading states, error handling, and a share button.
/// </summary>
interface LiveAqiCardProps {
  city: CityId
}

export default function LiveAqiCard({ city }: LiveAqiCardProps) {
  const { data: aqiData, error, isLoading } = useLiveAqi(city)
  const cityLabel = cityIdToLabel(city)

  // Loading skeleton while fetching AQI data
  if (isLoading) {
    return (
      <section className="bg-[rgb(var(--card))] rounded-xl p-4 sm:p-8 border border-[rgb(var(--border))] shadow-card animate-pulse-subtle">
        <div className="animate-pulse">
          <div className="flex items-baseline justify-between mb-4">
            <div className="h-6 bg-gray-300 dark:bg-gray-600 rounded w-48 loading-shimmer"></div>
            <div className="h-4 bg-gray-300 dark:bg-gray-600 rounded w-16 loading-shimmer"></div>
          </div>
          
          <div className="flex items-end gap-6 mb-4">
            <div className="h-20 bg-gray-300 dark:bg-gray-600 rounded w-32 loading-shimmer"></div>
            <div className="h-6 bg-gray-300 dark:bg-gray-600 rounded w-20 loading-shimmer"></div>
          </div>
          
          <div className="h-4 bg-gray-300 dark:bg-gray-600 rounded w-full mb-4 loading-shimmer"></div>
          <div className="h-3 bg-gray-300 dark:bg-gray-600 rounded w-48 loading-shimmer"></div>
        </div>
      </section>
    )
  }

  // Error state with retry button
  if (error) {
    return (
      <section className="bg-[rgb(var(--card))] rounded-xl p-4 sm:p-8 border border-red-300 dark:border-red-600 shadow-card md:hover:shadow-card-hover transition-all duration-300">
        <div className="text-center">
          <div className="text-4xl mb-4">⚠️</div>
          <h2 className="text-xl font-semibold text-red-600 dark:text-red-400 mb-2">
            Greška pri učitavanju podataka
          </h2>
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            {error.message || 'Molimo proverite internetsku konekciju i pokušajte ponovo.'}
          </p>
          <button
            onClick={() => window.location.reload()}
            className="px-4 py-2 bg-red-600 text-white rounded-lg 
                     hover:bg-red-700 hover:scale-105 active:scale-95
                     mobile-minimal-animation mobile-simple-hover
                     transition-all duration-200 shadow-md hover:shadow-lg"
          >
            🔄 Pokušaj ponovo
          </button>
        </div>
      </section>
    )
  }

  // No data available state
  if (!aqiData) {
    return (
      <section className="bg-[rgb(var(--card))] rounded-xl p-8 border border-[rgb(var(--border))] shadow-card">
        <div className="text-center text-gray-600 dark:text-gray-400">
          Nema dostupnih podataka za {cityLabel}
        </div>
      </section>
    )
  }

  /// <summary>
  /// Returns CSS class for AQI value color based on category
  /// </summary>
  /// <param name="aqi">The AQI value</param>
  /// <param name="category">The AQI category string</param>
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

  /// <summary>
  /// Translates English AQI category to Bosnian
  /// </summary>
  /// <param name="category">English AQI category</param>
  const translateAqiCategory = (category: string) => {
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

  /// <summary>
  /// Returns health advice text based on AQI category
  /// </summary>
  /// <param name="category">AQI category string</param>
  const getHealthAdvice = (category: string) => {
    switch (category.toLowerCase()) {
      case 'good':
        return 'Kvaliteta zraka je zadovoljavajuća. Uživajte u aktivnostima na otvorenom!'
      case 'moderate':
        return 'Prihvatljivo za većinu ljudi. Osjetljive osobe treba da ograniče dugotrajan boravak na otvorenom.'
      case 'unhealthy for sensitive groups':
        return 'Osjetljive grupe mogu osjetiti zdravstvene efekte. Ostala populacija vjerojatno neće biti pogođena.'
      case 'unhealthy':
        return 'Svi mogu početi osjećati zdravstvene efekte. Osjetljive grupe mogu imati ozbiljnije zdravstvene probleme.'
      case 'very unhealthy':
        return 'Zdravstvena upozorenja hitnih uslova. Cijela populacija je vjerovatna da bude pogođena.'
      case 'hazardous':
        return 'Zdravstvena uzbuna - svi mogu imati ozbiljnije zdravstvene efekte.'
      default:
        return 'Pratite uslove kvaliteta zraka.'
    }
  }

  /// <summary>
  /// Formats timestamp for display in Bosnian locale
  /// </summary>
  /// <param name="timestamp">Date object to format</param>
  const formatTimestamp = (timestamp: Date) => {
    return new Intl.DateTimeFormat('bs-BA', {
      dateStyle: 'short',
      timeStyle: 'short',
      timeZone: 'UTC',
    }).format(timestamp)
  }

  // Main card content with AQI data
  return (
    <section className="bg-[rgb(var(--card))] rounded-xl p-6 md:p-8 border border-[rgb(var(--border))] shadow-card md:hover:shadow-card-hover transition-all duration-300 md:hover:-translate-y-1">
      {/* Header with city name and live indicator */}
      <div className="flex align-center justify-between mb-6">
        <h2 className="sm:text-lg md:text-xl font-semibold text-[rgb(var(--text))]">
          Trenutni AQI — {cityLabel}
        </h2>
        <div className="flex items-center gap-2">
          <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse"></div>
          <span className="text-sm text-gray-500">Uživo</span>
        </div>
      </div>
      
      {/* Main AQI display with value and category */}
      <div className="flex flex-col md:flex-row md:items-end md:gap-6 mb-2 md:mb-6 text-center md:text-left animate-fade-in">
        <div className={`text-6xl font-bold ${getAqiColorClass(aqiData.overallAqi, aqiData.aqiCategory)} mb-2 md:mb-0 transition-all duration-500`}>
          {aqiData.overallAqi}
        </div>
        <div className="flex flex-col">
          <div className={`text-2xl font-medium ${getAqiColorClass(aqiData.overallAqi, aqiData.aqiCategory)} transition-all duration-300`}>
            {translateAqiCategory(aqiData.aqiCategory)}
          </div>
          {aqiData.dominantPollutant && (
            <div className="text-sm text-gray-500">
              Glavni: {aqiData.dominantPollutant}
            </div>
          )}
        </div>
      </div>

      {/* Health advice box */}
      <div className="bg-gray-50 dark:bg-gray-800 rounded-lg py-4 md:mb-4 transition-all duration-300 hover:bg-gray-100">
        <p className="text-center md:text-left text-sm text-gray-700 dark:text-gray-300">
          {getHealthAdvice(aqiData.aqiCategory)}
        </p>
      </div>

      {/* Footer with timestamp and share button */}
      <div className="hidden md:flex items-center justify-between text-xs text-gray-500">
        <span>Zadnje ažuriranje: {formatTimestamp(aqiData.timestamp)}</span>
        <button 
          onClick={() => {
            // Share current AQI data using Web Share API or fallback to clipboard
            if (navigator.share) {
              navigator.share({
                title: 'Kvaliteta zraka',
                text: 'Provjeri trenutni kvalitet zraka u tvom gradu: http://localhost:3000',
                url: 'http://localhost:3000'
              })
            } else {
              navigator.clipboard.writeText('Provjeri trenutni kvalitet zraka u tvom gradu: http://localhost:3000')
            }
          }}
          className="flex items-center gap-1 hover:text-blue-600 transition-colors p-1 -m-1"
          title="Podijeli"
        >
          <svg className="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
            <path d="M15 8a3 3 0 10-2.977-2.63l-4.94 2.47a3 3 0 100 4.319l4.94 2.47a3 3 0 10.895-1.789l-4.94-2.47C13.456 7.68 14.19 8 15 8z"/>
          </svg>
        </button>
      </div>

      {/* AQI scale reference (hidden on mobile) */}
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
    </section>
  )
}
