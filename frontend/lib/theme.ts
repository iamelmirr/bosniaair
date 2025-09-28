'use client'

import { useEffect, useState } from 'react'
type Theme = 'light' | 'dark' | 'system'
export function useTheme() {
  const [theme, setTheme] = useState<Theme>('light')
  const [resolvedTheme, setResolvedTheme] = useState<'light' | 'dark'>('light')

  useEffect(() => {
    const stored = localStorage.getItem('theme') as Theme | null
    if (stored && (stored === 'light' || stored === 'dark')) {
      setTheme(stored)
    } else {
      setTheme('light')
      localStorage.setItem('theme', 'light')
    }
  }, [])

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

  const toggleTheme = () => {
    const newTheme: Theme = resolvedTheme === 'light' ? 'dark' : 'light'
    setTheme(newTheme)
    localStorage.setItem('theme', newTheme)
    const isDark = newTheme === 'dark'
    setResolvedTheme(isDark ? 'dark' : 'light')
    updateDocument(isDark ? 'dark' : 'light')
  }

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