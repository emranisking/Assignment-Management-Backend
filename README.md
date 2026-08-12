# Assignment Management API

A layered ASP.NET Core (.NET 8) Web API for managing courses, classes, enrollments, assignments, submissions, grading, and result publication. Built with PostgreSQL + EF Core, JWT auth (Admin / Teacher / Student), Redis-backed caching of read endpoints, and asynchronous enrollment processing over RabbitMQ with concurrency protection.

> Note: this code was written without being compiled in the authoring environment (no .NET SDK / NuGet access there). It is structured for a normal `dotnet restore` / `docker compose` build on your machine. If a package restore surfaces a version nit, the pinned versions are all real, mutually compatible .NET 8 releases; adjust only if your local feed differs.

## Contents

- [Architecture](#architecture)
- [Requirements](#requirements)
- [Run with Docker (recommended)](#run-with-docker-recommended)
- [Run locally without Docker](#run-locally-without-docker)
- [Seeded accounts](#seeded-accounts)
- [Configuration reference](#configuration-reference)
- [How the core mechanics work](#how-the-core-mechanics-work)
- [Tests](#tests)
- [API documentation](#api-documentation)
- [Notes and known limitations](#notes-and-known-limitations)

## Architecture

Six projects, dependencies pointing inward:

```
AssignmentManagement.API             ASP.NET Core host: controllers, middleware, DI wiring, Swagger
AssignmentManagement.Application     Use cases: services, DTOs, interfaces (no infra dependencies)
AssignmentManagement.Domain          Entities and enums
AssignmentManagement.Infrastructure  EF Core, PostgreSQL, Redis, RabbitMQ, JWT, file storage
AssignmentManagement.Common          Cross-cutting: ApiResponse envelope, exceptions, constants
AssignmentManagement.Tests           xUnit unit tests (InMemory provider, Moq)
```

- API depends on Application + Infrastructure.
- Application depends on Domain + Common, plus EF Core abstractions only (it talks to the database through an `IAppDbContext` interface, not the concrete `DbContext`).
- Infrastructure depends on Application + Domain + Common and provides the concrete implementations.
- Domain and Common have no project dependencies.

Requests flow: controller -> application service -> `IAppDbContext` / infrastructure services -> PostgreSQL. Every response is wrapped in a consistent `ApiResponse<T>` envelope, and all thrown `AppException`s are converted to a structured `ErrorResponse` by a single exception middleware.

## Requirements

- To run with Docker: Docker + Docker Compose.
- To run locally: .NET 8 SDK, plus PostgreSQL 16, and optionally Redis 7 and RabbitMQ 3.13 (both are optional; see below).

## Run with Docker (recommended)

From the repository root:

```bash
docker compose up --build
```

This starts four containers: PostgreSQL, Redis, RabbitMQ (with its management UI), and the API. The API waits for the databases to report healthy, creates the schema, and seeds demo data on first boot.

- API base URL: `http://localhost:8080`
- Swagger UI: `http://localhost:8080/swagger`
- Health check: `http://localhost:8080/health`
- RabbitMQ management UI: `http://localhost:15672` (user `guest`, password `guest`)

Stop and remove everything, including volumes:

```bash
docker compose down -v
```

## Run locally without Docker

1. Start PostgreSQL and create the database (matching the default connection string, or point the env var at your own):

   ```bash
   createdb assignment_management
   ```

2. From the repository root, restore and build:

   ```bash
   dotnet restore
   dotnet build
   ```

3. Run the API:

   ```bash
   dotnet run --project AssignmentManagement.API
   ```

   The API listens on `http://localhost:5080` (see `Properties/launchSettings.json`). Swagger is at `http://localhost:5080/swagger`.

Redis and RabbitMQ are optional locally:

- If Redis is unreachable, caching degrades gracefully and every read simply hits the database. To skip it explicitly, set `Cache__Enabled=false`.
- If RabbitMQ is disabled (`RabbitMq__Enabled=false`), enrollment requests are processed inline and synchronously instead of via the worker, so the full enrollment flow still works end to end without a broker.

Configuration is read from `appsettings.json`, overridden by environment variables (double-underscore syntax). `.env.example` lists every variable for local runs.

## Seeded accounts

Seeded on first startup. Passwords are examples for local use; change them for anything real.

| Role    | Email                  | Password     |
|---------|------------------------|--------------|
| Admin   | admin@example.com      | Admin@123    |
| Teacher | teacher@example.com    | Teacher@123  |
| Student | student@example.com    | Student@123  |
| Student | student2@example.com   | Student@123  |

Also seeded: one course (`CSE101`) and one class (`Section A`) with capacity 2, so the "class full" path is easy to demonstrate with the two student accounts.

## Configuration reference

| Variable | Default (local) | Purpose |
|---|---|---|
| `ConnectionStrings__Postgres` | `Host=localhost;Port=5432;Database=assignment_management;Username=postgres;Password=postgres` | PostgreSQL connection string |
| `Jwt__Secret` | placeholder (change it) | HMAC signing key, must be at least 32 chars |
| `Jwt__Issuer` | `AssignmentManagement` | JWT issuer |
| `Jwt__Audience` | `AssignmentManagement.Client` | JWT audience |
| `Jwt__ExpiryMinutes` | `120` | Access token lifetime |
| `Redis__ConnectionString` | `localhost:6379` | Redis endpoint |
| `Cache__Enabled` | `true` | Master switch for read caching |
| `Cache__ExpirySeconds` | `60` | Global cache TTL in seconds |
| `RabbitMq__Enabled` | `true` | Master switch for async enrollment |
| `RabbitMq__Host` | `localhost` | Broker host |
| `RabbitMq__Port` | `5672` | Broker AMQP port |
| `RabbitMq__Username` / `RabbitMq__Password` | `guest` / `guest` | Broker credentials |
| `Enrollment__UseAsyncProcessing` | `true` | Force inline processing when `false`; auto-forced off if RabbitMq is disabled |
| `Storage__RootPath` | `storage` | Root folder for submitted PDF files |

## How the core mechanics work

### Authentication and authorization

JWT bearer tokens carry the user id, email, and role. Authorization is enforced at two levels: role gates via `[Authorize(Roles = ...)]` on controllers/actions, and resource ownership checks inside services (a teacher can only touch classes they are assigned to, a student can only read their own submissions and results, and so on).

### Enrollment concurrency

Capacity and the enrolled count live on the `Class` row. When a student requests enrollment, an `EnrollmentRequest` row is created with `Pending` status and the API returns `202 Accepted`; the caller polls the request to see the outcome. Processing (whether via the RabbitMQ worker or inline) is protected on three independent levels so a class can never oversell:

1. A pessimistic `SELECT ... FOR UPDATE` lock on the `Class` row serializes concurrent processing for the same class.
2. A `UNIQUE(StudentId, ClassId)` constraint on `Enrollment` makes duplicate enrollment impossible at the database level.
3. The processor is idempotent: it checks request status first, so redelivered messages do not double-apply.

Rejections carry a reason (`Class is full.` or `You are already enrolled in this class.`) back on the request.

### Caching

Read endpoints use `ICacheService.GetOrSetAsync` with a global TTL. Cache entries are grouped, and writes bump a version stamp for the affected group, which invalidates stale reads without needing to track individual keys. If Redis is down, the service falls back to hitting the source directly.

### Submissions and resubmissions

Submissions accept a single PDF up to 15 MB. Each upload is stored as a new version under the submission, so history is preserved. After grading, a student cannot upload again unless a resubmission request is approved by a teacher or admin, which reopens the submission for exactly one new version.

## Tests

```bash
dotnet test
```

The test project uses the EF Core InMemory provider and Moq. Coverage focuses on the parts where logic mistakes are costly: password hashing, JWT claim generation, course creation rules, and the enrollment processor (capacity rejection, duplicate rejection, and idempotency on already-processed requests). The enrollment processor skips the raw `FOR UPDATE` SQL when running on a non-relational provider, so these paths are exercised without a real database.

## API documentation

- `API_ENDPOINTS.md` documents every endpoint: method, path, required role, request body, and a curl example.
- `AssignmentManagement.postman_collection.json` is an importable Postman collection. Import it, set the `baseUrl` variable (`http://localhost:8080` for Docker or `http://localhost:5080` for local), and run the login request first; it saves the returned token into a collection variable that the other requests reuse automatically.

## Notes and known limitations

- Schema is created with EF Core `EnsureCreated()` plus seeding, which is convenient for demos but is not a migration history. For a real deployment, replace it with `dotnet ef migrations add InitialCreate` and `Database.Migrate()`.
- CORS is open to all origins for ease of local testing. Lock this down before deploying.
- File storage is local disk under `Storage__RootPath`. Swap `IFileStorageService` for a cloud implementation if you need durable or shared storage.
- The default JWT secret and seeded passwords are placeholders. Replace them outside local development.
