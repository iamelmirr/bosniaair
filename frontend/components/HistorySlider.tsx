'use client'

import WeeklyAqiSlider from './WeeklyAqiSlider'

interface HistorySliderProps {
  city: string
}

export default function HistorySlider({ city }: HistorySliderProps) {
  return (
    <section className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold text-[rgb(var(--text))]">
            Historical Trends
          </h2>
          <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
            Weekly air quality pattern for {city}
          </p>
        </div>
      </div>

      {/* Weekly AQI Slider */}
      <WeeklyAqiSlider city={city} />
    </section>
  )
}