import { useCallback, useEffect } from 'react'
import useSWR, { SWRConfiguration } from 'swr'
import { apiClient, AqiResponse, CompleteAqiResponse } from './api-client'
import { airQualityObservable } from './observable'
const defaultConfig: SWRConfiguration = {
  refreshInterval: 10 * 60 * 1000,
  revalidateOnFocus: true,
  revalidateOnReconnect: true,
  errorRetryCount: 3,
  errorRetryInterval: 5000,
}

const DEFAULT_OBSERVABLE_INTERVAL = 60 * 1000

function resolveInterval(config?: SWRConfiguration, fallback: number = DEFAULT_OBSERVABLE_INTERVAL) {
  const value = config?.refreshInterval
  return typeof value === 'number' && value > 0 ? value : fallback
}

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
    airQualityObservable.notify()

    return () => {
      airQualityObservable.setIntervalMs(previousInterval)
    }
  }, [intervalMs])

  return refreshAll
}