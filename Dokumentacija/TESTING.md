# Testing

This document describes testing for Autosalon OneZone.

## Running Tests

From the repository root:

```powershell
dotnet test "Implementacija/Autosalon OneZone/AutosalonOneZone.Tests/AutosalonOneZone.Tests.csproj"
```

## Test Types

### Unit Tests

Unit tests verify isolated validation and business rules:

- registration validation
- password validation
- profile validation
- vehicle validation
- view model business rules

### Integration Tests

Integration tests verify application flows and database state after actions:

- authentication and authorization
- admin-only access
- CRUD scenarios
- support inquiry status updates
- user management and related data cleanup

## Expected Guarantees

The tests should help verify that:

- unauthenticated users cannot access protected actions
- regular users cannot access admin actions
- administrators can manage vehicles, users, reviews and support inquiries
- validation messages and business rules remain consistent
- deleting related entities does not leave inconsistent data
- core smoke flows remain functional

## Manual Smoke Test Checklist

After larger UI, localization or authentication changes, manually verify:

- home page
- EN/BS language switcher
- login and registration
- forgot-password and reset-password flow
- vehicle catalog
- vehicle details
- cart and checkout
- profile and purchased items
- admin dashboard
- admin vehicles
- admin users
- admin reviews
- admin support inquiries

## Docker Check

```powershell
docker compose up --build
```

Then open:

```text
http://localhost:8080
```
