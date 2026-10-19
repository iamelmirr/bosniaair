// Unit tests for the station fallback rules. Run: npm test (Node 22.18+ runs .ts directly).
import { test } from 'node:test'
import assert from 'node:assert/strict'
import {
  boundsAround,
  cleanStationName,
  distanceKm,
  feedId,
  isFresh,
  rankFallbackCandidates,
  type WaqiBoundsStation,
} from './stations.ts'

const SARAJEVO: [number, number] = [43.8563, 18.4131]
const NOW = new Date('2026-10-19T00:30:00Z')
const hoursAgo = (h: number) => new Date(NOW.getTime() - h * 3_600_000).toISOString()

const station = (uid: number, lat: number, lon: number, aqi: string | number, time: string, name = `S${uid}`): WaqiBoundsStation => ({
  uid,
  lat,
  lon,
  aqi,
  station: { name, time },
})

test('distanceKm: Sarajevo to Vareš is about 34 km', () => {
  const km = distanceKm(SARAJEVO, [44.162, 18.3266])
  assert.ok(km > 33 && km < 36, `got ${km}`)
})

test('isFresh: 6 h is the limit, unparsable or missing times are not fresh', () => {
  assert.equal(isFresh(hoursAgo(5.9), NOW), true)
  assert.equal(isFresh(hoursAgo(6.1), NOW), false)
  assert.equal(isFresh('2026-10-19T09:00:00+09:00', NOW), true) // WAQI reports some stations in +09:00
  assert.equal(isFresh('not a date', NOW), false)
  assert.equal(isFresh(undefined, NOW), false)
})

test('rankFallbackCandidates: skips stale, no-AQI, too-far and primary stations; nearest first', () => {
  const stations = [
    station(10557, 43.8567, 18.3982, '-', hoursAgo(46), 'Sarajevo US Embassy, Bosnia Herzegovina'), // primary, silent
    station(9272, 43.8483, 18.3634, 91, hoursAgo(40)), // close but stale
    station(9263, 43.8647, 18.4194, '-', hoursAgo(1)), // close, fresh time, no AQI
    station(-706618, 44.162, 18.3266, '44', hoursAgo(0.3), 'Vareš (Župni ured Vareš)'), // ~34 km, fresh
    station(-475627, 44.1492, 17.811, '152', hoursAgo(0.2)), // ~57 km: another town
    station(-1, 43.95, 18.3, '60', hoursAgo(2), 'Closer one'), // ~14 km, fresh
    station(10557, 43.8567, 18.3982, '50', hoursAgo(1)), // primary id is never a fallback
  ]
  const ranked = rankFallbackCandidates(stations, SARAJEVO, { excludeUid: 10557, now: NOW })
  assert.deepEqual(ranked.map(c => c.uid), [-1, -706618])
  assert.equal(ranked[1].name, 'Vareš (Župni ured Vareš)')
  assert.equal(ranked[1].updatedAt, hoursAgo(0.3))
})

test('rankFallbackCandidates: nothing usable gives an empty list', () => {
  const stations = [station(9272, 43.8483, 18.3634, 91, hoursAgo(40))]
  assert.deepEqual(rankFallbackCandidates(stations, SARAJEVO, { now: NOW }), [])
})

test('helpers: feed id, station name, bounding box', () => {
  assert.equal(feedId(-706618), '@-706618')
  assert.equal(feedId(9319), '@9319')
  assert.equal(cleanStationName('Sarajevo Otoka, Bosnia Herzegovina'), 'Sarajevo Otoka')
  assert.equal(cleanStationName('Novi Travnik'), 'Novi Travnik')
  const [s, w, n, e] = boundsAround(SARAJEVO, 40).split(',').map(Number)
  assert.ok(s < SARAJEVO[0] && n > SARAJEVO[0] && w < SARAJEVO[1] && e > SARAJEVO[1])
  assert.ok(distanceKm(SARAJEVO, [n, SARAJEVO[1]]) >= 39.9)
  assert.ok(distanceKm(SARAJEVO, [SARAJEVO[0], e]) >= 39.9)
})
