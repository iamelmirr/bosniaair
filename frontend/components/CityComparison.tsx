'use client'

import React, { useState, useCallback, useMemo, useEffect } from 'react'
import { useLiveAqi } from '../lib/hooks'
import { CITY_OPTIONS, CityId, cityIdToLabel } from '../lib/utils'

interface CityComparisonProps {
  primaryCity: CityId
  comparisonCities: CityId[]
  onAddCity: (city: CityId) => void
  onRemoveCity: (city: CityId) => void
  maxComparisonCities?: number
}

interface CityCardProps {
  cityId: CityId
  isPrimary?: boolean
  onRemove?: (cityId: CityId) => void
  onDataUpdate: (cityId: CityId, data: LiveAirQualityData | null) => void
  referenceAqi?: number | null
}

interface LiveAirQualityData {
  city: string
  overallAqi: number
  aqiCategory: string
  color: string
  timestamp: Date
  dominantPollutant?: string
}

const AQI_CATEGORY_TRANSLATIONS = {
  Good: 'Dobro',
  Moderate: 'Umjereno',
  'Unhealthy for Sensitive Groups': 'Osjetljivo',
  Unhealthy: 'Nezdravo',
  'Very Unhealthy': 'Opasno',
  Hazardous: 'Fatalno'
} as const

const getAqiBackground = (aqi: number) => {
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

function CityCard({ cityId, isPrimary, onRemove, onDataUpdate, referenceAqi }: CityCardProps) {
  const { data, error, isLoading } = useLiveAqi(cityId)
  const cityLabel = cityIdToLabel(cityId)

  useEffect(() => {
    onDataUpdate(cityId, data ?? null)
    return () => {
      onDataUpdate(cityId, null)
    }
  }, [cityId, data, onDataUpdate])

  if (isLoading) {
    return (
      <div className={`rounded-xl border border-gray-200 dark:border-gray-700 ${isPrimary ? 'bg-blue-50 dark:bg-blue-900/10' : 'bg-[rgb(var(--card))]'} p-6 shadow-card`}>
        <div className="animate-pulse space-y-4">
          <div className="h-4 bg-gray-300 dark:bg-gray-700 rounded w-24 mx-auto"></div>
          <div className="h-12 bg-gray-300 dark:bg-gray-700 rounded w-16 mx-auto"></div>
          <div className="h-3 bg-gray-300 dark:bg-gray-700 rounded w-20 mx-auto"></div>
        </div>
      </div>
    )
  }

  if (error || !data) {
    return (
      <div className="rounded-xl border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/10 p-6 shadow-card">
        <div className="text-center space-y-3">
          <div className="text-sm font-semibold text-[rgb(var(--text))]">{cityLabel}</div>
          <div className="text-xs text-red-600 dark:text-red-400">Nema dostupnih podataka</div>
          {!isPrimary && onRemove && (
            <button
              onClick={() => onRemove(cityId)}
              className="text-xs text-red-500 hover:text-red-600"
            >
              Ukloni grad
            </button>
          )}
        </div>
      </div>
    )
  }

  const translatedCategory = AQI_CATEGORY_TRANSLATIONS[data.aqiCategory as keyof typeof AQI_CATEGORY_TRANSLATIONS] || data.aqiCategory
  const delta = referenceAqi != null ? data.overallAqi - referenceAqi : null

  return (
    <div className={`rounded-xl border ${getAqiBackground(data.overallAqi)} p-6 shadow-card hover:shadow-card-hover transition-all duration-300 hover:-translate-y-1 animate-fade-in ${isPrimary ? 'ring-2 ring-blue-200 dark:ring-blue-800' : ''}`}>
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <div className={`w-2 h-2 rounded-full ${isPrimary ? 'bg-blue-500' : 'bg-gray-400'}`}></div>
          <div className="text-sm font-semibold text-[rgb(var(--text))]">{cityLabel}</div>
        </div>
        {isPrimary ? (
          <span className="px-2 py-1 bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300 rounded-full text-xs font-medium">
            Glavni grad
          </span>
        ) : (
          onRemove && (
            <button
              onClick={() => onRemove(cityId)}
              className="text-xs text-gray-500 hover:text-red-500 transition-colors"
            >
              Ukloni
            </button>
          )
        )}
      </div>

      <div className="text-center mb-4">
        <div className={`text-4xl font-bold ${getAqiTextColor(data.overallAqi)} mb-1 transition-all duration-500`}>{data.overallAqi}</div>
        <div className="text-xs text-gray-500 dark:text-gray-400 uppercase tracking-wide">AQI Indeks</div>
      </div>

      <div className="text-center mb-4">
        <span className={`inline-block px-3 py-1 rounded-full text-xs font-semibold ${getAqiTextColor(data.overallAqi)} ${getAqiBackground(data.overallAqi)} border`}>
          {translatedCategory}
        </span>
      </div>

      {delta != null && delta !== 0 && (
        <div className="text-center text-xs text-gray-600 dark:text-gray-400">
          {delta > 0 ? `+${delta} u odnosu na glavni grad` : `${Math.abs(delta)} poena bolji AQI`}
        </div>
      )}

      <div className="text-center mt-4 text-[11px] text-gray-500 dark:text-gray-400">
        Zadnje ažuriranje: {new Intl.DateTimeFormat('bs-BA', { dateStyle: 'short', timeStyle: 'short' }).format(data.timestamp)}
      </div>
    </div>
  )
}

export default function CityComparison({
  primaryCity,
  comparisonCities,
  onAddCity,
  onRemoveCity,
  maxComparisonCities = 4
}: CityComparisonProps) {
  const [selectedCandidate, setSelectedCandidate] = useState<CityId | ''>('')
  const [cityData, setCityData] = useState<Partial<Record<CityId, LiveAirQualityData | null>>>({})

  const handleDataUpdate = useCallback((cityId: CityId, data: LiveAirQualityData | null) => {
    setCityData(prev => ({ ...prev, [cityId]: data }))
  }, [])

  const availableCities = useMemo(
    () => CITY_OPTIONS.filter(option => option.id !== primaryCity && !comparisonCities.includes(option.id)),
    [primaryCity, comparisonCities]
  )

  const primaryData = cityData[primaryCity] ?? null

  const rankedCities = useMemo(() => {
    const entries = [primaryCity, ...comparisonCities]
      .map(cityId => ({ cityId, data: cityData[cityId] }))
      .filter(entry => entry.data != null) as Array<{ cityId: CityId; data: LiveAirQualityData }>

    return entries.sort((a, b) => a.data.overallAqi - b.data.overallAqi)
  }, [cityData, primaryCity, comparisonCities])

  const handleAdd = () => {
    if (selectedCandidate && !comparisonCities.includes(selectedCandidate)) {
      onAddCity(selectedCandidate)
      setSelectedCandidate('')
    }
  }

  const slotsRemaining = maxComparisonCities - comparisonCities.length

  return (
    <section className="bg-[rgb(var(--card))] rounded-xl border border-[rgb(var(--border))] p-6 shadow-card space-y-6">
      <div className="text-center">
        <h2 className="text-xl font-semibold text-[rgb(var(--text))] mb-2">Poređenje gradova</h2>
        <p className="text-sm text-gray-600 dark:text-gray-400">
          Dodajte gradove za uporedni pregled kvaliteta zraka sa glavnim gradom.
        </p>
      </div>

      <div className="flex flex-col gap-4">
        <CityCard
          cityId={primaryCity}
          isPrimary
          onDataUpdate={handleDataUpdate}
        />

        {comparisonCities.length > 0 && (
          <div className="grid gap-4 md:grid-cols-2">
            {comparisonCities.map(cityId => (
              <CityCard
                key={cityId}
                cityId={cityId}
                onRemove={onRemoveCity}
                onDataUpdate={handleDataUpdate}
                referenceAqi={primaryData?.overallAqi ?? null}
              />
            ))}
          </div>
        )}
      </div>

      <div className="flex flex-col md:flex-row md:items-end gap-3 md:gap-4">
        <div className="flex-1">
          <label className="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">
            Dodaj novi grad za poređenje
          </label>
          <select
            value={selectedCandidate}
            onChange={event => setSelectedCandidate(event.target.value as CityId | '')}
            className="w-full rounded-lg border border-[rgb(var(--border))] bg-[rgb(var(--card))] px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400"
          >
            <option value="" disabled>
              {availableCities.length === 0 ? 'Svi gradovi su već dodani' : 'Odaberite grad'}
            </option>
            {availableCities.map(option => (
              <option key={option.id} value={option.id}>
                {option.name}
              </option>
            ))}
          </select>
        </div>

        <button
          onClick={handleAdd}
          disabled={!selectedCandidate || slotsRemaining <= 0}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
        >
          Dodaj grad
        </button>

        <span className="text-xs text-gray-500 dark:text-gray-400">
          Preostalo slotova: {Math.max(slotsRemaining, 0)}
        </span>
      </div>

      {rankedCities.length > 0 && (
        <div className="p-4 bg-gray-50 dark:bg-gray-800 rounded-xl border border-gray-200/40 dark:border-gray-700/40">
          <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-3">Aktuelni poredak</h3>
          <ol className="space-y-2 text-sm text-gray-600 dark:text-gray-300">
            {rankedCities.map(({ cityId, data }, index) => (
              <li key={cityId} className="flex items-center justify-between">
                <span>
                  {index + 1}. {cityIdToLabel(cityId)}
                </span>
                <span className="font-medium">AQI {data.overallAqi}</span>
              </li>
            ))}
          </ol>
        </div>
      )}
    </section>
  )
}
