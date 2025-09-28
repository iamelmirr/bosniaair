/**
 * Observable pattern implementation for air quality data refresh management.
 * Provides a centralized way to notify multiple subscribers when air quality data should be refreshed.
 * Manages automatic periodic refresh timers and manual refresh notifications.
 */

type RefreshHandler = () => void

interface SubscribeOptions {
  intervalMs?: number
}

/**
 * Event name used for air quality refresh notifications.
 */
const REFRESH_EVENT = 'aqi-refresh'

/**
 * Observable class for managing air quality data refresh events.
 * Implements the observer pattern to notify subscribers when data should be refreshed.
 * Handles automatic periodic refresh timers and manual refresh triggers.
 */
class AirQualityObservable {
  private readonly eventTarget = new EventTarget()
  private intervalId: number | null = null
  private subscriberCount = 0
  private intervalMs: number

  /**
   * Creates a new AirQualityObservable instance.
   * @param defaultIntervalMs - Default refresh interval in milliseconds
   */
  constructor(defaultIntervalMs: number) {
    this.intervalMs = defaultIntervalMs
  }

  /**
   * Subscribes a handler to refresh events.
   * Optionally sets a custom refresh interval for this subscription.
   *
   * @param handler - Function to call when refresh is triggered
   * @param options - Subscription options including custom interval
   * @returns Unsubscribe function to remove the handler
   */
  subscribe(handler: RefreshHandler, options?: SubscribeOptions): () => void {
    const { intervalMs } = options ?? {}

    if (intervalMs && intervalMs !== this.intervalMs) {
      this.intervalMs = intervalMs
      this.restartTimer()
    }

    const listener = () => handler()
    this.eventTarget.addEventListener(REFRESH_EVENT, listener)
    this.subscriberCount += 1
    this.ensureTimer()

    return () => {
      this.eventTarget.removeEventListener(REFRESH_EVENT, listener)
      this.subscriberCount = Math.max(0, this.subscriberCount - 1)
      if (this.subscriberCount === 0) {
        this.clearTimer()
      }
    }
  }

  /**
   * Notifies all subscribers that a refresh should occur.
   * Triggers the refresh event for all registered handlers.
   */
  notify(): void {
    this.eventTarget.dispatchEvent(new Event(REFRESH_EVENT))
  }

  /**
   * Gets the current refresh interval in milliseconds.
   * @returns Current interval value
   */
  getIntervalMs(): number {
    return this.intervalMs
  }

  /**
   * Sets a new refresh interval in milliseconds.
   * Restarts the timer with the new interval if it changed.
   *
   * @param value - New interval in milliseconds (must be > 0)
   * @throws Error if value is not greater than zero
   */
  setIntervalMs(value: number): void {
    if (value <= 0) {
      throw new Error('Interval must be greater than zero')
    }
    if (value === this.intervalMs) {
      return
    }
    this.intervalMs = value
    this.restartTimer()
  }

  /**
   * Stops the automatic refresh timer.
   * No more automatic refresh events will be triggered.
   */
  stop(): void {
    this.clearTimer()
  }

  /**
   * Ensures a refresh timer is running if there are subscribers.
   * Creates a new timer if one doesn't exist and we're in a browser environment.
   */
  private ensureTimer(): void {
    if (this.intervalId != null) {
      return
    }

    if (typeof window === 'undefined') {
      return
    }

    this.intervalId = window.setInterval(() => {
      this.notify()
    }, this.intervalMs)
  }

  /**
   * Restarts the refresh timer with current settings.
   * Clears existing timer and creates a new one if there are subscribers.
   */
  private restartTimer(): void {
    this.clearTimer()
    if (this.subscriberCount > 0) {
      this.ensureTimer()
    }
  }

  /**
   * Clears the current refresh timer.
   * Stops automatic refresh events.
   */
  private clearTimer(): void {
    if (this.intervalId != null && typeof window !== 'undefined') {
      window.clearInterval(this.intervalId)
    }
    this.intervalId = null
  }
}

/**
 * Singleton instance of AirQualityObservable with default 60-second interval.
 */
export const airQualityObservable = new AirQualityObservable(60 * 1000)
