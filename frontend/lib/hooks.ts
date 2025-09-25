import useSWR, { SWRConfiguration, mutate } from 'swr'
import { apiClient, AqiResponse, Measurement, GroupsResponse } from './api-client'

// Default SWR configuration
const defaultConfig: SWRConfiguration = {
  refreshInterval: 10 * 60 * 1000, // 10 minutes - synchronized with backend storage
  revalidateOnFocus: true,
  revalidateOnReconnect: true,
  errorRetryCount: 3,
  errorRetryInterval: 5000,
}

// Live data hooks
export function useLiveAqi(city: string, config?: SWRConfiguration) {
  const { data, error, isLoading, mutate } = useSWR<AqiResponse>(
    city ? `live-aqi-${city}` : null,
    () => apiClient.getLiveAqi(city),
    { ...defaultConfig, ...config }
  )

  return {
    data,
    error,
    isLoading,
    refresh: mutate,
  }
}

export function useLiveMeasurements(city: string, config?: SWRConfiguration) {
  const { data, error, isLoading, mutate } = useSWR<Measurement[]>(
    city ? `live-measurements-${city}` : null,
    () => apiClient.getLiveMeasurements(city),
    { ...defaultConfig, ...config }
  )

  return {
    data,
    error,
    isLoading,
    refresh: mutate,
  }
}

// History hooks removed - using today + forecast timeline instead

// Groups hook
export function useGroups(city: string, config?: SWRConfiguration) {
  const { data, error, isLoading, mutate } = useSWR<GroupsResponse>(
    city ? `groups-${city}` : null,
    () => apiClient.getGroups(city),
    { ...defaultConfig, ...config }
  )

  return {
    data,
    error,
    isLoading,
    refresh: mutate,
  }
}

// Compare hook removed - using multiple live calls instead

// Forecast hook - added for DailyTimeline
export function useForecast(city: string, config?: SWRConfiguration) {
  const { data, error, isLoading, mutate } = useSWR(
    city ? `forecast-${city}` : null,
    () => apiClient.getForecastData(city),
    { ...defaultConfig, ...config }
  )

  return {
    data,
    error,
    isLoading,
    refresh: mutate,
  }
}

// Utility hooks
export function useRefreshAll() {
  const refreshAll = () => {
    // Refresh all SWR caches by invalidating all keys
    mutate(() => true)
  }

  return refreshAll
}

// Hook for periodic data refreshing  
export function usePeriodicRefresh(intervalMs: number = 10 * 60 * 1000) {
  const refreshAll = useRefreshAll()

  // Use effect would be added here in a real implementation
  // For now, this is a placeholder that can be enhanced
  return refreshAll
}