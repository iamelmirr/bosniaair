#!/bin/bash

# SarajevoAir Test Script
# Ovaj script testira da li se frontend i backend sinhronizuju kako treba

echo "🧪 ===== SARAJEVO AIR SYSTEM TEST ====="
echo

# 1. Proverava servise
echo "1️⃣  PROVERA SERVISA:"
echo "   Backend (port 5001):"
if lsof -i :5001 > /dev/null 2>&1; then
    echo "   ✅ Backend radi na portu 5001"
else
    echo "   ❌ Backend NE RADI na portu 5001"
    exit 1
fi

echo "   Frontend (port 3000):"
if lsof -i :3000 > /dev/null 2>&1; then
    echo "   ✅ Frontend radi na portu 3000"
else
    echo "   ⚠️  Frontend ne radi (pokreni: cd frontend && npm run dev)"
fi
echo

# 2. Test API poziva
echo "2️⃣  TEST API POZIVA:"
API_RESPONSE=$(curl -s "http://localhost:5001/api/v1/live?city=Sarajevo")
if [ $? -eq 0 ]; then
    AQI_VALUE=$(echo "$API_RESPONSE" | jq -r '.overallAqi // "ERROR"')
    TIMESTAMP=$(echo "$API_RESPONSE" | jq -r '.timestamp // "ERROR"')
    echo "   ✅ API poziv uspešan"
    echo "   📊 AQI: $AQI_VALUE"
    echo "   ⏰ Vreme: $TIMESTAMP"
else
    echo "   ❌ API poziv neuspešan"
    exit 1
fi
echo

# 3. Provera baze podataka
echo "3️⃣  PROVERA BAZE PODATAKA:"
cd "$(dirname "$0")/backend/src/SarajevoAir.Api" || exit 1

if [ -f "sarajevoair-aqi.db" ]; then
    echo "   ✅ SQLite baza postoji"
    
    # Ukupan broj zapisa
    TOTAL_RECORDS=$(sqlite3 sarajevoair-aqi.db "SELECT COUNT(*) FROM SimpleAqiRecords;")
    echo "   📈 Ukupno zapisa: $TOTAL_RECORDS"
    
    # Poslednji zapis
    LAST_RECORD=$(sqlite3 sarajevoair-aqi.db "SELECT Timestamp, AqiValue FROM SimpleAqiRecords ORDER BY Timestamp DESC LIMIT 1;")
    echo "   📝 Poslednji zapis: $LAST_RECORD"
    
    # Zapisi iz poslednjih 10 minuta
    RECENT_RECORDS=$(sqlite3 sarajevoair-aqi.db "SELECT COUNT(*) FROM SimpleAqiRecords WHERE Timestamp > datetime('now', '-10 minutes');")
    echo "   🕐 Zapisi iz poslednjih 10 min: $RECENT_RECORDS"
else
    echo "   ❌ SQLite baza ne postoji"
    exit 1
fi
echo

# 4. Test sinhronizacije
echo "4️⃣  TEST SINHRONIZACIJE:"
echo "   🔄 Pravim novi API poziv..."

# Čeka malo i pravi novi poziv
sleep 2
NEW_API_RESPONSE=$(curl -s "http://localhost:5001/api/v1/live?city=Sarajevo")
NEW_AQI=$(echo "$NEW_API_RESPONSE" | jq -r '.overallAqi')

# Provera da li je dodat novi zapis (ili je preskočen zbog duplikata)
NEW_TOTAL=$(sqlite3 sarajevoair-aqi.db "SELECT COUNT(*) FROM SimpleAqiRecords;")

if [ "$NEW_TOTAL" -gt "$TOTAL_RECORDS" ]; then
    echo "   ✅ Novi zapis dodat u bazu (sinhronizacija radi)"
elif [ "$NEW_TOTAL" -eq "$TOTAL_RECORDS" ]; then
    echo "   ⚠️  Novi zapis nije dodat (verovatno zbog anti-duplicate logike)"
    echo "   ℹ️  Ovo je normalno ako je prethodnji poziv bio u poslednjih 5 minuta"
else
    echo "   ❌ Problem sa bazom podataka"
fi

echo "   📊 Trenutni AQI: $NEW_AQI"
echo

# 5. Frontend test
echo "5️⃣  FRONTEND TEST:"
if lsof -i :3000 > /dev/null 2>&1; then
    echo "   ✅ Frontend dostupan na: http://localhost:3000"
    echo "   🌐 SWR konfiguracija: refresh svakih 10 minuta"
else
    echo "   ❌ Frontend nije dostupan"
fi
echo

echo "🎉 ===== TEST ZAVRŠEN ====="
echo "💡 Za real-time monitoring:"
echo "   - Baza: sqlite3 backend/src/SarajevoAir.Api/sarajevoair-aqi.db"
echo "   - Logovi: tail -f backend.log"
echo "   - Frontend: http://localhost:3000"