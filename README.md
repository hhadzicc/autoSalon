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
- Structured Serilog audit logs with a private Seq viewer and rolling JSON backup.
- Docker Compose demo environment.
- Unit and integration tests for core application behavior.

## Tech Stack

- ASP.NET Core 8 MVC
- Entity Framework Core
- ASP.NET Core Identity
- SQL Server 2022
- Docker Compose
- Caddy reverse proxy
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

Docker Compose starts SQL Server, Seq, and the ASP.NET Core application. The app is
bound only to `127.0.0.1:8080`, while the database remains private inside the
Docker network. No `.env` file or domain is required for this local demo.

On startup, the application applies EF Core migrations and seeds demo data.

## Deployment Modes

The repository provides three deployment modes:

- **Default local:** zero-configuration ASP.NET HTTP entry point on
  `127.0.0.1:8080`, without an additional reverse proxy.
- **Cloudflare Tunnel VPS:** the same default Compose file keeps ASP.NET on
  `127.0.0.1:8080`, while host `cloudflared` provides the public TLS endpoint.
- **Standalone VPS:** Caddy owns host ports 80 and 443, obtains TLS certificates,
  and requires a domain plus private deployment credentials.

Configuration, request flows, multi-application hosting guidance, and the
standalone prerequisites are documented in
[Dokumentacija/DEPLOYMENT.md](Dokumentacija/DEPLOYMENT.md).

## Demo Accounts

Demo account details are documented in [Dokumentacija/DEMO_ACCOUNTS.md](Dokumentacija/DEMO_ACCOUNTS.md).

If a local `.env` file exists, Docker Compose uses those values. Without `.env`, public demo defaults from the documentation are used.

## Demo Payment

The Docker demo uses mock payments by default, so Stripe keys are not required.
Real Stripe processing is used only when `STRIPE_USE_MOCK_PAYMENTS=false` and a
server-side `STRIPE_SECRET_KEY` is configured. Demo mode refuses to start with
real payments enabled.

Successful test payment:

```text
Card number: 4242424242424242
Expiry date: any future date
CVV: any 3 digits
```

The card `4000000000000002` simulates a declined payment.

## Password Reset

The application supports a complete forgot-password flow using ASP.NET Core Identity tokens. Reset links expire after 30 minutes.

Email delivery is disabled by default. Set `EMAIL_SENDING_ENABLED=true` together
with `RESEND_API_KEY` and `FROM_EMAIL` to send reset links through Resend. The
forgot-password endpoint is limited to five submissions per IP address every 15
minutes. Password reset links and tokens are never written to application or
audit logs.

## Optional Periodic Demo Reset

Persistent demo data can be restored on startup and every 60 minutes by setting
`DEMO_MODE=true` and `DEMO_RESET_ENABLED=true`. The interval is configurable
through `DEMO_RESET_INTERVAL_MINUTES`. The application rejects unsafe reset
configuration unless demo seeding and mock payments are also enabled.

## Private Activity Logs

Serilog writes compact rolling JSON files to `runtime/logs`. Seq stores its own
searchable event database under `runtime/seq-data`. Both directories are ignored
by Git and survive application database resets and container replacement.

The Seq UI is bound only to the host loopback interface at
`http://127.0.0.1:5341`; it is not exposed through either public reverse proxy.
On a remote server, open an SSH tunnel and then use that local address:

```powershell
ssh -L 5341:127.0.0.1:5341 user@server
```

The local zero-configuration demo uses the documented password `SeqDemo124!`.
For deployment, generate a unique password hash as described in
[Dokumentacija/DEPLOYMENT.md](Dokumentacija/DEPLOYMENT.md) and keep the original
password outside the repository.

## Screenshots

Interface screenshots are available in [Dokumentacija/SCREENSHOTS.md](Dokumentacija/SCREENSHOTS.md).

## Tests

Run the test suite:

```powershell
dotnet test "Implementacija/Autosalon OneZone/AutosalonOneZone.Tests/AutosalonOneZone.Tests.csproj"
```

CI runs the same suite inside the .NET 8 SDK Docker stage, so neither GitHub
Actions nor the production server requires a host .NET installation. Successful
builds on `main` publish the application image to GitHub Container Registry.
Production deployment is a separate manually approved workflow step over the
private Tailscale network; see
[Dokumentacija/DEPLOYMENT.md](Dokumentacija/DEPLOYMENT.md#github-cicd).

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
