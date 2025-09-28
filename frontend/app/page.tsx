'use client'

import { useEffect, useState } from 'react'
import LiveAqiCard from '../components/LiveAqiCard'
import PollutantCard from '../components/PollutantCard'
import Header from '../components/Header'
import DailyTimeline from '../components/DailyTimeline'
import GroupCard from '../components/GroupCard'
import CityComparison from '../components/CityComparison'
import CitySelectorModal from '../components/CitySelectorModal'
import { useLiveAqi, usePeriodicRefresh } from '../lib/hooks'
import { airQualityObservable } from '../lib/observable'
import {
  cityIdToLabel,
  DEFAULT_PRIMARY_CITY,
  isValidCityId,
  CityId
} from '../lib/utils'

const PRIMARY_CITY_STORAGE_KEY = 'bosniaair.primaryCity'

/// <summary>
/// Main page component for the BosniaAir app. Displays live AQI, forecasts, and city selection.
/// Handles user preferences and responsive layout.
/// </summary>
export default function HomePage() {
  const [primaryCity, setPrimaryCity] = useState<CityId | null>(null)
  const [isMobile, setIsMobile] = useState(false)
  const [isPreferencesModalOpen, setPreferencesModalOpen] = useState(false)
  const [preferencesLoaded, setPreferencesLoaded] = useState(false)

  const cityLabel = primaryCity ? cityIdToLabel(primaryCity) : ''
  const { data: aqiData, error, isLoading } = useLiveAqi(primaryCity)
  usePeriodicRefresh(60 * 1000) // Refresh every minute

  // Detect mobile screen size and update on resize
  useEffect(() => {
    const checkIsMobile = () => {
      setIsMobile(window.innerWidth < 768)
    }

    checkIsMobile()
    window.addEventListener('resize', checkIsMobile)

    return () => window.removeEventListener('resize', checkIsMobile)
  }, [])

  // Load user's primary city preference from localStorage
  useEffect(() => {
    try {
      const storedPrimary = localStorage.getItem(PRIMARY_CITY_STORAGE_KEY)

      if (isValidCityId(storedPrimary)) {
        setPrimaryCity(storedPrimary)
        setPreferencesModalOpen(false)
      } else {
        setPrimaryCity(null)
        setPreferencesModalOpen(true)
      }
    } catch {
      setPrimaryCity(null)
      setPreferencesModalOpen(true)
    } finally {
      setPreferencesLoaded(true)
    }
  }, [])

  // Save primary city to localStorage when it changes
  useEffect(() => {
    if (!preferencesLoaded || !primaryCity) {
      return
    }
    localStorage.setItem(PRIMARY_CITY_STORAGE_KEY, primaryCity)
  }, [preferencesLoaded, primaryCity])

  // Trigger data refresh when city changes
  useEffect(() => {
    if (!preferencesLoaded || !primaryCity) {
      return
    }

    airQualityObservable.notify()
  }, [primaryCity, preferencesLoaded])

  // Handle city selection from modal
  const handleModalSave = (city: CityId) => {
    setPrimaryCity(city)
    setPreferencesModalOpen(false)
  }

  // Show city selection modal if no city is chosen yet
  if (!primaryCity) {
    return (
      <>
        <main className="min-h-screen py-6 flex items-center justify-center">
          <div className="text-center">
            <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-4">
              Kvaliteta vazduha u BiH
            </h1>
            <p className="text-gray-600 dark:text-gray-400">
              Molimo odaberite grad za praćenje kvaliteta vazduha
            </p>
          </div>
        </main>
        
        <CitySelectorModal
          isOpen={isPreferencesModalOpen}
          onClose={() => setPreferencesModalOpen(false)}
          onSave={handleModalSave}
          initialPrimaryCity={DEFAULT_PRIMARY_CITY}
        />
      </>
    )
  }

  // Display error state if data loading failed
  if (error) {
    return (
      <main className="min-h-screen py-6">
        <Header
          onOpenCitySettings={() => setPreferencesModalOpen(true)}
          selectedCityLabel={cityLabel}
        />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center py-12">
            <h2 className="text-2xl font-bold text-red-600 dark:text-red-400 mb-4">
              Greška pri učitavanju podataka
            </h2>
            <p className="text-gray-600 dark:text-gray-400 mb-4">
              Trenutno nema dostupnih podataka za kvalitet zraka u {cityLabel}.
            </p>
            <p className="text-sm text-gray-500 dark:text-gray-500">
              Molimo pokušajte ponovo kasnije ili kontaktirajte podršku.
            </p>
          </div>
        </div>
      </main>
    )
  }

  // Main application content with all AQI components
  return (
    <>
      <main className="min-h-screen pb-6">
        <Header
          onOpenCitySettings={() => setPreferencesModalOpen(true)}
          selectedCityLabel={cityLabel}
        />

        <div className="max-w-7xl mx-auto px-2 sm:px-2 lg:px-8">
          {/* Page title and description */}
          <div className="text-center mb-12">
            <h1 className="text-2xl md:text-4xl font-bold text-gray-900 dark:text-white mb-4">
              Kvalitet zraka u gradu {cityLabel}
            </h1>
            <p className="text-lg md:text-xl text-gray-600 dark:text-gray-400 mb-6">
              Praćenje indeksa kvaliteta zraka
            </p>
          </div>

          {/* Live AQI card showing current air quality */}
          <div className="mb-8">
            <LiveAqiCard city={primaryCity!} />
          </div>

          {/* Pollutant measurements grid */}
          <div className="mb-12">
            <h2 className="text-lg font-semibold mb-6 text-[rgb(var(--text))]">
              Mjerenja zagađivača
            </h2>
            <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-3 sm:gap-4">
              {aqiData?.measurements ? (
                aqiData.measurements
                  .filter(measurement => {
                    // On mobile, show only the most important pollutants
                    if (isMobile) {
                      const important = ['pm25', 'pm10', 'o3', 'no2']
                      return important.includes(measurement.parameter.toLowerCase().replace('.', ''))
                    }
                    return true
                  })
                  .map((measurement, index) => (
                    <div key={measurement.parameter} className="animate-fade-in" style={{ animationDelay: `${index * 0.1}s` }}>
                      <PollutantCard measurement={measurement} />
                    </div>
                  ))
              ) : isLoading ? (
                // Loading spinner while fetching data
                <div className="col-span-full text-center py-8 text-gray-500 dark:text-gray-400">
                  <div className="animate-spin inline-block w-6 h-6 border-[3px] border-current border-t-transparent text-blue-600 rounded-full" role="status" aria-label="loading">
                    <span className="sr-only">Loading...</span>
                  </div>
                  <p className="mt-2">Učitavam podatke...</p>
                </div>
              ) : (
                // No measurements available message
                <div className="col-span-full text-center py-8 text-gray-500 dark:text-gray-400">
                  Trenutno nema dostupnih mjerenja za {cityLabel}
                </div>
              )}
            </div>
          </div>

          {/* Daily timeline showing historical and forecast data */}
          <div className="mb-12">
            <DailyTimeline city={primaryCity!} />
          </div>

          {/* Health advice based on current AQI */}
          <div className="mb-12">
            <GroupCard city={primaryCity!} />
          </div>

          {/* City comparison component */}
          <div className="mb-12">
            <CityComparison key={primaryCity} primaryCity={primaryCity!} />
          </div>

          {/* About section with app features */}
          <div className="mb-8">
            <div className="bg-blue-50 dark:bg-blue-900/20 rounded-xl p-4 border border-blue-200 dark:border-blue-800/50">
              <h3 className="text-base font-semibold text-blue-900 dark:text-blue-100 mb-3">
                O aplikaciji
              </h3>
              <div className="text-sm text-blue-800 dark:text-blue-200 space-y-2">
                <p>• Real-time praćenje kvalitete zraka u Bosni i Hercegovini</p>
                <p>• Prognoza kvalitete zraka</p>
                <p>• Usporedba kvalitete zraka između gradova</p>
              </div>
            </div>
          </div>

          {/* Footer with data source and contact info */}
          <footer className="text-center py-6 border-t border-gray-200 dark:border-gray-700">
            <div className="text-sm text-gray-600 dark:text-gray-400 space-y-2">
              <p>
                Podaci se preuzimaju sa{' '}
                <a
                  href="https://aqicn.org"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-blue-600 dark:text-blue-400 hover:underline font-medium"
                >
                  aqicn.org
                </a>
              </p>
              <div className="pt-2">
                <p className="text-gray-500 dark:text-gray-500">Say hi :)</p>
                <p>
                  <a
                    href="mailto:besirovicelmir36@gmail.com"
                    className="text-blue-600 dark:text-blue-400 hover:underline"
                  >
                    besirovicelmir36@gmail.com
                  </a>
                </p>
              </div>
            </div>
          </footer>
        </div>
      </main>

      <CitySelectorModal
        isOpen={isPreferencesModalOpen}
        onClose={() => setPreferencesModalOpen(false)}
        onSave={handleModalSave}
        initialPrimaryCity={primaryCity || DEFAULT_PRIMARY_CITY}
      />
    </>
  )
}