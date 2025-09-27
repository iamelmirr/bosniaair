/*
===========================================================================================
                                FRONTEND UTILITY FUNCTIONS
===========================================================================================

PURPOSE & CROSS-COMPONENT FUNCTIONALITY:
Shared utility functions za data formatting, visualization, i common operations.
Provides consistent behavior across all React components.

UTILITY CATEGORIES:
- Number Formatting: Precision handling za different value ranges
- Date/Time Formatting: Localized temporal display
- AQI Visualization: Color coding i category mapping
- User Experience: Interactive features i accessibility
- Web APIs: Browser feature integration

DESIGN PRINCIPLES:
- Pure Functions: No side effects, predictable outputs
- Localization Ready: Bosnian locale support
- Type Safety: Full TypeScript coverage
- Browser Compatibility: Modern web API usage sa fallbacks
*/

// Common utility functions za aplikaciju

/*
=== NUMERIC FORMATTING ===

ADAPTIVE PRECISION DISPLAY:
Different decimal precision based on value magnitude
Optimized za readability u different contexts
*/

/// <summary>
/// Formats numeric values sa adaptive decimal precision
/// Smaller values get more decimals za better readability
/// </summary>
export function formatNumber(value: number, decimals: number = 1): string {
  /*
  PRECISION STRATEGY:
  < 1: Show 2 decimals za accuracy (0.25 μg/m³)
  < 10: Show specified decimals (5.4 μg/m³)  
  >= 10: Round to integers za cleaner display (127 μg/m³)
  */
  if (value < 1) {
    return value.toFixed(2)
  } else if (value < 10) {
    return value.toFixed(decimals)
  } else {
    return Math.round(value).toString()
  }
}

/*
=== DATE/TIME FORMATTING ===

LOCALIZED TEMPORAL DISPLAY:
Bosnian locale support za consistent date formatting
Intl.DateTimeFormat provides browser-native localization
*/

/// <summary>
/// Formats Date objects using Bosnian locale standards
/// Provides consistent date/time display across application
/// </summary>
export function formatDate(date: Date, locale: string = 'bs-BA'): string {
  return new Intl.DateTimeFormat(locale, {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(date)
}

/*
=== RELATIVE TIME FORMATTING ===

HUMAN-FRIENDLY TEMPORAL CONTEXT:
Shows "ago" format za recent timestamps
Falls back to absolute date za older content
*/

/// <summary>
/// Formats dates relative to current time sa human-friendly labels
/// Recent dates show "Xm ago", older dates show absolute format
/// </summary>
export function formatDateRelative(date: Date, locale: string = 'bs-BA'): string {
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMinutes = Math.floor(diffMs / (1000 * 60))
  const diffHours = Math.floor(diffMs / (1000 * 60 * 60))
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))

  /*
  RELATIVE TIME SCALE:
  < 1min: "Just now" za immediate feedback
  < 1hr: "Xm ago" za recent updates  
  < 1day: "Xh ago" za same-day content
  < 1week: "Xd ago" za recent history
  >= 1week: Absolute date za older content
  */
  if (diffMinutes < 1) {
    return 'Just now'
  } else if (diffMinutes < 60) {
    return `${diffMinutes}m ago`
  } else if (diffHours < 24) {
    return `${diffHours}h ago`
  } else if (diffDays < 7) {
    return `${diffDays}d ago`
  } else {
    return formatDate(date, locale)
  }
}

/*
=== AQI VISUALIZATION ===

EPA COLOR STANDARD MAPPING:
Official EPA Air Quality Index color scheme
Provides consistent visual indicators across all components
*/

/// <summary>
/// Maps AQI values to EPA standard color codes
/// Returns RGB color strings za consistent visual representation
/// </summary>
export function getAqiColor(aqi: number): string {
  /*
  EPA OFFICIAL COLOR SCHEME:
  0-50: Green (Good air quality)
  51-100: Yellow (Moderate - acceptable za most people)
  101-150: Orange (Unhealthy za sensitive groups)
  151-200: Red (Unhealthy za everyone)
  201-300: Purple (Very unhealthy - health warnings)
  301+: Maroon (Hazardous - emergency conditions)
  */
  if (aqi <= 50) return 'rgb(0, 153, 102)'    // Good - Green
  if (aqi <= 100) return 'rgb(255, 221, 0)'   // Moderate - Yellow
  if (aqi <= 150) return 'rgb(255, 153, 0)'   // USG - Orange
  if (aqi <= 200) return 'rgb(255, 0, 0)'     // Unhealthy - Red
  if (aqi <= 300) return 'rgb(153, 0, 153)'   // Very Unhealthy - Purple
  return 'rgb(126, 0, 35)'                     // Hazardous - Maroon
}

/*
=== AQI CATEGORY CLASSIFICATION ===

EPA STANDARD CATEGORIES:
Maps numeric AQI values to human-readable category names
Essential za health advisory i user communication
*/

/// <summary>
/// Maps AQI numeric values to EPA standard category names
/// Returns string labels za user-facing displays
/// </summary>
export function getAqiCategory(aqi: number): string {
  if (aqi <= 50) return 'Good'
  if (aqi <= 100) return 'Moderate'
  if (aqi <= 150) return 'Unhealthy for Sensitive Groups'
  if (aqi <= 200) return 'Unhealthy'
  if (aqi <= 300) return 'Very Unhealthy'
  return 'Hazardous'
}

export function getAqiCategoryClass(aqi: number): string {
  if (aqi <= 50) return 'text-aqi-good'
  if (aqi <= 100) return 'text-aqi-moderate'
  if (aqi <= 150) return 'text-aqi-usg'
  if (aqi <= 200) return 'text-aqi-unhealthy'
  if (aqi <= 300) return 'text-aqi-very-unhealthy'
  return 'text-aqi-hazardous'
}

export function getHealthAdvice(category: string, group?: string): string {
  const baseAdvice = {
    'Good': 'Air quality is satisfactory. Enjoy outdoor activities!',
    'Moderate': 'Air quality is acceptable for most people. Unusually sensitive people should consider reducing prolonged outdoor exertion.',
    'Unhealthy for Sensitive Groups': 'Members of sensitive groups may experience health effects. The general public is less likely to be affected.',
    'Unhealthy': 'Everyone may begin to experience health effects. Members of sensitive groups may experience more serious health effects.',
    'Very Unhealthy': 'Health warnings of emergency conditions. The entire population is more likely to be affected.',
    'Hazardous': 'Health alert: everyone may experience more serious health effects.',
  }

  const groupSpecificAdvice = {
    'Sportisti': {
      'Good': 'Perfect conditions for outdoor sports and exercise.',
      'Moderate': 'Good for most activities. Consider shorter, less intense workouts if sensitive.',
      'Unhealthy for Sensitive Groups': 'Reduce prolonged outdoor exercise. Consider indoor activities.',
      'Unhealthy': 'Avoid outdoor exercise. Choose indoor activities instead.',
      'Very Unhealthy': 'Cancel outdoor sports. Stay indoors and avoid physical exertion.',
      'Hazardous': 'Do not exercise outdoors under any circumstances.',
    },
    'Djeca': {
      'Good': 'Great day for outdoor play and school activities.',
      'Moderate': 'Generally safe for outdoor activities. Watch sensitive children.',
      'Unhealthy for Sensitive Groups': 'Limit outdoor time for children with asthma or allergies.',
      'Unhealthy': 'Keep children indoors. Cancel outdoor school activities.',
      'Very Unhealthy': 'Children should stay inside. Close school windows.',
      'Hazardous': 'Emergency conditions. Keep all children indoors.',
    },
    'Stariji': {
      'Good': 'Excellent conditions for outdoor activities and walks.',
      'Moderate': 'Generally safe. Those with heart/lung conditions should be cautious.',
      'Unhealthy for Sensitive Groups': 'Elderly should limit outdoor activities.',
      'Unhealthy': 'Stay indoors. Avoid physical exertion outside.',
      'Very Unhealthy': 'Remain indoors. Seek medical advice if experiencing symptoms.',
      'Hazardous': 'Emergency level. Stay inside and consider medical consultation.',
    },
    'Astmatičari': {
      'Good': 'Safe for normal activities. Keep rescue inhaler nearby.',
      'Moderate': 'Monitor symptoms. Reduce outdoor time if experiencing discomfort.',
      'Unhealthy for Sensitive Groups': 'High risk. Stay indoors and have medications ready.',
      'Unhealthy': 'Avoid going outside. Use prescribed medications as needed.',
      'Very Unhealthy': 'Emergency risk level. Stay indoors and monitor breathing closely.',
      'Hazardous': 'Extreme danger. Stay inside and seek immediate medical attention if needed.',
    },
  }

  if (group && groupSpecificAdvice[group as keyof typeof groupSpecificAdvice]) {
    return groupSpecificAdvice[group as keyof typeof groupSpecificAdvice][category as keyof typeof groupSpecificAdvice.Sportisti] || 
           baseAdvice[category as keyof typeof baseAdvice] || 
           'Monitor air quality conditions.'
  }

  return baseAdvice[category as keyof typeof baseAdvice] || 'Monitor air quality conditions.'
}

export function shareData(title: string, text: string, url?: string): Promise<void> {
  const shareData = {
    title,
    text,
    url: url || window.location.href,
  }

  if (navigator.share && navigator.canShare(shareData)) {
    return navigator.share(shareData)
  } else {
    // Fallback for browsers without Web Share API
    const shareText = `${title}\n\n${text}\n\n${shareData.url}`
    return navigator.clipboard.writeText(shareText).then(() => {
      // Could show a toast notification here
      console.log('Link copied to clipboard')
    })
  }
}

export function debounce<T extends (...args: any[]) => any>(
  func: T,
  delay: number
): (...args: Parameters<T>) => void {
  let timeoutId: NodeJS.Timeout
  return (...args: Parameters<T>) => {
    clearTimeout(timeoutId)
    timeoutId = setTimeout(() => func.apply(null, args), delay)
  }
}

export function throttle<T extends (...args: any[]) => any>(
  func: T,
  limit: number
): (...args: Parameters<T>) => void {
  let inThrottle: boolean
  return (...args: Parameters<T>) => {
    if (!inThrottle) {
      func.apply(null, args)
      inThrottle = true
      setTimeout(() => (inThrottle = false), limit)
    }
  }
}

export function classNames(...classes: (string | undefined | null | false)[]): string {
  return classes.filter(Boolean).join(' ')
}

// Constants for the app
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
export const DEFAULT_COMPARISON_CITIES: CityId[] = ['Tuzla', 'Mostar']

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

export function sanitizeComparisonCities(values: unknown): CityId[] {
  if (!Array.isArray(values)) {
    return [...DEFAULT_COMPARISON_CITIES]
  }

  const unique = new Set<CityId>()
  for (const value of values) {
    if (typeof value === 'string' && isValidCityId(value)) {
      unique.add(value)
    }
  }

  if (unique.size === 0) {
    DEFAULT_COMPARISON_CITIES.forEach(city => unique.add(city))
  }

  return Array.from(unique)
}

export const POLLUTANT_NAMES = {
  'pm25': 'PM2.5',
  'pm10': 'PM10', 
  'o3': 'O₃',
  'no2': 'NO₂',
  'so2': 'SO₂',
  'co': 'CO',
} as const

export const POLLUTANT_DESCRIPTIONS = {
  'pm25': 'Fine particulate matter (≤2.5μm)',
  'pm10': 'Coarse particulate matter (≤10μm)',
  'o3': 'Ground-level ozone',
  'no2': 'Nitrogen dioxide',
  'so2': 'Sulfur dioxide', 
  'co': 'Carbon monoxide',
} as const

export type PollutantKey = keyof typeof POLLUTANT_NAMES