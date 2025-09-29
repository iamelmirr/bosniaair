'use client'

import { useState, useEffect } from 'react'
import { Measurement } from '../lib/api-client'

/// <summary>
/// Component that displays a single air pollutant measurement with color-coded health status.
/// Shows the pollutant name, value, and unit with visual indicators based on EPA breakpoints.
/// </summary>
interface PollutantsProps {
  measurement: Measurement
  isOpen?: boolean
  onToggle?: () => void
  index?: number
  totalInRow?: number
}

export default function Pollutants({ measurement, isOpen = false, onToggle, index = 0, totalInRow = 2 }: PollutantsProps) {
  const [showTooltip, setShowTooltip] = useState(false)
  const [isMobile, setIsMobile] = useState(false)

  // Detect mobile screen size
  useEffect(() => {
    const checkIsMobile = () => {
      setIsMobile(window.innerWidth < 768)
    }

    checkIsMobile()
    window.addEventListener('resize', checkIsMobile)

    return () => window.removeEventListener('resize', checkIsMobile)
  }, [])

  /// <summary>
  /// Get detailed description for each pollutant parameter
  /// </summary>
  /// <param name="parameter">Pollutant parameter name</param>
  const getPollutantDescription = (parameter: string) => {
    const descriptions: Record<string, { title: string; description: string; source: string }> = {
      'pm25': {
        title: 'Sitne čestice',
        description: 'Sitne čestice iz izduvnih gasova koje prodiru duboko u pluća.',
        source: 'Najopasnije za zdravlje'
      },
      'pm10': {
        title: 'Krupne čestice',
        description: 'Veće čestice iz prašine i peludi. Iritiraju nos i grlo.',
        source: 'Uzrokuju kašalj'
      },
      'o3': {
        title: 'Prizemni ozon',
        description: 'Nastaje od sunčeva svjetla i zagađenja. Pogoršava astmu.',
        source: 'Opasan za djecu'
      },
      'no2': {
        title: 'Azot dioksid',
        description: 'Gas iz automobila i elektrana. Upala dišnih puteva.',
        source: 'Više kod prometnica'
      },
      'so2': {
        title: 'Sumpor dioksid',
        description: 'Gas iz uglja i industrije. Otežava disanje.',
        source: 'Od termoelektrana'
      },
      'co': {
        title: 'Ugljen monoksid',
        description: 'Bezbojni gas koji smanjuje kiseonik u krvi.',
        source: 'Iz automobila'
      },
    }
    
    const paramKey = parameter.toLowerCase().replace('.', '')
    return descriptions[paramKey] || {
      title: parameter.toUpperCase(),
      description: 'Mjerenje zagađenja zraka',
      source: 'Monitoring okruženja'
    }
  }

  /// <summary>
  /// Determines the health status of a pollutant measurement based on EPA breakpoints
  /// </summary>
  /// <param name="parameter">Pollutant parameter name (e.g., 'pm25', 'o3')</param>
  /// <param name="value">Measurement value</param>
  /// <returns>Status string: 'good', 'moderate', 'unhealthy', or 'very-unhealthy'</returns>
  const getStatusColor = (parameter: string, value: number) => {
    const breakpoints: Record<string, { good: number; moderate: number; unhealthy: number }> = {
      'pm25': { good: 12, moderate: 35.4, unhealthy: 55.4 },
      'pm10': { good: 54, moderate: 154, unhealthy: 254 },
      'o3': { good: 108, moderate: 140, unhealthy: 168 },
      'no2': { good: 100, moderate: 188, unhealthy: 677 },
      'so2': { good: 196, moderate: 304, unhealthy: 604 },
      'co': { good: 10, moderate: 20, unhealthy: 30 },
    }

    const paramKey = parameter.toLowerCase().replace('.', '')
    const limits = breakpoints[paramKey]
    
    if (!limits) return 'moderate'
    
    if (value <= limits.good) return 'good'
    if (value <= limits.moderate) return 'moderate'
    if (value <= limits.unhealthy) return 'unhealthy'
    return 'very-unhealthy'
  }

  /// <summary>
  /// Returns CSS background color class based on health status
  /// </summary>
  /// <param name="status">Health status string</param>
  const getStatusColorClass = (status: string) => {
    switch (status) {
      case 'good':
        return 'bg-aqi-good'
      case 'moderate':
        return 'bg-aqi-moderate'
      case 'unhealthy':
        return 'bg-aqi-unhealthy'
      case 'very-unhealthy':
        return 'bg-aqi-very'
      default:
        return 'bg-gray-400'
    }
  }

  /// <summary>
  /// Returns CSS border color class with transparency based on health status
  /// </summary>
  /// <param name="status">Health status string</param>
  const getBorderColorClass = (status: string) => {
    switch (status) {
      case 'good':
        return 'border-aqi-good/30'
      case 'moderate':
        return 'border-aqi-moderate/30'
      case 'unhealthy':
        return 'border-aqi-unhealthy/30'
      case 'very-unhealthy':
        return 'border-aqi-very/30'
      default:
        return 'border-[rgb(var(--border))]'
    }
  }

  /// <summary>
  /// Converts parameter codes to human-readable names with subscripts
  /// </summary>
  /// <param name="parameter">Parameter code (e.g., 'pm25', 'o3')</param>
  const getParameterName = (parameter: string) => {
    const names: Record<string, string> = {
      'pm25': 'PM2.5',
      'pm10': 'PM10',
      'o3': 'O₃',
      'no2': 'NO₂',
      'so2': 'SO₂',
      'co': 'CO',
      'nh3': 'NH₃',
    }
    return names[parameter.toLowerCase().replace('.', '')] || parameter.toUpperCase()
  }

  /// <summary>
  /// Formats measurement value with appropriate decimal places based on magnitude
  /// </summary>
  /// <param name="value">Raw measurement value</param>
  const formatValue = (value: number) => {
    if (value < 1) {
      return parseFloat(value.toFixed(2)).toString()
    } else if (value < 10) {
      return parseFloat(value.toFixed(1)).toString()
    } else {
      return Math.round(value).toString()
    }
  }

  // Calculate health status and styling classes
  const status = getStatusColor(measurement.parameter, measurement.value)
  const colorClass = getStatusColorClass(status)
  const borderClass = getBorderColorClass(status)
  const pollutantInfo = getPollutantDescription(measurement.parameter)

  // Handle tooltip toggle (click for mobile, hover for desktop)
  const handleCardClick = () => {
    if (isMobile) { // Mobile
      onToggle?.()
    } else { // Desktop
      setShowTooltip(!showTooltip)
    }
  }

  const handleMouseEnter = () => {
    if (!isMobile) { // Only on desktop
      setShowTooltip(true)
    }
  }

  const handleMouseLeave = () => {
    if (!isMobile) { // Only on desktop
      setShowTooltip(false)
    }
  }

  const isTooltipVisible = isMobile ? isOpen : showTooltip

  // Determine tooltip alignment for mobile (left vs right side)
  const isLeftSide = isMobile && (index % totalInRow === 0) // First card in row (left side)
  const isRightSide = isMobile && (index % totalInRow === totalInRow - 1) // Last card in row (right side)
  
  // Get tooltip positioning classes
  const getTooltipPositionClasses = () => {
    if (!isMobile) {
      return "left-1/2 transform -translate-x-1/2" // Centered for desktop
    }
    
    if (isLeftSide) {
      return "left-0" // Align left edge
    } else if (isRightSide) {
      return "right-0" // Align right edge  
    } else {
      return "left-1/2 transform -translate-x-1/2" // Center for middle cards
    }
  }

  // Get arrow positioning classes
  const getArrowPositionClasses = () => {
    if (!isMobile) {
      return "left-1/2 transform -translate-x-1/2" // Centered for desktop
    }
    
    if (isLeftSide) {
      return "left-6" // Position arrow above the card, not at edge
    } else if (isRightSide) {
      return "right-6" // Position arrow above the card, not at edge
    } else {
      return "left-1/2 transform -translate-x-1/2" // Centered for middle cards
    }
  }

  // Render pollutant card with tooltip
  return (
    <div 
      data-pollutant-card
      className={`relative bg-[rgb(var(--card))] rounded-lg p-2 sm:p-3 border ${borderClass} transition-all duration-300 cursor-pointer
        md:hover:shadow-md md:hover:-translate-y-1 md:hover:scale-105
        ${isTooltipVisible ? 'shadow-lg scale-105 z-10' : ''}
      `}
      onClick={handleCardClick}
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
    >
      {/* Pollutant name header */}
      <div className="text-center mb-3">
        <h3 className="text-sm font-medium text-[rgb(var(--text))] flex items-center justify-center gap-1">
          {getParameterName(measurement.parameter)}
          <span className="text-xs text-gray-400">ⓘ</span>
        </h3>
      </div>

      {/* Measurement value and unit */}
      <div className="text-center">
        <div className="text-xl font-bold text-[rgb(var(--text))] mb-1">
          {formatValue(measurement.value)}
        </div>
        <div className="text-xs text-gray-500 font-medium">
          {measurement.unit}
        </div>
      </div>

      {/* Status indicator dot */}
      <div className="absolute top-2 right-2">
        <div className={`w-2 h-2 rounded-full ${colorClass}`}></div>
      </div>

      {/* Tooltip bubble */}
      {isTooltipVisible && (
        <>
          {/* Backdrop for mobile to close tooltip */}
          <div 
            className="fixed inset-0 bg-black/20 z-40 md:hidden" 
            onClick={() => onToggle?.()}
          ></div>
          
          {/* Tooltip content */}
          <div data-pollutant-tooltip className={`absolute bottom-full mb-2 z-50 w-60 max-w-[85vw] ${getTooltipPositionClasses()}`}>
            <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl border border-gray-200 dark:border-gray-700 p-3">
              {/* Arrow pointing down */}
              <div className={`absolute top-full w-0 h-0 border-l-4 border-r-4 border-t-4 border-transparent border-t-white dark:border-t-gray-800 ${getArrowPositionClasses()}`}></div>
              
              {/* Content */}
              <div className="space-y-2">
                <h4 className="font-semibold text-sm text-gray-900 dark:text-gray-100">
                  {pollutantInfo.title}
                </h4>
                <p className="text-xs text-gray-700 dark:text-gray-300 leading-relaxed">
                  {pollutantInfo.description}
                </p>
                <div className="flex items-center gap-2 pt-1">
                  <div className={`w-2 h-2 rounded-full ${colorClass}`}></div>
                  <span className="text-xs font-medium text-gray-600 dark:text-gray-400">
                    {status === 'good' ? 'Dobro' : 
                     status === 'moderate' ? 'Umjereno' :
                     status === 'unhealthy' ? 'Nezdravo' :
                     status === 'very-unhealthy' ? 'Vrlo nezdravo' : status}
                  </span>
                </div>
                <p className="text-xs text-gray-500 dark:text-gray-500 italic">
                  {pollutantInfo.source}
                </p>
              </div>

              {/* Close button for mobile */}
              <button 
                className="absolute top-1 right-2 text-gray-400 hover:text-gray-600 md:hidden text-lg"
                onClick={(e) => {
                  e.stopPropagation()
                  onToggle?.()
                }}
              >
                ✕
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  )
}
