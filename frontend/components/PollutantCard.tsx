'use client'

import { Measurement } from '../lib/api-client'

interface PollutantCardProps {
  measurement: Measurement
}

export default function PollutantCard({ measurement }: PollutantCardProps) {
  const getStatusColor = (parameter: string, value: number) => {
    // EPA AQI breakpoints for common pollutants (simplified)
    const breakpoints: Record<string, { good: number; moderate: number; unhealthy: number }> = {
      'pm25': { good: 12, moderate: 35.4, unhealthy: 55.4 },
      'pm10': { good: 54, moderate: 154, unhealthy: 254 },
      'o3': { good: 108, moderate: 140, unhealthy: 168 }, // 8-hour average in µg/m³
      'no2': { good: 100, moderate: 188, unhealthy: 677 },
      'so2': { good: 196, moderate: 304, unhealthy: 604 },
      'co': { good: 10, moderate: 20, unhealthy: 30 }, // in mg/m³
    }

    const paramKey = parameter.toLowerCase().replace('.', '')
    const limits = breakpoints[paramKey]
    
    if (!limits) return 'moderate' // default for unknown parameters
    
    if (value <= limits.good) return 'good'
    if (value <= limits.moderate) return 'moderate'
    if (value <= limits.unhealthy) return 'unhealthy'
    return 'very-unhealthy'
  }

  const getStatusColorClass = (status: string) => {
    switch (status) {
      case 'good':
        return 'bg-aqi-good'
      case 'moderate':
        return 'bg-aqi-moderate'
      case 'unhealthy':
        return 'bg-aqi-unhealthy'
      case 'very-unhealthy':
        return 'bg-aqi-very-unhealthy'
      default:
        return 'bg-gray-400'
    }
  }

  const getParameterName = (parameter: string) => {
    const names: Record<string, string> = {
      'pm25': 'PM2.5',
      'pm10': 'PM10',
      'o3': 'O₃',
      'no2': 'NO₂',
      'so2': 'SO₂',
      'co': 'CO',
    }
    return names[parameter.toLowerCase().replace('.', '')] || parameter.toUpperCase()
  }

  const getParameterDescription = (parameter: string) => {
    const descriptions: Record<string, string> = {
      'pm25': 'Fine particulate matter',
      'pm10': 'Coarse particulate matter',
      'o3': 'Ground-level ozone',
      'no2': 'Nitrogen dioxide',
      'so2': 'Sulfur dioxide',
      'co': 'Carbon monoxide',
    }
    return descriptions[parameter.toLowerCase().replace('.', '')] || 'Air pollutant'
  }

  const formatValue = (value: number) => {
    if (value < 1) {
      return value.toFixed(2)
    } else if (value < 10) {
      return value.toFixed(1)
    } else {
      return Math.round(value).toString()
    }
  }

  const formatTimestamp = (timestamp: Date) => {
    return new Intl.DateTimeFormat('bs-BA', {
      timeStyle: 'short',
    }).format(timestamp)
  }

  const status = getStatusColor(measurement.parameter, measurement.value)

  return (
    <div className="bg-[rgb(var(--card))] rounded-lg p-4 border border-[rgb(var(--border))] shadow-card hover:shadow-card-hover transition-all group">
      {/* Header with status indicator */}
      <div className="flex items-center justify-between mb-3">
        <div className="flex items-center gap-2">
          <h3 className="font-medium text-[rgb(var(--text))] group-hover:text-blue-600 transition-colors">
            {getParameterName(measurement.parameter)}
          </h3>
          <div 
            className={`w-3 h-3 rounded-full ${getStatusColorClass(status)}`}
            title={`Status: ${status}`}
          />
        </div>
        
        <div className="text-xs text-gray-500">
          {formatTimestamp(measurement.timestamp)}
        </div>
      </div>
      
      {/* Value display */}
      <div className="flex items-baseline justify-between mb-2">
        <span className="text-2xl font-semibold text-[rgb(var(--text))]">
          {formatValue(measurement.value)}
        </span>
        <span className="text-sm text-gray-500 font-medium">
          {measurement.unit}
        </span>
      </div>
      
      {/* Parameter description */}
      <p className="text-xs text-gray-600 dark:text-gray-400 mb-3">
        {getParameterDescription(measurement.parameter)}
      </p>
      
      {/* Additional info */}
      <div className="space-y-1">
        {measurement.locationName && (
          <div className="flex items-center gap-1 text-xs text-gray-500">
            <span>📍</span>
            <span className="truncate" title={measurement.locationName}>
              {measurement.locationName}
            </span>
          </div>
        )}
        
        {measurement.sourceName && (
          <div className="flex items-center gap-1 text-xs text-gray-500">
            <span>🔗</span>
            <span className="truncate" title={measurement.sourceName}>
              {measurement.sourceName}
            </span>
          </div>
        )}
        
        {measurement.averagingPeriod && (
          <div className="flex items-center gap-1 text-xs text-gray-500">
            <span>⏱️</span>
            <span>
              {measurement.averagingPeriod.value} {measurement.averagingPeriod.unit} avg
            </span>
          </div>
        )}
      </div>
      
      {/* Hover effect indicator */}
      <div className="mt-3 pt-2 border-t border-gray-200 dark:border-gray-700 opacity-0 group-hover:opacity-100 transition-opacity">
        <div className="text-xs text-blue-600 dark:text-blue-400">
          View details →
        </div>
      </div>
    </div>
  )
}