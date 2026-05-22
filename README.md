# Autosalon OneZone

Autosalon OneZone is an ASP.NET Core MVC application for managing a car dealership. It includes a public vehicle catalog, user accounts, role-based access, shopping cart, orders, payments, reviews, support inquiries, password recovery, an admin dashboard, and bilingual UI support.

## Highlights

- Public vehicle catalog with search, featured vehicles and detailed vehicle pages.
- Shopping cart, checkout flow and order confirmation screen.
- ASP.NET Core Identity authentication with administrator, seller and buyer roles.
- Admin dashboard for vehicles, users, reviews and support inquiries.
- Secure password reset flow with optional Resend email delivery.
- English and Bosnian UI localization.
- SQL Server persistence through Entity Framework Core.
- Docker Compose demo environment.
- Unit and integration tests for core application behavior.

## Tech Stack

- ASP.NET Core 8 MVC
- Entity Framework Core
- ASP.NET Core Identity
- SQL Server 2022
- Docker Compose
- Bootstrap 5
- Resend email integration
- Stripe-ready payment abstraction with local mock payments

## Quick Start

Prerequisite: Docker Desktop.

```powershell
git clone https://github.com/hhadzicc/autoSalon.git
cd autoSalon
docker compose up --build
```

The application will be available at:

```text
http://localhost:8080
```

Docker Compose starts:

- `db`: SQL Server on local port `14333`
- `web`: ASP.NET Core application on local port `8080`

On startup, the application applies EF Core migrations and seeds demo data.

## Demo Accounts

Demo account details are documented in [Dokumentacija/DEMO_ACCOUNTS.md](Dokumentacija/DEMO_ACCOUNTS.md).

If a local `.env` file exists, Docker Compose uses those values. Without `.env`, public demo defaults from the documentation are used.

## Demo Payment

The Docker demo uses mock payments by default, so Stripe keys are not required.

Successful test payment:

```text
Card number: 4242424242424242
Expiry date: any future date
CVV: any 3 digits
```

The card `4000000000000002` simulates a declined payment.

## Password Reset

The application supports a complete forgot-password flow using ASP.NET Core Identity tokens. Reset links expire after 30 minutes.

If `RESEND_API_KEY` and `FROM_EMAIL` are configured, the reset link is sent by email. In Development mode without Resend configuration, the reset link is written to the application log so the flow can be tested locally without exposing API keys.

## Screenshots

Interface screenshots are available in [Dokumentacija/SCREENSHOTS.md](Dokumentacija/SCREENSHOTS.md).

## Tests

Run the test suite:

```powershell
dotnet test "Implementacija/Autosalon OneZone/AutosalonOneZone.Tests/AutosalonOneZone.Tests.csproj"
```

## Documentation

Current project documentation starts in [Dokumentacija/README.md](Dokumentacija/README.md).

The original MVP/OOAD documentation is archived in [Dokumentacija/Legacy_MVP_Documentation](Dokumentacija/Legacy_MVP_Documentation).

## Useful Commands

Stop containers:

```powershell
docker compose down
```

Reset the local demo database:

```powershell
docker compose down -v
docker compose up --build
```

Build locally without Docker:

```powershell
dotnet build "Implementacija/Autosalon OneZone/Autosalon OneZone/Autosalon OneZone.csproj"
```
