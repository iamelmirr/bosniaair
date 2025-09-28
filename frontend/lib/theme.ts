/**
 * Theme management utilities for the Sarajevo Air Quality application.
 * Provides a custom hook for managing light/dark/system theme preferences
 * with automatic persistence to localStorage and system preference detection.
 */

'use client'

import { useEffect, useState } from 'react'

/**
 * Available theme options for the application.
 */
type Theme = 'light' | 'dark' | 'system'

/**
 * Custom hook for managing application theme state.
 * Handles theme persistence, system preference detection, and DOM updates.
 *
 * @returns Object containing theme state and control functions
 */
export function useTheme() {
  const [theme, setTheme] = useState<Theme>('light')
  const [resolvedTheme, setResolvedTheme] = useState<'light' | 'dark'>('light')

  // Load saved theme from localStorage on mount
  useEffect(() => {
    const stored = localStorage.getItem('theme') as Theme | null
    if (stored && (stored === 'light' || stored === 'dark')) {
      setTheme(stored)
    } else {
      setTheme('light')
      localStorage.setItem('theme', 'light')
    }
  }, [])

  // Handle system theme changes and apply theme to document
  useEffect(() => {
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')
    const handleChange = () => {
      if (theme === 'system') {
        setResolvedTheme(mediaQuery.matches ? 'dark' : 'light')
        updateDocument(mediaQuery.matches ? 'dark' : 'light')
      }
    }
    mediaQuery.addEventListener('change', handleChange)
    const isDark = theme === 'dark' || (theme === 'system' && mediaQuery.matches)
    setResolvedTheme(isDark ? 'dark' : 'light')
    updateDocument(isDark ? 'dark' : 'light')
    return () => mediaQuery.removeEventListener('change', handleChange)
  }, [theme])

  /**
   * Updates the document's theme classes and meta theme color.
   * @param newTheme - The theme to apply ('light' or 'dark')
   */
  const updateDocument = (newTheme: 'light' | 'dark') => {
    const root = document.documentElement
    root.classList.remove('light', 'dark')
    root.classList.add(newTheme)
    const metaThemeColor = document.querySelector('meta[name="theme-color"]')
    if (metaThemeColor) {
      metaThemeColor.setAttribute(
        'content',
        newTheme === 'dark' ? '#1a1a1a' : '#ffffff'
      )
    }
  }

  /**
   * Toggles between light and dark themes.
   * Switches from current resolved theme to the opposite.
   */
  const toggleTheme = () => {
    const newTheme: Theme = resolvedTheme === 'light' ? 'dark' : 'light'
    setTheme(newTheme)
    localStorage.setItem('theme', newTheme)
    const isDark = newTheme === 'dark'
    setResolvedTheme(isDark ? 'dark' : 'light')
    updateDocument(isDark ? 'dark' : 'light')
  }

  /**
   * Sets the theme mode explicitly.
   * @param newTheme - The theme to set ('light', 'dark', or 'system')
   */
  const setThemeMode = (newTheme: Theme) => {
    setTheme(newTheme)
    localStorage.setItem('theme', newTheme)
    const isDark = newTheme === 'dark' || (newTheme === 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches)
    setResolvedTheme(isDark ? 'dark' : 'light')
    updateDocument(isDark ? 'dark' : 'light')
  }

  return {
    theme,
    resolvedTheme,
    toggleTheme,
    setTheme: setThemeMode,
  }
}

/**
 * Returns an appropriate emoji icon for the current theme state.
 * @param theme - The selected theme mode
 * @param resolvedTheme - The actual resolved theme (light or dark)
 * @returns Emoji representing the theme
 */
export function getThemeIcon(theme: Theme, resolvedTheme: 'light' | 'dark') {
  switch (theme) {
    case 'light':
      return '☀️'
    case 'dark':
      return '🌙'
    case 'system':
      return resolvedTheme === 'dark' ? '🌙' : '☀️'
    default:
      return '🌓'
  }
}