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

  const formatValue = (value: number) => {
    if (value < 1) {
      return value.toFixed(2)
    } else if (value < 10) {
      return value.toFixed(1)
    } else {
      return Math.round(value).toString()
    }
  }

  const status = getStatusColor(measurement.parameter, measurement.value)
  const colorClass = getStatusColorClass(status)

  return (
    <div className={`bg-[rgb(var(--card))] rounded-lg p-3 border transition-shadow hover:shadow-md ${
      status === 'good' ? 'border-green-200 hover:border-green-300' :
      status === 'moderate' ? 'border-yellow-200 hover:border-yellow-300' :
      status === 'unhealthy' ? 'border-orange-200 hover:border-orange-300' :
      status === 'very-unhealthy' ? 'border-red-200 hover:border-red-300' :
      'border-[rgb(var(--border))]'
    }`}>
      {/* Parameter name - centered */}
      <div className="text-center mb-3">
        <h3 className="text-sm font-medium text-[rgb(var(--text))]">
          {getParameterName(measurement.parameter)}
        </h3>
      </div>
      
      {/* Value display - clean and minimal */}
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
