# Deployment

## Local Docker

The default Compose file starts the complete application locally:

```powershell
docker compose up --build
```

The application is available at `http://localhost:8080`. No `.env` file is
required for the demo defaults.

Use a private `.env` only to override values such as demo passwords, ports,
email delivery or payment configuration.

## Hosted Demo

The public demo is available at:

[https://autosalon.hamzahadzic.site](https://autosalon.hamzahadzic.site)

The server uses the default `docker-compose.yml`. The web container listens only
on `127.0.0.1:8080`, while a host Cloudflare Tunnel provides the public HTTPS
endpoint. SQL Server and Seq are not publicly exposed.

Persistent database data is stored in Docker volumes. Uploaded vehicle images,
rolling logs and Seq data are stored under the ignored `runtime/` directory.

## CI/CD

GitHub Actions runs the following flow:

1. Validate both Compose files.
2. Run tests in the .NET 8 SDK Docker stage.
3. Build and publish the application image to GHCR.
4. Deploy manually when the workflow is started with `deploy=true`.

The deployment job connects to the server through Tailscale using short-lived
OIDC authentication, then deploys the exact image created for that commit.
Regular pushes test and publish the image but intentionally skip production
deployment.

Production configuration is split between:

- GitHub's protected `production` environment for deployment credentials;
- a private `.env` on the server for application secrets;
- Tailscale access rules for the private SSH connection.

## Standalone VPS

`docker-compose.standalone.yml` is an alternative for a server that does not use
Cloudflare Tunnel. It adds Caddy on ports 80 and 443 and requires a domain plus
production values in `.env`.

```powershell
docker compose -f docker-compose.standalone.yml up -d
```

This variant is not used by the current hosted demo, but it is kept and
validated by CI as a portable deployment option.
