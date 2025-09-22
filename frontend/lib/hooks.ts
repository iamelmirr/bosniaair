import useSWR, { SWRConfiguration, mutate } from 'swr'
import { apiClient, AqiResponse, Measurement, HistoryResponse, GroupsResponse, CompareResponse } from './api-client'

// Default SWR configuration
const defaultConfig: SWRConfiguration = {
  refreshInterval: 5 * 60 * 1000, // 5 minutes
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

// History hooks
export function useHistory(
  city: string,
  parameter: string,
  startDate: Date,
  endDate: Date,
  aggregation: 'hourly' | 'daily' = 'hourly',
  config?: SWRConfiguration
) {
  const key = city && parameter && startDate && endDate 
    ? `history-${city}-${parameter}-${startDate.toISOString()}-${endDate.toISOString()}-${aggregation}`
    : null

  const { data, error, isLoading, mutate } = useSWR<HistoryResponse>(
    key,
    () => apiClient.getHistory(city, parameter, startDate, endDate, aggregation),
    { 
      ...defaultConfig, 
      refreshInterval: 15 * 60 * 1000, // 15 minutes for history data
      ...config 
    }
  )

  return {
    data,
    error,
    isLoading,
    refresh: mutate,
  }
}

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

// Compare hook
export function useCompare(cities: string[], config?: SWRConfiguration) {
  const key = cities.length > 0 ? `compare-${cities.join('-')}` : null

  const { data, error, isLoading, mutate } = useSWR<CompareResponse>(
    key,
    () => apiClient.compareCities(cities),
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
export function usePeriodicRefresh(intervalMs: number = 5 * 60 * 1000) {
  const refreshAll = useRefreshAll()

  // Use effect would be added here in a real implementation
  // For now, this is a placeholder that can be enhanced
  return refreshAll
}