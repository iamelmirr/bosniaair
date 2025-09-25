#!/bin/bash

# 📊 SARAJEVOAIR DATABASE CHECKER SCRIPT
# Koristi ovaj script za brzu proveru stanja baze podataka

DB_PATH="/Users/elmirbesirovic/Desktop/projects/sarajevoairvibe/backend/src/SarajevoAir.Api/sarajevoair-aqi.db"

echo "🏛️  SarajevoAir Database Status"
echo "================================"

# 1. Osnovne statistike
echo "📈 OSNOVNE STATISTIKE:"
sqlite3 "$DB_PATH" "
SELECT 
    '📊 Ukupno zapisa: ' || COUNT(*) as info
FROM SimpleAqiRecords

UNION ALL

SELECT 
    '🏙️  Broj gradova: ' || COUNT(DISTINCT City)
FROM SimpleAqiRecords

UNION ALL

SELECT 
    '📅 Najstariji zapis: ' || MIN(datetime(Timestamp))
FROM SimpleAqiRecords

UNION ALL

SELECT 
    '⏰ Najnoviji zapis: ' || MAX(datetime(Timestamp))
FROM SimpleAqiRecords;
"

echo ""
echo "🌆 STATISTIKE PO GRADOVIMA:"
sqlite3 "$DB_PATH" "
SELECT 
    '🏙️  ' || City || ': ' || COUNT(*) || ' zapisa (AQI: ' || MIN(AqiValue) || '-' || MAX(AqiValue) || ', avg: ' || ROUND(AVG(AqiValue), 1) || ')'
FROM SimpleAqiRecords 
GROUP BY City;
"

echo ""
echo "⏰ POSLEDNJI ZAPISI (10):"
sqlite3 "$DB_PATH" "
SELECT 
    '🕐 ID:' || Id || ' | ' || City || ' | AQI:' || AqiValue || ' | ' || datetime(Timestamp)
FROM SimpleAqiRecords 
ORDER BY Id DESC 
LIMIT 10;
"

echo ""
echo "🔍 RECENT ACTIVITY (poslednjih 30min):"
COUNT=$(sqlite3 "$DB_PATH" "SELECT COUNT(*) FROM SimpleAqiRecords WHERE datetime(Timestamp) > datetime('now', '-30 minutes');")
echo "📈 Zapisi u poslednjih 30min: $COUNT"

if [ $COUNT -gt 0 ]; then
    sqlite3 "$DB_PATH" "
    SELECT 
        '⚡ ' || datetime(Timestamp) || ' | ' || City || ' | AQI:' || AqiValue
    FROM SimpleAqiRecords 
    WHERE datetime(Timestamp) > datetime('now', '-30 minutes')
    ORDER BY Timestamp DESC
    LIMIT 5;
    "
fi

echo ""
echo "💡 KORISNE KOMANDE:"
echo "🔧 Za direktan pristup bazi:"
echo "   cd /Users/elmirbesirovic/Desktop/projects/sarajevoairvibe/backend/src/SarajevoAir.Api"
echo "   sqlite3 sarajevoair-aqi.db"
echo ""
echo "🗂️  Osnovne SQL komande:"
echo "   .tables                          # Lista tabela"
echo "   .schema SimpleAqiRecords         # Struktura tabele"
echo "   SELECT * FROM SimpleAqiRecords LIMIT 5;  # Primi 5 zapisa"
echo "   .quit                           # Izlaz"
