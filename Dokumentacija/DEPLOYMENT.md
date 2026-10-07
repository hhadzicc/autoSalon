# Deployment

Autosalon OneZone provides one default Docker setup for local use or deployment
behind a host Cloudflare Tunnel, plus a separate Caddy setup for a VPS that owns
its public ingress directly.

## Mode Overview

| Mode | Compose file | Host entry point | Typical use |
| --- | --- | --- | --- |
| Default local | `docker-compose.yml` | `127.0.0.1:8080` over HTTP | Local development and portfolio demo |
| Cloudflare Tunnel VPS | `docker-compose.yml` | `127.0.0.1:8080` behind host `cloudflared` | Current hosted portfolio deployment |
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

browser on 127.0.0.1:5341
  -> private Seq log viewer
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

## Cloudflare Tunnel VPS

The hosted portfolio deployment uses the default Compose file. The application
remains bound to server loopback, and the existing host `cloudflared` service
forwards `https://autosalon.hamzahadzic.site` to:

```text
http://127.0.0.1:8080
```

Cloudflare terminates public TLS. Caddy is not started, and the server does not
need public ports 80 or 443 for this mode. A private server `.env` contains at
least:

```env
WEB_IMAGE=ghcr.io/hhadzicc/autosalon:latest
ASPNETCORE_ENVIRONMENT=Production
APP_HTTP_PORT=8080
APP_NETWORK_SUBNET=172.30.250.0/24
REVERSE_PROXY_KNOWN_PROXY=172.30.250.1
MSSQL_SA_PASSWORD=replace-with-a-strong-password
DEMO_MODE=true
DEMO_RESET_ENABLED=true
DEMO_RESET_INTERVAL_MINUTES=60
SEQ_ADMIN_PASSWORD_HASH=replace-with-a-generated-seq-password-hash
```

The fixed application subnet makes the Docker host gateway predictable. ASP.NET
trusts forwarded headers only from `172.30.250.1`, which is the host-side proxy
address seen by the web container. This preserves the original HTTPS scheme and
client address without trusting forwarded headers from arbitrary containers.

Start this mode manually with:

```sh
docker compose --env-file .env up -d --no-build
```

CI/CD supplies the exact `WEB_IMAGE`, pulls it first, and performs this startup
through `deploy/deploy.sh`.

## Standalone VPS with Automatic HTTPS

Use standalone mode when this project is responsible for the VPS web ingress:

```powershell
docker compose -f docker-compose.standalone.yml up -d
```

Before starting it:

1. Point the domain's DNS `A` and, when used, `AAAA` records to the VPS.
2. Allow inbound TCP ports 80 and 443. UDP 443 is optional for HTTP/3.
3. Create a private `.env` containing the required values.

```env
WEB_IMAGE=ghcr.io/hhadzicc/autosalon:latest
APP_DOMAIN=autosalon.example.com
MSSQL_SA_PASSWORD=replace-with-a-strong-password
DEMO_ADMIN_PASSWORD=replace-with-a-strong-password
DEMO_BUYER_PASSWORD=replace-with-a-strong-password
DEMO_SELLER_PASSWORD=replace-with-a-strong-password
DEMO_MODE=true
DEMO_RESET_ENABLED=true
DEMO_RESET_INTERVAL_MINUTES=60
SEQ_ADMIN_PASSWORD_HASH=replace-with-a-generated-seq-password-hash
```

Generate the Seq hash locally before deployment and store only the resulting
hash in the server's private `.env`. Keep the original password in a password
manager:

```powershell
'a-long-unique-password' | docker run --rm -i datalust/seq:latest config hash
```

Replace the example domain with a domain controlled by the deployer. The
standalone Compose file has no fallback for these values and rejects an
incomplete configuration before starting containers.

`WEB_IMAGE` selects the already tested application image. Standalone deployment
does not compile source code on the VPS. The CI/CD workflow normally overrides
this value with the immutable `sha-<commit>` image for the selected commit.

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

Seq publishes its UI only on the server's `127.0.0.1` interface and is not part
of the Caddy routing configuration. Reach it from an administrator workstation
through an SSH tunnel:

```powershell
ssh -L 5341:127.0.0.1:5341 user@server
```

While that session is open, browse to `http://127.0.0.1:5341`. Seq data is
stored in `runtime/seq-data`, and the independent rolling JSON backup is stored
in `runtime/logs`. Both paths are excluded from Git and persist across database
resets and container replacement.

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

## GitHub CI/CD

The workflow in `.github/workflows/ci-cd.yml` has three separate jobs:

1. `verify` validates both Compose files, runs all tests inside the .NET 8 SDK
   Docker stage, and builds the final runtime image.
2. `publish` runs only for `main` and publishes immutable `sha-<commit>` plus
   moving `latest` tags to GitHub Container Registry.
3. `deploy` runs only when the workflow is manually started with the `deploy`
   option enabled. It joins the private tailnet, uploads the deployment files
   from the exact workflow revision, deploys its verified SHA image, and
   performs an internal health check.

The server can remain offline while CI and image publishing run. An offline
server only prevents the optional manual deployment job from succeeding.

### One-time server preparation

Install Docker Engine, Docker Compose, and Tailscale on the VPS. Create a
non-root deployment user with Docker access and prepare its deployment
directory:

```sh
install -d -m 755 "$HOME/apps/autosalon"
cd "$HOME/apps/autosalon"
```

Upload `.env.example` once from an administrator workstation, then create the
private configuration:

```sh
cp .env.example .env
chmod 600 .env
```

Replace every example production value in `.env`. Keep that file, runtime
uploads, logs, Seq data, and Docker volumes on the server; all are excluded from
Git. Add a dedicated deployment public key to the deployment user's
`~/.ssh/authorized_keys`. Its private half belongs only in the protected GitHub
production environment.

The server does not need GitHub repository credentials. On every deployment,
the workflow uploads only `.env.example`, the default Compose file, and the
deployment script from the selected commit. The private `.env` and runtime
directories are never transferred or replaced.

The server needs no public SSH port. Allow the ephemeral Tailscale identity
`tag:ci` to reach the server on port 22 in the tailnet access policy. Standard
SSH key authentication is still used after the private Tailscale connection is
established.

### GitHub production environment

Create a GitHub environment named `production` and add these secrets:

| Secret | Purpose |
| --- | --- |
| `TS_OAUTH_CLIENT_ID` | Tailscale OAuth client identifier |
| `TS_OAUTH_SECRET` | Tailscale OAuth client secret with permission to create `tag:ci` ephemeral nodes |
| `DEPLOY_SSH_PRIVATE_KEY` | Dedicated private key for the VPS deployment user |
| `DEPLOY_SSH_HOST_KEY` | Verified `known_hosts` entry for the Tailscale hostname or address |

Add these non-secret environment variables:

| Variable | Example |
| --- | --- |
| `DEPLOY_HOST` | `autosalon-server` or its Tailscale IP |
| `DEPLOY_USER` | `deploy` |
| `DEPLOY_PATH` | `/home/deploy/apps/autosalon` |

Create the Tailscale OAuth client with the `auth_keys` write scope and the
`tag:ci` tag. Verify the server's SSH host-key fingerprint directly on the VPS
before storing its `ssh-keyscan` output as `DEPLOY_SSH_HOST_KEY`.

### First deployment

After the workflow has reached `main`, open GitHub Actions, select `CI/CD`, use
`Run workflow`, select `main`, and enable `deploy`. Normal pushes to `main`
continue to test and publish images but deliberately do not deploy without this
manual approval.

The deployment script validates Compose configuration, pulls all required
images, recreates changed services without building on the server, and checks
the loopback application endpoint. On failure, it prints the latest web and
database logs and exits with a failed GitHub Actions job.

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
items, reviews, support inquiries, vehicle changes, non-seeded accounts, and
all active authentication sessions, then restores the seed catalog and demo
account state. The application performs one reset at startup and then repeats
it at the configured interval. Uploaded non-demo vehicle images and private
activity logs remain on disk. A visible demo notice shows that payments are
simulated and, when cleanup is enabled, the approximate time until the next
reset.

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
