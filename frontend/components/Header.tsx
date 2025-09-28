'use client'

import { useState } from 'react'
import { useTheme, getThemeIcon } from '../lib/theme'

interface HeaderProps {
  onOpenCitySettings?: () => void
  selectedCityLabel?: string
}

export default function Header({ onOpenCitySettings, selectedCityLabel }: HeaderProps) {
  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const { theme, resolvedTheme, toggleTheme } = useTheme()
  const cityLabel = selectedCityLabel ?? 'Sarajevo'



  return (
    <header className="sticky top-0 bg-[rgb(var(--bg))] z-50 py-4 mb-8 border-b border-[rgb(var(--border))] backdrop-blur-sm bg-opacity-90">
      <div className="flex items-center justify-between">
        {/* Logo and Title */}
        <div className="flex items-center space-x-4">
          <div className="flex items-center space-x-2">
            <div className="w-8 h-8 bg-gradient-to-br from-blue-500 to-green-500 rounded-lg flex items-center justify-center">
              <span className="text-white font-bold text-sm">SA</span>
            </div>
            <h1 className="text-2xl font-bold text-[rgb(var(--text))]">
              SarajevoAir
            </h1>
          </div>
          <span className="hidden md:inline-block text-sm px-2 py-1 bg-blue-100 dark:bg-blue-900 text-blue-600 dark:text-blue-400 rounded-full">
            v1.0
          </span>
        </div>
        
        {/* Desktop Navigation */}
        <div className="hidden md:flex items-center space-x-4">
          {/* Theme Toggle */}
          <button 
            onClick={toggleTheme}
            className="p-2 rounded-lg bg-[rgb(var(--card))] border border-[rgb(var(--border))] hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors group"
            title={`Trenutna tema: ${theme} (klikni za promenu)`}
          >
            <div className="w-5 h-5 relative">
              {/* Sun icon (visible in light mode) */}
              <svg 
                className={`absolute inset-0 w-5 h-5 text-yellow-500 transition-opacity group-hover:scale-110 transform duration-200 ${
                  resolvedTheme === 'dark' ? 'opacity-0' : 'opacity-100'
                }`}
                fill="currentColor" 
                viewBox="0 0 20 20"
              >
                <path fillRule="evenodd" d="M10 2a1 1 0 011 1v1a1 1 0 11-2 0V3a1 1 0 011-1zm4 8a4 4 0 11-8 0 4 4 0 018 0zm-.464 4.95l.707.707a1 1 0 001.414-1.414l-.707-.707a1 1 0 00-1.414 1.414zm2.12-10.607a1 1 0 010 1.414l-.706.707a1 1 0 11-1.414-1.414l.707-.707a1 1 0 011.414 0zM17 11a1 1 0 100-2h-1a1 1 0 100 2h1zm-7 4a1 1 0 011 1v1a1 1 0 11-2 0v-1a1 1 0 011-1zM5.05 6.464A1 1 0 106.465 5.05l-.708-.707a1 1 0 00-1.414 1.414l.707.707zm1.414 8.486l-.707.707a1 1 0 01-1.414-1.414l.707-.707a1 1 0 011.414 1.414zM4 11a1 1 0 100-2H3a1 1 0 000 2h1z" clipRule="evenodd" />
              </svg>
              
              {/* Moon icon (visible in dark mode) */}
              <svg 
                className={`absolute inset-0 w-5 h-5 text-gray-300 transition-opacity group-hover:scale-110 transform duration-200 ${
                  resolvedTheme === 'dark' ? 'opacity-100' : 'opacity-0'
                }`}
                fill="currentColor" 
                viewBox="0 0 20 20"
              >
                <path d="M17.293 13.293A8 8 0 016.707 2.707a8.001 8.001 0 1010.586 10.586z" />
              </svg>
            </div>
          </button>

          {/* City preferences */}
          <button
            onClick={() => onOpenCitySettings?.()}
            className="px-4 py-2 border border-[rgb(var(--border))] rounded-lg bg-[rgb(var(--card))] text-[rgb(var(--text))] hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors flex items-center gap-2"
            title="Odaberi glavne gradove"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 7h18M3 12h18M3 17h18" />
            </svg>
            <span>{cityLabel}</span>
          </button>


        </div>

        {/* Mobile Menu Button */}
        <div className="md:hidden">
          <button
            onClick={() => setIsMenuOpen(!isMenuOpen)}
            className="p-2 rounded-lg bg-[rgb(var(--card))] border border-[rgb(var(--border))] hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
          >
            <svg className="w-5 h-5 text-[rgb(var(--text))]" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              {isMenuOpen ? (
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              ) : (
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
              )}
            </svg>
          </button>
        </div>
      </div>

      {/* Mobile Menu */}
      {isMenuOpen && (
        <div className="md:hidden mt-4 pt-4 border-t border-[rgb(var(--border))] space-y-4">
          {/* Mobile Action Buttons */}
          <div className="flex gap-2">
            <button 
              onClick={() => {
                toggleTheme()
                setIsMenuOpen(false)
              }}
              className="flex-1 px-4 py-2 border border-[rgb(var(--border))] rounded-lg bg-[rgb(var(--card))] text-[rgb(var(--text))] hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors text-center"
            >
              {getThemeIcon(theme, resolvedTheme)} Tema
            </button>
            
            <button 
              onClick={() => {
                onOpenCitySettings?.()
                setIsMenuOpen(false)
              }}
              className="flex-1 px-4 py-2 border border-[rgb(var(--border))] rounded-lg bg-[rgb(var(--card))] text-[rgb(var(--text))] hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors text-center"
            >
              Gradovi
            </button>


          </div>
        </div>
      )}
    </header>
  )
}