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
│ • LiveAqiCard       │────│ • useLiveAqi()           │────│ • getLive()         │
│ • DailyTimeline     │────│ • useComplete()          │────│ • getComplete()     │
│ • CityComparison    │────│ • useSnapshots()         │────│ • getSnapshots()    │
│ • GroupCard         │────│ • useComplete()          │────│ • getComplete()     │
└─────────────────────┘    └──────────────────────────┘    └─────────────────────┘

CACHING STRATEGY:
10-minute refresh interval aligns sa backend refresh service
Balances data freshness sa API rate limiting i performance
*/

import { useCallback, useEffect } from 'react'
import useSWR, { SWRConfiguration } from 'swr'
import { apiClient, AqiResponse, ForecastResponse, CompleteAqiResponse } from './api-client'
import { airQualityObservable } from './observable'

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

const DEFAULT_OBSERVABLE_INTERVAL = 60 * 1000

function resolveInterval(config?: SWRConfiguration, fallback: number = DEFAULT_OBSERVABLE_INTERVAL) {
  const value = config?.refreshInterval
  return typeof value === 'number' && value > 0 ? value : fallback
}

/*
===========================================================================================
                               UNIFIED CITY-AWARE HOOKS
===========================================================================================

SINGLE ENTRY POINTS:
Sve funkcije koriste nove /api/v1/air-quality endpoint-e bez posebnih Sarajevo pravila
*/

/// <summary>
/// Hook za live AQI podatke sa automatskim 60s refresh intervalom
/// </summary>
export function useLiveAqi(cityId: string | null, config?: SWRConfiguration) {
  const { data, error, isLoading, mutate } = useSWR<AqiResponse>(
    cityId ? `aqi-live-${cityId}` : null,
    () => apiClient.getLive(cityId!),
    {
      ...defaultConfig,
      ...config,
      refreshInterval: 0,
      revalidateOnFocus: true,
      revalidateOnMount: true,
    }
  )

  const intervalMs = resolveInterval(config)

  useEffect(() => {
    if (!cityId) {
      return
    }

    const unsubscribe = airQualityObservable.subscribe(() => {
      void mutate()
    }, { intervalMs })

    return () => {
      unsubscribe()
    }
  }, [cityId, mutate, intervalMs])

  const refresh = useCallback(() => mutate(), [mutate])

  return {
    data,
    error,
    isLoading,
    refresh,
  }
}

/// <summary>
/// Hook za forecast podatke prebačen na unified backend endpoint
/// </summary>
export function useForecast(cityId: string | null, config?: SWRConfiguration) {
  const { data, error, isLoading, mutate } = useSWR<ForecastResponse>(
    cityId ? `aqi-forecast-${cityId}` : null,
    () => apiClient.getForecast(cityId!),
    {
      ...defaultConfig,
      ...config,
      refreshInterval: 0,
      revalidateOnFocus: true,
      revalidateOnMount: true,
    }
  )

  const intervalMs = resolveInterval(config)

  useEffect(() => {
    if (!cityId) {
      return
    }

    const unsubscribe = airQualityObservable.subscribe(() => {
      void mutate()
    }, { intervalMs })

    return () => {
      unsubscribe()
    }
  }, [cityId, mutate, intervalMs])

  const refresh = useCallback(() => mutate(), [mutate])

  return {
    data,
    error,
    isLoading,
    refresh,
  }
}

/// <summary>
/// Hook za kompletan payload (live + forecast) u jednom pozivu
/// </summary>
export function useComplete(cityId: string | null, config?: SWRConfiguration) {
  const { data, error, isLoading, mutate } = useSWR<CompleteAqiResponse>(
    cityId ? `aqi-complete-${cityId}` : null,
    () => apiClient.getComplete(cityId!),
    {
      ...defaultConfig,
      ...config,
      refreshInterval: 0,
      revalidateOnFocus: true,
      revalidateOnMount: true,
    }
  )

  const intervalMs = resolveInterval(config)

  useEffect(() => {
    if (!cityId) {
      return
    }

    const unsubscribe = airQualityObservable.subscribe(() => {
      void mutate()
    }, { intervalMs })

    return () => {
      unsubscribe()
    }
  }, [cityId, mutate, intervalMs])

  const refresh = useCallback(() => mutate(), [mutate])

  return {
    data,
    error,
    isLoading,
    refresh,
  }
}

/// <summary>
/// Helper hook za batch dohvat snapshot podataka (korisno za comparison grid)
/// </summary>
export function useSnapshots(cityIds: string[] | null, config?: SWRConfiguration) {
  const normalized = cityIds && cityIds.length > 0 ? cityIds : null
  const subscriptionKey = normalized ? normalized.join(',') : null
  const { data, error, isLoading, mutate } = useSWR<Record<string, AqiResponse>>(
    normalized ? ['aqi-snapshots', ...normalized] : null,
    () => apiClient.getSnapshots(normalized || undefined),
    {
      ...defaultConfig,
      ...config,
      refreshInterval: 0,
      revalidateOnFocus: true,
      revalidateOnMount: true,
    }
  )

  const intervalMs = resolveInterval(config)

  useEffect(() => {
    if (!subscriptionKey) {
      return
    }

    const unsubscribe = airQualityObservable.subscribe(() => {
      void mutate()
    }, { intervalMs })

    return () => {
      unsubscribe()
    }
  }, [subscriptionKey, mutate, intervalMs])

  const refresh = useCallback(() => mutate(), [mutate])

  return {
    data,
    error,
    isLoading,
    refresh,
  }
}

export function useRefreshAll() {
  const refreshAll = useCallback(() => {
    airQualityObservable.notify()
  }, [])

  return refreshAll
}

export function usePeriodicRefresh(intervalMs: number = 10 * 60 * 1000) {
  const refreshAll = useRefreshAll()

  useEffect(() => {
    const previousInterval = airQualityObservable.getIntervalMs()
    airQualityObservable.setIntervalMs(intervalMs)

    return () => {
      airQualityObservable.setIntervalMs(previousInterval)
    }
  }, [intervalMs])

  return refreshAll
}