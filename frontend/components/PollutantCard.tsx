'use client'

import { Measurement } from '../lib/api-client'

/// <summary>
/// Component that displays a single air pollutant measurement with color-coded health status.
/// Shows the pollutant name, value, and unit with visual indicators based on EPA breakpoints.
/// </summary>
interface PollutantCardProps {
  measurement: Measurement
}

export default function PollutantCard({ measurement }: PollutantCardProps) {
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

  // Render pollutant card with color-coded status indicator
  return (
    <div className={`bg-[rgb(var(--card))] rounded-lg p-2 sm:p-3 border ${borderClass} md:hover:shadow-md transition-all duration-300 md:hover:-translate-y-1 md:hover:scale-105`}>
      {/* Pollutant name header */}
      <div className="text-center mb-3">
        <h3 className="text-sm font-medium text-[rgb(var(--text))]">
          {getParameterName(measurement.parameter)}
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
    </div>
  )
}
