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
import {
  boundsAround,
  cleanStationName,
  distanceKm,
  feedId,
  isFresh,
  MAX_STATION_AGE_HOURS,
  rankFallbackCandidates,
  WaqiBoundsStation,
} from './stations'

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

/**
 * Supported cities: the primary WAQI station and the city centre (lat, lng) that the
 * fallback measures distance from when the primary station stops reporting.
 */
export const CITIES = {
  Sarajevo: { name: 'Sarajevo', station: '@10557', geo: [43.8563, 18.4131] }, // Sarajevo US Embassy
  Tuzla: { name: 'Tuzla', station: '@9319', geo: [44.5384, 18.6671] }, // Tuzla Skver (old A462985 stopped reporting)
  Zenica: { name: 'Zenica', station: '@9267', geo: [44.2017, 17.9039] },
  Mostar: { name: 'Mostar', station: '@14726', geo: [43.3438, 17.8078] },
  Travnik: { name: 'Travnik', station: '@14693', geo: [44.2264, 17.6658] },
  Bihac: { name: 'Bihac', station: '@13578', geo: [44.8169, 15.8708] },
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

/** Which WAQI station the numbers come from. */
export interface StationSourceDto {
  /** WAQI station id, e.g. "@10557" (crowd-sourced stations have negative ids, e.g. "@-706618"). */
  stationId: string
  stationName: string
  /** True when the city's primary station is silent and a nearby station is used instead. */
  isFallback: boolean
  /** Distance from the city centre (km, one decimal). */
  distanceKm: number
  /** When this station last reported (UTC ISO). */
  updatedAt: string
  /** True when no station near the city reported within maxAgeHours: the values shown are old. */
  stale: boolean
  maxAgeHours: number
  primaryStationId: string
  primaryStationName: string
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
  source: StationSourceDto
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
  /** False when WAQI has no current forecast for this city (forecast is then empty). */
  available: boolean
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
  idx?: number
  city?: { name?: string; geo?: number[] }
  dominentpol?: string
  iaqi?: Record<string, { v: number } | undefined>
  time?: { s?: string; tz?: string; v?: number; iso?: string }
  forecast?: { daily?: { pm25?: WaqiForecastEntry[]; pm10?: WaqiForecastEntry[]; o3?: WaqiForecastEntry[] } }
}

async function waqiGet<T>(path: string, what: string): Promise<T> {
  const sep = path.includes('?') ? '&' : '?'
  const url = `${WAQI_BASE_URL}${path}${sep}token=${waqiToken()}`
  let res: Response
  try {
    res = await fetch(url, {
      headers: { 'User-Agent': 'BosniaAir/1.0' },
      cache: 'no-store',
      signal: AbortSignal.timeout(15000),
    })
  } catch (err) {
    throw new DataUnavailableError(`WAQI request failed for ${what}: ${(err as Error).message}`)
  }
  if (!res.ok) {
    throw new DataUnavailableError(`WAQI returned HTTP ${res.status} for ${what}`)
  }
  const body = (await res.json()) as { status?: string; data?: T | string }
  if (body.status !== 'ok' || !body.data || typeof body.data !== 'object') {
    throw new DataUnavailableError(`WAQI returned status '${body.status}' for ${what}`)
  }
  return body.data as T
}

function fetchFeed(stationId: string, what: string): Promise<WaqiData> {
  return waqiGet<WaqiData>(`/feed/${stationId}/`, what)
}

/** A station feed counts as reporting when it has an AQI and a measurement time within the age limit. */
function feedIsLive(data: WaqiData, now: Date): boolean {
  return overallAqi(data) !== null && isFresh(data.time?.iso, now)
}

/** What the endpoints are built from: the primary station plus the station the live numbers come from. */
interface CityStations {
  primary: WaqiData
  live: WaqiData
  source: StationSourceDto
}

/** How many nearby candidates are tried (one feed call each) before giving up. */
const MAX_FALLBACK_ATTEMPTS = 3

async function fetchCityStationsUncached(city: CityKey): Promise<CityStations> {
  const cfg = CITIES[city]
  const now = new Date()
  const primary = await fetchFeed(cfg.station, city)
  const primaryName = cleanStationName(primary.city?.name)
  const sourceFor = (data: WaqiData, stationId: string, isFallback: boolean, stale: boolean): StationSourceDto => {
    const geo = data.city?.geo
    const km = geo && geo.length >= 2 ? distanceKm(cfg.geo, [geo[0], geo[1]]) : 0
    return {
      stationId,
      stationName: cleanStationName(data.city?.name),
      isFallback,
      distanceKm: Math.round(km * 10) / 10,
      updatedAt: parseTimestamp(data.time),
      stale,
      maxAgeHours: MAX_STATION_AGE_HOURS,
      primaryStationId: cfg.station,
      primaryStationName: primaryName,
    }
  }

  if (feedIsLive(primary, now)) {
    return { primary, live: primary, source: sourceFor(primary, cfg.station, false, false) }
  }

  // The primary station is silent: use the nearest station around the city that still reports.
  try {
    const stations = await waqiGet<WaqiBoundsStation[]>(
      `/v2/map/bounds?latlng=${boundsAround(cfg.geo)}&networks=all`,
      `stations near ${city}`,
    )
    const candidates = rankFallbackCandidates(Array.isArray(stations) ? stations : [], cfg.geo, {
      excludeUid: typeof primary.idx === 'number' ? primary.idx : undefined,
      now,
    })
    for (const candidate of candidates.slice(0, MAX_FALLBACK_ATTEMPTS)) {
      const id = feedId(candidate.uid)
      try {
        const data = await fetchFeed(id, `${city} fallback ${id}`)
        if (feedIsLive(data, now)) {
          return { primary, live: data, source: sourceFor(data, id, true, false) }
        }
      } catch (err) {
        console.warn((err as Error).message)
      }
    }
  } catch (err) {
    console.warn((err as Error).message)
  }

  // Nothing nearby reports either: serve the primary station's last values, flagged as stale.
  return { primary, live: primary, source: sourceFor(primary, cfg.station, false, true) }
}

/**
 * Cached per city: a few WAQI calls at most per city per UPSTREAM_REVALIDATE_SECONDS,
 * shared by all requests (replaces the .NET background scheduler + DB cache).
 * Errors are thrown and therefore never cached.
 */
const fetchCityStations = unstable_cache(fetchCityStationsUncached, ['waqi-city-stations-v2'], {
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

function mapLive(city: CityKey, data: WaqiData, source: StationSourceDto): LiveAqiResponse {
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
      locationName: source.stationName,
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
    source,
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

  return { city: CITIES[city].name, forecast, timestamp: parseTimestamp(data.time), available: forecast.length > 0 }
}

/** The primary station's forecast; if it has none from today on, the live station's (when that is another station). */
function forecastFor(city: CityKey, stations: CityStations): ForecastResponse {
  const primary = mapForecast(city, stations.primary)
  // (Compare by flag, not identity: the cache hands back deserialised copies.)
  if (primary.available || !stations.source.isFallback) return primary
  const live = mapForecast(city, stations.live)
  return live.available ? live : primary
}

// ---------------------------------------------------------------------------
// Public service functions (same semantics as AirQualityService.cs)
// ---------------------------------------------------------------------------

export async function getLive(city: CityKey): Promise<LiveAqiResponse> {
  const stations = await fetchCityStations(city)
  return mapLive(city, stations.live, stations.source)
}

/** A city without a current forecast gets 200 with forecast: [] and available: false, not an error. */
export async function getForecast(city: CityKey): Promise<ForecastResponse> {
  return forecastFor(city, await fetchCityStations(city))
}

export async function getComplete(city: CityKey): Promise<CompleteAqiResponse> {
  const stations = await fetchCityStations(city)
  return {
    liveData: mapLive(city, stations.live, stations.source),
    forecastData: forecastFor(city, stations),
    retrievedAt: new Date().toISOString(),
  }
}
