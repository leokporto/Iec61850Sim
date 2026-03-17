# Security

NEVER read or modify: `.env`, `.env.*`, `*.key`, `*.pem`, `*.pfx`, `*.crt`, `secrets.json`
Use instead: `.env.example`, `appsettings.example.json`

READ-ONLY (never modify): `ConfigFiles/*.cfg|*.icd|*.scl`, `src/Iec61850Sim.Web/Config/*.cfg|*.icd`