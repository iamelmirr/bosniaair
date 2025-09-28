# Rješavanje Problema (Troubleshooting)

Ovdje su navedeni neki uobičajeni problemi i njihova rješenja.

---

### Backend

**Problem: Aplikacija se ne pokreće i javlja grešku "WAQI API token is not configured".**

- **Rješenje:** Niste postavili `WAQI_API_TOKEN` varijablu. Kreirajte `.env` datoteku u korijenskom direktoriju projekta i dodajte svoj API ključ. Pogledajte [Upute za pokretanje](#backend-upute) u `README.md`.

**Problem: Podaci se ne prikazuju, a u logovima se vidi greška 401 ili 403 s WAQI API-ja.**

- **Rješenje:** Vaš `WAQI_API_TOKEN` je vjerojatno neispravan ili je istekao. Provjerite ispravnost ključa na [WAQI platformi](https://aqicn.org/data-platform/token/).

**Problem: Baza podataka je prazna ili se ne ažurira.**

- **Rješenje:**
  1.  Provjerite da li `AirQualityScheduler` servis radi. Pogledajte logove za greške vezane uz `AirQualityScheduler`.
  2.  Provjerite da li backend ima dozvole za pisanje u `bosniaair-aqi.db` datoteku.
  3.  Pokušajte ručno obrisati `.db` datoteku; Entity Framework će je automatski ponovno kreirati pri sljedećem pokretanju.

---

### Frontend

**Problem: Podaci se ne prikazuju, a u konzoli preglednika se vidi "CORS error".**

- **Rješenje:**
  1.  Provjerite da li je backend server pokrenut i dostupan na `http://localhost:5000`.
  2.  U backend projektu, u `appsettings.json` ili kroz varijable okruženja, provjerite da li je `FRONTEND_ORIGIN` postavljen na `http://localhost:3000`.
  3.  Ako ste promijenili port za frontend, morate ažurirati CORS postavke na backendu.

**Problem: Aplikacija prikazuje "Greška pri učitavanju podataka".**

- **Rješenje:**
  1.  Provjerite da li je backend API pokrenut.
  2.  Otvorite "Network" tab u developerskim alatima preglednika i provjerite status API poziva prema `http://localhost:5000/api/v1/...`. Ako je status `500`, provjerite logove na backendu za detalje o grešci.

**Problem: `npm install` ne uspijeva.**

- **Rješenje:**
  1.  Provjerite da li koristite kompatibilnu verziju Node.js (v18+).
  2.  Pokušajte obrisati `node_modules` direktorij i `package-lock.json` datoteku, a zatim ponovno pokrenite `npm install`.
      ```bash
      rm -rf node_modules package-lock.json
      npm install
      ```
