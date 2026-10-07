# Testing

## Automated Tests

Run the test project from the repository root:

```powershell
dotnet test "Implementacija/Autosalon OneZone/AutosalonOneZone.Tests/AutosalonOneZone.Tests.csproj"
```

The suite contains unit and integration tests for validation, authentication,
authorization, administrative actions and common data-management flows.

CI runs the tests inside the Dockerfile's .NET 8 SDK stage before publishing an
application image.

## Manual Verification

The following flows should be verified after significant UI or authentication
changes:

- English/Bosnian language switching;
- login, registration and password reset;
- vehicle search, filters and details;
- cart, checkout and purchase history;
- administrator and seller dashboards;
- desktop and mobile layouts.

A complete local smoke test can be performed by starting the Docker environment
and opening `http://localhost:8080`:

```powershell
docker compose up --build
```
