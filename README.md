# AutoSalon OneZone

AutoSalon OneZone je ASP.NET Core MVC aplikacija za upravljanje autosalonom. Aplikacija pokriva pregled vozila, korisničke naloge, role, korpu, narudžbe, plaćanje i administrativni dio.

## Tehnologije

- ASP.NET Core 8 MVC
- Entity Framework Core
- ASP.NET Core Identity
- SQL Server
- Docker Compose
- Stripe integracija, uz mock način plaćanja za lokalni demo

## Brzo pokretanje preko Dockera

Preduvjet je instaliran Docker Desktop.

```powershell
git clone https://github.com/hhadzicc/autoSalon.git
cd autoSalon
docker compose up --build
```

Nakon pokretanja aplikacija je dostupna na:

```text
http://localhost:8080
```

Docker Compose diže dvije usluge:

- `db`: SQL Server 2022 na lokalnom portu `14333`
- `web`: ASP.NET Core aplikaciju na lokalnom portu `8080`

Na prvom pokretanju aplikacija automatski primijeni EF Core migracije i ubaci demo podatke.

## Demo nalozi

Detaljniji pregled demo naloga je u [DEMO_ACCOUNTS.md](DEMO_ACCOUNTS.md).

| Rola | Email | Lozinka |
| --- | --- | --- |
| Administrator | `admin@autosalon.local` | `Admin123!` |
| Prodavac | `prodavac@autosalon.local` | `Prodavac123!` |
| Kupac | `kupac@autosalon.local` | `Kupac123!` |

## Demo plaćanje

Docker demo koristi mock način plaćanja, tako da za pokretanje nisu potrebni Stripe ključevi.

Za uspješno test plaćanje može se koristiti:

```text
Broj kartice: 4242424242424242
Datum isteka: bilo koji budući datum
CVV: bilo koje 3 cifre
```

Kartica `4000000000000002` simulira odbijenu transakciju.

## Varijable okruženja

Repozitorij sadrži `.env.example` kao primjer lokalne konfiguracije. Za lokalne izmjene konfiguracije potrebno je napraviti `.env` fajl:

```powershell
Copy-Item .env.example .env
```

Zatim izmijeni vrijednosti u `.env` i ponovo pokreni:

```powershell
docker compose up --build
```

Najbitnije varijable su:

| Varijabla | Opis |
| --- | --- |
| `MSSQL_SA_PASSWORD` | Lozinka lokalnog SQL Server containera |
| `DEMO_ADMIN_EMAIL` / `DEMO_ADMIN_PASSWORD` | Demo administrator |
| `DEMO_SELLER_EMAIL` / `DEMO_SELLER_PASSWORD` | Demo prodavac |
| `DEMO_BUYER_EMAIL` / `DEMO_BUYER_PASSWORD` | Demo kupac |
| `STRIPE_USE_MOCK_PAYMENTS` | `true` za mock plaćanje, `false` za Stripe test mode |
| `STRIPE_SECRET_KEY` / `STRIPE_PUBLISHABLE_KEY` | Stripe test ključevi za lokalno testiranje Stripe integracije |

## Korisne naredbe

Za gašenje containera:

```powershell
docker compose down
```

Za brisanje lokalne demo baze i potpuno svjež start:

```powershell
docker compose down -v
docker compose up --build
```

Za lokalni build bez Dockera:

```powershell
dotnet build "Implementacija/Autosalon OneZone/Autosalon OneZone.sln"
```
