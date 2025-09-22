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

const GROUP_DESCRIPTIONS = {
  'Sportisti': 'Outdoor athletes and active individuals',
  'Djeca': 'Children and young people under 18',
  'Stariji': 'Adults over 65 years',
  'Astmatičari': 'People with asthma and respiratory conditions',
} as const

const RISK_COLORS = {
  'low': 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400',
  'moderate': 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400',
  'high': 'bg-orange-100 text-orange-800 dark:bg-orange-900/30 dark:text-orange-400',
  'very-high': 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400',
} as const

export default function GroupCard({ city }: GroupCardProps) {
  const { data, error, isLoading } = useGroups(city)

  if (isLoading) {
    return (
      <section className="bg-[rgb(var(--card))] rounded-xl p-6 border border-[rgb(var(--border))] shadow-card">
        <div className="animate-pulse">
          <div className="h-6 bg-gray-300 dark:bg-gray-600 rounded w-64 mb-4"></div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {[1, 2, 3, 4].map((i) => (
              <div key={i} className="border border-gray-200 dark:border-gray-700 rounded-lg p-4">
                <div className="h-4 bg-gray-300 dark:bg-gray-600 rounded w-20 mb-2"></div>
                <div className="h-3 bg-gray-300 dark:bg-gray-600 rounded w-full mb-2"></div>
                <div className="h-3 bg-gray-300 dark:bg-gray-600 rounded w-3/4"></div>
              </div>
            ))}
          </div>
        </div>
      </section>
    )
  }

  if (error) {
    return (
      <section className="bg-[rgb(var(--card))] rounded-xl p-6 border border-red-300 dark:border-red-600 shadow-card">
        <div className="text-center">
          <div className="text-4xl mb-4">👥</div>
          <h2 className="text-lg font-semibold text-red-600 dark:text-red-400 mb-2">
            Health Groups Unavailable
          </h2>
          <p className="text-gray-600 dark:text-gray-400">
            {error.message || 'Unable to load health group recommendations'}
          </p>
        </div>
      </section>
    )
  }

  if (!data) {
    return (
      <section className="bg-[rgb(var(--card))] rounded-xl p-6 border border-[rgb(var(--border))] shadow-card">
        <div className="text-center py-8">
          <div className="text-4xl mb-4">👥</div>
          <h2 className="text-lg font-semibold text-[rgb(var(--text))] mb-2">
            No Health Data
          </h2>
          <p className="text-gray-600 dark:text-gray-400">
            No health group data available for {city}
          </p>
        </div>
      </section>
    )
  }

  return (
    <section className="bg-[rgb(var(--card))] rounded-xl p-6 border border-[rgb(var(--border))] shadow-card">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-xl font-semibold text-[rgb(var(--text))]">
            Health Groups Dashboard
          </h2>
          <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
            Personalized recommendations for different groups
          </p>
        </div>
        
        {/* Overall AQI Badge */}
        <div className="text-center">
          <div className={`text-2xl font-bold ${getAqiCategoryClass(data.currentAqi)}`}>
            {data.currentAqi}
          </div>
          <div className="text-xs text-gray-500 uppercase tracking-wide">
            {data.aqiCategory}
          </div>
        </div>
      </div>

      {/* Groups Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
        {data.groups.map(({ group, currentRecommendation, riskLevel }) => (
          <div
            key={group.groupName}
            className="border border-gray-200 dark:border-gray-700 rounded-lg p-4 hover:shadow-md transition-shadow"
          >
            {/* Group Header */}
            <div className="flex items-center justify-between mb-3">
              <div className="flex items-center gap-2">
                <span className="text-2xl" role="img" aria-label={group.groupName}>
                  {GROUP_ICONS[group.groupName]}
                </span>
                <div>
                  <h3 className="font-semibold text-[rgb(var(--text))]">
                    {group.groupName}
                  </h3>
                  <p className="text-xs text-gray-500">
                    {GROUP_DESCRIPTIONS[group.groupName]}
                  </p>
                </div>
              </div>
              
              {/* Risk Level Badge */}
              <span
                className={classNames(
                  'px-2 py-1 rounded-full text-xs font-medium',
                  RISK_COLORS[riskLevel]
                )}
              >
                {riskLevel.replace('-', ' ')}
              </span>
            </div>

            {/* Recommendation */}
            <div className="bg-gray-50 dark:bg-gray-800 rounded-lg p-3">
              <p className="text-sm text-gray-700 dark:text-gray-300">
                {currentRecommendation}
              </p>
            </div>

            {/* AQI Threshold Info */}
            <div className="mt-3 pt-3 border-t border-gray-200 dark:border-gray-700">
              <div className="flex items-center justify-between text-xs text-gray-500">
                <span>Threshold: {group.aqiThreshold}</span>
                <span>Current: {data.currentAqi}</span>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* General Information */}
      <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-4 border border-blue-200 dark:border-blue-800">
        <div className="flex items-start gap-3">
          <div className="flex-shrink-0">
            <svg className="w-5 h-5 text-blue-600 dark:text-blue-400 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
            </svg>
          </div>
          <div className="flex-1">
            <h4 className="text-sm font-medium text-blue-900 dark:text-blue-100 mb-1">
              Health Recommendations
            </h4>
            <p className="text-sm text-blue-800 dark:text-blue-200">
              These recommendations are based on current air quality levels and EPA guidelines. 
              Consult healthcare providers for personalized medical advice.
            </p>
          </div>
        </div>
      </div>

      {/* Last Updated */}
      <div className="mt-4 text-center text-xs text-gray-500">
        Last updated: {new Intl.DateTimeFormat('bs-BA', {
          dateStyle: 'short',
          timeStyle: 'short',
        }).format(data.timestamp)}
      </div>
    </section>
  )
}