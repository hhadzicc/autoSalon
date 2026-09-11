# Deployment

Autosalon OneZone provides a simple local Docker setup and a separate Caddy
setup for direct public deployment on a VPS.

## Mode Overview

| Mode | Compose file | Host entry point | Typical use |
| --- | --- | --- | --- |
| Default local | `docker-compose.yml` | `127.0.0.1:8080` over HTTP | Local development and portfolio demo |
| Standalone VPS | `docker-compose.standalone.yml` | Ports `80` and `443` | Direct public deployment with a domain |

## Default Local Mode

Start the local environment with:

```powershell
docker compose up -d --build
```

No `.env` file, domain, or reverse proxy is required. The application is
available at:

```text
http://127.0.0.1:8080
```

Request flow:

```text
browser
  -> ASP.NET Core on 127.0.0.1:8080
  -> SQL Server on the private Docker network
```

Only the ASP.NET Core container publishes a host port, and it is restricted to
the host loopback interface. SQL Server does not publish a host port and is
reachable only by the application through Docker's internal service name
`db:1433`.

To change the local application port, add this to a private `.env` file:

```env
APP_HTTP_PORT=8081
```

The application's internal port remains `8080`.

## Standalone VPS with Automatic HTTPS

Use standalone mode when this project is responsible for the VPS web ingress:

```powershell
docker compose -f docker-compose.standalone.yml up -d --build
```

Before starting it:

1. Point the domain's DNS `A` and, when used, `AAAA` records to the VPS.
2. Allow inbound TCP ports 80 and 443. UDP 443 is optional for HTTP/3.
3. Create a private `.env` containing the required values.

```env
APP_DOMAIN=autosalon.example.com
MSSQL_SA_PASSWORD=replace-with-a-strong-password
DEMO_ADMIN_PASSWORD=replace-with-a-strong-password
DEMO_BUYER_PASSWORD=replace-with-a-strong-password
DEMO_SELLER_PASSWORD=replace-with-a-strong-password
DEMO_MODE=true
DEMO_RESET_ENABLED=true
DEMO_RESET_INTERVAL_MINUTES=60
```

Replace the example domain with a domain controlled by the deployer. The
standalone Compose file has no fallback for these values and rejects an
incomplete configuration before starting containers.

Request flow:

```text
Internet on ports 80/443
  -> Caddy with automatic HTTPS and HTTP-to-HTTPS redirect
  -> ASP.NET Core over the private Docker network
  -> SQL Server over the private Docker network
```

Only Caddy publishes public ports. The ASP.NET Core and SQL Server containers
remain private. Caddy obtains and renews the TLS certificate automatically.
Certificate state, database data, and ASP.NET Core Data Protection keys are
stored in named Docker volumes and are not committed to Git.

ASP.NET Core accepts `X-Forwarded-For` and `X-Forwarded-Proto` only from the
fixed private address assigned to the standalone Caddy container. HTTPS
redirection inside ASP.NET is disabled in Docker because Caddy owns the public
redirect and sends private HTTP traffic to the application.

## Hosting Multiple Web Applications

Only one process can own host ports 80 and 443. If a VPS hosts several web
applications, use one central edge proxy and give every application a separate
loopback port or Docker network route. The standalone mode is intended for the
case where Autosalon itself owns the VPS ingress.

## Environment Files

Docker Compose automatically reads `.env` from the project directory.
`.env.example` is documentation and is not loaded automatically. Never commit
the real `.env`, passwords, API keys, certificates, or private keys.

`STANDALONE_NETWORK_SUBNET` and `STANDALONE_PROXY_IP` are private Docker network
values. Their defaults normally require no changes.

## Public Demo Safety

Outbound password-reset email is off by default. Enable it only with all three
private environment values:

```env
EMAIL_SENDING_ENABLED=true
RESEND_API_KEY=re_...
FROM_EMAIL=noreply@example.com
```

The forgot-password POST endpoint permits five requests per client IP in a
15-minute window. Keep provider keys only in the private `.env` file.

Periodic cleanup is also opt-in. It removes runtime orders, payments, cart
items, reviews, support inquiries, and vehicle changes, then restores the seed
catalog and demo account state. Registered Identity accounts are not deleted.
The application performs one reset at startup and then repeats it at the
configured interval. A visible demo notice shows that payments are simulated
and, when cleanup is enabled, the approximate time until the next reset.

`DEMO_RESET_ENABLED=true` is accepted only when all of these are true:

```env
DEMO_MODE=true
STRIPE_USE_MOCK_PAYMENTS=true
```

`Database__SeedDemoData` must also be enabled by the deployment configuration.
The interval cannot be shorter than five minutes. Real Stripe mode requires
`STRIPE_USE_MOCK_PAYMENTS=false` and a non-empty `STRIPE_SECRET_KEY`.

## Validation

Default configuration:

```powershell
docker compose config --quiet
```

Standalone configuration after setting the required values:

```powershell
docker compose -f docker-compose.standalone.yml config --quiet
```

## Stop

```powershell
docker compose down
```

For standalone mode:

```powershell
docker compose -f docker-compose.standalone.yml down
```
