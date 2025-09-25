/**
 * TEMA I UPRAVLJANJE PRIKAZOM - NEXT.JS FRONTEND
 * 
 * Ovaj fajl implementira sistem za upravljanje temama (svetla/tamna/sistem) 
 * koji omogućava korisnicima da biraju između različitih vizuelnih režima
 * aplikacije. Koristi React hooks pattern sa localStorage perzistencijom.
 * 
 * KLJUČNE FUNKCIONALNOSTI:
 * • Custom React hook za upravljanje temama
 * • Automatska detekcija sistemskih preferencija
 * • Lokalno čuvanje tema u browser localStorage
 * • Dinamičko ažuriranje HTML root elementa
 * • Meta theme-color ažuriranje za mobile UX
 * 
 * PODRŠKA ZA RAZVOJNE OKRUŽENJA:
 * • Next.js App Router kompatibilnost
 * • SSR/Client-side hydration handling
 * • TypeScript tip safety
 * • React 18 Concurrent Features
 */

'use client'

import { useEffect, useState } from 'react'

/**
 * TYPE DEFINICIJE ZA TEMA SISTEM
 * 
 * Theme tip definiše moguće načine rada tema:
 * • 'light' - Forsirano svetla tema (bele boje, crn tekst)
 * • 'dark' - Forsirano tamna tema (tamne boje, svetao tekst)  
 * • 'system' - Automatski prati sistemske preferencije korisnika
 * 
 * Ovaj pristup omogućava maksimalnu fleksibilnost - korisnici mogu
 * eksplicitno da biraju temu ili da prate sistemske postavke.
 */
type Theme = 'light' | 'dark' | 'system'

/**
 * CENTRALNI HOOK ZA UPRAVLJANJE TEMAMA
 * 
 * useTheme() je glavni React hook koji encapsulira svu logiku
 * za upravljanje temama u aplikaciji. Koristi se u bilo kom
 * komponenti da omogući promenu i praćenje trenutne teme.
 * 
 * REACT STATE ARHITEKTURA:
 * ┌─────────────────────────────────────────────────────────────┐
 * │                    useTheme() Hook                          │
 * │                                                             │
 * │  ┌─────────────┐    ┌──────────────────┐    ┌─────────────┐ │
 * │  │   theme     │    │  resolvedTheme   │    │ localStorage│ │
 * │  │ (preference)│───▶│   (actual)       │───▶│   persist   │ │
 * │  └─────────────┘    └──────────────────┘    └─────────────┘ │
 * │         │                     │                      ▲      │
 * │         │                     ▼                      │      │
 * │         │            ┌──────────────────┐            │      │
 * │         │            │ document.root    │            │      │
 * │         │            │ class updates    │            │      │
 * │         │            └──────────────────┘            │      │
 * │         │                     │                      │      │
 * │         ▼                     ▼                      │      │
 * │  ┌─────────────────────────────────────────────────────────┐ │
 * │  │              Browser Visual Update                      │ │
 * │  │  • CSS class changes (light/dark)                      │ │
 * │  │  • Meta theme-color updates                            │ │
 * │  │  • System media query listening                       │ │
 * │  └─────────────────────────────────────────────────────────┘ │
 * └─────────────────────────────────────────────────────────────┘
 * 
 * RETURN OBJEKTI:
 * • theme: Trenutna preferencija korisnika ('light'|'dark'|'system')
 * • resolvedTheme: Stvarna tema koja se koristi ('light'|'dark')
 * • toggleTheme: Funkcija za prebacivanje između svetle/tamne
 * • setTheme: Funkcija za eksplicitno postavljanje teme
 */
export function useTheme() {
  // STATE ZA KORISNIČKU PREFERENCIJU TEME
  // Default je 'light' umesto 'system' radi konzistentnog UX-a
  const [theme, setTheme] = useState<Theme>('light') // Default to light instead of system
  
  // STATE ZA STVARNU TEMU KOJA SE KORISTI 
  // Ovo je uvek 'light' ili 'dark' - nikad 'system'
  // Kad je theme='system', resolvedTheme se automatski kalkuliše
  const [resolvedTheme, setResolvedTheme] = useState<'light' | 'dark'>('light')

  /**
   * EFFECT ZA INICIJALIZACIJU TEME IZ LOCALSTORAGE
   * 
   * Ovaj useEffect se izvršava samo jednom kada se komponenta mount-uje
   * i učitava sačuvanu preferenciju korisnika. Ako nema sačuvane 
   * preferencije, postavlja se defaultna svetla tema.
   * 
   * HYDRATION STRATEGY:
   * Next.js SSR prvo renderuje sa default vrednošću, a zatim na 
   * client-side hydration učitava pravu preferenciju. Ovo sprečava
   * FOUC (Flash of Unstyled Content) probleme.
   * 
   * BROWSER STORAGE INTEGRATION:
   * • Čita localStorage.getItem('theme')
   * • Validira da je vrednost validna Theme opcija
   * • Postavlja default 'light' ako nema validan cache
   * • Automatski persist-uje default u localStorage
   */
  useEffect(() => {
    // UČITAJ TEMU IZ LOCALSTORAGE ILI KORISTI DEFAULT SVETLU
    const stored = localStorage.getItem('theme') as Theme | null
    if (stored && (stored === 'light' || stored === 'dark')) {
      setTheme(stored)
    } else {
      // Default na svetlu temu za čistiji UX - većina korisnika preferira svetlu
      setTheme('light')
      localStorage.setItem('theme', 'light')
    }
  }, []) // Prazan dependency array - izvršava se samo na mount

  /**
   * EFFECT ZA PRAĆENJE PROMENA TEMA I SISTEMSKIH PREFERENCIJA
   * 
   * Ovaj useEffect se izvršava svaki put kad se promeni theme state
   * i uspostavlja sve potrebne event listenere i DOM ažuriranja.
   * 
   * MEDIA QUERY MONITORING:
   * Prati sistemsku preferenciju korisnika putem CSS Media Query
   * '(prefers-color-scheme: dark)' koji osluškuje OS tema promene.
   * 
   * CONDITIONAL SYSTEM THEME LOGIC:
   * Ako korisnik ima postavku 'system', tema se automatski menja
   * kad korisnik promeni OS temu (npr. iz svetle na tamnu u macOS).
   * 
   * DOM UPDATE PIPELINE:
   * theme state → resolvedTheme state → updateDocument() → Visual change
   */
  useEffect(() => {
    // PRAĆENJE SISTEMSKIH PROMENA TEMA PUTEM MEDIA QUERY
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')
    const handleChange = () => {
      if (theme === 'system') {
        // Samo reaguj na sistemske promene ako je tema postavljena na 'system'
        setResolvedTheme(mediaQuery.matches ? 'dark' : 'light')
        updateDocument(mediaQuery.matches ? 'dark' : 'light')
      }
    }

    // REGISTRUJ EVENT LISTENER ZA SISTEMSKE PROMENE
    mediaQuery.addEventListener('change', handleChange)
    
    // INICIJALNA KONFIGURACIJA TEMA - uvek se izvršava kad se theme promeni
    const isDark = theme === 'dark' || (theme === 'system' && mediaQuery.matches)
    setResolvedTheme(isDark ? 'dark' : 'light')
    updateDocument(isDark ? 'dark' : 'light')

    // CLEANUP: ukloni event listener kad se komponenta unmount-uje ili theme promeni
    return () => mediaQuery.removeEventListener('change', handleChange)
  }, [theme]) // Dependency array sa 'theme' - reaguj na sve promene teme

  /**
   * FUNKCIJA ZA AŽURIRANJE DOM-a SA NOVOM TEMOM
   * 
   * updateDocument() je ključna funkcija koja implementira stvarnu
   * vizuelnu promenu teme u browser-u. Menja CSS klase i meta tagove.
   * 
   * CSS CLASS MANAGEMENT:
   * • Uklanja postojeće 'light'/'dark' klase sa document.documentElement
   * • Dodaje novu klasu prema newTheme parametru
   * • CSS fajlovi koriste ove klase za tema-specifične stilove
   * 
   * MOBILE UX OPTIMIZACIJA:
   * • Ažurira meta[name="theme-color"] za browser UI boju
   * • Tamna tema: #1a1a1a (jako tamna siva)
   * • Svetla tema: #ffffff (čisto bela)
   * • Ovo utiče na status bar boju na mobilnim uređajima
   * 
   * @param newTheme - 'light' ili 'dark' tema za primenu
   */
  const updateDocument = (newTheme: 'light' | 'dark') => {
    // DOBIJ REFERENCU NA ROOT HTML ELEMENT
    const root = document.documentElement
    
    // OČISTI POSTOJEĆE TEMA KLASE DA SPREČI KONFLIKTE
    root.classList.remove('light', 'dark')
    
    // PRIMENI NOVU TEMA KLASU - CSS fajlovi koriste ovu klasu
    root.classList.add(newTheme)
    
    // AŽURIRAJ META THEME-COLOR TAG ZA MOBILE BROWSER UI
    const metaThemeColor = document.querySelector('meta[name="theme-color"]')
    if (metaThemeColor) {
      metaThemeColor.setAttribute(
        'content',
        newTheme === 'dark' ? '#1a1a1a' : '#ffffff'
      )
    }
  }

  /**
   * FUNKCIJA ZA PREBACIVANJE IZMEĐU SVETLE I TAMNE TEME
   * 
   * toggleTheme() implementira jednostavnu logiku za brzo prebacivanje
   * između dve glavne teme. Ne uključuje 'system' opciju jer je to
   * namenjen za eksplicitne korisničke akcije (klik na dugme).
   * 
   * TOGGLE LOGIKA:
   * • Ako je trenutno svetla → prebaci na tamnu
   * • Ako je trenutno tamna → prebaci na svetlu
   * • Koristi resolvedTheme umesto theme da podrži system mode
   * 
   * PERSISTENCE & IMMEDIATE UPDATE:
   * 1. Ažurira React state (setTheme)
   * 2. Cuva u localStorage za buduće sesije
   * 3. Ažurira resolvedTheme za konzistentnost
   * 4. Poziva updateDocument() za trenutnu vizuelnu promenu
   */
  const toggleTheme = () => {
    // JEDNOSTAVNO PREBACIVANJE: samo između svetle i tamne
    const newTheme: Theme = resolvedTheme === 'light' ? 'dark' : 'light'
    
    // AŽURIRAJ REACT STATE
    setTheme(newTheme)
    
    // PERSISITUJ U LOCALSTORAGE ZA BUDUĆE SESIJE
    localStorage.setItem('theme', newTheme)

    // AŽURIRAJ RESOLVED THEME I DOM
    const isDark = newTheme === 'dark'
    setResolvedTheme(isDark ? 'dark' : 'light')
    updateDocument(isDark ? 'dark' : 'light')
  }

  /**
   * FUNKCIJA ZA EKSPLICITNO POSTAVLJANJE TEME
   * 
   * setThemeMode() omogućava precizno postavljanje bilo koje teme
   * uključujući 'system' opciju. Koristi se kada korisnik eksplicitno
   * bira temu iz dropdown menija ili settings panela.
   * 
   * SYSTEM THEME RESOLUTION:
   * Kada se postavlja 'system' tema, funkcija odmah proverava
   * trenutnu sistemsku preferenciju putem matchMedia API-ja
   * i kalkuliše odgovarajuću resolvedTheme vrednost.
   * 
   * COMPREHENSIVE UPDATE PIPELINE:
   * 1. Ažurira theme state
   * 2. Persisituje u localStorage
   * 3. Kalkuliše isDark bazično na newTheme i sistemskim preferencijama
   * 4. Ažurira resolvedTheme
   * 5. Poziva updateDocument() za vizuelnu promenu
   * 
   * @param newTheme - Nova tema: 'light' | 'dark' | 'system'
   */
  const setThemeMode = (newTheme: Theme) => {
    // AŽURIRAJ REACT STATE SA NOVOM TEMOM
    setTheme(newTheme)
    
    // PERSISITUJ PREFERENCIJU U LOCALSTORAGE
    localStorage.setItem('theme', newTheme)

    // KALKULIŠI DA LI TREBA TAMNA TEMA
    // Za 'system' proveri sistemske preferencije, za ostale koristi direktno
    const isDark = newTheme === 'dark' || (newTheme === 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches)
    
    // AŽURIRAJ RESOLVED THEME I PRIMENI VIZUELNE PROMENE
    setResolvedTheme(isDark ? 'dark' : 'light')
    updateDocument(isDark ? 'dark' : 'light')
  }

  /**
   * HOOK RETURN OBJEKAT
   * 
   * Vraća sve potrebne state-ove i funkcije za rad sa temama.
   * Ovaj objekat se destructure-uje u komponentama koje koriste hook.
   * 
   * PRIMER KORIŠĆENJA U KOMPONENTI:
   * ```typescript
   * const { theme, resolvedTheme, toggleTheme, setTheme } = useTheme()
   * 
   * return (
   *   <button onClick={toggleTheme}>
   *     Trenutna tema: {resolvedTheme}
   *   </button>
   * )
   * ```
   */
  return {
    // TRENUTNA KORISNIČKA PREFERENCIJA ('light'|'dark'|'system')
    theme,
    
    // STVARNA TEMA KOJA SE KORISTI ('light'|'dark')
    resolvedTheme,
    
    // FUNKCIJA ZA BRZO PREBACIVANJE IZMEĐU SVETLE/TAMNE
    toggleTheme,
    
    // FUNKCIJA ZA PRECIZNO POSTAVLJANJE BILO KOJE TEME
    setTheme: setThemeMode,
  }
}

/**
 * HELPER FUNKCIJA ZA TEMA IKONE
 * 
 * getThemeIcon() je utility funkcija koja vraća odgovarajuće emoji
 * ikone za različite tema režime. Koristi se u UI komponentama
 * za vizuelno predstavljanje trenutne teme.
 * 
 * ICON MAPPING LOGIC:
 * • 'light' tema → ☀️ (sunce emoji)
 * • 'dark' tema → 🌙 (mesec emoji) 
 * • 'system' tema → dinamička ikona bazirana na resolvedTheme
 * • default/fallback → 🌓 (polu mesec - reprezentuje promenu)
 * 
 * SMART SYSTEM ICON:
 * Za 'system' temu, ikona se dinamički menja u zavisnosti od
 * trenutno resolvovane teme, što daje korisnicima vizuelni 
 * indikator o tome koju temu sistem trenutno koristi.
 * 
 * @param theme - Korisnička preferencija teme
 * @param resolvedTheme - Stvarna tema koja se koristi
 * @returns Emoji string za prikazivanje u UI-ju
 * 
 * PRIMER KORIŠĆENJA:
 * ```typescript
 * const { theme, resolvedTheme } = useTheme()
 * const icon = getThemeIcon(theme, resolvedTheme)
 * 
 * return <button>{icon} Promeni temu</button>
 * ```
 */
export function getThemeIcon(theme: Theme, resolvedTheme: 'light' | 'dark') {
  switch (theme) {
    case 'light':
      // EKSPLICITNA SVETLA TEMA - uvek sunce
      return '☀️'
    case 'dark':
      // EKSPLICITNA TAMNA TEMA - uvek mesec
      return '🌙'
    case 'system':
      // SISTEMSKA TEMA - prikaži ikonu na osnovu trenutno resolvovane teme
      return resolvedTheme === 'dark' ? '🌙' : '☀️'
    default:
      // FALLBACK ZA NEOČEKIVANE VREDNOSTI
      return '🌓'
  }
}