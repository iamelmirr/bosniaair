# Deployment

Ovaj dokument opisuje preporučeni način za deployment BosniaAir aplikacije.

## Backend (.NET API)

Backend se može deployati kao samostalna aplikacija na bilo kojem hostingu koji podržava **.NET 8**.

### Preporučene opcije:

1.  **Kontejnerizacija (Docker):**
    - Projekt sadrži `Dockerfile` koji olakšava kreiranje Docker image-a.
    - **Koraci:**
      1.  Buildajte Docker image: `docker build -t bosniaair-api .`
      2.  Pokrenite kontejner: `docker run -p 8080:80 -e WAQI_API_TOKEN=vas_token bosniaair-api`
    - Ovaj image se može deployati na platforme kao što su **Azure Container Apps**, **AWS Fargate**, ili **DigitalOcean App Platform**.

2.  **PaaS (Platform as a Service):**
    - **Azure App Service:** Direktno objavljivanje iz Visual Studija ili putem CI/CD pipeline-a.
    - **Heroku:** Iako nije primarno za .NET, moguće je deployati koristeći Docker.

### Konfiguracija

Ključna varijabla okruženja koju trebate postaviti je `WAQI_API_TOKEN`. Također, `FRONTEND_ORIGIN` treba biti postavljen na URL vaše frontend aplikacije kako bi CORS ispravno radio.

## Frontend (Next.js)

Frontend je optimiziran za deployment na platformama koje podržavaju **Next.js**.

### Preporučene opcije:

1.  **Vercel:**
    - Kao kreatori Next.js-a, Vercel nudi najbolju podršku i performanse.
    - **Koraci:**
      1.  Povežite svoj Git repozitorij (GitHub, GitLab, Bitbucket) s Vercelom.
      2.  Vercel će automatski prepoznati Next.js projekt i konfigurirati build postavke.
      3.  Postavite varijablu okruženja `NEXT_PUBLIC_API_URL` na URL vašeg deployanog backend API-ja.
    - Deployment se automatski pokreće nakon svakog `git push`-a na glavnu granu.

2.  **Netlify:**
    - Slično Vercelu, nudi jednostavan deployment s Git-a.

3.  **Samostalno Hostiranje (Node.js server):**
    - Buildajte aplikaciju: `npm run build`
    - Pokrenite server: `npm start`
    - Ovo zahtijeva da sami upravljate Node.js serverom.

---

## CI/CD (Kontinuirana Integracija i Isporuka)

Preporučuje se postavljanje CI/CD pipeline-a (npr. pomoću **GitHub Actions**) za automatizaciju testiranja, buildanja i deploymenta obje aplikacije.
