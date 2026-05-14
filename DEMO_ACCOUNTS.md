# Demo Accounts

The Docker demo seeds three accounts on the first database startup.

| Role | Email | Password |
| --- | --- | --- |
| Administrator | `admin@autosalon.local` | `Admin123!` |
| Seller | `prodavac@autosalon.local` | `Prodavac123!` |
| Buyer | `kupac@autosalon.local` | `Kupac123!` |

These accounts are only demo credentials for the local Docker environment.

If a local `.env` file defines different `DEMO_*` values, Docker Compose uses those values instead of the public defaults above.

If the SQL Server Docker volume already exists, changing `.env` does not reset existing user passwords. For a fresh local demo database, run:

```powershell
docker compose down -v
docker compose up --build
```
