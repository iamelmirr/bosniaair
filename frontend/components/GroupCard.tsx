'use client'

import { useGroups } from '../lib/hooks'
import { getHealthAdvice, getAqiCategoryClass, classNames } from '../lib/utils'

interface GroupCardProps {
  city: string
}

const GROUP_ICONS = {
  'Sportisti': '🏃‍♂️',
  'Djeca': '👶',
  'Stariji': '👴',
  'Astmatičari': '🫁',
} as const

const RISK_COLORS = {
  'low': 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400',
  'moderate': 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400',
  'high': 'bg-orange-100 text-orange-800 dark:bg-orange-900/30 dark:text-orange-400',
  'very-high': 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400',
} as const

const RISK_TRANSLATIONS = {
  'low': 'Nizak',
  'moderate': 'Umjeren',
  'high': 'Visok',
  'very-high': 'Vrlo visok',
} as const

export default function GroupCard({ city }: GroupCardProps) {
  const { data, error, isLoading } = useGroups(city)

  if (isLoading) {
    return (
      <section className="bg-[rgb(var(--card))] rounded-xl p-4 border border-[rgb(var(--border))] shadow-card">
        <div className="animate-pulse">
          <div className="h-5 bg-gray-300 dark:bg-gray-600 rounded w-48 mb-3"></div>
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
            {[1, 2, 3, 4].map((i) => (
              <div key={i} className="border border-gray-200 dark:border-gray-700 rounded-lg p-3">
                <div className="h-4 bg-gray-300 dark:bg-gray-600 rounded w-16 mb-2"></div>
                <div className="h-2 bg-gray-300 dark:bg-gray-600 rounded w-full mb-1"></div>
                <div className="h-2 bg-gray-300 dark:bg-gray-600 rounded w-3/4"></div>
              </div>
            ))}
          </div>
        </div>
      </section>
    )
  }

  if (error) {
    return (
      <section className="bg-[rgb(var(--card))] rounded-xl p-4 border border-red-300 dark:border-red-600 shadow-card">
        <div className="text-center">
          <div className="text-3xl mb-3">👥</div>
          <h2 className="text-base font-semibold text-red-600 dark:text-red-400 mb-2">
            Greška pri učitavanju
          </h2>
          <p className="text-sm text-gray-600 dark:text-gray-400">
            {error.message || 'Nije moguće učitati preporuke'}
          </p>
        </div>
      </section>
    )
  }

  if (!data) {
    return (
      <section className="bg-[rgb(var(--card))] rounded-xl p-4 border border-[rgb(var(--border))] shadow-card">
        <div className="text-center py-6">
          <div className="text-3xl mb-3">👥</div>
          <h2 className="text-base font-semibold text-[rgb(var(--text))] mb-2">
            Nema podataka
          </h2>
          <p className="text-sm text-gray-600 dark:text-gray-400">
            Zdravstveni podaci nisu dostupni za {city}
          </p>
        </div>
      </section>
    )
  }

  return (
    <section className="bg-[rgb(var(--card))] rounded-xl p-4 border border-[rgb(var(--border))] shadow-card hover:shadow-card-hover transition-all">
      {/* Compact Header */}
      <div className="flex items-center justify-between mb-3">
        <div>
          <h2 className="text-lg font-semibold text-[rgb(var(--text))]">
            Zdravstveni savjeti
          </h2>
          <p className="text-xs text-gray-600 dark:text-gray-400">
            {data.city} • osjetljive grupe
          </p>
        </div>
        
        {/* Current AQI */}
        <div className="text-center">
          <div className={`text-lg font-bold ${getAqiCategoryClass(data.currentAqi)}`}>
            {data.currentAqi}
          </div>
          <div className="text-xs text-gray-500 uppercase tracking-wide">
            AQI
          </div>
        </div>
      </div>

      {/* Compact Groups Grid - 2x2 on mobile, 4x1 on desktop */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
        {data.groups.map(({ group, currentRecommendation, riskLevel }) => (
          <div
            key={group.groupName}
            className="border border-gray-200 dark:border-gray-700 rounded-lg p-3 hover:shadow-sm transition-shadow"
          >
            {/* Compact Group Header */}
            <div className="flex items-center justify-between mb-2">
              <div className="flex items-center gap-1.5">
                <span className="text-sm" role="img" aria-label={group.groupName}>
                  {GROUP_ICONS[group.groupName]}
                </span>
                <h3 className="font-medium text-[rgb(var(--text))] text-sm">
                  {group.groupName}
                </h3>
              </div>
              
              {/* Compact Risk Badge */}
              <span
                className={classNames(
                  'px-1.5 py-0.5 rounded text-xs font-medium',
                  RISK_COLORS[riskLevel]
                )}
              >
                {RISK_TRANSLATIONS[riskLevel]}
              </span>
            </div>

            {/* Compact Recommendation */}
            <div className="bg-gray-50 dark:bg-gray-800 rounded-md p-2">
              <p className="text-xs text-gray-700 dark:text-gray-300 leading-relaxed">
                {currentRecommendation}
              </p>
            </div>
          </div>
        ))}
      </div>
    </section>
  )
}
