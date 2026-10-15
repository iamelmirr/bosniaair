import { getComplete } from '@/lib/server/air-quality'
import { handleCityRequest, preflight } from '@/lib/server/http'

export const dynamic = 'force-dynamic'

export async function GET(_req: Request, { params }: { params: { city: string } }) {
  return handleCityRequest(params.city, getComplete)
}

export const OPTIONS = preflight
