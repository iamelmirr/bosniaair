# API Dokumentacija

Ovaj dokument opisuje ključne endpoint-e dostupne putem BosniaAir API-ja.

**Osnovni URL:** `http://localhost:5000/api/v1`

---

## Endpoints

### 1. Dohvati kompletne AQI podatke

Dohvaća i live i forecast podatke za određeni grad u jednom pozivu. Ovo je glavni endpoint koji koristi frontend aplikacija.

- **URL:** `/air-quality/{city}/complete`
- **Metoda:** `GET`
- **Parametri:**
  - `city` (string, obavezno): Identifikator grada (npr. `Sarajevo`, `Tuzla`, `Zenica`).
- **Uspješan odgovor (200 OK):**
  ```json
  {
    "liveData": {
      "city": "Sarajevo",
      "overallAqi": 55,
      "aqiCategory": "Moderate",
      "color": "#EAB308",
      "healthMessage": "Prihvatljivo za većinu ljudi...",
      "timestamp": "2025-09-28T21:00:00Z",
      "measurements": [
        {
          "parameter": "pm25",
          "value": 55,
          "unit": "µg/m³",
          "timestamp": "2025-09-28T21:00:00Z"
        }
      ],
      "dominantPollutant": "pm25"
    },
    "forecastData": {
      "city": "Sarajevo",
      "forecast": [
        {
          "date": "2025-09-29",
          "aqi": 60,
          "dayName": "Ponedjeljak"
        }
      ]
    },
    "retrievedAt": "2025-09-28T21:45:00Z"
  }
  ```
- **Greška (404 Not Found):** Ako podaci za traženi grad nisu dostupni.

### 2. Dohvati live AQI podatke

Dohvaća samo podatke o kvaliteti zraka u stvarnom vremenu.

- **URL:** `/air-quality/{city}/live`
- **Metoda:** `GET`
- **Uspješan odgovor (200 OK):** Vraća `liveData` objekt kao što je prikazano gore.

### 3. Dohvati prognozu

Dohvaća samo podatke o prognozi kvalitete zraka.

- **URL:** `/air-quality/{city}/forecast`
- **Metoda:** `GET`
- **Uspješan odgovor (200 OK):** Vraća `forecastData` objekt.

### 4. Health Check

Provjerava status i ispravnost API-ja.

- **URL:** `/health`
- **Metoda:** `GET`
- **Uspješan odgovor (200 OK):**
  ```
  Healthy
  ```

---

## Modeli Podataka

### `LiveAqiResponse`

| Polje | Tip | Opis |
| --- | --- | --- |
| `city` | string | Naziv grada. |
| `overallAqi` | number | Glavna AQI vrijednost. |
| `aqiCategory` | string | Kategorija kvalitete zraka (npr. `Moderate`). |
| `healthMessage` | string | Zdravstvena preporuka. |
| `timestamp` | Date | Vrijeme mjerenja. |
| `measurements` | `Measurement[]` | Niz mjerenja pojedinačnih zagađivača. |
| `dominantPollutant` | string | Dominantni zagađivač. |

### `ForecastResponse`

| Polje | Tip | Opis |
| --- | --- | --- |
| `city` | string | Naziv grada. |
| `forecast` | `ForecastDay[]` | Niz dnevnih prognoza. |

### `ForecastDay`

| Polje | Tip | Opis |
| --- | --- | --- |
| `date` | string | Datum prognoze. |
| `aqi` | number | Predviđena AQI vrijednost. |
| `dayName` | string | Naziv dana. |
