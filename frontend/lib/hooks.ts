/*
===========================================================================================
                                  REACT HOOKS LIBRARY
===========================================================================================

PURPOSE & STATE MANAGEMENT:
Custom React hooks za API state management sa SWR (stale-while-revalidate).
Provides consistent data fetching, caching, i error handling across components.

SWR INTEGRATION BENEFITS:
- Background data revalidation za fresh content
- Automatic caching za improved performance  
- Network error handling sa retry logic
- Focus revalidation za up-to-date data
- Optimistic updates za better UX

HOOK ARCHITECTURE:
┌─────────────────────┐    ┌──────────────────────────┐    ┌─────────────────────┐
│  REACT COMPONENTS   │────│      CUSTOM HOOKS        │────│    API CLIENT       │
│                     │    │    (This Library)        │    │                     │
│ • LiveAqiCard       │────│ • useLiveAqi()           │────│ • getLiveAqi()      │
│ • ForecastTimeline  │────│ • useForecastData()      │────│ • getForecastData() │
│ • DailyAqiCard      │────│ • useDailyData()         │────│ • getDailyData()    │
│ • GroupCard         │────│ • useHealthGroups()      │────│ • getGroups()       │
└─────────────────────┘    └──────────────────────────┘    └─────────────────────┘

CACHING STRATEGY:
10-minute refresh interval aligns sa backend refresh service
Balances data freshness sa API rate limiting i performance
*/

import useSWR, { SWRConfiguration, mutate } from 'swr'
import { apiClient, AqiResponse, Measurement, GroupsResponse } from './api-client'

/*
=== SWR GLOBAL CONFIGURATION ===

OPTIMIZED CACHE SETTINGS:
Synchronized sa backend refresh patterns za consistency
Error handling sa exponential backoff za resilience
*/

/// <summary>
/// Default SWR configuration optimized za air quality data patterns
/// Balances freshness, performance, i API rate limiting
/// </summary>
const defaultConfig: SWRConfiguration = {
  refreshInterval: 10 * 60 * 1000, // 10 minutes - synchronized sa backend storage
  revalidateOnFocus: true,          // Fresh data kada user returns to tab
  revalidateOnReconnect: true,      // Revalidate after network reconnection  
  errorRetryCount: 3,               // Retry failed requests 3 times
  errorRetryInterval: 5000,         // 5 second delay between retries
}

/*
===========================================================================================
                                   LIVE DATA HOOKS
===========================================================================================

REAL-TIME AIR QUALITY STATE:
Hooks za current AQI data sa automatic background refresh
Provides loading states, error handling, i manual refresh capabilities
*/

/// <summary>
/// Hook za live AQI data sa SWR caching i background refresh
/// Provides complete air quality information za specified city
/// </summary>
export function useLiveAqi(city: string, config?: SWRConfiguration) {
  const { data, error, isLoading, mutate } = useSWR<AqiResponse>(
    city ? `live-aqi-${city}` : null,    // Conditional fetching - null disables
    () => apiClient.getLiveAqi(city),    // API call function
    { ...defaultConfig, ...config }     // Merge custom config sa defaults
  )

  return {
    data,                 // AqiResponse ili undefined
    error,               // Error object if request fails
    isLoading,           // Boolean loading state
    refresh: mutate,     // Manual refresh function
  }
}

/// <summary>
/// Hook za detailed pollutant measurements sa SWR caching
/// Focuses on individual measurement data rather than aggregated AQI
/// </summary>
export function useLiveMeasurements(city: string, config?: SWRConfiguration) {
  const { data, error, isLoading, mutate } = useSWR<Measurement[]>(
    city ? `live-measurements-${city}` : null,
    () => apiClient.getLiveMeasurements(city),
    { ...defaultConfig, ...config }
  )

  return {
    data,                 // Measurement[] ili undefined
    error,               // Error object if request fails  
    isLoading,           // Boolean loading state
    refresh: mutate,     // Manual refresh function
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