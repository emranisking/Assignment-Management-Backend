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
- [RabbitMQ integration](#rabbitmq-integration)
- [PostgreSQL pessimistic locking](#postgresql-pessimistic-locking)
- [Duplicate enrollment protection](#duplicate-enrollment-protection)
- [Enrollment idempotency](#enrollment-idempotency)
- [Redis caching](#redis-caching)
- [Redis cache invalidation](#redis-cache-invalidation)
- [RabbitMQ + PostgreSQL + Redis together](#rabbitmq--postgresql--redis-together)
- [High-concurrency enrollment example](#high-concurrency-enrollment-example)
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

````

- API depends on Application + Infrastructure.
- Application depends on Domain + Common, plus EF Core abstractions only (it talks to the database through an `IAppDbContext` interface, not the concrete `DbContext`).
- Infrastructure depends on Application + Domain + Common and provides the concrete implementations.
- Domain and Common have no project dependencies.

Requests flow: controller -> application service -> `IAppDbContext` / infrastructure services -> PostgreSQL. Every response is wrapped in a consistent `ApiResponse<T>` envelope, and all thrown `AppException`s are converted to a structured `ErrorResponse` by a single exception middleware.

The backend follows a layered architecture with separation of concerns:

```text
AssignmentManagement.API
        |
        v
AssignmentManagement.Application
        |
        v
AssignmentManagement.Domain
        ^
        |
AssignmentManagement.Infrastructure
        |
        v
PostgreSQL / Redis / RabbitMQ / File Storage
````

The `AssignmentManagement.Common` project provides cross-cutting components shared across the application, including:

```text
Common
├── Base services
├── Pagination models
├── API response models
├── Constants
├── Exceptions
└── Other shared models/utilities
```

Authentication and authorization are implemented using JWT Bearer authentication.

```text
Client
   |
   | Login
   v
Authentication API
   |
   | JWT
   v
Client
   |
   | Authorization: Bearer <token>
   v
ASP.NET Core API
   |
   +--> Role Authorization
   |
   +--> Resource-level authorization
   |
   v
Application Services
```

Supported roles:

```text
Admin
Teacher
Student
```

Role-based authorization is enforced through ASP.NET Core authorization attributes and resource-level checks inside application services.

## Requirements

* To run with Docker: Docker + Docker Compose.
* To run locally: .NET 8 SDK, plus PostgreSQL 16, and optionally Redis 7 and RabbitMQ 3.13 (both are optional; see below).

## Run with Docker (recommended)

From the repository root:

```bash
docker compose up --build
```

This starts four containers: PostgreSQL, Redis, RabbitMQ (with its management UI), and the API. The API waits for the databases to report healthy, creates the schema, and seeds demo data on first boot.

* API base URL: `http://localhost:8080`
* Swagger UI: `http://localhost:8080/swagger`
* Health check: `http://localhost:8080/health`
* RabbitMQ management UI: `http://localhost:15672` (user `guest`, password `guest`)

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

* If Redis is unreachable, caching degrades gracefully and every read simply hits the database. To skip it explicitly, set `Cache__Enabled=false`.
* If RabbitMQ is disabled (`RabbitMq__Enabled=false`), enrollment requests are processed inline and synchronously instead of via the worker, so the full enrollment flow still works end to end without a broker.

## Seeded accounts

Seeded on first startup. Passwords are examples for local use; change them for anything real.

| Role    | Email                                               | Password    |
| ------- | --------------------------------------------------- | ----------- |
| Admin   | [admin@example.com](mailto:admin@example.com)       | Admin@123   |
| Teacher | [teacher@example.com](mailto:teacher@example.com)   | Teacher@123 |
| Student | [student@example.com](mailto:student@example.com)   | Student@123 |
| Student | [student2@example.com](mailto:student2@example.com) | Student@123 |

Also seeded: one course (`CSE101`) and one class (`Section A`) with capacity 2, so the "class full" path is easy to demonstrate with the two student accounts.

## Configuration reference

| Variable                                    | Default (local)                                                                               | Purpose                                                                       |
| ------------------------------------------- | --------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| `ConnectionStrings__Postgres`               | `Host=localhost;Port=5432;Database=assignment_management;Username=postgres;Password=postgres` | PostgreSQL connection string                                                  |
| `Jwt__Secret`                               | placeholder (change it)                                                                       | HMAC signing key, must be at least 32 chars                                   |
| `Jwt__Issuer`                               | `AssignmentManagement`                                                                        | JWT issuer                                                                    |
| `Jwt__Audience`                             | `AssignmentManagement.Client`                                                                 | JWT audience                                                                  |
| `Jwt__ExpiryMinutes`                        | `120`                                                                                         | Access token lifetime                                                         |
| `Redis__ConnectionString`                   | `localhost:6379`                                                                              | Redis endpoint                                                                |
| `Cache__Enabled`                            | `true`                                                                                        | Master switch for read caching                                                |
| `Cache__ExpirySeconds`                      | `60`                                                                                          | Global cache TTL in seconds                                                   |
| `RabbitMq__Enabled`                         | `true`                                                                                        | Master switch for async enrollment                                            |
| `RabbitMq__Host`                            | `localhost`                                                                                   | Broker host                                                                   |
| `RabbitMq__Port`                            | `5672`                                                                                        | Broker AMQP port                                                              |
| `RabbitMq__Username` / `RabbitMq__Password` | `guest` / `guest`                                                                             | Broker credentials                                                            |
| `Enrollment__UseAsyncProcessing`            | `true`                                                                                        | Force inline processing when `false`; auto-forced off if RabbitMq is disabled |
| `Storage__RootPath`                         | `storage`                                                                                     | Root folder for submitted PDF files                                           |

## How the core mechanics work

### Authentication and authorization

JWT bearer tokens carry the user id, email, and role. Authorization is enforced at two levels: role gates via `[Authorize(Roles = ...)]` on controllers/actions, and resource ownership checks inside services (a teacher can only touch classes they are assigned to, a student can only read their own submissions and results, and so on).

The authentication flow is:

```text
POST /api/auth/login
        |
        v
Validate email/password
        |
        v
Generate JWT
        |
        v
Return access token
        |
        v
Client stores token
        |
        v
Authorization: Bearer <JWT>
        |
        v
ASP.NET Core JWT middleware
        |
        +--> Validate signature
        +--> Validate issuer
        +--> Validate audience
        +--> Validate expiration
        +--> Read user/role claims
        |
        v
Controller authorization
        |
        v
Application service
```

JWT is responsible for authentication and role-based access control. Business/resource authorization is still performed inside application services where necessary.

For example, a teacher may have the `Teacher` role but still cannot modify an assignment belonging to another teacher's class.

### Enrollment concurrency

Capacity and the enrolled count live on the `Class` row. When a student requests enrollment, an `EnrollmentRequest` row is created with `Pending` status and the API returns `202 Accepted`; the caller polls the request to see the outcome. Processing (whether via the RabbitMQ worker or inline) is protected on three independent levels so a class can never oversell:

1. A pessimistic `SELECT ... FOR UPDATE` lock on the `Class` row serializes concurrent processing for the same class.
2. A `UNIQUE(StudentId, ClassId)` constraint on `Enrollment` makes duplicate enrollment impossible.
3. The processor is idempotent: it checks request status first, so redelivered messages do not double-apply.

Rejections carry a reason (`Class is full.` or `You are already enrolled in this class.`) back on the request.

The complete enrollment flow is:

```text
Student
   |
   | POST /api/enrollment-requests
   v
ASP.NET Core API
   |
   | Validate request
   | Create EnrollmentRequest
   | Status = Pending
   |
   v
RabbitMQ
   |
   | enrollment-requests queue
   v
EnrollmentConsumer
   |
   v
Enrollment Processor
   |
   | BEGIN TRANSACTION
   |
   | SELECT Class ... FOR UPDATE
   |
   | Check duplicate enrollment
   |
   | Check class capacity
   |
   | Create Enrollment
   |
   | Update enrolled count
   |
   | Update EnrollmentRequest
   |
   | COMMIT
   |
   v
Student polls request status
```

The important architectural separation is:

```text
RabbitMQ
    |
    +--> Asynchronous processing
```

while:

```text
PostgreSQL
    |
    +--> Source of truth
    +--> Transaction management
    +--> Capacity enforcement
    +--> Duplicate protection
```

### Why RabbitMQ does not handle concurrency by itself

RabbitMQ is responsible for transporting and buffering messages. It does not decide whether a class has an available seat.

For example, two messages may arrive:

```text
EnrollmentRequest A
EnrollmentRequest B
```

RabbitMQ can deliver both messages to workers.

The database is responsible for deciding:

```text
Is there still a seat?
Is this student already enrolled?
Can the enrollment be committed?
```

Therefore the architecture intentionally separates messaging from data consistency.

### Enrollment request states

An enrollment request follows a simple lifecycle:

```text
Pending
   |
   +----> Approved
   |
   +----> Rejected
```

A rejected request contains a reason.

Examples:

```text
Class is full.
```

or:

```text
You are already enrolled in this class.
```

The HTTP API returns quickly with `202 Accepted`, while the actual enrollment operation can complete asynchronously.

---

# RabbitMQ integration

RabbitMQ is used for asynchronous enrollment processing.

The purpose of RabbitMQ in this system is to prevent the HTTP request from directly performing the potentially contended enrollment transaction.

Instead:

```text
HTTP Request
     |
     v
Create EnrollmentRequest
     |
     v
Publish Message
     |
     v
RabbitMQ Queue
     |
     v
Background Consumer
     |
     v
Database Transaction
```

This provides a buffer between incoming enrollment requests and database processing.

## RabbitMQ components

The infrastructure contains three important responsibilities:

```text
IMessagePublisher
       |
       v
RabbitMqPublisher
       |
       v
RabbitMQ
       |
       v
EnrollmentConsumer
       |
       v
Enrollment Processor
```

### `IMessagePublisher`

The application uses an abstraction:

```csharp
IMessagePublisher
```

instead of directly depending on RabbitMQ.

This keeps the application layer independent of the messaging technology.

Conceptually:

```text
Application
     |
     v
IMessagePublisher
     |
     v
RabbitMqPublisher
     |
     v
RabbitMQ
```

When RabbitMQ is disabled, the application can use:

```text
NoOpMessagePublisher
```

and process the enrollment synchronously.

### RabbitMQ queue

Enrollment messages are published to:

```text
enrollment-requests
```

The queue contains messages representing enrollment requests.

Conceptually, a message contains information such as:

```json
{
  "requestId": 123,
  "studentId": 5,
  "classId": 10
}
```

The exact message contract is defined by the application's enrollment message model.

## RabbitMQ producer flow

When a student requests enrollment:

```text
Student
   |
   | POST /api/enrollment-requests
   v
Enrollment Service
   |
   +--> Validate student
   +--> Validate class
   +--> Create EnrollmentRequest
   |       Status = Pending
   |
   +--> Publish EnrollmentRequest message
             |
             v
          RabbitMQ
```

The API does not wait for the complete enrollment transaction.

It returns:

```http
202 Accepted
```

The response contains the enrollment request identifier.

Example:

```json
{
  "success": true,
  "message": "Enrollment request accepted.",
  "data": {
    "requestId": 123,
    "status": "Pending"
  }
}
```

The client can then check:

```http
GET /api/enrollment-requests/123
```

to determine whether the request was approved or rejected.

## RabbitMQ consumer

`EnrollmentConsumer` is a hosted background service.

When the application starts and RabbitMQ is enabled:

```text
ASP.NET Core
      |
      v
EnrollmentConsumer
      |
      v
Connect to RabbitMQ
      |
      v
Listen to enrollment-requests
```

When a message arrives:

```text
RabbitMQ
   |
   | EnrollmentRequest message
   v
EnrollmentConsumer
   |
   v
Enrollment Processor
   |
   v
PostgreSQL
```

The consumer is responsible for receiving the message. The business rules remain in the enrollment processing layer.

This prevents RabbitMQ-specific code from being mixed with the actual enrollment business logic.

## RabbitMQ and worker concurrency

RabbitMQ allows multiple messages to be processed by workers, but database concurrency still needs to be controlled.

For example:

```text
100 enrollment requests
        |
        v
RabbitMQ
        |
        +---- Worker 1
        +---- Worker 2
        +---- Worker 3
        +---- ...
```

If many students are attempting to enroll in the same class, the workers may compete for the same database row.

PostgreSQL pessimistic locking handles this contention.

The important point is:

```text
RabbitMQ controls asynchronous work distribution.

PostgreSQL controls business-data consistency.
```

## RabbitMQ failure behavior

RabbitMQ is an infrastructure component, not the source of truth.

If RabbitMQ is disabled:

```text
RabbitMq__Enabled=false
```

the system can process enrollment synchronously.

The flow becomes:

```text
Student
   |
   v
API
   |
   v
Enrollment Processor
   |
   v
PostgreSQL
```

This makes local development possible without requiring RabbitMQ.

When RabbitMQ is enabled:

```text
Student
   |
   v
API
   |
   v
RabbitMQ
   |
   v
EnrollmentConsumer
   |
   v
PostgreSQL
```

---

# PostgreSQL pessimistic locking

RabbitMQ provides asynchronous processing, but it does not solve database concurrency.

The most important concurrency problem is class capacity.

For example:

```text
Class capacity = 40
Current enrollment = 39
```

Suppose two students attempt to enroll at exactly the same time.

Without locking:

```text
Worker A -> reads 39
Worker B -> reads 39

Worker A -> inserts enrollment
Worker B -> inserts enrollment

Result = 41 students
```

This violates the class capacity.

To prevent this, the enrollment processor uses a PostgreSQL row-level pessimistic lock.

## `SELECT ... FOR UPDATE`

Inside a database transaction, the processor locks the specific class row.

Conceptually:

```sql
BEGIN;

SELECT *
FROM classes
WHERE id = @classId
FOR UPDATE;
```

The `FOR UPDATE` clause locks the selected class row until the transaction finishes.

The processor can then safely check the capacity:

```text
Class capacity = 40
Enrolled count = 39

39 < 40
    |
    v
Allow enrollment
```

After inserting the enrollment:

```sql
INSERT INTO enrollments (...);

UPDATE classes
SET enrolled_count = enrolled_count + 1
WHERE id = @classId;

COMMIT;
```

The transaction releases the lock.

## Why the lock is placed on the Class row

The lock is intentionally applied to the specific class being enrolled in.

For example:

```text
Class 101
```

is locked while its capacity is checked and updated.

Enrollment for another class can continue:

```text
Worker A -> Class 101 -> locked
Worker B -> Class 202 -> can continue
```

We do not lock the entire enrollment table.

This keeps contention limited to the specific class that is experiencing concurrent enrollment.

## Concurrent enrollment example

Assume:

```text
Capacity = 1
Current enrollment = 0
```

Two students request enrollment:

```text
Student A
Student B
```

RabbitMQ may contain:

```text
Message A
Message B
```

Worker A:

```text
BEGIN
   |
   v
LOCK Class #10
   |
   v
Read capacity
   |
   v
0 / 1
   |
   v
Insert Student A
   |
   v
Update enrolled count -> 1
   |
   v
COMMIT
```

Worker B:

```text
BEGIN
   |
   v
LOCK Class #10
   |
   v
WAIT
```

Worker B waits because Worker A currently owns the lock.

After Worker A commits:

```text
Worker B obtains lock
        |
        v
Read class
        |
        v
1 / 1
        |
        v
Class is full
        |
        v
Reject enrollment
```

The final result is:

```text
Capacity = 1
Successful enrollments = 1
Rejected enrollments = 1
```

The class cannot be oversold.

---

# Duplicate enrollment protection

Pessimistic locking protects class capacity, but we also need protection against the same student enrolling in the same class more than once.

The `Enrollment` table uses a database-level unique constraint:

```text
UNIQUE(StudentId, ClassId)
```

For example:

```text
StudentId | ClassId
----------|--------
5         | 101
```

The following second enrollment is invalid:

```text
StudentId | ClassId
----------|--------
5         | 101
```

The database rejects the duplicate.

This is important because duplicate requests can happen through several mechanisms:

```text
Student double-clicks Enroll
        |
        v
Two HTTP requests
```

or:

```text
Two messages published
        |
        v
RabbitMQ
```

or:

```text
Message redelivery
        |
        v
Same request processed again
```

The database remains the final protection against duplicate enrollment.

---

# Enrollment idempotency

RabbitMQ-based systems should assume that messages can potentially be delivered more than once.

For example:

```text
Message #123
    |
    v
EnrollmentConsumer
    |
    v
Processing
```

If the consumer crashes or a message is redelivered, the same enrollment request could be received again.

Therefore, the enrollment processor checks the `EnrollmentRequest` status before processing.

For example:

```text
EnrollmentRequest #123

Status = Approved
```

If the same message arrives again:

```text
Already processed
      |
      v
Do nothing
```

This prevents the same request from being applied multiple times.

The enrollment system therefore has three independent protection mechanisms:

```text
                   Enrollment
                       |
       +---------------+---------------+
       |               |               |
       v               v               v
 Pessimistic        Unique         Idempotent
    Lock           Constraint        Request
       |               |               |
       v               v               v
 Capacity          Duplicate       Message
 protection        protection      protection
```

These mechanisms solve different failure scenarios and are intentionally used together.

---

# Redis caching

Redis is used as a distributed cache for frequently accessed read endpoints.

The database remains the source of truth.

```text
PostgreSQL
    |
    | Source of truth
    v
Application
    |
    v
Redis
    |
    | Cached read data
    v
API response
```

Redis is not used to permanently store business data.

If Redis is deleted or becomes unavailable, the application can still retrieve the data from PostgreSQL.

## Cache-aside pattern

The application uses:

```text
ICacheService
```

for caching.

The basic flow is:

```text
Request
   |
   v
Check Redis
   |
   +------------------+
   |                  |
   v                  v
Cache HIT          Cache MISS
   |                  |
   v                  v
Return data       PostgreSQL
                      |
                      v
                  Store in Redis
                      |
                      v
                  Return data
```

### Cache hit

Example:

```text
GET /api/courses
        |
        v
      Redis
        |
        v
    Cache HIT
        |
        v
    Return data
```

PostgreSQL does not need to be queried.

### Cache miss

Example:

```text
GET /api/courses
        |
        v
      Redis
        |
       MISS
        |
        v
   PostgreSQL
        |
        v
    Store result
        |
        v
      Redis
        |
        v
    Return data
```

This reduces database reads for frequently requested resources.

---

# Redis cache expiration

Cache entries have a configurable TTL.

The default value is:

```text
Cache__ExpirySeconds=60
```

For example:

```text
10:00:00
GET /api/courses
        |
        v
Redis MISS
        |
        v
PostgreSQL
        |
        v
Redis SET
TTL = 60 seconds
```

After the TTL expires:

```text
10:01:00
```

the cache entry is no longer valid.

The next request queries PostgreSQL and creates a fresh cache entry.

---

# Redis cache invalidation

Caching introduces a consistency problem.

For example:

```text
GET /api/courses
```

returns:

```text
CSE101
CSE102
```

and the result is cached.

An administrator then creates:

```text
CSE103
```

If the old cache is still used, the API could return:

```text
CSE101
CSE102
```

without:

```text
CSE103
```

To handle this, the application uses cache grouping/versioning.

Conceptually:

```text
Course cache group
       |
       v
Version = 1
```

Cached key:

```text
courses:list:v1
```

When a course changes:

```text
Create / Update / Delete Course
             |
             v
Increment cache version
             |
             v
Version = 2
```

The next read uses:

```text
courses:list:v2
```

Therefore the previous cached result is no longer used.

This avoids having to track and delete every individual cache key.

---

# Redis failure handling

Redis is an optimization, not the source of truth.

If Redis is unavailable:

```text
API
 |
 v
Redis
 |
 X Connection failure
 |
 v
PostgreSQL
 |
 v
Response
```

The application continues operating using the database.

Therefore:

```text
Redis available
    -> faster reads

Redis unavailable
    -> database reads

PostgreSQL unavailable
    -> application cannot retrieve authoritative data
```

This is intentional.

PostgreSQL contains the authoritative business data.

---

# RabbitMQ + PostgreSQL + Redis together

RabbitMQ, PostgreSQL, and Redis solve different problems.

They are not interchangeable.

```text
                    +----------------+
                    |    Student     |
                    +-------+--------+
                            |
                            | Enrollment Request
                            v
                    +---------------+
                    | ASP.NET Core  |
                    |      API      |
                    +-------+-------+
                            |
                            v
                    +---------------+
                    |   RabbitMQ    |
                    |     Queue     |
                    +-------+-------+
                            |
                            v
                    +---------------+
                    |  Enrollment   |
                    |   Consumer    |
                    +-------+-------+
                            |
                            | Transaction
                            v
                    +---------------+
                    |  PostgreSQL   |
                    |               |
                    | FOR UPDATE    |
                    | Unique Key    |
                    | Enrollment    |
                    +---------------+
```

For read operations:

```text
Client
   |
   v
ASP.NET Core API
   |
   v
Redis
   |
   +---- Cache HIT ----> Return response
   |
   +---- Cache MISS
             |
             v
        PostgreSQL
             |
             v
        Store in Redis
             |
             v
        Return response
```

The responsibilities are therefore:

```text
RabbitMQ
    |
    +--> Asynchronous processing
    +--> Queueing
    +--> Buffering workload


PostgreSQL
    |
    +--> Source of truth
    +--> Transactions
    +--> Pessimistic locking
    +--> Capacity enforcement
    +--> Unique constraints
    +--> Enrollment state


Redis
    |
    +--> Read caching
    +--> Reduce database load
    +--> Faster responses
    +--> TTL-based expiration
    +--> Cache versioning/invalidation
```

---

# High-concurrency enrollment example

Consider:

```text
Course: CSE101
Class: Section A
Capacity: 2
Current enrollment: 0
```

Three students attempt to enroll at approximately the same time:

```text
Student A
Student B
Student C
```

The requests are accepted by the API:

```text
Student A ──+
Student B ──+----> ASP.NET Core API
Student C ──+
```

The API creates enrollment requests:

```text
Request A -> Pending
Request B -> Pending
Request C -> Pending
```

Messages are then published:

```text
Request A
Request B
Request C
     |
     v
RabbitMQ
```

Workers process the messages.

### Student A

```text
BEGIN
   |
   v
LOCK Class A
   |
   v
0 / 2 seats used
   |
   v
Student A is not enrolled
   |
   v
Create Enrollment
   |
   v
1 / 2 seats used
   |
   v
Request A = Approved
   |
   v
COMMIT
```

### Student B

```text
BEGIN
   |
   v
LOCK Class A
   |
   v
1 / 2 seats used
   |
   v
Student B is not enrolled
   |
   v
Create Enrollment
   |
   v
2 / 2 seats used
   |
   v
Request B = Approved
   |
   v
COMMIT
```

### Student C

```text
BEGIN
   |
   v
LOCK Class A
   |
   v
2 / 2 seats used
   |
   v
Class is full
   |
   v
Request C = Rejected
   |
   v
ROLLBACK
```

Final state:

```text
Student A -> Approved
Student B -> Approved
Student C -> Rejected

Capacity = 2
Enrollment count = 2
```

The system never creates more enrollments than the available capacity.

---

# Why all three technologies are used

The architecture intentionally assigns one responsibility to each component.

### RabbitMQ answers:

> "When and where should the enrollment work be processed?"

```text
API
 |
 v
RabbitMQ
 |
 v
Consumer
```

### PostgreSQL pessimistic locking answers:

> "Can this enrollment safely happen right now?"

```text
BEGIN
 |
 v
Lock Class
 |
 v
Check capacity
 |
 v
Create enrollment
 |
 v
COMMIT
```

### Unique constraint answers:

> "Can this student enroll in the same class twice?"

```text
UNIQUE(StudentId, ClassId)
```

### Idempotency answers:

> "What happens if the same enrollment request is processed again?"

```text
Already Approved/Rejected
        |
        v
Do not process again
```

### Redis answers:

> "Can this frequently requested read be served without querying PostgreSQL?"

```text
Redis HIT
   |
   v
Return cached data
```

Together:

```text
                  Assignment Management System
                              |
          +-------------------+-------------------+
          |                   |                   |
          v                   v                   v
      RabbitMQ            PostgreSQL            Redis
          |                   |                   |
          v                   v                   v
   Async processing       Data integrity       Fast reads
   Queue/buffering        Transactions         Cache-aside
                          Row locking           TTL
                          Unique keys            Versioning
```

This separation keeps:

```text
RabbitMQ
    = asynchronous work

PostgreSQL
    = correctness and source of truth

Redis
    = performance optimization
```

---

# Submissions and resubmissions

Submissions accept a single PDF up to 15 MB. Each upload is stored as a new version under the submission, so history is preserved. After grading, a student cannot upload again unless a resubmission request is approved by a teacher or admin, which reopens the submission for exactly one new version.

The submission flow is:

```text
Student
   |
   v
Published Assignment
   |
   v
Upload PDF
   |
   v
Submission Version 1
   |
   +---- Before deadline
   |         |
   |         v
   |      Normal
   |
   +---- After deadline
             |
             v
           Late
```

If the student needs to submit again:

```text
Student
   |
   v
Request Resubmission
   |
   v
Teacher/Admin
   |
   +---- Reject
   |
   +---- Approve
             |
             v
       New submission version
```

The system preserves the previous submission version instead of destroying the historical record.

---

# Assignment lifecycle

Assignments follow a controlled lifecycle:

```text
Draft
  |
  | Publish
  v
Published
```

Only the assigned teacher can create and manage assignments for their class.

The backend performs resource-level authorization in addition to role-based authorization.

For example:

```text
Teacher A
    |
    +--> Class A
            |
            +--> Assignment 1
```

Teacher A can manage Assignment 1.

But:

```text
Teacher B
    |
    X
    |
    +--> Assignment 1
```

is rejected even though Teacher B has the `Teacher` role.

---

# Result publication

Teachers can grade submissions with:

```text
Marks
Feedback
```

Students cannot see marks before the result is published.

The result lifecycle is:

```text
Student submits
      |
      v
Teacher reviews
      |
      v
Teacher grades
      |
      v
Marks + feedback stored
      |
      v
Teacher publishes result
      |
      v
Student can see result
```

This separates grading from result visibility.

Before publication:

```text
Student:

Submission: Submitted
Result: Pending
Marks: Hidden
Feedback: Hidden
```

After publication:

```text
Student:

Submission: Graded
Marks: 85 / 100
Feedback: Good work.
```

---

## Tests

```bash
dotnet test
```

The test project uses the EF Core InMemory provider and Moq. Coverage focuses on the parts where logic mistakes are costly: password hashing, JWT claim generation, course creation rules, and the enrollment processor (capacity rejection, duplicate rejection, and idempotency on already-processed requests). The enrollment processor skips the raw `FOR UPDATE` SQL when running on a non-relational provider, so these paths are exercised without a real database.

Important enrollment scenarios covered by tests include:

```text
Teacher / student authorization
Course creation rules
Class capacity rules
Duplicate enrollment
Class-full rejection
Enrollment request idempotency
Already processed enrollment request
Password hashing
JWT claim generation
```

For a real PostgreSQL environment, the pessimistic locking behavior is exercised using PostgreSQL's row-level locking:

```sql
SELECT ...
FROM classes
WHERE id = @classId
FOR UPDATE;
```

The InMemory provider does not support PostgreSQL-specific `FOR UPDATE` syntax, so the processor avoids executing the raw locking statement when the test provider is non-relational.

---

## API documentation

* `API_ENDPOINTS.md` documents every endpoint: method, path, required role, request body, and a curl example.
* `AssignmentManagement.postman_collection.json` is an importable Postman collection. Import it, set the `baseUrl` variable (`http://localhost:8080` for Docker or `http://localhost:5080` for local), and run the login request first; it saves the returned token into a collection variable that the other requests reuse automatically.

Swagger is available at:

```text
http://localhost:8080/swagger
```

when running with Docker.

For local development:

```text
http://localhost:5080/swagger
```

Swagger supports JWT authentication through the `Authorize` button.

After logging in:

```text
Login
  |
  v
Copy JWT
  |
  v
Swagger -> Authorize
  |
  v
Enter token
  |
  v
Call protected endpoints
```

---

## Notes and known limitations

* Schema is created with EF Core `EnsureCreated()` plus seeding, which is convenient for demos but is not a migration history. For a real deployment, replace it with `dotnet ef migrations add InitialCreate` and `Database.Migrate()`.
* CORS is open to all origins for ease of local testing. Lock this down before deploying.
* File storage is local disk under `Storage__RootPath`. Swap `IFileStorageService` for a cloud implementation if you need durable or shared storage.
* The default JWT secret and seeded passwords are placeholders. Replace them outside local development.

Additional architecture considerations:

* RabbitMQ is used for asynchronous enrollment processing, but PostgreSQL remains the source of truth for enrollment state and class capacity.
* RabbitMQ alone does not guarantee that a class cannot be oversold. PostgreSQL transactions and pessimistic locking provide the actual concurrency protection.
* Redis is a cache only. Business-critical data must never depend on Redis being available.
* The database unique constraint on `(StudentId, ClassId)` provides database-level protection against duplicate enrollment.
* Enrollment processing is designed to be idempotent so a previously processed enrollment request is not applied again if its RabbitMQ message is redelivered.
* PostgreSQL `SELECT ... FOR UPDATE` locking is database-specific. The production deployment uses PostgreSQL, while unit tests using EF Core InMemory skip the raw locking statement.
* For production RabbitMQ deployments, credentials should not use the default `guest/guest` account. Use environment-specific credentials and secure RabbitMQ configuration.
* For production deployments, RabbitMQ consumer retry, dead-letter queues, monitoring, and message durability should be configured according to operational requirements.
* For production deployments, Redis should use authentication/TLS and an appropriate eviction policy.
* For production deployments, the local filesystem implementation for submitted PDFs should be replaced with durable object storage such as S3-compatible storage or cloud blob storage.
* For production deployments, JWT secrets should be stored in a secure secret-management system rather than source-controlled configuration.
* The current CORS policy is intentionally permissive for local development and should be restricted to known frontend origins before deployment.

````

### One important thing I changed in the README

I preserved your original statement:

> "Capacity and the enrolled count live on the `Class` row."

and built the RabbitMQ/locking explanation around that model.

The resulting enrollment architecture is therefore:

```text
                 STUDENT
                    |
                    | POST enrollment
                    v
              ASP.NET Core API
                    |
                    | 202 Accepted
                    |
                    v
              EnrollmentRequest
                 Pending
                    |
                    v
                RabbitMQ
                    |
                    v
          EnrollmentConsumer
                    |
                    v
          BEGIN TRANSACTION
                    |
                    v
        SELECT Class FOR UPDATE
                    |
              +-----+-----+
              |           |
          Capacity OK?   Duplicate?
              |           |
             Yes          Yes
              |           |
              v           v
        Create Enrollment
              |
              v
       Increment count
              |
              v
      Request = Approved
              |
              v
            COMMIT
````

So the responsibilities are very clean:

**RabbitMQ = asynchronous processing**
**PostgreSQL transaction + `FOR UPDATE` = concurrency control**
**Unique constraint = duplicate protection**
**Idempotency = safe message redelivery**
**Redis = read performance/cache**
