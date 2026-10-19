/**
 * Station selection rules (pure functions, no I/O, unit-tested in stations.test.ts).
 *
 * Each city has a primary WAQI station. When it has not reported for more than
 * MAX_STATION_AGE_HOURS, the API falls back to the nearest station that has, as long
 * as it is within MAX_FALLBACK_DISTANCE_KM of the city centre.
 */

/** A station older than this is treated as not reporting. */
export const MAX_STATION_AGE_HOURS = 6

/** A fallback station further away than this would describe another place, not the city. */
export const MAX_FALLBACK_DISTANCE_KM = 40

/** One entry of WAQI's /v2/map/bounds response. */
export interface WaqiBoundsStation {
  uid: number
  lat: number
  lon: number
  aqi: string | number
  station?: { name?: string; time?: string }
}

export interface FallbackCandidate {
  uid: number
  name: string
  distanceKm: number
  updatedAt: string
}

/** Great-circle distance in kilometres. */
export function distanceKm(a: readonly [number, number], b: readonly [number, number]): number {
  const R = 6371
  const rad = (x: number) => (x * Math.PI) / 180
  const dLat = rad(b[0] - a[0])
  const dLon = rad(b[1] - a[1])
  const h = Math.sin(dLat / 2) ** 2 + Math.cos(rad(a[0])) * Math.cos(rad(b[0])) * Math.sin(dLon / 2) ** 2
  return 2 * R * Math.asin(Math.sqrt(h))
}

/** True when the ISO timestamp parses and is at most maxAgeHours old (small clock skew into the future is fine). */
export function isFresh(iso: string | undefined, now: Date = new Date(), maxAgeHours = MAX_STATION_AGE_HOURS): boolean {
  if (!iso) return false
  const t = Date.parse(iso)
  if (Number.isNaN(t)) return false
  return now.getTime() - t <= maxAgeHours * 3_600_000
}

/** WAQI path segment for a station uid: crowd-sourced stations have negative uids ("@-706618"). */
export function feedId(uid: number | string): string {
  return `@${uid}`
}

/** "Sarajevo Otoka, Bosnia Herzegovina" -> "Sarajevo Otoka". */
export function cleanStationName(name: string | undefined): string {
  return (name ?? '').replace(/,\s*Bosnia(?: and)? Herzegovina\s*$/i, '').trim() || 'nepoznata stanica'
}

/** The bounding box (south, west, north, east) that covers radiusKm around a point. */
export function boundsAround(center: readonly [number, number], radiusKm = MAX_FALLBACK_DISTANCE_KM): string {
  const dLat = radiusKm / 111
  const dLon = radiusKm / (111 * Math.cos((center[0] * Math.PI) / 180))
  const f = (n: number) => n.toFixed(4)
  return [center[0] - dLat, center[1] - dLon, center[0] + dLat, center[1] + dLon].map(f).join(',')
}

/**
 * Stations that may replace a silent primary station, nearest first: they report a numeric
 * AQI, have reported within MAX_STATION_AGE_HOURS and lie within maxDistanceKm of the city.
 */
export function rankFallbackCandidates(
  stations: WaqiBoundsStation[],
  center: readonly [number, number],
  options: { excludeUid?: number; now?: Date; maxDistanceKm?: number } = {},
): FallbackCandidate[] {
  const { excludeUid, now = new Date(), maxDistanceKm = MAX_FALLBACK_DISTANCE_KM } = options
  return stations
    .filter(s => s.uid !== excludeUid)
    .filter(s => Number.isFinite(Number(s.aqi)) && String(s.aqi).trim() !== '')
    .filter(s => isFresh(s.station?.time, now))
    .map(s => ({
      uid: s.uid,
      name: cleanStationName(s.station?.name),
      distanceKm: distanceKm(center, [s.lat, s.lon]),
      updatedAt: new Date(Date.parse(s.station!.time!)).toISOString(),
    }))
    .filter(c => c.distanceKm <= maxDistanceKm)
    .sort((a, b) => a.distanceKm - b.distanceKm)
}
