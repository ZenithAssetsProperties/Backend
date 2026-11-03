## Description
<!-- Provide a clear summary of the change, motivation, and context. -->

## Type of Change
- [ ] 🚀 New feature (non-breaking change adding functionality)
- [ ] 🐛 Bug fix (non-breaking change resolving an issue)
- [ ] ⚡ Performance optimization
- [ ] 🔒 Security fix / hardening
- [ ] 🛠️ Refactoring / Architectural improvement
- [ ] ⚠️ Breaking change (fix or feature requiring database/API contract migration)

## Domain Modules Impacted
- [ ] `Domain` (Aggregates, Value Objects, Domain Events)
- [ ] `Application` (Commands, Queries, MediatR Behaviors)
- [ ] `Infrastructure` (EF Core, Redis, Paystack, S3 Storage)
- [ ] `WebApi` (Controllers, Routing, Middleware, Swagger)

## Database & Migration Checklist
- [ ] Schema changes reviewed for PostgreSQL backward compatibility.
- [ ] Proper indexes added for high-traffic queries.
- [ ] No long-running locks or unindexed table scans introduced.

## Security & Reliability Review
- [ ] RBAC / Authorization permissions tested (`[Authorize(Roles = "...")]`).
- [ ] Rate limiting verified for new or updated endpoints.
- [ ] Sensitive customer or title document data masked / access-controlled.
- [ ] No hardcoded secrets or sensitive credentials committed.

## How Has This Been Tested?
<!-- Describe the tests executed to verify your changes. -->
- [ ] Local clean Release build verified (`dotnet build ZenithAssets.sln -c Release`).
- [ ] Docker container build verified (`docker compose build`).
- [ ] Manual endpoint smoke test via Swagger / Postman.

