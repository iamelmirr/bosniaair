'use client'

import { useEffect, useMemo, useState } from 'react'
import { CITY_OPTIONS, CityId } from '../lib/utils'

interface CitySelectorModalProps {
  isOpen: boolean
  onClose: () => void
  onSave: (primaryCity: CityId, comparisonCities: CityId[]) => void
  initialPrimaryCity: CityId
  initialComparisonCities: CityId[]
  maxComparisonCities?: number
}

export default function CitySelectorModal({
  isOpen,
  onClose,
  onSave,
  initialPrimaryCity,
  initialComparisonCities,
  maxComparisonCities = 4
}: CitySelectorModalProps) {
  const [primarySelection, setPrimarySelection] = useState<CityId>(initialPrimaryCity)
  const [comparisonSelection, setComparisonSelection] = useState<CityId[]>(initialComparisonCities)

  useEffect(() => {
    if (isOpen) {
      setPrimarySelection(initialPrimaryCity)
      setComparisonSelection(initialComparisonCities)
    }
  }, [isOpen, initialPrimaryCity, initialComparisonCities])

  const availableComparisonCities = useMemo(
    () => CITY_OPTIONS.filter(option => option.id !== primarySelection),
    [primarySelection]
  )

  const handleToggleComparison = (cityId: CityId) => {
    setComparisonSelection(prev => {
      if (prev.includes(cityId)) {
        return prev.filter(id => id !== cityId)
      }
      if (prev.length >= maxComparisonCities) {
        return prev
      }
      return [...prev, cityId]
    })
  }

  const handleSave = () => {
    const filteredComparisons = comparisonSelection.filter(cityId => cityId !== primarySelection)
    onSave(primarySelection, filteredComparisons)
    onClose()
  }

  if (!isOpen) {
    return null
  }

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
      <div className="w-full max-w-xl rounded-2xl bg-[rgb(var(--card))] border border-[rgb(var(--border))] shadow-2xl">
        <div className="p-6 border-b border-[rgb(var(--border))]">
          <h2 className="text-xl font-semibold text-[rgb(var(--text))]">Odaberi gradove</h2>
          <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
            Izaberite glavni grad i gradove za poređenje. Promjene će biti spremljene u vašem uređaju.
          </p>
        </div>

        <div className="p-6 space-y-6 max-h-[70vh] overflow-y-auto">
          <section>
            <h3 className="text-sm font-semibold text-[rgb(var(--text))] mb-3">Glavni grad</h3>
            <div className="grid gap-2 sm:grid-cols-2">
              {CITY_OPTIONS.map(option => (
                <label
                  key={option.id}
                  className={`flex items-center gap-3 rounded-xl border px-3 py-2 cursor-pointer transition-colors ${
                    option.id === primarySelection
                      ? 'border-blue-400 bg-blue-50 dark:bg-blue-900/20'
                      : 'border-[rgb(var(--border))] hover:border-blue-300'
                  }`}
                >
                  <input
                    type="radio"
                    name="primary-city"
                    checked={option.id === primarySelection}
                    onChange={() => {
                      setPrimarySelection(option.id)
                      if (comparisonSelection.includes(option.id)) {
                        setComparisonSelection(prev => prev.filter(id => id !== option.id))
                      }
                    }}
                    className="text-blue-500 focus:ring-blue-400"
                  />
                  <span className="text-sm font-medium text-[rgb(var(--text))]">{option.name}</span>
                </label>
              ))}
            </div>
          </section>

          <section>
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-sm font-semibold text-[rgb(var(--text))]">Gradovi za poređenje</h3>
              <span className="text-xs text-gray-500 dark:text-gray-400">
                {comparisonSelection.length}/{maxComparisonCities}
              </span>
            </div>
            <div className="grid gap-2 sm:grid-cols-2">
              {availableComparisonCities.map(option => {
                const isChecked = comparisonSelection.includes(option.id)
                const disabled = !isChecked && comparisonSelection.length >= maxComparisonCities
                return (
                  <label
                    key={option.id}
                    className={`flex items-center gap-3 rounded-xl border px-3 py-2 cursor-pointer transition-colors ${
                      isChecked
                        ? 'border-blue-400 bg-blue-50 dark:bg-blue-900/20'
                        : 'border-[rgb(var(--border))] hover:border-blue-300'
                    } ${disabled ? 'opacity-50 cursor-not-allowed' : ''}`}
                  >
                    <input
                      type="checkbox"
                      checked={isChecked}
                      onChange={() => handleToggleComparison(option.id)}
                      disabled={disabled}
                      className="text-blue-500 focus:ring-blue-400"
                    />
                    <span className="text-sm font-medium text-[rgb(var(--text))]">{option.name}</span>
                  </label>
                )
              })}
            </div>
            {availableComparisonCities.length === 0 && (
              <p className="text-xs text-gray-500 dark:text-gray-400 mt-2">
                Svi dostupni gradovi su iskorišteni u poređenju.
              </p>
            )}
          </section>
        </div>

        <div className="p-6 border-t border-[rgb(var(--border))] flex justify-end gap-3">
          <button
            onClick={onClose}
            className="px-4 py-2 rounded-lg border border-[rgb(var(--border))] bg-[rgb(var(--card))] text-sm text-[rgb(var(--text))] hover:bg-gray-100 dark:hover:bg-gray-800"
          >
            Odustani
          </button>
          <button
            onClick={handleSave}
            className="px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-700 text-sm font-medium text-white"
          >
            Sačuvaj promjene
          </button>
        </div>
      </div>
    </div>
  )
}
