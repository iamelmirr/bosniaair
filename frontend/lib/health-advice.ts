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

export const HEALTH_GROUPS: HealthGroup[] = [
  {
    name: 'Astmatičari',
    icon: '🫁',
    description: 'Osobe sa astmom i respiratornim problemima',
    threshold: 35
  },
  {
    name: 'Sportisti', 
    icon: '🏃‍♂️',
    description: 'Aktivni sportisti i rekreativci',
    threshold: 55
  },
  {
    name: 'Djeca',
    icon: '👶', 
    description: 'Djeca i mladi do 18 godina',
    threshold: 65
  },
  {
    name: 'Stariji',
    icon: '👴',
    description: 'Odrasli stariji od 65 godina', 
    threshold: 70
  }
];
export function getAqiCategory(aqi: number): string {
  if (aqi <= 30) return 'excellent';
  if (aqi <= 50) return 'good';
  if (aqi <= 100) return 'moderate';
  if (aqi <= 150) return 'unhealthy_sensitive';
  if (aqi <= 200) return 'unhealthy';
  if (aqi <= 300) return 'very_unhealthy';
  return 'hazardous';
}

export function getAqiColorClass(aqi: number): string {
  const category = getAqiCategory(aqi) as keyof typeof AQI_COLORS;
  return AQI_COLORS[category] || AQI_COLORS.good;
}

export function getRiskLevel(aqi: number, groupName: string): 'low' | 'moderate' | 'high' | 'very-high' {
  const group = HEALTH_GROUPS.find(g => g.name === groupName);
  const threshold = group?.threshold || 70;

  if (aqi <= 30) return 'low';
  if (aqi <= threshold) return 'low';
  if (aqi <= 50) {
    return groupName === 'Astmatičari' ? 'moderate' : 'low';
  }
  if (aqi <= 100) return 'moderate';
  if (aqi <= 150) return 'high';
  if (aqi <= 200) return 'very-high';
  return 'very-high';
}
 
function getHealthIcon(aqi: number, groupName: string): string {
  const baseGroup = HEALTH_GROUPS.find(g => g.name === groupName);
  return baseGroup?.icon || '📊';
}
export function getHealthAdvice(aqi: number, groupName: string): HealthAdvice {
  return {
    group: groupName,
    riskLevel: getRiskLevel(aqi, groupName),
    recommendation: getRecommendationForGroup(aqi, groupName),
    icon: getHealthIcon(aqi, groupName),
    description: HEALTH_GROUPS.find(g => g.name === groupName)?.description || ''
  }
}

function getRecommendationForGroup(aqi: number, groupName: string): string {
  const group = HEALTH_GROUPS.find(g => g.name === groupName);
  const threshold = group?.threshold || 70;

  if (aqi <= 30) {
    switch (groupName) {
      case 'Astmatičari': return '🌟 Izvrsno! Sve aktivnosti na otvorenom su sigurne. Uvijek nosite inhaler.';
      case 'Sportisti': return '🏃‍♂️ Savršeno za intenzivne treninge! Iskoristite ovo vrijeme za najbolje rezultate.';
      case 'Djeca': return '👶 Fantastično! Neograničeno vrijeme za igru i sport napolju.';
      case 'Stariji': return '👴 Odličo za sve aktivnosti - šetnje, rad u bašti, sport.';
      default: return 'Savršen kvalitet zraka za sve aktivnosti!';
    }
  }
  
  if (aqi <= 50) {
    switch (groupName) {
      case 'Astmatičari': 
        return aqi > threshold 
          ? '⚠️ PAŽNJA! Možete osjetiti blage simptome. Skratite boravak napolju i pripremite lijekove.'
          : '😊 Dobro za većinu aktivnosti. Pazite na simptome i imajte inhaler pri sebi.';
      case 'Sportisti': 
        return aqi > threshold
          ? '🏃‍♂️ OPREZ sportisti! Smanjite intenzitet treninga za 20-30% ili prebacite u zatvoreni prostor.'
          : '💪 Odlično za treninge! Možda osjetite blažu zamorenost ranije nego obično.';
      case 'Djeca': 
        return aqi > threshold
          ? '👶 Pazite na djecu! Kratke aktivnosti vani su ok, dugotrajne igre ograničiti.'
          : '🎯 Dobro za igru napolju. Pazite na znakove umora ili kašlja.';
      case 'Stariji': 
        return aqi > threshold
          ? '👴 Umjereno. Kratke šetnje su ok, izbjegavajte naporne poslove u bašti.'
          : '🚶‍♂️ Dobro za šetnje i blagu aktivnost. Pazite kako se osjećate.';
      default: return 'Prihvatljivo za većinu ljudi.';
    }
  }

  if (aqi <= 100) {
    switch (groupName) {
      case 'Astmatičari': return '⚠️ Umjeren rizik za astmatičare. Skratite boravak napolju i pripremite inhaler.';
      case 'Sportisti': return '🏋️‍♂️ Prebacite treninge u teretanu ili smanjite intenzitet za 50%. Više pauza za odmor.';
      case 'Djeca': return '👶 Ograničiti vanjsku igru. Kratke šetnje ok, dugotrajni sport izbjegavati.';
      case 'Stariji': return '👴 Kratke aktivnosti napolju. Izbjegavajte naporne radove i dugotrajno izlaganje.';
      default: return 'Osjetljive grupe trebaju paziti. Zdravi odrasli mogu normalne aktivnosti.';
    }
  }

  if (aqi <= 150) {
    switch (groupName) {
      case 'Astmatičari': return '🚨 Visok rizik za astmatičare! Ostanite unutra. Pri simptomima kontaktirajte ljekara.';
      case 'Sportisti': return '🏠 Visok rizik! Sve treninge prebaciti u zatvorene prostore.';
      case 'Djeca': return '🏠 Visok rizik za djecu! Moraju ostati unutra osim hitnih izlazaka.';
      case 'Stariji': return '🏠 Visok rizik! Ostanite u zatvorenom. Zatvorite prozore i koristite čišća zraka.';
      default: return 'Visok rizik za sve osjetljive grupe. Ograničiti izlaganje na otvorenom.';
    }
  }
  if (aqi <= 200) {
    return '🚨 HITNO! Svi moraju ostati unutra. Zatvorite prozore, koristite čišće zraka ako imate.';
  }
  return '☢️ ZDRAVSTVENA HITNOST! Ne izlazite osim u krajnjoj nuždi. Kontaktirajte zdravstvene službe pri simptomima.';
}

export function getAllHealthAdvice(aqi: number): HealthAdvice[] {
  return HEALTH_GROUPS.map(group => getHealthAdvice(aqi, group.name));
}
export const RISK_COLORS = {
  'low': 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-400 border-emerald-200 dark:border-emerald-700',
  'moderate': 'bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-400 border-amber-200 dark:border-amber-700',
  'high': 'bg-orange-100 text-orange-800 dark:bg-orange-900/30 dark:text-orange-400 border-orange-200 dark:border-orange-700',
  'very-high': 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400 border-red-200 dark:border-red-700',
} as const;

export const RISK_TRANSLATIONS = {
  'low': 'Nizak rizik',
  'moderate': 'Umjeren rizik', 
  'high': 'Visok rizik',
  'very-high': 'Vrlo visok rizik',
} as const;

export const AQI_COLORS = {
  excellent: 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400',
  good: 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400',
  moderate: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400',
  unhealthy_sensitive: 'bg-orange-100 text-orange-800 dark:bg-orange-900/30 dark:text-orange-400',
  unhealthy: 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400',
  very_unhealthy: 'bg-purple-100 text-purple-800 dark:bg-purple-900/30 dark:text-purple-400',
  hazardous: 'bg-gray-900 text-white dark:bg-gray-100 dark:text-gray-900'
} as const;