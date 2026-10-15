import { NextResponse } from 'next/server'
import { CORS_HEADERS } from '@/lib/server/http'

export const dynamic = 'force-dynamic'

export function GET() {
  return NextResponse.json({ status: 'Healthy', time: new Date().toISOString() }, { headers: CORS_HEADERS })
}
