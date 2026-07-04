# Booking Management Service

A production-style booking management service for shared resources (meeting rooms, equipment, labs). Built as a backend engineering take-home: **.NET 10 Web API + PostgreSQL (EF Core)** backend and a **Next.js / TypeScript** frontend.

The headline feature is **database-level concurrency protection**: overlapping bookings are impossible even under concurrent requests, enforced by a PostgreSQL GiST exclusion constraint — not just application code.

## Documentation

Detailed docs with diagrams live in [`docs/`](docs/):

| Doc | Covers |
|---|---|
| [Database](docs/database.md) | ER diagram, schema/indexes, the exclusion-constraint concurrency guard, seed data |
| [Backend](docs/backend.md) | Layered architecture, request pipeline, endpoints, booking-creation and auth sequence diagrams, status state machines |
| [Frontend](docs/frontend.md) | Route map & access control, auth flow, component structure, API client data flow |

---

## Running the project

### Docker (recommended)

```bash
docker compose up --build
```

| Service  | URL                          |
|----------|------------------------------|
| Frontend | http://localhost:3000        |
| API      | http://localhost:5168        |
| Swagger  | http://localhost:5168/swagger |
| Postgres | localhost:5433               |

Migrations run automatically on API startup, seeding eight resources (rooms, equipment, a lab, a workspace — one deliberately in `Maintenance`). These accounts are seeded so the app is explorable immediately:

| Account | Email | Password | Notes |
|---|---|---|---|
| Admin | `admin@bookingmanager.local` | `Admin123!` | Always seeded |
| Demo user | `alice@bookingmanager.local` | `Password1!` | Dev only, with sample bookings |
| Demo user | `bob@bookingmanager.local` | `Password1!` | Dev only, with sample bookings |

The demo users come with sample bookings over the next two days, including a back-to-back pair (boundary rule) and a cancelled booking (freed slot).

(Dev-only defaults — override with `JWT_SIGNING_KEY`, `SEED_ADMIN_EMAIL`, `SEED_ADMIN_PASSWORD` env vars.)

### Local development

```bash
docker compose up -d postgres          # database only (port 5433)
cd backend && dotnet run --project src/BookingManager.Api    # API on http://localhost:5168
cd frontend/booking-manager-ui && npm install && npm run dev # UI on http://localhost:3000
```

### Tests

```bash
cd backend
dotnet test --filter "Category!=Integration"   # unit + full-pipeline tests (no DB needed)
dotnet test                                     # everything (needs the compose Postgres on 5433)
```

The `Integration`-tagged test is the concurrency proof: it fires two overlapping booking requests at real Postgres simultaneously and asserts exactly one wins.

---

## Architecture

```
backend/
  src/BookingManager.Api/
    Controllers/       Auth, Resources, Bookings, Admin
    Services/          Business logic (BookingService, ResourceService, AuthService, AuditService, TokenService)
    Models/            EF entities + enums
    Data/              DbContext, migrations (incl. exclusion constraint), seeding
    Dtos/              Request/response contracts (summary vs. detail DTOs)
    Middleware/        Exception -> { code, message } mapping
    Validation/        BookingRules (duration, past-booking, UTC enforcement)
  tests/BookingManager.Tests/
frontend/booking-manager-ui/   Next.js app (auth, bookings, availability, admin panel)
```

Controllers are thin; services own the business rules and return DTOs. There is no separate repository layer — EF Core's `DbContext` already is a unit-of-work/repository, and an extra abstraction would add indirection without value at this size.

---

## API

All successful responses are wrapped in `{ "data": ... }`; paged lists add `"meta": { page, pageSize, totalCount, totalPages }`. All errors share one shape:

```json
{ "code": "BOOKING_CONFLICT", "message": "This resource is already booked during the selected time." }
```

### Authentication

| Method | Route | Notes |
|--------|-------|-------|
| POST | `/api/auth/register` | `{ fullName, email, password }` → 201 + JWT |
| POST | `/api/auth/login` | `{ email, password }` → `{ accessToken, expiresAt, user }` |
| POST | `/api/auth/logout` | Stateless no-op (client discards the token) |

Send the token as `Authorization: Bearer <accessToken>`. All other endpoints require it.

### Resources

| Method | Route | Access |
|--------|-------|--------|
| GET | `/api/resources?type=&status=` | any user (cached ~5 min) |
| GET | `/api/resources/{id}` | any user |
| GET | `/api/resources/{id}/availability?from=&to=&durationMinutes=` | any user |
| POST / PUT / DELETE | `/api/resources[/{id}]` | admin only |

`DELETE` is soft when the resource has bookings (sets status `Disabled`, preserving history); hard delete only when it has none.

### Bookings

| Method | Route | Access |
|--------|-------|--------|
| POST | `/api/bookings` | `{ resourceId, startDateTime, endDateTime }` — owner comes from the JWT |
| GET | `/api/bookings?resourceId=&from=&to=&status=&page=&pageSize=` | own bookings only |
| GET | `/api/bookings/{id}` | owner or admin (others get 404) |
| POST | `/api/bookings/{id}/cancel` | owner or admin |

### Admin

| Method | Route |
|--------|-------|
| GET | `/api/admin/bookings?userId=&resourceId=&from=&to=&status=&page=&pageSize=` |
| GET | `/api/admin/audit-logs?action=&entityType=&actorUserId=&page=&pageSize=` |

### Error codes

| HTTP | Code | Meaning |
|------|------|---------|
| 400 | `VALIDATION_ERROR` | Bad input (details in `errors` when field-level) |
| 400 | `BOOKING_IN_PAST` | Start time before now (1-minute grace) |
| 400 | `INVALID_DURATION` | Outside 15 min – 24 h |
| 400 | `INVALID_TIMESTAMP` | Timestamp not unambiguous UTC (`...Z`) |
| 401 | `UNAUTHORIZED` / `INVALID_CREDENTIALS` | Missing/invalid token, bad login |
| 403 | `FORBIDDEN` | Authenticated but not allowed |
| 404 | `NOT_FOUND` | Missing resource/booking (incl. other users' bookings) |
| 409 | `BOOKING_CONFLICT` | Overlapping booking |
| 409 | `BOOKING_ALREADY_CANCELLED` / `BOOKING_ALREADY_COMPLETED` | Invalid cancel |
| 409 | `RESOURCE_UNAVAILABLE` | Resource disabled / in maintenance |
| 409 | `EMAIL_TAKEN` | Duplicate registration |
| 429 | `RATE_LIMIT_EXCEEDED` | Too many requests |
| 500 | `INTERNAL_ERROR` | Unexpected (internals never leaked) |

---

## Design write-up

### A. How overlaps are defined and enforced, and why

Two bookings overlap when `newStart < existingEnd AND newEnd > existingStart`, using **half-open intervals `[start, end)`**. A booking ending at 11:00 does not conflict with one starting at 11:00 — that is the natural semantics for room bookings (back-to-back meetings must work), and it matches PostgreSQL's default `tstzrange` bounds so the application rule and the database rule are literally the same predicate.

Enforcement is two-layered:

1. **Application pre-check** — an indexed `EXISTS` query (`AnyAsync`) before insert, giving fast, friendly 409s for the common case.
2. **Database exclusion constraint** — the authority:

```sql
ALTER TABLE "Bookings" ADD CONSTRAINT "EX_Bookings_ResourceId_TimeRange"
EXCLUDE USING gist ("ResourceId" WITH =, tstzrange("StartDateTime","EndDateTime") WITH &&)
WHERE ("Status" = 'Active');
```

Only `Active` rows participate, so cancelled bookings free their slot automatically. Bookings are never hard-deleted (soft cancel via status + `cancelledAt`/`cancelledBy`), preserving history and the audit trail. A `Completed` status is *derived at read time* (an Active booking whose end has passed) rather than stored — no background job, and completed past bookings can't conflict with future ones by definition.

### B. Concurrency assumptions

Assume many API instances and many concurrent users targeting the same popular resources. **Application-level validation alone cannot prevent double booking**: two requests can both run the overlap check, both see "no conflict", and both insert — a classic check-then-act (TOCTOU) race. Serializing in-process (locks) fails the moment there is a second API instance.

So correctness lives where the data lives. The GiST exclusion constraint makes conflicting inserts impossible *atomically*, regardless of how many app instances race. The losing transaction receives an exclusion violation (`23P01`, occasionally surfacing as a deadlock under contention), which the service translates to `409 BOOKING_CONFLICT`. The application pre-check is purely a UX optimization; the constraint is the guarantee. This is verified by an integration test that races two overlapping inserts against real Postgres.

**Tradeoffs of the constraint approach**: it requires the `btree_gist` extension (PostgreSQL-specific — porting to another database means reimplementing the guard, e.g. serializable transactions or application locks); GiST index maintenance adds write cost per insert; and the business rule lives partly in the schema, so it must be covered by migrations + integration tests rather than only unit tests. In exchange we get correctness that cannot be bypassed by any code path — including future ones that forget the pre-check.

### C. What breaks first at scale

The first bottleneck is **the single PostgreSQL primary**, specifically write contention on hot resources: every booking insert for the same resource touches the same GiST index region, so a very popular room effectively serializes at the index. Cheaper limits appear earlier: offset pagination (`OFFSET n`) degrades on deep pages (fix: keyset pagination on `(StartDateTime, Id)`); the availability endpoint is read-heavy and uncached because invalidating it correctly on every booking write is non-trivial; and in-process `IMemoryCache` and rate-limiter state don't span instances — the first horizontal scale-out step needs Redis for both.

### D. Evolving into a distributed system

Incrementally, in order of payoff:

1. **Stateless scale-out** — the API is already stateless (JWT); move rate limiting and the resource cache to Redis and run N replicas behind a load balancer. The DB constraint keeps bookings correct with no coordination between instances.
2. **Read/write split** — availability and list queries go to read replicas; writes (a small fraction of traffic) stay on the primary.
3. **Partition by resource** — a booking only ever conflicts within one `resourceId`, a natural shard key. Shard bookings by resource (Citus or app-level routing); the exclusion constraint still holds per shard because a conflict can never span shards.
4. **Events via outbox** — the audit log already records every state change in the same transaction as the change; promote it to a transactional outbox publishing `BookingCreated`/`BookingCancelled` events to a broker for notifications, projections, and analytics. This is also the path to event sourcing without a rewrite.

### E. Which tradeoff I prioritized

**Correctness.** A booking system that occasionally double-books is worthless regardless of speed, so the invariant is enforced at the database and everything else works around that choice: soft-deletes preserve truth, timestamps are rejected unless unambiguous UTC, ownership filters live in the query itself rather than post-hoc checks. Performance is addressed where it's cheap and doesn't threaten correctness — indexes (including a partial index on active bookings), `EXISTS` conflict checks instead of loading rows into memory, `AsNoTracking` reads, pagination, summary DTOs on lists, and caching only for resources, whose staleness is harmless. Simplicity was spent deliberately: auth, auditing, and rate limiting add real complexity, but they are what make the service production-shaped rather than a CRUD demo.

### Extension task: Option 1 — Concurrency

Chosen because double booking is the one failure mode that silently corrupts the domain (two people in one room) and cannot be retrofitted by adding code later — it has to be a schema-level decision. The race, the guard, and its tradeoffs are described in section B; the proof is `BookingConcurrencyTests` (two simultaneous overlapping requests → exactly one success, one 409, exactly one Active row in the DB). Audit logging, the availability query, and JWT auth were additionally implemented as production-readiness features, but the concurrency guard is the extension submission.

---

## Security notes

- **JWT auth** (60-min expiry, HMAC-SHA256). The booking owner is always taken from the token — `userId` in a request body is rejected by design, so users cannot create bookings on others' behalf.
- **Authorization**: the `User` role manages only its own bookings; `Admin` sees/cancels everything and owns resource CRUD. Enforced via an `AdminOnly` policy plus ownership filters inside the queries.
- **IDOR**: fetching or cancelling another user's booking returns **404, not 403**, so booking IDs cannot be probed for existence.
- **Rate limiting** (per-user when authenticated, per-IP otherwise): auth 5/min/IP (brute force), booking writes 10/min/user (spam and double-click floods), reads 100/min/user (DB-heavy queries), global 200/min/IP. Limits are configurable in `appsettings.json`; exceeding one returns 429 in the standard error shape.
- **Input validation** at three layers: DataAnnotations (shape), `BookingRules` (domain: UTC-only timestamps, 15 min–24 h duration, no past bookings with a 1-minute clock-skew grace), and the DB constraint (concurrency).
- **Error hygiene**: unhandled exceptions are logged server-side in full; clients only ever see `{ "code": "INTERNAL_ERROR", ... }` — no SQL, no stack traces.
- **Audit log**: append-only record of every mutation (actor, action, old/new JSON snapshots, IP), written **in the same transaction** as the change so log and data cannot diverge. Failed logins are audited too.
- Passwords are hashed with ASP.NET Identity's `PasswordHasher` (salted PBKDF2). Login failures return the same error for unknown email vs. wrong password (no account enumeration).

## Assumptions

- Bookings are minute-scale office reservations: minimum 15 minutes, maximum 24 hours (constants in `Validation/BookingRules.cs`).
- All API timestamps are UTC; values without an explicit UTC marker are rejected (`INVALID_TIMESTAMP`) rather than silently reinterpreted. The frontend converts local input to UTC and renders in the viewer's local time.
- Logout is client-side token disposal — acceptable with 60-minute tokens; a server-side deny-list would be the next step if instant revocation were required.
- Availability searches are capped at 31 days and never offer slots in the past.
- Resources change rarely → the resource list is cached in memory for 5 minutes and invalidated on every resource write. Availability is deliberately **not** cached: it changes with every booking, and a stale answer causes user-visible conflicts.
- The admin account is seeded at startup (configurable via env vars) since there is no user-management UI.
