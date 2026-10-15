import { NextResponse } from 'next/server'
import { CityKey, DataUnavailableError, resolveCity } from './air-quality'

/** Public, read-only data: any origin may read it (e.g. a widget polling every few minutes). */
export const CORS_HEADERS = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Methods': 'GET, OPTIONS',
  'Access-Control-Allow-Headers': 'Content-Type, Cache-Control',
  'Access-Control-Max-Age': '600',
}

const CACHE_HEADERS = {
  // Shared CDN cache for 2 minutes, then serve stale while revalidating in the background.
  'Cache-Control': 'public, max-age=0, s-maxage=120, stale-while-revalidate=600',
}

export function preflight() {
  return new NextResponse(null, { status: 204, headers: CORS_HEADERS })
}

/** Resolves the city route param and runs the handler, mapping errors like the .NET controller did. */
export async function handleCityRequest<T>(
  cityParam: string,
  handler: (city: CityKey) => Promise<T>,
) {
  const city = resolveCity(cityParam)
  if (!city) {
    return NextResponse.json(
      { error: 'Unknown city', message: `Unsupported city '${cityParam}'` },
      { status: 400, headers: CORS_HEADERS },
    )
  }
  try {
    const body = await handler(city)
    return NextResponse.json(body, { headers: { ...CORS_HEADERS, ...CACHE_HEADERS } })
  } catch (err) {
    const message = (err as Error).message
    if (err instanceof DataUnavailableError) {
      console.warn(message)
      return NextResponse.json(
        { error: 'Data unavailable', message },
        { status: 503, headers: CORS_HEADERS },
      )
    }
    console.error(err)
    return NextResponse.json(
      { error: 'Internal server error', message },
      { status: 500, headers: CORS_HEADERS },
    )
  }
}
