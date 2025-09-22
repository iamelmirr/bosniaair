import './globals.css'
import { Inter } from 'next/font/google'
import type { Metadata } from 'next'

const inter = Inter({ 
  subsets: ['latin'],
  display: 'swap',
  variable: '--font-inter'
})

export const metadata: Metadata = {
  metadataBase: new URL(process.env.NEXT_PUBLIC_BASE_URL || 'http://localhost:3000'),
  title: {
    default: 'SarajevoAir - Real-time Air Quality Monitoring',
    template: '%s | SarajevoAir',
  },
  description: 'Real-time air quality monitoring for Sarajevo and Bosnia & Herzegovina. Track AQI, pollutants, and get health recommendations.',
  keywords: [
    'sarajevo',
    'air quality',
    'AQI',
    'pollution',
    'bosnia',
    'environment',
    'health',
    'monitoring',
    'real-time',
  ],
  authors: [{ name: 'SarajevoAir Team' }],
  creator: 'SarajevoAir',
  publisher: 'SarajevoAir',
  formatDetection: {
    email: false,
    address: false,
    telephone: false,
  },
  openGraph: {
    type: 'website',
    locale: 'bs_BA',
    url: '/',
    siteName: 'SarajevoAir',
    title: 'SarajevoAir - Real-time Air Quality Monitoring',
    description: 'Real-time air quality monitoring for Sarajevo and Bosnia & Herzegovina.',
    images: [
      {
        url: '/og-image.png',
        width: 1200,
        height: 630,
        alt: 'SarajevoAir - Air Quality Dashboard',
      },
    ],
  },
  twitter: {
    card: 'summary_large_image',
    title: 'SarajevoAir - Real-time Air Quality Monitoring',
    description: 'Track real-time air quality in Sarajevo and Bosnia & Herzegovina.',
    images: ['/og-image.png'],
  },
  icons: {
    icon: '/favicon.ico',
    shortcut: '/favicon-16x16.png',
    apple: '/apple-touch-icon.png',
  },
  manifest: '/manifest.json',
  alternates: {
    canonical: '/',
  },
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en" suppressHydrationWarning>
      <head>
        <link rel="icon" href="/favicon.ico" />
        <link rel="apple-touch-icon" sizes="180x180" href="/apple-touch-icon.png" />
        <link rel="icon" type="image/png" sizes="32x32" href="/favicon-32x32.png" />
        <link rel="icon" type="image/png" sizes="16x16" href="/favicon-16x16.png" />
        <link rel="manifest" href="/site.webmanifest" />
      </head>
      <body className={`${inter.variable} font-sans antialiased`}>
        <div className="min-h-screen bg-[rgb(var(--bg))] text-[rgb(var(--text))] transition-colors duration-300">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            {children}
          </div>
        </div>
        <div id="modal-root" />
      </body>
    </html>
  )
}