'use client'

import { useComplete } from '../lib/hooks'
import { getAqiCategoryClass, classNames, cityIdToLabel, CityId } from '../lib/utils'
import { getAllHealthAdvice, RISK_COLORS, RISK_TRANSLATIONS } from '../lib/health-advice'

interface GroupCardProps {
  city: CityId
}

// All group constants moved to health-advice.ts

// AQI Category translations to Bosnian
const getAqiCategoryBosnian = (aqi: number): string => {
  if (aqi <= 50) return 'Dobro'
  if (aqi <= 100) return 'Umjereno'
  if (aqi <= 150) return 'Osjetljivo'
  if (aqi <= 200) return 'Nezdravo'
  if (aqi <= 300) return 'Opasno'
  return 'Fatalno'
}

export default function GroupCard({ city }: GroupCardProps) {
  const cityLabel = cityIdToLabel(city)
  const { data: completeData, error, isLoading } = useComplete(city)
  const liveData = completeData?.liveData
  const healthAdvice = liveData ? getAllHealthAdvice(liveData.overallAqi) : null

  if (isLoading) {
    return (
      <section className="bg-[rgb(var(--card))] rounded-xl p-6 border border-[rgb(var(--border))] shadow-card animate-pulse-subtle">
        <div className="animate-pulse">
          <div className="flex items-center gap-2 mb-4">
            <div className="animate-heartbeat text-2xl">👥</div>
            <div className="h-6 bg-gray-300 dark:bg-gray-600 rounded w-48 loading-shimmer"></div>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {[1, 2, 3, 4].map((i) => (
              <div key={i} className="border border-gray-200 dark:border-gray-700 rounded-lg p-4 animate-fade-in-up" style={{ animationDelay: `${i * 100}ms` }}>
                <div className="flex items-center gap-2 mb-2">
                  <div className="w-6 h-6 bg-gray-300 dark:bg-gray-600 rounded-full loading-shimmer"></div>
                  <div className="h-4 bg-gray-300 dark:bg-gray-600 rounded w-20 loading-shimmer"></div>
                </div>
                <div className="h-3 bg-gray-300 dark:bg-gray-600 rounded w-full mb-2 loading-shimmer"></div>
                <div className="h-3 bg-gray-300 dark:bg-gray-600 rounded w-3/4 loading-shimmer"></div>
              </div>
            ))}
          </div>
        </div>
      </section>
    )
  }

  if (error) {
    return (
      <section className="bg-[rgb(var(--card))] rounded-xl p-6 border border-red-300 dark:border-red-600 shadow-card hover:shadow-card-hover transition-all duration-300">
        <div className="text-center">
          <div className="text-4xl mb-4">👥</div>
          <h2 className="text-lg font-semibold text-red-600 dark:text-red-400 mb-2">
            Greška pri učitavanju zdravstvenih savjeta
          </h2>
          <p className="text-gray-600 dark:text-gray-400">
            {error.message || 'Nije moguće učitati preporuke za zdravstvene grupe'}
          </p>
        </div>
      </section>
    )
  }

  if (!liveData || !healthAdvice) {
    return (
      <section className="bg-[rgb(var(--card))] rounded-xl p-6 border border-[rgb(var(--border))] shadow-card">
        <div className="text-center py-8">
          <div className="text-4xl mb-4">👥</div>
          <h2 className="text-lg font-semibold text-[rgb(var(--text))] mb-2">
            Nema zdravstvenih podataka
          </h2>
          <p className="text-gray-600 dark:text-gray-400">
            Zdravstveni podaci nisu dostupni za {cityLabel}
          </p>
        </div>
      </section>
    )
  }

  return (
    <section className="bg-[rgb(var(--card))] rounded-xl p-6 border border-[rgb(var(--border))] shadow-card">
      {/* Header */}
      <div className="flex items-center justify-between mb-4">
        <div>
          <h2 className="text-xl font-semibold text-[rgb(var(--text))]">
            Zdravstveni savjeti
          </h2>
          <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
            Preporuke za osjetljive grupe stanovništva u gradu {cityLabel}
          </p>
        </div>
        
        {/* Overall AQI Badge */}
        <div className="text-center">
          <div className={`text-2xl font-bold transition-colors duration-200 ${getAqiCategoryClass(liveData.overallAqi)}`}>
            {liveData.overallAqi}
          </div>
          <div className="text-xs text-gray-500 uppercase tracking-wide">
            {getAqiCategoryBosnian(liveData.overallAqi)}
          </div>
        </div>
      </div>

      {/* Groups Grid - Single column on mobile, double column on tablet+ */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {healthAdvice.map((advice, index) => (
          <div
            key={advice.group}
            className="border border-gray-200 dark:border-gray-700 rounded-lg p-4 
                     hover:bg-gray-50 dark:hover:bg-gray-800/50 
                     mobile-minimal-animation mobile-simple-hover
                     md:hover:shadow-sm md:hover:border-blue-200 dark:md:hover:border-blue-700
                     animate-fade-in-up"
            style={{ animationDelay: `${index * 100}ms` }}
          >
            {/* Group Header */}
            <div className="flex items-start justify-between mb-3">
              <div className="flex items-center gap-2">
                <span className="text-2xl" role="img" aria-label={advice.group}>
                  {advice.icon}
                </span>
                <div>
                  <h3 className="font-semibold text-[rgb(var(--text))]">
                    {advice.group}
                  </h3>
                  <p className="text-xs text-gray-500 dark:text-gray-400">
                    {advice.description}
                  </p>
                </div>
              </div>
              
              {/* Risk Level Badge */}
              <span
                className={classNames(
                  'px-2 py-1 rounded-full text-xs font-medium flex-shrink-0',
                  RISK_COLORS[advice.riskLevel]
                )}
              >
                {RISK_TRANSLATIONS[advice.riskLevel]}
              </span>
            </div>

            {/* Health Recommendation */}
            <div className="bg-gray-50 dark:bg-gray-800 rounded-md p-3">
              <p className="text-sm text-gray-700 dark:text-gray-300 leading-relaxed">
                {advice.recommendation}
              </p>
            </div>
          </div>
        ))}
      </div>
    </section>
  )
}
