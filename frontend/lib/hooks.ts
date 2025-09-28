/**
 * Custom React hooks for BosniaAir application.
 * Provides data fetching hooks for air quality information using SWR caching
 * and observable patterns for real-time updates.
 */

import { useCallback, useEffect } from 'react'
import useSWR, { SWRConfiguration } from 'swr'
import { apiClient, AqiResponse, CompleteAqiResponse } from './api-client'
import { airQualityObservable } from './observable'

/**
 * Default SWR configuration for air quality data fetching.
 * Configures refresh intervals, retry logic, and focus/reconnect behavior.
 */
const defaultConfig: SWRConfiguration = {
  refreshInterval: 10 * 60 * 1000,
  revalidateOnFocus: true,
  revalidateOnReconnect: true,
  errorRetryCount: 3,
  errorRetryInterval: 5000,
}

/**
 * Default interval for observable updates in milliseconds.
 */
const DEFAULT_OBSERVABLE_INTERVAL = 60 * 1000

/**
 * Resolves the refresh interval from configuration or provides a fallback.
 * @param config - Optional SWR configuration
 * @param fallback - Fallback interval in milliseconds
 * @returns Resolved interval value
 */
function resolveInterval(config?: SWRConfiguration, fallback: number = DEFAULT_OBSERVABLE_INTERVAL) {
  const value = config?.refreshInterval
  return typeof value === 'number' && value > 0 ? value : fallback
}

/**
 * Custom hook for fetching live air quality data for a specific city.
 * Uses SWR for caching and provides real-time updates via observable pattern.
 *
 * @param cityId - City identifier or null to disable fetching
 * @param config - Optional SWR configuration overrides
 * @returns Object containing data, error, loading state, and refresh function
 */
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

/**
 * Custom hook for fetching complete air quality data (live + forecast) for a specific city.
 * Uses SWR for caching and provides real-time updates via observable pattern.
 *
 * @param cityId - City identifier or null to disable fetching
 * @param config - Optional SWR configuration overrides
 * @returns Object containing data, error, loading state, and refresh function
 */
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

/**
 * Custom hook for manually triggering refresh of all air quality data.
 * Notifies all subscribers of the observable to refresh their data.
 *
 * @returns Function to trigger global refresh
 */
export function useRefreshAll() {
  const refreshAll = useCallback(() => {
    airQualityObservable.notify()
  }, [])

  return refreshAll
}

/**
 * Custom hook for setting up periodic refresh of air quality data.
 * Automatically refreshes all data at specified intervals.
 *
 * @param intervalMs - Refresh interval in milliseconds (default: 10 minutes)
 * @returns Function to manually trigger refresh
 */
export function usePeriodicRefresh(intervalMs: number = 10 * 60 * 1000) {
  const refreshAll = useRefreshAll()

  useEffect(() => {
    const previousInterval = airQualityObservable.getIntervalMs()
    airQualityObservable.setIntervalMs(intervalMs)
    airQualityObservable.notify()

    return () => {
      airQualityObservable.setIntervalMs(previousInterval)
    }
  }, [intervalMs])

  return refreshAll
}