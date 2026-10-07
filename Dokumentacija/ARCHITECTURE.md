# Architecture

Autosalon OneZone follows the standard ASP.NET Core MVC structure.

```text
Browser
  -> ASP.NET Core MVC controllers and Razor views
  -> Application services
  -> Entity Framework Core
  -> SQL Server
```

## Application

- **Razor Views and Bootstrap** provide the responsive English/Bosnian UI.
- **Controllers and view models** handle requests and input validation.
- **Services** isolate payments, email delivery, image storage and demo reset
  behavior from controllers.
- **Entity Framework Core** manages application data and migrations.
- **ASP.NET Core Identity** provides authentication, password hashing and
  role-based authorization.

The main roles are Administrator, Seller and Buyer. Administrative actions are
protected on the server and are not based only on hidden UI controls.

## Data and Integrations

SQL Server stores users, vehicles, carts, purchases, reviews and support
inquiries. Vehicle uploads, application logs and Seq data are stored outside the
database so they can persist across container replacement and demo-data resets.

The application supports:

- mock payments for the demo and an optional Stripe implementation;
- optional password-reset email delivery through Resend;
- structured activity logs through Serilog and a private Seq instance.

External services are disabled unless their server-side configuration is
provided. Passwords, card details and password-reset tokens are not written to
activity logs.

## Docker Runtime

The default Docker Compose setup starts:

- the ASP.NET Core application;
- SQL Server;
- Seq for private log inspection.

Only the application and Seq bind to localhost ports. SQL Server remains inside
the Docker network. Production uses the same application image published by the
CI workflow.
