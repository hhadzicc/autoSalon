# Demo Accounts

The Docker demo seeds three local accounts.

| Role | Email | Password |
| --- | --- | --- |
| Administrator | `admin@autosalon.local` | `Admin123!` |
| Seller | `prodavac@autosalon.local` | `Prodavac123!` |
| Buyer | `kupac@autosalon.local` | `Kupac123!` |

These credentials are intended only for the local Docker demo.

If a local `.env` file defines different `DEMO_*` values, Docker Compose uses those values instead of the public defaults above.

The application syncs existing demo user passwords with the configured `DEMO_*` values when the container starts.
