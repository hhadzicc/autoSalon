# AutoSalon OneZone

AutoSalon OneZone je ASP.NET Core MVC aplikacija za upravljanje autosalonom. Aplikacija pokriva pregled vozila, korisnicke naloge, role, korpu, narudzbe, placanje i administrativni dio.

## Tehnologije

- ASP.NET Core 8 MVC
- Entity Framework Core
- ASP.NET Core Identity
- SQL Server
- Docker Compose
- Stripe integracija, uz mock payment mode za javni demo

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

Docker Compose dize dvije usluge:

- `db`: SQL Server 2022 na lokalnom portu `14333`
- `web`: ASP.NET Core aplikaciju na lokalnom portu `8080`

Na prvom pokretanju aplikacija automatski primijeni EF Core migracije i ubaci demo podatke.

## Demo nalozi

| Rola | Email | Lozinka |
| --- | --- | --- |
| Administrator | `admin@autosalon.local` | `Admin123!` |
| Prodavac | `prodavac@autosalon.local` | `Prodavac123!` |
| Kupac | `kupac@autosalon.local` | `Kupac123!` |

## Demo placanje

Docker demo koristi mock payment mode, tako da nisu potrebni stvarni Stripe kljucevi.

Za uspjesno test placanje moze se koristiti:

```text
Broj kartice: 4242424242424242
Datum isteka: bilo koji buduci datum
CVV: bilo koje 3 cifre
```

Kartica `4000000000000002` simulira odbijenu transakciju.

## Environment varijable

Prave tajne se ne cuvaju u repozitoriju. Fajl `.env` je ignorisan kroz Git, a `.env.example` sluzi kao javni template.

Ako zelis promijeniti demo vrijednosti:

```powershell
Copy-Item .env.example .env
```

Zatim izmijeni `.env` lokalno i ponovo pokreni:

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
| `STRIPE_USE_MOCK_PAYMENTS` | `true` za mock placanje, `false` za stvarni Stripe test mode |
| `STRIPE_SECRET_KEY` / `STRIPE_PUBLISHABLE_KEY` | Stripe test kljucevi za privatno lokalno testiranje |

## Korisne komande

Za gasenje containera:

```powershell
docker compose down
```

Za brisanje lokalne demo baze i potpuno svjez start:

```powershell
docker compose down -v
docker compose up --build
```

Za lokalni build bez Dockera:

```powershell
dotnet build "Implementacija/Autosalon OneZone/Autosalon OneZone.sln"
```

## Sigurnosne napomene

- Ne commitati `.env`, connection stringove, lozinke ili Stripe kljuceve.
- Demo lozinke iz `.env.example` nisu stvarne tajne; koriste se samo za lokalni Docker demo.
- Za privatno testiranje stvarnog Stripe flowa koristi `.env` i `STRIPE_USE_MOCK_PAYMENTS=false`.
- Za produkciju koristiti posebne produkcijske tajne kroz hosting provider, ne kroz `appsettings.json`.
