'use client'

import { useLiveAqi } from '../lib/hooks'
import { shareData } from '../lib/utils'
import { useState, useEffect } from 'react'
import LiveAqiCard from '../components/LiveAqiCard'
import PollutantCard from '../components/PollutantCard'
import Header from '../components/Header'
import DailyTimeline from '../components/DailyTimeline'
import GroupCard from '../components/GroupCard'
import CityComparison from '../components/CityComparison'

export default function HomePage() {
  const [isMobile, setIsMobile] = useState(false)
  
  // Fixed city as Sarajevo - no city selector
  const selectedCity = 'Sarajevo'
  const { data: aqiData, error, isLoading } = useLiveAqi(selectedCity)

  useEffect(() => {
    const checkIsMobile = () => {
      setIsMobile(window.innerWidth < 768)
    }
    
    checkIsMobile()
    window.addEventListener('resize', checkIsMobile)
    
    return () => window.removeEventListener('resize', checkIsMobile)
  }, [])

  const handleShare = async () => {
    try {
      await shareData(
        'SarajevoAir - Kvaliteta vazduha',
        'Proverite trenutnu kvalitetu vazduha u Sarajevu! Praćenje u realnom vremenu.',
        window.location.href
      )
    } catch (error) {
      console.log('Share failed:', error)
    }
  }

  if (error) {
    return (
      <main className="min-h-screen py-6">
        <Header onShare={handleShare} />
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center py-12">
            <h2 className="text-2xl font-bold text-red-600 dark:text-red-400 mb-4">
              Greška pri učitavanju podataka
            </h2>
            <p className="text-gray-600 dark:text-gray-400 mb-4">
              Nema dostupnih podataka za kvalitet vazduha u Sarajevu.
            </p>
            <p className="text-sm text-gray-500 dark:text-gray-500">
              Molimo pokušajte ponovo kasnije ili kontaktirajte podršku.
            </p>
          </div>
        </div>
      </main>
    )
  }

  return (
    <main className="min-h-screen py-6">
      <Header onShare={handleShare} />
      
      <div className="max-w-7xl mx-auto px-2 sm:px-2 lg:px-8">
        {/* Title Section - Focused on Sarajevo */}
        <div className="text-center mb-12">
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
            Kvaliteta vazduha u Sarajevu
          </h1>
          <p className="text-xl text-gray-600 dark:text-gray-400 mb-6">
            Praćenje kvaliteta vazduha u realnom vremenu
          </p>
        </div>

        {/* Main AQI Card - Full Width */}
        <div className="mb-8">
          <LiveAqiCard city="Sarajevo" />
        </div>

        {/* Pollutants Section */}
        <div className="mb-12">
          <h2 className="text-lg font-semibold mb-6 text-[rgb(var(--text))]">
            Merenja zagađivača
          </h2>
          <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-3 sm:gap-4">
                {aqiData?.measurements ? (
                  aqiData.measurements
                    .filter((measurement) => {
                      // On mobile, show only the most important pollutants
                      if (isMobile) {
                        const important = ['pm25', 'pm10', 'o3', 'no2']
                        return important.includes(measurement.parameter.toLowerCase().replace('.', ''))
                      }
                      return true
                    })
                    .map((measurement) => (
                    <PollutantCard 
                      key={measurement.parameter}
                      measurement={measurement}
                    />
                  ))
                ) : isLoading ? (
                  // Loading state
                  <div className="col-span-full text-center py-8 text-gray-500 dark:text-gray-400">
                    <div className="animate-spin inline-block w-6 h-6 border-[3px] border-current border-t-transparent text-blue-600 rounded-full" role="status" aria-label="loading">
                      <span className="sr-only">Loading...</span>
                    </div>
                    <p className="mt-2">Učitavam podatke...</p>
                  </div>
                ) : (
                  <div className="col-span-full text-center py-8 text-gray-500 dark:text-gray-400">
                    Nema dostupnih podataka o zagađivačima
                  </div>
                )}
              </div>
        </div>

        {/* Daily Timeline Section */}
        <div className="mb-12">
          <DailyTimeline city="Sarajevo" />
        </div>

        {/* Health Information Section */}
        <div className="mb-12">
          <GroupCard city="Sarajevo" />
        </div>

        {/* Compare Section - Optional for nearby cities */}
        <div className="mb-12">
          <CityComparison defaultCity="Sarajevo" />
        </div>

        {/* Info Section */}
        <div className="mb-8">
          <div className="bg-blue-50 dark:bg-blue-900/20 rounded-xl p-4 border border-blue-200 dark:border-blue-800/50">
            <h3 className="text-base font-semibold text-blue-900 dark:text-blue-100 mb-3">
              O aplikaciji
            </h3>
            <div className="text-sm text-blue-800 dark:text-blue-200 space-y-2">
              <p>• Real-time praćenje kvaliteta vazduha u Bosni i Hercegovini</p>
              <p>• 7-dnevni timeline sa prošlim i budućim podacima</p>
              <p>• Poređenje između gradova za bolji pregled situacije</p>
            </div>
          </div>
        </div>

        {/* Footer */}
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
  )
}