'use client'

import { useLiveAqi } from '../lib/hooks'
import { cityIdToLabel, CityId } from '../lib/utils'

interface LiveAqiCardProps {
  city: CityId
}

export default function LiveAqiCard({ city }: LiveAqiCardProps) {
  const { data: aqiData, error, isLoading } = useLiveAqi(city)
  const cityLabel = cityIdToLabel(city)

  if (isLoading) {
    return (
      <section className="bg-[rgb(var(--card))] rounded-xl p-8 border border-[rgb(var(--border))] shadow-card hover:shadow-card-hover transition-all duration-300">
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
      <section className="bg-[rgb(var(--card))] rounded-xl p-8 border border-red-300 dark:border-red-600 shadow-card hover:shadow-card-hover transition-all duration-300">
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
            className="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors"
          >
            Pokušaj ponovo
          </button>
        </div>
      </section>
    )
  }

  if (!aqiData) {
    return (
      <section className="bg-[rgb(var(--card))] rounded-xl p-8 border border-[rgb(var(--border))] shadow-card">
        <div className="text-center text-gray-600 dark:text-gray-400">
          Nema dostupnih podataka za {cityLabel}
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

  const getHealthAdvice = (category: string) => {
    switch (category.toLowerCase()) {
      case 'good':
        return 'Kvaliteta zraka je zadovoljavajuća. Uživajte u aktivnostima na otvorenom!'
      case 'moderate':
        return 'Prihvatljivo za većinu ljudi. Osjetljive osobe treba da ograniče dugotrajan boravak na otvorenom.'
      case 'unhealthy for sensitive groups':
        return 'Osjetljive grupe mogu osjetiti zdravstvene efekte. Ostala populacija vjerojatno neće biti pogođena.'
      case 'unhealthy':
        return 'Svi mogu početi da osjeća zdravstvene efekte. Osjetljive grupe mogu imati ozbiljnije zdravstvene probleme.'
      case 'very unhealthy':
        return 'Zdravstvena upozorenja hitnih uslova. Cijela populacija je vjerojatnija da bude pogođena.'
      case 'hazardous':
        return 'Zdravstvena uzbuna: svi mogu imati ozbiljnije zdravstvene efekte.'
      default:
        return 'Pratite uslove kvaliteta zraka.'
    }
  }

  const formatTimestamp = (timestamp: Date) => {
    return new Intl.DateTimeFormat('bs-BA', {
      dateStyle: 'short',
      timeStyle: 'short',
      timeZone: 'UTC', // Timestamp is already in local Sarajevo time, don't convert
    }).format(timestamp)
  }

  return (
    <section className="bg-[rgb(var(--card))] rounded-xl p-8 border border-[rgb(var(--border))] shadow-card hover:shadow-card-hover transition-all duration-300 hover:-translate-y-1">
      {/* Header */}
      <div className="flex items-baseline justify-between mb-6">
        <h2 className="text-xl font-semibold text-[rgb(var(--text))]">
          Trenutni AQI — {cityLabel}
        </h2>
        <div className="flex items-center gap-2">
          <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse"></div>
          <span className="text-sm text-gray-500">Uživo</span>
        </div>
      </div>
      
      {/* Main AQI Display */}
      <div className="flex flex-col md:flex-row md:items-end md:gap-6 mb-6 text-center md:text-left animate-fade-in">
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
      
      {/* Health Message */}
      <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-4 mb-4 transition-all duration-300 hover:bg-gray-100 dark:hover:bg-gray-700">
        <p className="text-sm text-gray-700 dark:text-gray-300">
          {getHealthAdvice(aqiData.aqiCategory)}
        </p>
      </div>
      
      {/* Timestamp */}
      <div className="flex items-center justify-between text-xs text-gray-500">
        <span>Zadnje ažuriranje: {formatTimestamp(aqiData.timestamp)}</span>
        <button 
          onClick={() => {
            if (navigator.share) {
              navigator.share({
                title: 'Kvaliteta zraka u ' + cityLabel,
                text: 'Trenutni AQI: ' + aqiData.overallAqi + ' (' + translateAqiCategory(aqiData.aqiCategory) + ')',
                url: window.location.href
              })
            } else {
              navigator.clipboard.writeText(window.location.href)
            }
          }}
          className="flex items-center gap-1 hover:text-blue-600 transition-colors p-1 -m-1"
          title="Podijeli"
        >
          <svg className="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
            <path d="M15 8a3 3 0 10-2.977-2.63l-4.94 2.47a3 3 0 100 4.319l4.94 2.47a3 3 0 10.895-1.789l-4.94-2.47a3.027 3.027 0 000-.74l4.94-2.47C13.456 7.68 14.19 8 15 8z"/>
          </svg>
        </button>
      </div>
      
      {/* AQI Scale Reference */}
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
