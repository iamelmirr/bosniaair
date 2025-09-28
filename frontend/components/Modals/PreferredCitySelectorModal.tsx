'use client'

import { useEffect, useState } from 'react'
import { CITY_OPTIONS, CityId } from '../../lib/utils'

/**
 * Props for the PreferredCitySelectorModal component.
 */
interface PreferredCitySelectorModalProps {
  isOpen: boolean
  onClose: () => void
  onSave: (primaryCity: CityId) => void
  initialPrimaryCity: CityId
}

/**
 * PreferredCitySelectorModal component provides a modal interface for selecting the primary city
 * to monitor air quality. Features a clean list of available cities with selection indicators
 * and keyboard accessibility (Escape key support).
 *
 * @param isOpen - Whether the modal is visible
 * @param onClose - Callback to close the modal
 * @param onSave - Callback when a city is selected and saved
 * @param initialPrimaryCity - The initially selected city
 */
export default function PreferredCitySelectorModal({
  isOpen,
  onClose,
  onSave,
  initialPrimaryCity
}: PreferredCitySelectorModalProps) {
  const [primarySelection, setPrimarySelection] = useState<CityId>(initialPrimaryCity)

  useEffect(() => {
    if (isOpen) {
      setPrimarySelection(initialPrimaryCity)
    }
  }, [isOpen, initialPrimaryCity])

  const handleCitySelect = (cityId: CityId) => {
    setPrimarySelection(cityId)
    onSave(cityId)
    onClose()
  }

  useEffect(() => {
    if (!isOpen) return

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        e.preventDefault()
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [isOpen])

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm flex items-center justify-center p-4">
      <div 
        className="bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-sm mx-auto overflow-hidden"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="px-6 py-4 border-b border-gray-200 dark:border-gray-700">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white text-center">
            Odaberite grad
          </h2>
          <p className="text-sm text-gray-500 dark:text-gray-400 text-center mt-1">
            Glavni grad za praćenje kvaliteta vazduha
          </p>
        </div>

        <div>
          {CITY_OPTIONS.map((option, index) => (
            <button
              key={option.id}
              onClick={() => handleCitySelect(option.id)}
              className={`w-full px-6 py-4 text-left hover:bg-gray-50 dark:hover:bg-gray-800 transition-all duration-200 flex items-center justify-between group ${
                index !== CITY_OPTIONS.length - 1 ? 'border-b border-gray-100 dark:border-gray-700' : ''
              } ${
                primarySelection === option.id 
                  ? 'bg-blue-50 dark:bg-blue-900/20' 
                  : ''
              }`}
            >
              <div className="flex items-center space-x-3">
                <div className={`w-3 h-3 rounded-full border-2 transition-all ${
                  primarySelection === option.id
                    ? 'bg-blue-500 border-blue-500'
                    : 'border-gray-300 dark:border-gray-500 group-hover:border-blue-400'
                }`}>
                  {primarySelection === option.id && (
                    <div className="w-full h-full rounded-full bg-white transform scale-50"></div>
                  )}
                </div>
                
                <span className={`text-base font-medium transition-colors ${
                  primarySelection === option.id
                    ? 'text-blue-600 dark:text-blue-400'
                    : 'text-gray-900 dark:text-gray-100 group-hover:text-blue-600 dark:group-hover:text-blue-400'
                }`}>
                  {option.name}
                </span>
              </div>
              
              {primarySelection === option.id && (
                <svg className="w-5 h-5 text-blue-500 animate-in fade-in duration-200" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                </svg>
              )}
            </button>
          ))}
        </div>


      </div>
    </div>
  )
}