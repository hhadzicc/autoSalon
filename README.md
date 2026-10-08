# Autosalon OneZone

Autosalon OneZone is a responsive ASP.NET Core MVC application for browsing,
selling and purchasing vehicles.

[Live demo](https://autosalon.hamzahadzic.site)

## Features

- Vehicle catalog with search, filters, sorting and detailed vehicle pages.
- Shopping cart, checkout and mock card payments for the public demo.
- Buyer profiles, purchase history and vehicle reviews.
- Administrator and seller dashboards for vehicles, users, reviews and support.
- Role-based access through ASP.NET Core Identity.
- English and Bosnian interface localization.
- Responsive layouts for desktop and mobile devices.
- Optional demo-data reset and private structured activity logs.

## Tech Stack

- ASP.NET Core 8 MVC and Razor Views
- Entity Framework Core and SQL Server 2022
- ASP.NET Core Identity
- Bootstrap 5 and JavaScript
- Docker Compose
- xUnit integration and unit tests
- GitHub Actions, GHCR and Tailscale deployment
- Serilog and Seq

## Demo Accounts

| Role | Email | Password |
| --- | --- | --- |
| Administrator | `admin@autosalon.local` | `Admin123!` |
| Seller | `prodavac@autosalon.local` | `Prodavac123!` |
| Buyer | `kupac@autosalon.local` | `Kupac123!` |

These credentials are intended only for the demo environment.

## Run Locally

Docker Desktop is the only prerequisite.

```powershell
git clone https://github.com/hhadzicc/autoSalon.git
cd autoSalon
docker compose up --build
```

Open [http://localhost:8080](http://localhost:8080). The application applies
database migrations and seeds demo data automatically.

Stop the containers with:

```powershell
docker compose down
```

The default setup does not require an `.env` file. Copy `.env.example` to
`.env` only when you want to override the demo settings.

## Demo Payment

The demo uses a local mock payment provider and never processes a real card.
Any 16-digit card number is accepted except the decline example below.

```text
Accepted example: 4242 4242 4242 4242
Declined example: 4000 0000 0000 0002
Expiry: current or future date in MM/YY format
CVV: any three or four digits
```

## Tests

```powershell
dotnet test "Implementacija/Autosalon OneZone/AutosalonOneZone.Tests/AutosalonOneZone.Tests.csproj"
```

The same test project is executed by the GitHub Actions workflow before an
application image is published.

## Documentation

- [Screenshots](Dokumentacija/SCREENSHOTS.md)
- [Architecture](Dokumentacija/ARCHITECTURE.md)
- [Deployment](Dokumentacija/DEPLOYMENT.md)
- [Testing](Dokumentacija/TESTING.md)
