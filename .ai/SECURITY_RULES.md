# Repository Security Rules

The following files must never be read or modified by AI agents:

- .env
- .env.*
- *.key
- *.pem
- *.pfx
- *.crt
- secrets.json

These files may contain credentials or secrets.

If access to configuration values is required, use example files instead:

.env.example
appsettings.example.json

The following files can be read but not modified:

- ConfigFiles/*.cfg
- ConfigFiles/*.icd
- ConfigFiles/*.scl
- src/Iec61850Sim.Web/Config/*.cfg
- src/Iec61850Sim.Web/Config/*.icd