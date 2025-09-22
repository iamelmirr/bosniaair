-- Database initialization script for SarajevoAir
-- This script will run automatically when the PostgreSQL container starts

-- Enable UUID extension for generating UUIDs
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create indexes manually (in addition to those created by EF migrations)
-- These will be created by EF migrations, but this serves as documentation

-- Useful for complex queries and reporting
CREATE INDEX IF NOT EXISTS idx_measurements_timestamp_pollutants 
ON measurements(timestamp_utc DESC) 
WHERE pm25 IS NOT NULL OR pm10 IS NOT NULL;

-- For location-based queries
CREATE INDEX IF NOT EXISTS idx_locations_coordinates 
ON locations(lat, lon) 
WHERE lat IS NOT NULL AND lon IS NOT NULL;

-- For daily aggregates queries
CREATE INDEX IF NOT EXISTS idx_daily_aggregates_date_aqi 
ON daily_aggregates(date DESC, max_aqi DESC NULLS LAST);

-- Insert some sample data (optional - remove in production)
-- This will be populated by the OpenAQ background worker in real usage