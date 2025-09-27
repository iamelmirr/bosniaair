'use client'

import { useEffect, useState } from 'react'
import { CITY_OPTIONS, CityId, cityIdToLabel } from '../lib/utils'

interface CitySelectorModalProps {
  isOpen: boolean
  onClose: () => void
  onSave: (primaryCity: CityId) => void
  initialPrimaryCity: CityId
}

export default function CitySelectorModal({
  isOpen,
  onClose,
  onSave,
  initialPrimaryCity
}: CitySelectorModalProps) {
  const [primarySelection, setPrimarySelection] = useState<CityId>(initialPrimaryCity)

  useEffect(() => {
    if (isOpen) {
      setPrimarySelection(initialPrimaryCity)
    }
  }, [isOpen, initialPrimaryCity])

  const handleSave = () => {
    onSave(primarySelection)
    onClose()
  }

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-[rgb(var(--card))] rounded-xl shadow-xl max-w-md w-full mx-4 max-h-[90vh] overflow-y-auto">
        <div className="p-6">
          <div className="flex items-center justify-between mb-6">
            <h2 className="text-xl font-semibold text-[rgb(var(--text))]">Odaberite glavni grad</h2>
            <button
              onClick={onClose}
              className="text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 transition-colors"
            >
              ✕
            </button>
          </div>

          <div className="space-y-4">
            <div>
              <h3 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">
                Glavni grad
              </h3>
              <div className="space-y-2">
                {CITY_OPTIONS.map(option => (
                  <label key={option.id} className="flex items-center space-x-3 cursor-pointer">
                    <input
                      type="radio"
                      name="primaryCity"
                      value={option.id}
                      checked={primarySelection === option.id}
                      onChange={(e) => setPrimarySelection(e.target.value as CityId)}
                      className="w-4 h-4 text-blue-600 bg-[rgb(var(--card))] border-gray-300 focus:ring-blue-500 dark:focus:ring-blue-600 dark:ring-offset-gray-800 focus:ring-2 dark:bg-gray-700 dark:border-gray-600"
                    />
                    <span className="text-sm text-[rgb(var(--text))]">{option.name}</span>
                  </label>
                ))}
              </div>
            </div>
          </div>

          <div className="flex gap-3 mt-6">
            <button
              onClick={onClose}
              className="flex-1 px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 rounded-lg transition-colors"
            >
              Otkaži
            </button>
            <button
              onClick={handleSave}
              className="flex-1 px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg transition-colors"
            >
              Sačuvaj
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}