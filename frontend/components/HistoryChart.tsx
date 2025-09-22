'use client'

import { useEffect, useRef } from 'react'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler,
} from 'chart.js'
import { Line } from 'react-chartjs-2'
import { useHistory } from '../lib/hooks'
import { formatDate, getAqiColor, POLLUTANT_NAMES, type PollutantKey } from '../lib/utils'

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler
)

interface HistoryChartProps {
  city: string
  parameter: PollutantKey
  days: number
  aggregation?: 'hourly' | 'daily'
}

export default function HistoryChart({ 
  city, 
  parameter, 
  days, 
  aggregation = 'hourly' 
}: HistoryChartProps) {
  const chartRef = useRef<ChartJS<'line'>>(null)
  
  const endDate = new Date()
  const startDate = new Date(endDate.getTime() - (days * 24 * 60 * 60 * 1000))
  
  const { data, error, isLoading } = useHistory(
    city,
    parameter,
    startDate,
    endDate,
    aggregation
  )

  useEffect(() => {
    // Update chart theme when component mounts or theme changes
    const chart = chartRef.current
    if (chart) {
      const isDark = document.documentElement.classList.contains('dark')
      
      chart.options.plugins = {
        ...chart.options.plugins,
        legend: {
          ...chart.options.plugins?.legend,
          labels: {
            color: isDark ? '#e5e7eb' : '#374151',
          },
        },
      }
      
      chart.options.scales = {
        ...chart.options.scales,
        x: {
          ...chart.options.scales?.x,
          ticks: {
            color: isDark ? '#9ca3af' : '#6b7280',
          },
          grid: {
            color: isDark ? '#374151' : '#e5e7eb',
          },
        },
        y: {
          ...chart.options.scales?.y,
          ticks: {
            color: isDark ? '#9ca3af' : '#6b7280',
          },
          grid: {
            color: isDark ? '#374151' : '#e5e7eb',
          },
        },
      }
      
      chart.update('none')
    }
  }, [])

  if (isLoading) {
    return (
      <div className="bg-[rgb(var(--card))] rounded-xl p-6 border border-[rgb(var(--border))]">
        <div className="animate-pulse">
          <div className="h-6 bg-gray-300 dark:bg-gray-600 rounded w-48 mb-4"></div>
          <div className="h-64 bg-gray-300 dark:bg-gray-600 rounded"></div>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="bg-[rgb(var(--card))] rounded-xl p-6 border border-red-300 dark:border-red-600">
        <div className="text-center">
          <div className="text-2xl mb-2">📊</div>
          <h3 className="text-lg font-semibold text-red-600 dark:text-red-400 mb-2">
            Chart data unavailable
          </h3>
          <p className="text-gray-600 dark:text-gray-400 text-sm">
            {error.message || 'Unable to load historical data'}
          </p>
        </div>
      </div>
    )
  }

  if (!data || data.data.length === 0) {
    return (
      <div className="bg-[rgb(var(--card))] rounded-xl p-6 border border-[rgb(var(--border))]">
        <div className="text-center py-8">
          <div className="text-4xl mb-4">📈</div>
          <h3 className="text-lg font-semibold text-[rgb(var(--text))] mb-2">
            No historical data
          </h3>
          <p className="text-gray-600 dark:text-gray-400">
            No {POLLUTANT_NAMES[parameter]} data available for {city} in the last {days} days.
          </p>
        </div>
      </div>
    )
  }

  const chartData = {
    labels: data.data.map(point => {
      const date = new Date(point.timestamp)
      return aggregation === 'daily' 
        ? date.toLocaleDateString('bs-BA')
        : date.toLocaleString('bs-BA', { month: 'short', day: 'numeric', hour: '2-digit' })
    }),
    datasets: [
      {
        label: `${POLLUTANT_NAMES[parameter]} Concentration`,
        data: data.data.map(point => point.value),
        borderColor: getAqiColor(data.data[data.data.length - 1]?.aqi || 50),
        backgroundColor: `${getAqiColor(data.data[data.data.length - 1]?.aqi || 50)}20`,
        fill: true,
        tension: 0.3,
        pointRadius: aggregation === 'daily' ? 4 : 2,
        pointHoverRadius: 6,
        borderWidth: 2,
      },
    ],
  }

  const chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'top' as const,
        labels: {
          usePointStyle: true,
          padding: 20,
        },
      },
      tooltip: {
        callbacks: {
          title: (context: any) => {
            const index = context[0].dataIndex
            return formatDate(new Date(data.data[index].timestamp))
          },
          label: (context: any) => {
            const value = context.parsed.y
            const index = context.dataIndex
            const point = data.data[index]
            
            let label = `${POLLUTANT_NAMES[parameter]}: ${value.toFixed(2)} µg/m³`
            
            if (point.aqi) {
              label += `\nAQI: ${point.aqi}`
            }
            
            if (point.category) {
              label += ` (${point.category})`
            }
            
            return label
          },
        },
        backgroundColor: 'rgba(0, 0, 0, 0.8)',
        titleColor: '#fff',
        bodyColor: '#fff',
        borderColor: 'rgba(255, 255, 255, 0.2)',
        borderWidth: 1,
        cornerRadius: 8,
      },
    },
    scales: {
      x: {
        display: true,
        title: {
          display: true,
          text: 'Time',
        },
        grid: {
          display: false,
        },
      },
      y: {
        display: true,
        title: {
          display: true,
          text: `${POLLUTANT_NAMES[parameter]} (µg/m³)`,
        },
        beginAtZero: true,
        grid: {
          color: 'rgba(0, 0, 0, 0.1)',
        },
      },
    },
    interaction: {
      intersect: false,
      mode: 'index' as const,
    },
  }

  return (
    <div className="bg-[rgb(var(--card))] rounded-xl p-6 border border-[rgb(var(--border))] shadow-card">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h3 className="text-lg font-semibold text-[rgb(var(--text))]">
            {POLLUTANT_NAMES[parameter]} History
          </h3>
          <p className="text-sm text-gray-600 dark:text-gray-400">
            {city} • Last {days} days • {aggregation} data
          </p>
        </div>
        
        <div className="flex items-center gap-2 text-sm text-gray-500">
          <div className="w-2 h-2 rounded-full" style={{ backgroundColor: getAqiColor(data.data[data.data.length - 1]?.aqi || 50) }}></div>
          <span>{data.data.length} points</span>
        </div>
      </div>

      {/* Chart */}
      <div className="h-64 sm:h-80">
        <Line
          ref={chartRef}
          data={chartData}
          options={chartOptions}
        />
      </div>

      {/* Summary Stats */}
      <div className="mt-6 pt-4 border-t border-gray-200 dark:border-gray-700">
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-sm">
          <div className="text-center">
            <div className="font-semibold text-[rgb(var(--text))]">
              {Math.max(...data.data.map(d => d.value)).toFixed(1)}
            </div>
            <div className="text-gray-500">Peak</div>
          </div>
          <div className="text-center">
            <div className="font-semibold text-[rgb(var(--text))]">
              {Math.min(...data.data.map(d => d.value)).toFixed(1)}
            </div>
            <div className="text-gray-500">Low</div>
          </div>
          <div className="text-center">
            <div className="font-semibold text-[rgb(var(--text))]">
              {(data.data.reduce((sum, d) => sum + d.value, 0) / data.data.length).toFixed(1)}
            </div>
            <div className="text-gray-500">Average</div>
          </div>
          <div className="text-center">
            <div className="font-semibold text-[rgb(var(--text))]">
              {data.data[data.data.length - 1]?.value.toFixed(1) || 'N/A'}
            </div>
            <div className="text-gray-500">Latest</div>
          </div>
        </div>
      </div>
    </div>
  )
}