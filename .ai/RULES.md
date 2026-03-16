# Development Rules

## Mandatory Development Rules

1. Always verify the official project repository before writing code unless the files are already edited locally.

https://github.com/leokporto/Iec61850Sim

2. Always use the real API of libiec61850.
3. Never invent methods.
4. Always validate the official documentation.
5. Prefer simple and robust implementations.
6. Follow the unit testing protocol.
7. DO NOT EVER read or edit .env files.

---

## Architecture Principles

The project must follow:

- SOLID
- Separation of Concerns
- Dependency Injection
- Low coupling

Vertical slices should be used to separate domains.

Example domains:

- Simulation
- Commands
- Device

---

## libiec61850 Specific Rules

The .NET API differs from the C API.

Never assume parity between them.

Always validate methods using official documentation.

---

## Coding Language Rules

Code must be written in English.

## Sensitive Information

- DO NOT EVER read or edit .env files.
- Never include sensitive information in the codebase. Always use environment variables for configuration.
