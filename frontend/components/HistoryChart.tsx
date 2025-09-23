'use client'

import React from 'react'
import DailyAqiCard from './DailyAqiCard'

interface HistoryChartProps {
  city?: string
}

export default function HistoryChart({ city = 'Sarajevo' }: HistoryChartProps) {
  return <DailyAqiCard city={city} />
}
