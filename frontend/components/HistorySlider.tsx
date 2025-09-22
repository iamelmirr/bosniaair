'use client'

import { useState } from 'react'
import HistoryChart from './HistoryChart'
import { POLLUTANT_NAMES, classNames, type PollutantKey } from '../lib/utils'

interface HistorySliderProps {
  city: string
}

const TIME_PERIODS = [
  { days: 1, label: '24h', aggregation: 'hourly' as const },
  { days: 3, label: '3 days', aggregation: 'hourly' as const },
  { days: 7, label: '7 days', aggregation: 'hourly' as const },
  { days: 30, label: '30 days', aggregation: 'daily' as const },
]

type TimePeriod = typeof TIME_PERIODS[number]

const POLLUTANTS = Object.entries(POLLUTANT_NAMES).map(([key, name]) => ({
  key: key as PollutantKey,
  name,
}))

export default function HistorySlider({ city }: HistorySliderProps) {
  const [selectedPeriod, setSelectedPeriod] = useState<TimePeriod>(TIME_PERIODS[2]) // Default to 7 days
  const [selectedPollutant, setSelectedPollutant] = useState<PollutantKey>('pm25')

  return (
    <section className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold text-[rgb(var(--text))]">
            Historical Trends
          </h2>
          <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
            Explore air quality patterns over time
          </p>
        </div>
        
        {/* Mobile-friendly controls icon */}
        <div className="md:hidden">
          <svg className="w-5 h-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 100 4m0-4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 100 4m0-4v2m0-6V4" />
          </svg>
        </div>
      </div>

      {/* Controls */}
      <div className="bg-[rgb(var(--card))] rounded-xl p-4 border border-[rgb(var(--border))] space-y-4">
        {/* Pollutant Selection */}
        <div>
          <label className="block text-sm font-medium text-[rgb(var(--text))] mb-3">
            Select Pollutant
          </label>
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-6 gap-2">
            {POLLUTANTS.map((pollutant) => (
              <button
                key={pollutant.key}
                onClick={() => setSelectedPollutant(pollutant.key)}
                className={classNames(
                  'px-3 py-2 rounded-lg text-sm font-medium transition-all duration-200',
                  'focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                  selectedPollutant === pollutant.key
                    ? 'bg-blue-600 text-white shadow-md'
                    : 'bg-gray-100 dark:bg-gray-700 text-[rgb(var(--text))] hover:bg-gray-200 dark:hover:bg-gray-600'
                )}
              >
                {pollutant.name}
              </button>
            ))}
          </div>
        </div>

        {/* Time Period Selection */}
        <div>
          <label className="block text-sm font-medium text-[rgb(var(--text))] mb-3">
            Time Period
          </label>
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
            {TIME_PERIODS.map((period) => (
              <button
                key={period.label}
                onClick={() => setSelectedPeriod(period)}
                className={classNames(
                  'px-4 py-3 rounded-lg text-sm font-medium transition-all duration-200',
                  'focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                  selectedPeriod.days === period.days
                    ? 'bg-blue-600 text-white shadow-md transform scale-105'
                    : 'bg-gray-100 dark:bg-gray-700 text-[rgb(var(--text))] hover:bg-gray-200 dark:hover:bg-gray-600 hover:scale-105'
                )}
              >
                <div className="text-base font-semibold">{period.label}</div>
                <div className="text-xs opacity-75">
                  {period.aggregation}
                </div>
              </button>
            ))}
          </div>
        </div>

        {/* Current Selection Summary */}
        <div className="flex flex-wrap items-center gap-2 pt-2 border-t border-gray-200 dark:border-gray-700">
          <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400">
            <span>📊</span>
            <span>
              Showing <strong className="text-[rgb(var(--text))]">{POLLUTANT_NAMES[selectedPollutant]}</strong> for 
              <strong className="text-[rgb(var(--text))]"> {city}</strong> over the last
              <strong className="text-[rgb(var(--text))]"> {selectedPeriod.label}</strong>
            </span>
          </div>
        </div>
      </div>

      {/* Chart */}
      <HistoryChart
        city={city}
        parameter={selectedPollutant}
        days={selectedPeriod.days}
        aggregation={selectedPeriod.aggregation}
      />

      {/* Information Panel */}
      <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-4 border border-blue-200 dark:border-blue-800">
        <div className="flex items-start gap-3">
          <div className="flex-shrink-0">
            <svg className="w-5 h-5 text-blue-600 dark:text-blue-400 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <div className="flex-1">
            <h4 className="text-sm font-medium text-blue-900 dark:text-blue-100 mb-1">
              About {POLLUTANT_NAMES[selectedPollutant]} Monitoring
            </h4>
            <p className="text-sm text-blue-800 dark:text-blue-200">
              {getPollutantInfo(selectedPollutant)}
            </p>
          </div>
        </div>
      </div>
    </section>
  )
}

function getPollutantInfo(pollutant: PollutantKey): string {
  const info = {
    pm25: 'Fine particles (≤2.5μm) that penetrate deep into lungs and bloodstream. Primary concern for respiratory and cardiovascular health.',
    pm10: 'Coarse particles (≤10μm) from dust, pollen, and mold. Can irritate eyes, nose, and throat.',
    o3: 'Ground-level ozone formed by reactions between pollutants and sunlight. Triggers asthma and respiratory issues.',
    no2: 'Nitrogen dioxide from vehicle emissions and power plants. Linked to respiratory symptoms and reduced lung function.',
    so2: 'Sulfur dioxide from fossil fuel combustion. Can cause breathing difficulties and aggravate existing heart conditions.',
    co: 'Colorless, odorless gas from incomplete combustion. Reduces oxygen delivery to organs and tissues.',
  }
  
  return info[pollutant] || 'Monitor levels for health and environmental impact.'
}