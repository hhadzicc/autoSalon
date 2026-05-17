# Demo Accounts

The Docker demo seeds three local accounts.

| Role | Email variable | Password variable |
| --- | --- | --- |
| Administrator | `DEMO_ADMIN_EMAIL` | `DEMO_ADMIN_PASSWORD` |
| Seller | `DEMO_SELLER_EMAIL` | `DEMO_SELLER_PASSWORD` |
| Buyer | `DEMO_BUYER_EMAIL` | `DEMO_BUYER_PASSWORD` |

If a local `.env` file exists, Docker Compose uses those values for the demo accounts. That is the expected setup for local development.

Without `.env`, Docker Compose falls back to public demo defaults:

| Role | Email | Password |
| --- | --- | --- |
| Administrator | `admin@autosalon.local` | `Admin123!` |
| Seller | `prodavac@autosalon.local` | `Prodavac123!` |
| Buyer | `kupac@autosalon.local` | `Kupac123!` |

These are only local Docker demo credentials. The application syncs existing demo user passwords with the configured `DEMO_*` values when the container starts.
