/**
 * Server-side air quality logic (port of the former .NET backend).
 *
 * Fetches station data from the WAQI API and maps it to the same JSON shape the
 * .NET API returned, so the frontend components work unchanged. The .NET backend
 * kept a SQLite/Postgres table that a background worker refreshed every 10 minutes;
 * here the Next.js Data Cache (fetch revalidate) plays that role, so no database
 * or long-running process is needed.
 */

import net from 'node:net'
import { unstable_cache } from 'next/cache'

// api.waqi.info is hosted in Tokyo. Node's default happy-eyeballs timeout (250 ms per
// address) is shorter than the TCP connect time from Europe, and its IPv6 address is
// often unreachable, so fetch() fails with ETIMEDOUT. Give each attempt more time.
if (typeof net.setDefaultAutoSelectFamilyAttemptTimeout === 'function') {
  net.setDefaultAutoSelectFamilyAttemptTimeout(2500)
}

/** How long a WAQI response is reused before it is fetched again (seconds). */
export const UPSTREAM_REVALIDATE_SECONDS = 300

const WAQI_BASE_URL = 'https://api.waqi.info'

/**
 * WAQI token from the hosting environment (WAQI_API_TOKEN). There is no fallback:
 * read per request, so a missing token fails with a clear error instead of at build time.
 */
function waqiToken(): string {
  const token = process.env.WAQI_API_TOKEN?.trim()
  if (!token) {
    throw new Error('WAQI_API_TOKEN is not set: add it to the environment (free token: https://aqicn.org/data-platform/token/)')
  }
  return token
}

/** Supported cities and their WAQI station identifiers. */
export const CITIES = {
  Sarajevo: { name: 'Sarajevo', station: '@10557' }, // Sarajevo US Embassy
  Tuzla: { name: 'Tuzla', station: '@9319' }, // Tuzla Skver (old A462985 stopped reporting)
  Zenica: { name: 'Zenica', station: '@9267' },
  Mostar: { name: 'Mostar', station: '@14726' },
  Travnik: { name: 'Travnik', station: '@14693' },
  Bihac: { name: 'Bihac', station: '@13578' },
} as const

export type CityKey = keyof typeof CITIES

/** Case-insensitive city lookup (the .NET enum binding was case-insensitive too). */
export function resolveCity(input: string): CityKey | null {
  const wanted = decodeURIComponent(input).toLowerCase().replace('ć', 'c')
  const keys = Object.keys(CITIES) as CityKey[]
  return keys.find(key => key.toLowerCase() === wanted) ?? null
}

export class DataUnavailableError extends Error {}

// ---------------------------------------------------------------------------
// Response types (identical field names to the .NET DTOs, camelCase)
// ---------------------------------------------------------------------------

export interface MeasurementDto {
  id: string
  city: string
  locationName: string
  parameter: string
  value: number
  unit: string
  timestamp: string
  sourceName: string
  coordinates: null
  averagingPeriod: null
}

export interface LiveAqiResponse {
  city: string
  overallAqi: number
  aqiCategory: string
  color: string
  healthMessage: string
  timestamp: string
  measurements: MeasurementDto[]
  dominantPollutant: string
}

interface PollutantRange {
  avg: number
  min: number
  max: number
}

export interface ForecastDayDto {
  date: string
  aqi: number
  category: string
  color: string
  pollutants: { pm25: PollutantRange | null; pm10: PollutantRange | null; o3: PollutantRange | null }
}

export interface ForecastResponse {
  city: string
  forecast: ForecastDayDto[]
  timestamp: string
}

export interface CompleteAqiResponse {
  liveData: LiveAqiResponse
  forecastData: ForecastResponse
  retrievedAt: string
}

// ---------------------------------------------------------------------------
// WAQI raw types
// ---------------------------------------------------------------------------

interface WaqiForecastEntry {
  avg: number
  day: string
  max: number
  min: number
}

interface WaqiData {
  aqi: number | string
  dominentpol?: string
  iaqi?: Record<string, { v: number } | undefined>
  time?: { s?: string; tz?: string; v?: number; iso?: string }
  forecast?: { daily?: { pm25?: WaqiForecastEntry[]; pm10?: WaqiForecastEntry[]; o3?: WaqiForecastEntry[] } }
}

async function fetchStationUncached(city: CityKey): Promise<WaqiData> {
  const url = `${WAQI_BASE_URL}/feed/${CITIES[city].station}/?token=${waqiToken()}`
  let res: Response
  try {
    res = await fetch(url, {
      headers: { 'User-Agent': 'BosniaAir/1.0' },
      cache: 'no-store',
      signal: AbortSignal.timeout(15000),
    })
  } catch (err) {
    throw new DataUnavailableError(`WAQI request failed for ${city}: ${(err as Error).message}`)
  }
  if (!res.ok) {
    throw new DataUnavailableError(`WAQI returned HTTP ${res.status} for ${city}`)
  }
  const body = (await res.json()) as { status?: string; data?: WaqiData | string }
  if (body.status !== 'ok' || !body.data || typeof body.data !== 'object') {
    throw new DataUnavailableError(`WAQI returned status '${body.status}' for ${city}`)
  }
  return body.data
}

/**
 * Cached station fetch: one WAQI call per city per UPSTREAM_REVALIDATE_SECONDS,
 * shared by all requests (replaces the .NET background scheduler + DB cache).
 * Errors are thrown and therefore never cached.
 */
const fetchStation = unstable_cache(fetchStationUncached, ['waqi-station-v1'], {
  revalidate: UPSTREAM_REVALIDATE_SECONDS,
})

// ---------------------------------------------------------------------------
// Mapping (ported from AirQualityMapper.cs)
// ---------------------------------------------------------------------------

function getAqiInfo(aqi: number): { category: string; color: string; message: string } {
  if (aqi <= 50) return { category: 'Good', color: '#00E400', message: 'Air quality is considered satisfactory, and air pollution poses little or no risk.' }
  if (aqi <= 100) return { category: 'Moderate', color: '#FFFF00', message: 'Air quality is acceptable for most people. However, for some pollutants there may be a moderate health concern for a very small number of people who are unusually sensitive to air pollution.' }
  if (aqi <= 150) return { category: 'Unhealthy for Sensitive Groups', color: '#FF7E00', message: 'Members of sensitive groups may experience health effects. The general public is not likely to be affected.' }
  if (aqi <= 200) return { category: 'Unhealthy', color: '#FF0000', message: 'Everyone may begin to experience health effects; members of sensitive groups may experience more serious health effects.' }
  if (aqi <= 300) return { category: 'Very Unhealthy', color: '#8F3F97', message: 'Health warnings of emergency conditions. The entire population is more likely to be affected.' }
  return { category: 'Hazardous', color: '#7E0023', message: 'Health alert: everyone may experience more serious health effects.' }
}

const POLLUTANT_NAMES: Record<string, string> = {
  pm25: 'PM2.5',
  pm10: 'PM10',
  no2: 'NO2',
  o3: 'O3',
  so2: 'SO2',
  co: 'CO',
}

/** Measurement time as a real UTC ISO string. */
function parseTimestamp(time: WaqiData['time']): string {
  if (time?.iso) {
    const d = new Date(time.iso)
    if (!isNaN(d.getTime())) return d.toISOString()
  }
  if (time?.v && time.v > 0) {
    // WAQI's "v" is local station time expressed as epoch seconds; prefer iso above.
    return new Date(time.v * 1000).toISOString()
  }
  return new Date().toISOString()
}

/** Today's date in Sarajevo as yyyy-MM-dd. */
function sarajevoToday(): string {
  return new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Europe/Sarajevo',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).format(new Date())
}

function overallAqi(data: WaqiData): number | null {
  const n = typeof data.aqi === 'number' ? data.aqi : Number(data.aqi)
  if (Number.isFinite(n)) return Math.round(n)
  // Station reports "-" when its composite AQI is missing: fall back to the highest sub-index.
  const subs = ['pm25', 'pm10', 'o3', 'no2', 'so2', 'co']
    .map(p => data.iaqi?.[p]?.v)
    .filter((v): v is number => typeof v === 'number')
  return subs.length > 0 ? Math.round(Math.max(...subs)) : null
}

function mapLive(city: CityKey, data: WaqiData): LiveAqiResponse {
  const aqi = overallAqi(data)
  if (aqi === null) {
    throw new DataUnavailableError(`No AQI value available for ${city}`)
  }
  const timestamp = parseTimestamp(data.time)
  const name = CITIES[city].name
  const { category, color, message } = getAqiInfo(aqi)

  const measurements: MeasurementDto[] = []
  const add = (key: string, parameter: string, unit: string) => {
    const v = data.iaqi?.[key]?.v
    if (typeof v !== 'number') return
    measurements.push({
      id: `${city}_${timestamp}_${parameter.toLowerCase()}`,
      city: name,
      locationName: name,
      parameter,
      value: v,
      unit,
      timestamp,
      sourceName: 'WAQI',
      coordinates: null,
      averagingPeriod: null,
    })
  }
  add('pm25', 'PM2.5', 'μg/m³')
  add('pm10', 'PM10', 'μg/m³')
  add('o3', 'O3', 'μg/m³')
  add('no2', 'NO2', 'μg/m³')
  add('co', 'CO', 'mg/m³')
  add('so2', 'SO2', 'μg/m³')

  return {
    city: name,
    overallAqi: aqi,
    aqiCategory: category,
    color,
    healthMessage: message,
    timestamp,
    measurements,
    dominantPollutant: POLLUTANT_NAMES[(data.dominentpol ?? '').toLowerCase()] ?? 'Unknown',
  }
}

function mapForecast(city: CityKey, data: WaqiData): ForecastResponse {
  const daily = data.forecast?.daily
  const days = new Map<string, ForecastDayDto['pollutants']>()
  const merge = (entries: WaqiForecastEntry[] | undefined, key: 'pm25' | 'pm10' | 'o3') => {
    for (const e of entries ?? []) {
      if (!/^\d{4}-\d{2}-\d{2}$/.test(e.day)) continue
      const slot = days.get(e.day) ?? { pm25: null, pm10: null, o3: null }
      slot[key] = { avg: Math.round(e.avg), min: Math.round(e.min), max: Math.round(e.max) }
      days.set(e.day, slot)
    }
  }
  merge(daily?.pm25, 'pm25')
  merge(daily?.pm10, 'pm10')
  merge(daily?.o3, 'o3')

  const today = sarajevoToday()
  const forecast = Array.from(days.keys())
    .sort()
    .filter(date => date >= today)
    .map(date => {
      const pollutants = days.get(date)!
      const aqi = pollutants.pm25?.avg ?? pollutants.pm10?.avg ?? pollutants.o3?.avg ?? 0
      const { category, color } = getAqiInfo(aqi)
      return { date, aqi, category, color, pollutants }
    })

  return { city: CITIES[city].name, forecast, timestamp: parseTimestamp(data.time) }
}

// ---------------------------------------------------------------------------
// Public service functions (same semantics as AirQualityService.cs)
// ---------------------------------------------------------------------------

export async function getLive(city: CityKey): Promise<LiveAqiResponse> {
  return mapLive(city, await fetchStation(city))
}

export async function getForecast(city: CityKey): Promise<ForecastResponse> {
  const result = mapForecast(city, await fetchStation(city))
  if (result.forecast.length === 0) {
    throw new DataUnavailableError(`No forecast available for ${city}`)
  }
  return result
}

export async function getComplete(city: CityKey): Promise<CompleteAqiResponse> {
  const data = await fetchStation(city)
  return {
    liveData: mapLive(city, data),
    forecastData: mapForecast(city, data),
    retrievedAt: new Date().toISOString(),
  }
}
