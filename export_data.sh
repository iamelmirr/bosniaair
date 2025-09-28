#!/bin/bash

# Export SQLite data to CSV for PostgreSQL import
DATABASE_PATH="/Users/elmirbesirovic/Desktop/projects/sarajevoairvibe/backend/src/BosniaAir.Api/bosniaair-aqi.db"
OUTPUT_FILE="/Users/elmirbesirovic/Desktop/projects/sarajevoairvibe/data_export.csv"

echo "Exporting data from SQLite to CSV..."

sqlite3 "$DATABASE_PATH" <<EOF
.mode csv
.headers on
.output "$OUTPUT_FILE"
SELECT * FROM AirQualityRecords;
.quit
EOF

echo "Export completed! File saved as: $OUTPUT_FILE"
echo "You can now import this data into PostgreSQL using:"
echo "COPY AirQualityRecords FROM '$OUTPUT_FILE' WITH CSV HEADER;"