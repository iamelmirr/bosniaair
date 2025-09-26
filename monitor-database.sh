#!/bin/bash

# 📊 SARAJEVO AIR - DATABASE MONITOR
# Prati sve promjene u database-u svakih 10 minuta

echo "🔍 SARAJEVO AIR DATABASE MONITOR"
echo "================================"
echo "Monitoring: $(date)"
echo ""

# Funkcija za clear ekran
clear_screen() {
    clear
    echo "🔍 SARAJEVO AIR DATABASE MONITOR - $(date)"
    echo "================================"
    echo ""
}

# Funkcija za pokazivanje latest measurements
show_live_data() {
    echo "📊 LATEST LIVE MEASUREMENTS (Last 3 records):"
    echo "--------------------------------------------"
    sqlite3 sarajevoair-aqi.db "
    SELECT 
        printf('%s | AQI: %3d | PM2.5: %5.1f | PM10: %5.1f | O3: %5.1f | CO: %4.1f mg/m³', 
            datetime(Timestamp, 'localtime'), 
            AqiValue, Pm25, Pm10, O3, Co
        ) as 'Live Data'
    FROM SarajevoMeasurements 
    ORDER BY Timestamp DESC 
    LIMIT 3;"
    echo ""
}

# Funkcija za pokazivanje forecast
show_forecast_data() {
    echo "🔮 CURRENT FORECAST DATA:"
    echo "------------------------"
    sqlite3 sarajevoair-aqi.db "
    SELECT 
        printf('%s | AQI: %3.0f | PM2.5: %3.0f (min: %3.0f, max: %3.0f)', 
            date(Date), 
            Pm25Avg, Pm25Avg, Pm25Min, Pm25Max
        ) as 'Forecast'
    FROM SarajevoForecasts 
    ORDER BY Date ASC;"
    echo ""
}

# Funkcija za count records
show_stats() {
    echo "📈 DATABASE STATISTICS:"
    echo "----------------------"
    LIVE_COUNT=$(sqlite3 sarajevoair-aqi.db "SELECT COUNT(*) FROM SarajevoMeasurements;")
    FORECAST_COUNT=$(sqlite3 sarajevoair-aqi.db "SELECT COUNT(*) FROM SarajevoForecasts;")
    LAST_UPDATE=$(sqlite3 sarajevoair-aqi.db "SELECT datetime(MAX(Timestamp), 'localtime') FROM SarajevoMeasurements;")
    
    echo "Live measurements: $LIVE_COUNT records"
    echo "Forecast records: $FORECAST_COUNT records"
    echo "Last update: $LAST_UPDATE"
    echo ""
}

# Infinite loop za monitoring
while true; do
    clear_screen
    show_live_data
    show_forecast_data
    show_stats
    
    echo "⏱️  Next refresh in 30 seconds... (Ctrl+C to exit)"
    sleep 30
done