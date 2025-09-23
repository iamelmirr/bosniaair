'use client'

import { useLiveAqi } from '../lib/hooks'
import { shareData } from '../lib/utils'
import LiveAqiCard from '../components/LiveAqiCard'
import PollutantCard from '../components/PollutantCard'
import Header from '../components/Header'
import HistorySlider from '../components/HistorySlider'
import GroupCard from '../components/GroupCard'
import CityComparison from '../components/CityComparison'

export default function HomePage() {
  
  // Fixed city as Sarajevo - no city selector
  const selectedCity = 'Sarajevo'
  const { data: aqiData, error, isLoading } = useLiveAqi(selectedCity)

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
      
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Title Section - Focused on Sarajevo */}
        <div className="text-center mb-12">
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
            Kvaliteta vazduha u Sarajevu
          </h1>
          <p className="text-xl text-gray-600 dark:text-gray-400 mb-6">
            Praćenje kvaliteta vazduha u realnom vremenu
          </p>
        </div>

        {/* Current Air Quality Section */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 mb-12">
          <div className="lg:col-span-1">
            <LiveAqiCard city="Sarajevo" />
          </div>
          
          <div className="lg:col-span-2 lg:flex lg:items-center">
            <div className="w-full">
              <h2 className="text-lg font-semibold mb-4 text-[rgb(var(--text))] lg:hidden">
                Merenja zagađivača
              </h2>
              <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4 lg:gap-6">
                {aqiData?.measurements ? (
                  aqiData.measurements.map((measurement) => (
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
          </div>
        </div>

        {/* Historical Data Section */}
        <div className="mb-12">
          <div className="bg-white dark:bg-gray-800 rounded-xl shadow-lg p-6">
            <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-6">
              Istorijski podaci
            </h2>
            
            <HistorySlider city="Sarajevo" />
          </div>
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
        <div className="mb-12">
          <div className="bg-blue-50 dark:bg-blue-900/20 rounded-xl p-6">
            <h3 className="text-lg font-semibold text-blue-900 dark:text-blue-100 mb-3">
              O podacima
            </h3>
            <div className="text-sm text-blue-800 dark:text-blue-200 space-y-2">
              <p>
                • Podaci se ažuriraju svakih 30 minuta sa OpenAQ platforme
              </p>
              <p>
                • AQI (Indeks kvaliteta vazduha) se računa prema EPA standardima
              </p>
              <p>
                • Merenja pokrivaju područje Sarajeva i najbližu okolinu
              </p>
              <p>
                • Podaci su dostupni javnosti za edukaciju i informisanje
              </p>
            </div>
          </div>
        </div>

        {/* Footer */}
        <footer className="text-center py-8 border-t border-gray-200 dark:border-gray-700">
          <div className="text-sm text-gray-600 dark:text-gray-400 space-y-2">
            <p>
              Podaci o kvalitetu vazduha prikupljeni putem{' '}
              <a 
                href="https://openaq.org" 
                target="_blank" 
                rel="noopener noreferrer"
                className="text-blue-600 dark:text-blue-400 hover:underline font-medium"
              >
                OpenAQ platforme
              </a>
            </p>
            <p>
              AQI kalkulacija u skladu sa EPA standardima • Podaci za edukaciju i informisanje
            </p>
          </div>
        </footer>
      </div>
    </main>
  )
}