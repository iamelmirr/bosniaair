type RefreshHandler = () => void

interface SubscribeOptions {
  intervalMs?: number
}

const REFRESH_EVENT = 'aqi-refresh'

class AirQualityObservable {
  private readonly eventTarget = new EventTarget()
  private intervalId: number | null = null
  private subscriberCount = 0
  private intervalMs: number

  constructor(defaultIntervalMs: number) {
    this.intervalMs = defaultIntervalMs
  }

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

  notify(): void {
    this.eventTarget.dispatchEvent(new Event(REFRESH_EVENT))
  }

  getIntervalMs(): number {
    return this.intervalMs
  }

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

  stop(): void {
    this.clearTimer()
  }

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

  private restartTimer(): void {
    this.clearTimer()
    if (this.subscriberCount > 0) {
      this.ensureTimer()
    }
  }

  private clearTimer(): void {
    if (this.intervalId != null && typeof window !== 'undefined') {
      window.clearInterval(this.intervalId)
    }
    this.intervalId = null
  }
}

export const airQualityObservable = new AirQualityObservable(60 * 1000)
