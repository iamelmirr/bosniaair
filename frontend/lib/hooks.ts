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
import { apiClient, AqiResponse, Measurement, SarajevoCompleteResponse, ForecastResponse } from './api-client'

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
                                   NEW: SARAJEVO-SPECIFIC HOOKS
===========================================================================================

OPTIMIZED SARAJEVO OPERATIONS:
Specialized hooks za glavnu funkcionalnost aplikacije
Kombinovani pozivi za performance optimization
*/

/// <summary>
/// NEW: Kombinovani hook za sve Sarajevo podatke (live + forecast)
/// Optimizovano za glavnu stranicu - jedan HTTP poziv umjesto dva
/// </summary>
export function useSarajevoComplete(config?: SWRConfiguration) {
  const { data, error, isLoading, mutate } = useSWR<SarajevoCompleteResponse>(
    'sarajevo-complete',
    () => apiClient.getSarajevoComplete(),
    { ...defaultConfig, ...config }
  )

  return {
    data,
    error,
    isLoading,
    refresh: mutate,
  }
}

/// <summary>
/// NEW: Hook za samo live AQI za Sarajevo
/// FORCE FRESH na prvom load-u da se podaci odmah ažuriraju
/// </summary>
export function useSarajevoLive(config?: SWRConfiguration) {
  const { data, error, isLoading, mutate } = useSWR<AqiResponse>(
    'sarajevo-live',
    () => apiClient.getSarajevoLive(false), // � KORISTI CACHED PODATKE IZ BACKGROUND SERVICE
    { 
      ...defaultConfig, 
      ...config,
      refreshInterval: 30 * 1000,      // 🔄 ČESTI REFRESH IZ BAZE (30s)
      revalidateOnFocus: true,          // 🎯 REFRESH KAD KORISNIK SE VRATI NA TAB
      revalidateOnMount: true,          // 🚀 REFRESH NA MOUNT KOMPONENTE  
    }
  )

  return {
    data,
    error,
    isLoading,
    refresh: mutate,
  }
}

/// <summary>
/// NEW: Hook za samo forecast za Sarajevo
/// FORCE FRESH na prvom load-u da se podaci odmah ažuriraju
/// </summary>
export function useSarajevoForecast(config?: SWRConfiguration) {
  const { data, error, isLoading, mutate } = useSWR<ForecastResponse>(
    'sarajevo-forecast',
    () => apiClient.getSarajevoForecast(false), // � KORISTI CACHED PODATKE IZ BACKGROUND SERVICE
    { 
      ...defaultConfig, 
      ...config,
      refreshInterval: 30 * 1000,      // 🔄 SYNCED SA LIVE AQI (30s) - ISTI KAO useLiveAqi!
      revalidateOnFocus: true,          // 🎯 REFRESH KAD KORISNIK SE VRATI NA TAB
      revalidateOnMount: true,          // 🚀 REFRESH NA MOUNT KOMPONENTE  
    }
  )

  return {
    data,
    error,
    isLoading,
    refresh: mutate,
  }
}

/*
===========================================================================================
                                   CITIES DATA HOOKS
===========================================================================================

ON-DEMAND DATA ZA OSTALE GRADOVE:
Smart routing na osnovu grada - Sarajevo vs ostali
*/

/// <summary>
/// UPDATED: Smart routing hook za live AQI data
/// Sarajevo → SarajevoService, ostali → CitiesService  
/// </summary>
export function useLiveAqi(city: string, config?: SWRConfiguration) {
  const { data, error, isLoading, mutate } = useSWR<AqiResponse>(
    city ? `live-aqi-${city}` : null,
    () => {
      if (city.toLowerCase() === 'sarajevo') {
        // 🔄 NE FORCE FRESH - koristi cached podatke iz background service
        return apiClient.getSarajevoLive(false) 
      } else {
        return apiClient.getCityLive(city)
      }
    },
    { 
      ...defaultConfig, 
      ...config,
      // � OPTIMIZOVANE OPCIJE ZA SERVER-SIDE PROCESSING
      refreshInterval: 30 * 1000, // 🔄 BRŽI REFRESH IZ BAZE (30s)
      revalidateOnFocus: true,     // 🎯 REFRESH KAD SE VRATI NA TAB
      revalidateOnMount: true,     // 🚀 REFRESH NA MOUNT
    }
  )

  return {
    data,
    error,
    isLoading,
    refresh: mutate,
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

// Groups hook - REMOVED: functionality moved to static health-advice.ts
// Za health recommendations, koristi local getHealthAdvice() function

// Compare hook removed - using multiple live calls instead

// Forecast hook - UPDATED for new architecture
export function useForecast(city: string, config?: SWRConfiguration) {
  const { data, error, isLoading, mutate } = useSWR<ForecastResponse>(
    city ? `forecast-${city}` : null,
    () => {
      if (city.toLowerCase() === 'sarajevo') {
        return apiClient.getSarajevoForecast()
      } else {
        // For other cities, forecast not implemented yet in simplified architecture
        throw new Error(`Forecast not available for ${city}. Only Sarajevo supported.`)
      }
    },
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

/*
===========================================================================================
                                   DAILY DATA HOOKS
===========================================================================================

HISTORICAL DAILY PATTERNS:
Optimized za Sarajevo - koristi complete response for daily cards
*/

/// <summary>
/// NEW: Hook za daily AQI data (optimized za Sarajevo)
/// Za druge gradove, vraća error jer nisu supported u new architecture
/// </summary>
export function useDaily(city: string, config?: SWRConfiguration) {
  const { data, error, isLoading, mutate } = useSWR<SarajevoCompleteResponse>(
    city ? `daily-${city}` : null,
    () => {
      if (city.toLowerCase() === 'sarajevo') {
        return apiClient.getSarajevoComplete()
      } else {
        throw new Error(`Daily data not available for ${city}. Only Sarajevo supported.`)
      }
    },
    { ...defaultConfig, ...config }
  )

  return {
    data,
    error,
    isLoading,
    refresh: mutate,
  }
}