'use client'

import { useState } from 'react'
import { useLiveMeasurements } from '../lib/hooks'
import { shareData, type City } from '../lib/utils'
import LiveAqiCard from '../components/LiveAqiCard'
import PollutantCard from '../components/PollutantCard'
import Header from '../components/Header'
import HistorySlider from '../components/HistorySlider'
import GroupCard from '../components/GroupCard'
import ComparePanel from '../components/ComparePanel'

export default function HomePage() {
  const [selectedCity, setSelectedCity] = useState<City>('Sarajevo')
  const { data: measurements, error, isLoading } = useLiveMeasurements(selectedCity)

  const handleShare = async () => {
    try {
      await shareData(
        'SarajevoAir - Air Quality',
        `Check out the current air quality in ${selectedCity}! Real-time monitoring for Bosnia & Herzegovina.`,
        window.location.href
      )
    } catch (error) {
      console.log('Share failed:', error)
    }
  }

  return (
    <main className="min-h-screen py-6">
      <Header
        selectedCity={selectedCity}
        onCityChange={setSelectedCity}
        onShare={handleShare}
      />

      {/* Main Content */}
      <div className="space-y-8">
        {/* Live AQI Card */}
        <LiveAqiCard city={selectedCity} />

        {/* Pollutants Grid */}
        <section>
          <h2 className="text-lg font-semibold mb-4 text-[rgb(var(--text))]">
            Pollutant Measurements
          </h2>
          
          {isLoading && (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {[1, 2, 3, 4, 5, 6].map((i) => (
                <div key={i} className="bg-[rgb(var(--card))] rounded-lg p-4 border border-[rgb(var(--border))] animate-pulse">
                  <div className="h-4 bg-gray-300 dark:bg-gray-600 rounded w-16 mb-3"></div>
                  <div className="h-8 bg-gray-300 dark:bg-gray-600 rounded w-24 mb-2"></div>
                  <div className="h-3 bg-gray-300 dark:bg-gray-600 rounded w-full"></div>
                </div>
              ))}
            </div>
          )}
          
          {error && (
            <div className="text-center py-8 text-red-600 dark:text-red-400">
              <p>Failed to load pollutant data: {error.message}</p>
            </div>
          )}
          
          {measurements && measurements.length > 0 && (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {measurements.map((measurement) => (
                <PollutantCard
                  key={`${measurement.parameter}-${measurement.timestamp}`}
                  measurement={measurement}
                />
              ))}
            </div>
          )}
          
          {measurements && measurements.length === 0 && !isLoading && (
            <div className="text-center py-8 text-gray-600 dark:text-gray-400">
              <p>No pollutant data available for {selectedCity}</p>
            </div>
          )}
        </section>

        {/* History Chart */}
        <HistorySlider city={selectedCity} />

        {/* Health Groups Dashboard */}
        <GroupCard city={selectedCity} />

        {/* City Comparison */}
        <ComparePanel currentCity={selectedCity} />
      </div>

      {/* Footer */}
      <footer className="mt-16 py-8 border-t border-[rgb(var(--border))] text-center">
        <div className="flex flex-col items-center space-y-4">
          <p className="text-sm text-gray-600">
            Data from{' '}
            <a 
              href="https://openaq.org" 
              target="_blank" 
              rel="noopener noreferrer"
              className="text-blue-600 hover:text-blue-800 underline"
            >
              OpenAQ
            </a>
          </p>
          <p className="text-xs text-gray-500">
            Built with Next.js, TypeScript & Tailwind CSS
          </p>
        </div>
      </footer>
    </main>
  )
}