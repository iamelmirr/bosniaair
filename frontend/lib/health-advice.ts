/*
===========================================================================================
                               HEALTH ADVICE - CLIENT SIDE LOGIC
===========================================================================================

PURPOSE: Sve zdravstvene preporuke prebačene u frontend kao statička logika
Eliminiše potrebu za backend HealthAdviceService i /api/v1/groups endpoint

BUSINESS VALUE:
- Instant loading (zero network calls)
- Offline functionality  
- Consistent recommendations
- Simplified backend architecture

DESIGN: Copied EXACTLY from backend HealthAdviceService.cs for consistency
*/

export interface HealthGroup {
  name: string;
  icon: string;
  description: string;
  threshold: number;
}

export interface HealthAdvice {
  group: string;
  riskLevel: 'low' | 'moderate' | 'high' | 'very-high';
  recommendation: string;
  icon: string;
  description: string;
}

// Statičke definicije zdravstvenih grupa - IDENTIČNE kao u backend-u
export const HEALTH_GROUPS: HealthGroup[] = [
  {
    name: 'Sportisti',
    icon: '🏃‍♂️',
    description: 'Aktivni sportisti i rekreativci',
    threshold: 100
  },
  {
    name: 'Djeca',
    icon: '👶',
    description: 'Djeca i mladi do 18 godina',
    threshold: 75
  },
  {
    name: 'Stariji',
    icon: '👴',
    description: 'Odrasli stariji od 65 godina',
    threshold: 75
  },
  {
    name: 'Astmatičari',
    icon: '🫁',
    description: 'Osobe sa astmom i respiratory problemima',
    threshold: 50
  }
];

// Preporuke kopirane IDENTIČNO iz backend HealthAdviceService.GetRecommendations()
const HEALTH_RECOMMENDATIONS = {
  'Sportisti': {
    good: "Idealno vrijeme za sve sportske aktivnosti. Uživajte u treningu vani!",
    moderate: "Dobro za većinu aktivnosti. Kraće pauze ako osjećate nelagodu.",
    unhealthy_sensitive: "Ograničite intenzivne treninge. Preferirajte zatvorene prostore.",
    unhealthy: "Izbjegavajte outdoor treninge. Koristite teretane i zatvorene objekte.",
    very_unhealthy: "Sve aktivnosti samo u zatvorenim prostorima s filtracijom zraka.",
    hazardous: "Otkazujte sve outdoor aktivnosti. Ostanite u zatvorenom."
  },
  'Djeca': {
    good: "Sjajno za igru napolju! Djeca mogu da se igraju bez ograničenja.",
    moderate: "Dobro za vanjsku igru. Pazite na signs umora ili kašlja.",
    unhealthy_sensitive: "Ograničite aktivan vrijeme napolju. Kraće šetnje su ok.",
    unhealthy: "Djeca trebaju ostati unutra. Samo kratke izleti vani.",
    very_unhealthy: "Sva djeca moraju ostati u zatvorenim prostorima.",
    hazardous: "Hitno! Djeca ne smiju izlaziti napolju. Zatvorite prozore."
  },
  'Stariji': {
    good: "Excellent za šetnje i outdoor aktivnosti. Uživajte u svježem zraku!",
    moderate: "Dobro za većinu aktivnosti. Izbjegavajte previše naporne radove.",
    unhealthy_sensitive: "Ograničite vrijeme napolju. Kratke šetnje blizu kuće.",
    unhealthy: "Ostanite unutra osim za neophodne izlaske.",
    very_unhealthy: "Važno: ostanite u zatvorenom prostoru s dobrom ventilacijom.",
    hazardous: "Hitno ostanite unutra! Zatvorite prozore i vrata."
  },
  'Astmatičari': {
    good: "Sigurno za normale aktivnosti. Uvijek imajte inhaler pri sebi.",
    moderate: "Oprez: možda ćete osjetiti lakše simptome. Smanjite aktivnost.",
    unhealthy_sensitive: "Visok rizik za astmatičare. Ostanite unutra.",
    unhealthy: "Danger! Ostanite u zatvorenom. Pripremite lijekove.",
    very_unhealthy: "Kriza rizik! Samo emergency izlasci. Kontaktirajte doktora.",
    hazardous: "EMERGENCY! Hitno ostanite unutra! Pozovite doktora ako imate simptome."
  }
};

// Helper funkcije - IDENTIČNE kao u backend HealthAdviceService.cs
export function getAqiCategory(aqi: number): string {
  if (aqi <= 50) return 'good';
  if (aqi <= 100) return 'moderate';
  if (aqi <= 150) return 'unhealthy_sensitive';
  if (aqi <= 200) return 'unhealthy';
  if (aqi <= 300) return 'very_unhealthy';
  return 'hazardous';
}

export function getRiskLevel(aqi: number, groupName: string): 'low' | 'moderate' | 'high' | 'very-high' {
  const group = HEALTH_GROUPS.find(g => g.name === groupName);
  const threshold = group?.threshold || 100;
  
  // Same logic as backend GetRiskLevel method
  if (aqi <= 50) return 'low';
  if (aqi <= 100 && aqi <= threshold) return 'low';
  if (aqi <= 100) return 'moderate';
  if (aqi <= 150) return 'moderate';
  if (aqi <= 200) return 'high';
  return 'very-high';
}

/*
===========================================================================================
                               PUBLIC API - MAIN EXPORTS
===========================================================================================
*/

/// <summary>
/// Main public function za health advice based on AQI
/// Combines base advice sa group-specific recommendations
/// </summary>
// Simplified getHealthAdvice that works with existing structure
export function getHealthAdvice(aqi: number, groupName: string): HealthAdvice {
  return {
    group: groupName,
    riskLevel: getRiskLevel(aqi, groupName),
    recommendation: getRecommendationForGroup(aqi, groupName),
    icon: HEALTH_GROUPS.find(g => g.name === groupName)?.icon || '📊',
    description: HEALTH_GROUPS.find(g => g.name === groupName)?.description || ''
  }
}

// Helper function za recommendations
function getRecommendationForGroup(aqi: number, groupName: string): string {
  const category = getAqiCategory(aqi)
  
  // Bosnian recommendations based on AQI and group
  if (aqi <= 50) {
    return groupName === 'Sportisti' ? 'Odličo vrijeme za sve aktivnosti na otvorenom.' :
           groupName === 'Djeca' ? 'Slobodno vrijeme za igru napolju.' :  
           groupName === 'Stariji' ? 'Preporučuju se šetnje i blaga aktivnost.' :
           'Normalne aktivnosti bez ograničenja.'
  }
  
  if (aqi <= 100) {
    return groupName === 'Sportisti' ? 'Umjeren nivo - smaniti intenzitet treninga.' :
           groupName === 'Djeca' ? 'Ograničiti dugotrajne aktivnosti napolju.' :
           groupName === 'Stariji' ? 'Izbjegavati napornu aktivnost.' :
           'Osjetljive osobe mogu osjetiti probleme.'
  }
  
  if (aqi <= 150) {
    return groupName === 'Sportisti' ? 'Premestiti trening u zatvorene prostore.' :
           groupName === 'Djeca' ? 'Smaniti aktivnosti na otvorenom.' :
           groupName === 'Stariji' ? 'Ostati u zatvorenom prostoru.' :
           'Značajno smanjiti aktivnost napolju.'
  }
  
  return 'Izbegavati sve aktivnosti na otvorenom.'
}

export function getAllHealthAdvice(aqi: number): HealthAdvice[] {
  return HEALTH_GROUPS.map(group => getHealthAdvice(aqi, group.name));
}

// Utility funkcije za UI
export const RISK_COLORS = {
  'low': 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400',
  'moderate': 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400',
  'high': 'bg-orange-100 text-orange-800 dark:bg-orange-900/30 dark:text-orange-400',
  'very-high': 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400',
} as const;

export const RISK_TRANSLATIONS = {
  'low': 'Nizak rizik',
  'moderate': 'Umjeren rizik',
  'high': 'Visok rizik',
  'very-high': 'Vrlo visok rizik',
} as const;