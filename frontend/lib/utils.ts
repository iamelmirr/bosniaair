/**
 * Utility functions for the Sarajevo Air Quality application.
 * Contains helper functions for CSS classes, AQI categorization, and city management.
 */

/**
 * Combines multiple CSS class names, filtering out falsy values.
 * Useful for conditional class application in React components.
 *
 * @param classes - Variable number of class names (strings, undefined, null, or false)
 * @returns Space-separated string of valid class names
 */
export function classNames(...classes: (string | undefined | null | false)[]): string {
  return classes.filter(Boolean).join(' ')
}

/**
 * Returns the appropriate CSS class for AQI category styling.
 * Maps AQI values to color-coded text classes based on EPA standards.
 *
 * @param aqi - Air Quality Index value
 * @returns CSS class name for the AQI category
 */
export function getAqiCategoryClass(aqi: number): string {
  if (aqi <= 50) return 'text-aqi-good'
  if (aqi <= 100) return 'text-aqi-moderate'
  if (aqi <= 150) return 'text-aqi-usg'
  if (aqi <= 200) return 'text-aqi-unhealthy'
  if (aqi <= 300) return 'text-aqi-very-unhealthy'
  return 'text-aqi-hazardous'
}

/**
 * Available cities for air quality monitoring in Bosnia and Herzegovina.
 */
export const CITY_OPTIONS = [
  { id: 'Sarajevo', name: 'Sarajevo' },
  { id: 'Tuzla', name: 'Tuzla' },
  { id: 'Mostar', name: 'Mostar' },
  { id: 'Travnik', name: 'Travnik' },
  { id: 'Zenica', name: 'Zenica' },
  { id: 'Bihac', name: 'Bihać' }
] as const

/**
 * Type representing valid city IDs from the CITY_OPTIONS array.
 */
export type CityId = (typeof CITY_OPTIONS)[number]['id']

/**
 * Default primary city for air quality monitoring.
 */
export const DEFAULT_PRIMARY_CITY: CityId = 'Sarajevo'

/**
 * Converts a city ID to its display name.
 * @param cityId - The city identifier
 * @returns Display name of the city, or the ID if not found
 */
export function cityIdToLabel(cityId: CityId): string {
  const match = CITY_OPTIONS.find(city => city.id === cityId)
  return match ? match.name : cityId
}

/**
 * Type guard to check if a string is a valid city ID.
 * @param value - The value to check
 * @returns True if the value is a valid CityId
 */
export function isValidCityId(value: string | null | undefined): value is CityId {
  if (!value) {
    return false
  }
  return CITY_OPTIONS.some(city => city.id === value)
}