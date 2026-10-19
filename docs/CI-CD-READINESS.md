# EcoBike CI/CD readiness

## Release model

- CI builds the Vite frontend with `npm ci`, then builds, tests and publishes a self-contained `linux-x64` ASP.NET Core release.
- The generated release excludes `mioto.db`, SQLite WAL/SHM sidecars and secrets.
- The deploy workflow is manual-only and requires approval from the protected `ecobike-production` GitHub Environment.
- Deployments use `/srv/ecobike/releases`, switch `/srv/ecobike/current`, restart only `ecobike.service`, and check `http://127.0.0.1:5081/health`.
- Rollback changes application code only. It does not reverse database schema changes.

## Required GitHub configuration

Environment: `ecobike-production`

Variables:

- `ECOBIKE_SSH_HOST`
- `ECOBIKE_SSH_USER`
- `ECOBIKE_SSH_PORT`

Secret:

- `ECOBIKE_SSH_PRIVATE_KEY`
- `ECOBIKE_SSH_KNOWN_HOSTS`

The values are intentionally not stored in this repository.

## SQLite first deployment

Before the first deployment, the VPS owner should create `/srv/ecobike/shared/data`, make a verified backup of the original coursework database, and copy the preserved database to `/srv/ecobike/shared/data/mioto.db`. Releases must never copy, seed, migrate or replace that file. Any future schema migration requires a separate reviewed backup-and-migration procedure.

## Local commands

```text
npm ci
npm run build
dotnet restore MiotoCarRental.sln --runtime linux-x64
dotnet build MiotoCarRental.sln -c Release --no-restore
dotnet test MiotoCarRental.sln -c Release --no-build --no-restore
dotnet publish MiotoCarRental.csproj -c Release -r linux-x64 --self-contained true --no-restore -o artifacts/publish
```
