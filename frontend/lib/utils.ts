export function classNames(...classes: (string | undefined | null | false)[]): string {
  return classes.filter(Boolean).join(' ')
}

export function getAqiCategoryClass(aqi: number): string {
  if (aqi <= 50) return 'text-aqi-good'
  if (aqi <= 100) return 'text-aqi-moderate'
  if (aqi <= 150) return 'text-aqi-usg'
  if (aqi <= 200) return 'text-aqi-unhealthy'
  if (aqi <= 300) return 'text-aqi-very-unhealthy'
  return 'text-aqi-hazardous'
}

export const CITY_OPTIONS = [
  { id: 'Sarajevo', name: 'Sarajevo' },
  { id: 'Tuzla', name: 'Tuzla' },
  { id: 'Mostar', name: 'Mostar' },
  { id: 'Travnik', name: 'Travnik' },
  { id: 'Zenica', name: 'Zenica' },
  { id: 'Bihac', name: 'Bihać' }
] as const

export type CityId = (typeof CITY_OPTIONS)[number]['id']

export const DEFAULT_PRIMARY_CITY: CityId = 'Sarajevo'

export function cityIdToLabel(cityId: CityId): string {
  const match = CITY_OPTIONS.find(city => city.id === cityId)
  return match ? match.name : cityId
}

export function isValidCityId(value: string | null | undefined): value is CityId {
  if (!value) {
    return false
  }
  return CITY_OPTIONS.some(city => city.id === value)
}