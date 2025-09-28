# BosniaAir 🇧🇦docker-compose up --build

# BosniaAir – Simplified Air Quality Monitoring

**BosniaAir** je moderna web aplikacija za praćenje kvalitete zraka u stvarnom vremenu za gradove širom Bosne i Hercegovine. Aplikacija pruža korisnicima ažurirane informacije o indeksu kvalitete zraka (AQI), dominantnim zagađivačima, prognozi i zdravstvenim preporukama.

[![FSnapshots of Sarajevo's AQI are stored in `bosniaair-aqi.db` (auto-created). The hosted worker keeps that table up to date every 10 minutes.ontend](https://img.shields.io/badge/Frontend-Next.js%2014-blue)](https://nextjs.org/)

Izgrađena je s modernim tehnologijama, uključujući **.NET 8** za backend i **Next.js (React)** za frontend, te je dizajnirana da bude brza, responzivna i laka za korištenje.[![Backend](https://img.shields.io/badge/Backend-.NET%208-purple)](https://dotnet.microsoft.com/)

[![Database](https://img.shields.io/badge/Database-SQLite-green)](https://www.sqlite.org/)

![BosniaAir Screenshot](https://i.imgur.com/EXAMPLE.png) <!-- TODO: Replace with a real screenshot -->

ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:5000" dotnet run --project /Users/elmirbesirovic/Desktop/projects/sarajevoairvibe/backend/src/BosniaAir.Api/BosniaAir.Api.csproj

---

sqlite3 bosniaair-aqi.db "SELECT * FROM SimpleAqiRecords ORDER BY Timestamp DESC LIMIT 10;"

## 📋 Sadržaj

sqlite3 bosniaair-aqi.db "SELECT * FROM SarajevoMeasurements ORDER BY Timestamp DESC LIMIT 5;"

- [Ključne Funkcionalnosti](#-ključne-funkcionalnosti)

- [Tehnologije](#-tehnologije)sqlite3 bosniaair-aqi.db "SELECT * FROM SarajevoForecasts ORDER BY Date DESC LIMIT 10;"

- [Arhitektura](#-arhitektura)

  - [Backend](#backend)

  - [Frontend](#frontend)Lightweight full-stack project that tracks Sarajevo’s air quality, built to demonstrate a clear repository → service → controller flow in ASP.NET Core with a matching Next.js UI.

  - [Tok Podataka](#tok-podataka)

- [Lokalno Pokretanje](#-lokalno-pokretanje)## ✨ What’s Included

  - [Preduvjeti](#preduvjeti)

  - [Backend Upute](#backend-upute)- Live Sarajevo AQI, refreshed every 10 minutes

  - [Frontend Upute](#frontend-upute)- 5‑day AQI outlook built from live data snapshots

- [Baza Podataka](#-baza-podataka)- Health advice for critical groups (Sportisti, Djeca, Stariji, Astmatičari)

  - [Shema](#shema)- City comparison screen that always fetches fresh AQI (no caching)

  - [Korisni Upiti](#korisni-upiti)- SQLite persistence for Sarajevo’s live AQI history

- [Doprinos](#-doprinos)- Minimal service layer that mirrors the classic `Repository → Service → Controller` stack Matej described

- [Licenca](#-licenca)

## 🧱 Architecture at a Glance

---

```

## ✨ Ključne FunkcionalnostiNext.js frontend

          │

- **Praćenje u Stvarnom Vremenu:** Prikaz AQI vrijednosti uživo za odabrani grad.ASP.NET Core API (BosniaAir.Api)

- **Vremenska Prognoza:** Prognoza kvalitete zraka za naredne dane.          ├── Controllers  → HTTP endpoints

- **Detaljni Podaci o Zagađivačima:** Prikaz koncentracija ključnih zagađivača (PM2.5, PM10, O3, NO2, CO, SO2).          ├── Services     → Business logic + orchestrations

- **Zdravstvene Preporuke:** Savjeti za osjetljive grupe i opću populaciju na temelju trenutnog AQI.          ├── Repository   → EF Core over SQLite

- **Usporedba Gradova:** Mogućnost usporedbe kvalitete zraka između različitih gradova.          ├── DTOs         → Responses to the frontend

- **Responzivni Dizajn:** Optimizirano za korištenje na desktop i mobilnim uređajima.          └── Hosted Worker→ 10 min refresh of Sarajevo live AQI

- **Prilagodljive Postavke:** Korisnici mogu odabrati primarni grad za praćenje.```

- **Svijetla i Tamna Tema:** Automatsko prepoznavanje teme sustava uz mogućnost ručne promjene.

All former `Domain`, `Application`, `Infrastructure`, and external worker projects were removed. Everything now lives inside `BosniaAir.Api`, keeping the focus on the repository/service/controller pattern with a single data model (`SimpleAqiRecord`).

---

## 🗂️ Repository Layout

## 🛠️ Tehnologije

```

| Komponenta | Tehnologija | Opis |backend/

| --- | --- | --- |├── BosniaAir.sln            # Solution with Api + Tests

| **Backend** | **.NET 8** | Robustan i skalabilan API za obradu i isporuku podataka. |├── src/

| | **ASP.NET Core** | Framework za izgradnju web API-ja. |│   └── BosniaAir.Api/       # Controllers, services, repository, DTOs, hosted refresh

| | **Entity Framework Core** | ORM za interakciju s bazom podataka. |└── tests/

| | **SQLite** | Lagana, serverska baza podataka za lokalno spremanje podataka. |     └── BosniaAir.Tests/     # Unit tests for services

| | **Serilog** | Fleksibilno logiranje za praćenje rada aplikacije. |

| | **Hangfire** | (Planirano) Za pozadinsko izvršavanje zadataka (npr. dohvaćanje podataka). |frontend/

| **Frontend** | **Next.js 14** | React framework za serverski renderirane i statičke web stranice. |└── …                          # Next.js app (unchanged)

| | **React** | Biblioteka za izgradnju korisničkih sučelja. |```

| | **TypeScript** | Statička tipizacija za robusniji i održiviji kod. |

| | **Tailwind CSS** | Utility-first CSS framework za brz i responzivan dizajn. |Backups of the original clean-architecture projects are kept under `backend_BACKUP_*` if you ever need to reference the older layout.

| | **SWR** | React Hooks biblioteka za dohvaćanje i keširanje podataka. |

| **Baza Podataka** | **SQLite** | Spremanje povijesnih i prognoziranih podataka o kvaliteti zraka. |## ⚙️ Backend Quickstart

| **API Izvor** | **WAQI API** | World Air Quality Index (WAQI) projekt za podatke o kvaliteti zraka. |

1. **Set your API key**

---    ```bash

    export AQICN_API_KEY=your_key_here

## 🏗️ Arhitektura    ```



### Backend2. **Run the API**

    ```bash

Backend je izgrađen koristeći **Clean Architecture** principe, odvajajući logiku u različite slojeve:    cd backend/src/BosniaAir.Api

    dotnet run

- **API sloj (`BosniaAir.Api`):** Sadrži kontrolere, DTOs (Data Transfer Objects) i middleware. Odgovoran je za primanje HTTP zahtjeva i slanje odgovora.    ```

- **Servisni sloj:** Sadrži poslovnu logiku, kao što je dohvaćanje podataka s vanjskog API-ja, transformacija podataka i keširanje.

- **Repozitorij sloj:** Apstrahira pristup podacima, omogućujući komunikaciju s bazom podataka (SQLite) putem Entity Framework Core.3. **Available endpoints** (Swagger at `http://localhost:5000/swagger`)

- **Entitetski sloj:** Sadrži domenske modele koji predstavljaju osnovne strukture podataka.    - `GET /live` – Sarajevo live AQI (force refresh via `?refresh=true`)

    - `GET /forecast` – 5-day outlook based on stored snapshots

### Frontend    - `GET /groups` – Health guidance for critical groups

    - `GET /daily` – 7-day timeline built from SQLite history

Frontend je moderna **Next.js** aplikacija koja koristi **React Hooks** za upravljanje stanjem i dohvaćanje podataka.    - `GET /compare?cities=Sarajevo,Tuzla` – Always hits the upstream API, no caching

    - `GET /admin/snapshots` – Inspect/remove stored snapshots (for debugging)

- **Komponente:** UI je podijeljen u reaktivne komponente (`LiveAqiPanel`, `ForecastTimeline`, `Pollutants`, itd.).

- **Dohvaćanje Podataka:** Koristi se **SWR** (`stale-while-revalidate`) za efikasno keširanje i ažuriranje podataka s backend API-ja.Snapshots of Sarajevo’s AQI are stored in `sarajevoair-aqi.db` (auto-created). The hosted worker keeps that table up to date every 10 minutes.

- **Stiliziranje:** **Tailwind CSS** se koristi za brz i konzistentan dizajn, s podrškom za tamnu temu.

- **Stanje:** Lokalno stanje se upravlja pomoću `useState` i `useEffect`, dok se globalne postavke (npr. odabrani grad) spremaju u `localStorage`.## 🧪 Tests



### Tok PodatakaUnit tests exercise the new services directly (repository fallbacks, group advice logic, city comparison error handling).



1.  **Pozadinski servis (`AirQualityScheduler`):** Periodično (npr. svakih 15 minuta) dohvaća svježe podatke s **WAQI API-ja**.```bash

2.  **Spremanje u Bazu:** Dohvaćeni podaci (live i forecast) se obrađuju i spremaju u **SQLite** bazu podataka.cd backend

3.  **Frontend Zahtjev:** Kada korisnik otvori aplikaciju, frontend šalje zahtjev backend API-ju (`/api/v1/air-quality/{city}/complete`).dotnet test

4.  **API Odgovor:** Backend dohvaća najnovije podatke iz svoje baze i šalje ih frontendu u optimiziranom formatu.```

5.  **Prikaz na UI:** Frontend prima podatke i prikazuje ih korisniku kroz različite komponente. **SWR** automatski ažurira podatke u pozadini.

## 🖥️ Frontend Quickstart

---

```bash

## 🚀 Lokalno Pokretanjecd frontend

pnpm install

Za pokretanje projekta lokalno, potrebno je postaviti i pokrenuti backend i frontend odvojeno.pnpm dev

```

### Preduvjeti

Set `NEXT_PUBLIC_API_BASE_URL` to your backend URL (defaults to `http://localhost:5000`).

- **.NET 8 SDK:** [Preuzmi ovdje](https://dotnet.microsoft.com/download/dotnet/8.0)

- **Node.js (v18+):** [Preuzmi ovdje](https://nodejs.org/)## ✅ What Changed (and Why)

- **Git:** [Preuzmi ovdje](https://git-scm.com/)

- **API Ključ:** Potreban je besplatni API ključ od [WAQI API](https://aqicn.org/data-platform/token/).- **One project**: everything runs from `BosniaAir.Api`; the extra projects were deleted to keep focus on the core flow.

- **SQLite persistence**: a single `SimpleAqiRecord` entity powers history, forecast, and admin views.

### Backend Upute- **Clear layering**: controllers → services → repository; DTOs are explicitly separated from EF entities.

- **City comparison**: now always calls the upstream API (`forceFresh: true`), matching the “no cache” requirement.

1.  **Klonirajte repozitorij:**- **Background refresh**: implemented as an `IHostedService` inside the API; no separate worker project needed.

    ```bash

    git clone https://github.com/iamelmirr/sarajevoairvibe.git

    cd sarajevoairvibe

    ```## 📄 License



2.  **Postavite API ključ:**MIT – see [LICENSE](LICENSE).
    - U korijenskom direktoriju projekta (`sarajevoairvibe`), kreirajte `.env` datoteku.
    - Dodajte svoj WAQI API ključ u `.env` datoteku:
      ```
      WAQI_API_TOKEN=vas_api_kljuc_ovdje
      ```

3.  **Pokrenite backend server:**
    - Otvorite terminal i navigirajte do backend API direktorija:
      ```bash
      cd backend/src/BosniaAir.Api
      ```
    - Pokrenite aplikaciju:
      ```bash
      dotnet run
      ```
    - Backend API će biti dostupan na `http://localhost:5000`. Možete provjeriti status na `http://localhost:5000/health`.

### Frontend Upute

1.  **Instalirajte ovisnosti:**
    - Otvorite **novi terminal** i navigirajte do frontend direktorija:
      ```bash
      cd frontend
      ```
    - Instalirajte sve potrebne pakete:
      ```bash
      npm install
      ```

2.  **Pokrenite frontend aplikaciju:**
    ```bash
    npm run dev
    ```
    - Frontend aplikacija će biti dostupna na `http://localhost:3000`.

Sada biste trebali imati potpuno funkcionalnu aplikaciju koja radi lokalno!

---

## 🗃️ Baza Podataka

Projekt koristi **SQLite** za lokalno spremanje podataka, što olakšava postavljanje i korištenje bez potrebe za vanjskim serverom baze podataka. Datoteka baze se nalazi na putanji `backend/src/BosniaAir.Api/bosniaair-aqi.db`.

### Shema

Glavna tablica je `AirQualityRecords` i sadrži sljedeće važne stupce:

| Stupac | Tip | Opis |
| --- | --- | --- |
| `Id` | INTEGER | Primarni ključ. |
| `City` | TEXT | Naziv grada (npr. `Sarajevo`). |
| `RecordType` | TEXT | Tip zapisa (`Live` ili `Forecast`). |
| `Timestamp` | TEXT | Vrijeme mjerenja u UTC formatu. |
| `AqiValue` | INTEGER | Ukupna AQI vrijednost. |
| `DominantPollutant` | TEXT | Dominantni zagađivač. |
| `Pm25`, `Pm10`, ... | REAL | Vrijednosti pojedinačnih zagađivača. |
| `ForecastJson` | TEXT | JSON string koji sadrži podatke o prognozi. |
| `CreatedAt` | TEXT | Vrijeme kreiranja zapisa. |

### Korisni Upiti

Možete koristiti bilo koji SQLite preglednik ili CLI za izvršavanje upita nad bazom.

**Prikaži 5 najnovijih "live" mjerenja:**
```sql
SELECT * FROM AirQualityRecords WHERE RecordType = 'Live' ORDER BY Timestamp DESC LIMIT 5;
```

**Prikaži najnovije unose prognoze za svaki grad:**
```sql
SELECT City, RecordType, Timestamp, CreatedAt FROM AirQualityRecords WHERE RecordType = 'Forecast' ORDER BY CreatedAt DESC LIMIT 5;
```

---

## 🤝 Doprinos

Doprinosi su dobrodošli! Ako imate prijedloge za poboljšanje ili želite prijaviti grešku, slobodno otvorite "issue" ili pošaljite "pull request".

---

## 📄 Licenca

Ovaj projekt je pod **MIT licencom**. Pogledajte `LICENSE` datoteku za više detalja.
