# API Endpoints

Every endpoint in the Assignment Management API, grouped by area. Roles in the "Auth" column are the roles allowed to call it; "Any" means any authenticated user; "Public" means no token required.

## Conventions

- Base URL: `http://localhost:8080` (Docker) or `http://localhost:5080` (local). Examples below use `http://localhost:8080`.
- All responses use a common envelope:

  ```json
  { "success": true, "message": null, "data": { }, "errors": null }
  ```

  On failure:

  ```json
  { "success": false, "message": "Class is full.", "data": null, "errors": null, "statusCode": 409, "traceId": "..." }
  ```

- Authenticated requests need `Authorization: Bearer <token>`. Get a token from `POST /api/auth/login`.
- Enum fields (role, status, day of week) serialize as strings, for example `"Admin"`, `"Open"`, `"Monday"`.
- Paginated list endpoints accept `?pageNumber=1&pageSize=20` and return a `PaginationResponse` with `items`, `pageNumber`, `pageSize`, `totalCount`, `totalPages`.

---

## Auth

| Method | Path | Auth | Purpose |
|---|---|---|---|
| POST | `/api/auth/register` | Public | Register a new student account |
| POST | `/api/auth/login` | Public | Log in, receive a JWT |
| GET | `/api/auth/me` | Any | Current user's profile |

`POST /api/auth/register` registers a Student. Body:

```json
{ "name": "New Student", "email": "new.student@example.com", "password": "Passw0rd!" }
```

`POST /api/auth/login` body:

```json
{ "email": "admin@example.com", "password": "Admin@123" }
```

Response `data`:

```json
{
  "accessToken": "eyJhbGciOi...",
  "expiresAtUtc": "2026-08-12T12:00:00Z",
  "user": { "id": 1, "name": "Admin", "email": "admin@example.com", "role": "Admin", "isActive": true }
}
```

Example:

```bash
curl -s -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Admin@123"}'
```

```bash
TOKEN=... # accessToken from the login response
curl -s http://localhost:8080/api/auth/me -H "Authorization: Bearer $TOKEN"
```

---

## Users (Admin only)

All endpoints under `/api/users` require the Admin role.

| Method | Path | Purpose |
|---|---|---|
| GET | `/api/users?pageNumber=1&pageSize=20&role=Teacher` | List users, optional role filter |
| GET | `/api/users/{id}` | Get one user |
| POST | `/api/users` | Create a user with any role |
| PUT | `/api/users/{id}` | Update name / active flag |
| PATCH | `/api/users/{id}/activate` | Activate |
| PATCH | `/api/users/{id}/deactivate` | Deactivate |

`POST /api/users` body:

```json
{ "name": "Prof. Rahman", "email": "rahman@example.com", "password": "Passw0rd!", "role": "Teacher" }
```

`PUT /api/users/{id}` body:

```json
{ "name": "Prof. A. Rahman", "isActive": true }
```

```bash
curl -s -X POST http://localhost:8080/api/users \
  -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"Prof. Rahman","email":"rahman@example.com","password":"Passw0rd!","role":"Teacher"}'
```

---

## Courses

| Method | Path | Auth | Purpose |
|---|---|---|---|
| GET | `/api/courses?pageNumber=1&pageSize=20&search=cse` | Any | List courses, optional search |
| GET | `/api/courses/{id}` | Any | Get one course |
| POST | `/api/courses` | Admin | Create a course |
| PUT | `/api/courses/{id}` | Admin | Update a course |
| DELETE | `/api/courses/{id}` | Admin | Delete a course |

`POST /api/courses` body (code is normalized to uppercase; duplicate codes are rejected with 409):

```json
{ "code": "CSE110", "name": "Intro to Programming", "description": "Basics", "creditHours": 3 }
```

`PUT /api/courses/{id}` body:

```json
{ "name": "Introduction to Programming", "description": "Updated", "creditHours": 3 }
```

```bash
curl -s -X POST http://localhost:8080/api/courses \
  -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" \
  -d '{"code":"CSE110","name":"Intro to Programming","creditHours":3}'
```

---

## Classes

| Method | Path | Auth | Purpose |
|---|---|---|---|
| GET | `/api/classes?pageNumber=1&pageSize=20&courseId=1` | Any | List classes, optional course filter |
| GET | `/api/classes/{id}` | Any | Get one class |
| POST | `/api/classes` | Admin | Create a class |
| PUT | `/api/classes/{id}` | Admin | Update a class |
| PATCH | `/api/classes/{id}/assign-teacher` | Admin | Assign a teacher |
| PATCH | `/api/classes/{id}/status` | Admin | Set class status |
| GET | `/api/classes/{id}/students` | Admin, Teacher | Roster |
| GET | `/api/classes/{classId}/assignments?pageNumber=1&pageSize=20` | Any | Assignments in a class |
| POST | `/api/classes/{classId}/assignments` | Admin, Teacher | Create an assignment |
| POST | `/api/classes/{classId}/enrollment-requests` | Student | Request enrollment (returns 202) |

`POST /api/classes` body (times are `HH:mm` 24h; `dayOfWeek` is a string like `"Monday"`):

```json
{
  "courseId": 1,
  "name": "Section B",
  "teacherId": 2,
  "dayOfWeek": "Monday",
  "startTime": "09:00",
  "endTime": "11:00",
  "capacity": 40,
  "enrollmentDeadline": "2026-09-30T23:59:59Z"
}
```

`PATCH /api/classes/{id}/assign-teacher` body:

```json
{ "teacherId": 2 }
```

`PATCH /api/classes/{id}/status` body (`Open`, `Closed`, or `Cancelled`):

```json
{ "status": "Closed" }
```

`POST /api/classes/{classId}/assignments` body:

```json
{ "title": "Homework 1", "description": "Chapters 1-3", "deadline": "2026-10-15T23:59:59Z", "maxMarks": 100 }
```

`POST /api/classes/{classId}/enrollment-requests` takes no body and returns `202 Accepted` with the created request (status `Pending`). Poll it via the enrollment endpoints below.

```bash
# Student requests enrollment into class 1
curl -s -X POST http://localhost:8080/api/classes/1/enrollment-requests \
  -H "Authorization: Bearer $STUDENT_TOKEN"
```

---

## Enrollments

| Method | Path | Auth | Purpose |
|---|---|---|---|
| GET | `/api/enrollment-requests/{requestId}` | Any (own request, or staff) | Check a request's status |
| GET | `/api/enrollment-requests?pageNumber=1&pageSize=20` | Student | List my requests |
| GET | `/api/enrollments/me` | Student | My active enrollments |
| DELETE | `/api/enrollments/{enrollmentId}` | Student | Drop an enrollment |

A request response looks like:

```json
{
  "requestId": 5, "classId": 1, "status": "Approved",
  "reason": null, "createdAt": "2026-08-12T10:00:00Z",
  "processedAt": "2026-08-12T10:00:01Z", "message": "Enrollment approved."
}
```

When rejected, `status` is `Rejected` and `reason` is `Class is full.` or `You are already enrolled in this class.`

```bash
curl -s http://localhost:8080/api/enrollment-requests/5 -H "Authorization: Bearer $STUDENT_TOKEN"
curl -s http://localhost:8080/api/enrollments/me   -H "Authorization: Bearer $STUDENT_TOKEN"
```

---

## Teacher applications

A teacher applies to teach a course; an admin approves or rejects.

| Method | Path | Auth | Purpose |
|---|---|---|---|
| POST | `/api/teacher-applications` | Teacher | Apply to teach a course |
| GET | `/api/teacher-applications?pageNumber=1&pageSize=20` | Admin, Teacher | List applications |
| GET | `/api/teacher-applications/{id}` | Admin, Teacher | Get one |
| PATCH | `/api/teacher-applications/{id}/approve` | Admin | Approve |
| PATCH | `/api/teacher-applications/{id}/reject` | Admin | Reject |

`POST /api/teacher-applications` body:

```json
{ "courseId": 1 }
```

Approve / reject body (note optional):

```json
{ "note": "Approved for fall term." }
```

```bash
curl -s -X POST http://localhost:8080/api/teacher-applications \
  -H "Authorization: Bearer $TEACHER_TOKEN" -H "Content-Type: application/json" \
  -d '{"courseId":1}'
```

---

## Assignments

| Method | Path | Auth | Purpose |
|---|---|---|---|
| GET | `/api/assignments/{id}` | Any | Get one assignment |
| PUT | `/api/assignments/{id}` | Admin, Teacher | Update |
| PATCH | `/api/assignments/{id}/publish` | Admin, Teacher | Publish (Draft -> Published) |
| PATCH | `/api/assignments/{id}/publish-results` | Admin, Teacher | Publish results to students |
| DELETE | `/api/assignments/{id}` | Admin, Teacher | Delete |
| GET | `/api/assignments/{assignmentId}/submissions?pageNumber=1&pageSize=20` | Admin, Teacher | List submissions |
| POST | `/api/assignments/{assignmentId}/submissions` | Student | Submit a PDF |

`PUT /api/assignments/{id}` body: same shape as create (`title`, `description`, `deadline`, `maxMarks`).

`POST /api/assignments/{assignmentId}/submissions` is `multipart/form-data` with a single `file` field. The file must be a PDF and at most 15 MB.

```bash
# Student submits a PDF for assignment 1
curl -s -X POST http://localhost:8080/api/assignments/1/submissions \
  -H "Authorization: Bearer $STUDENT_TOKEN" \
  -F "file=@/path/to/homework.pdf;type=application/pdf"
```

```bash
# Teacher publishes results for assignment 1
curl -s -X PATCH http://localhost:8080/api/assignments/1/publish-results \
  -H "Authorization: Bearer $TEACHER_TOKEN"
```

---

## Submissions

| Method | Path | Auth | Purpose |
|---|---|---|---|
| GET | `/api/submissions/{id}` | Any (own, or staff) | Submission details |
| GET | `/api/submissions/{id}/versions` | Any (own, or staff) | Version history |
| GET | `/api/submissions/{id}/download?version=2` | Any (own, or staff) | Download a PDF (latest, or a given version) |
| POST | `/api/submissions/{id}/versions` | Student | Upload a new version (resubmission) |
| POST | `/api/submissions/{id}/grade` | Admin, Teacher | Grade |

`POST /api/submissions/{id}/versions` is `multipart/form-data` with a `file` field, allowed only after a resubmission request has been approved.

`POST /api/submissions/{id}/grade` body:

```json
{ "marks": 85, "feedback": "Good work, tighten section 3." }
```

```bash
# Download the latest version
curl -s -OJ http://localhost:8080/api/submissions/1/download -H "Authorization: Bearer $TOKEN"

# Grade submission 1
curl -s -X POST http://localhost:8080/api/submissions/1/grade \
  -H "Authorization: Bearer $TEACHER_TOKEN" -H "Content-Type: application/json" \
  -d '{"marks":85,"feedback":"Good work."}'
```

---

## Resubmissions

A student asks to resubmit a graded submission; a teacher or admin approves or rejects. Approval reopens the submission for one new version.

| Method | Path | Auth | Purpose |
|---|---|---|---|
| POST | `/api/submissions/{submissionId}/resubmission-requests` | Student | Request a resubmission |
| GET | `/api/resubmission-requests?pageNumber=1&pageSize=20` | Any (scoped) | List requests |
| PATCH | `/api/resubmission-requests/{id}/approve` | Admin, Teacher | Approve |
| PATCH | `/api/resubmission-requests/{id}/reject` | Admin, Teacher | Reject |

`POST .../resubmission-requests` body:

```json
{ "reason": "I uploaded the wrong file." }
```

Approve / reject body (note optional):

```json
{ "note": "One more attempt granted." }
```

```bash
curl -s -X POST http://localhost:8080/api/submissions/1/resubmission-requests \
  -H "Authorization: Bearer $STUDENT_TOKEN" -H "Content-Type: application/json" \
  -d '{"reason":"Wrong file uploaded."}'
```

---

## Results

| Method | Path | Auth | Purpose |
|---|---|---|---|
| GET | `/api/results/me` | Student | My published results |
| GET | `/api/classes/{classId}/results` | Admin, Teacher | Full results grid for a class |

`GET /api/results/me` returns only assignments whose results have been published and graded. Example `data` item:

```json
{
  "assignmentId": 1, "assignmentTitle": "Homework 1",
  "classId": 1, "className": "Section A", "courseCode": "CSE101",
  "maxMarks": 100, "marks": 85, "feedback": "Good work.",
  "submissionStatus": "Graded", "gradedAt": "2026-08-12T11:00:00Z"
}
```

```bash
curl -s http://localhost:8080/api/results/me -H "Authorization: Bearer $STUDENT_TOKEN"
curl -s http://localhost:8080/api/classes/1/results -H "Authorization: Bearer $TEACHER_TOKEN"
```

---

## Health

| Method | Path | Auth | Purpose |
|---|---|---|---|
| GET | `/health` | Public | Liveness probe |
| GET | `/` | Public | Redirects to `/swagger` |

```bash
curl -s http://localhost:8080/health
```
