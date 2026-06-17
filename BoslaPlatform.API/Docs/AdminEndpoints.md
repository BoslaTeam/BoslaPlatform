Admin Feature — API Reference
============================

Overview
--------
This document describes the Admin feature API endpoints and seeding behavior. All admin endpoints require an authenticated user in the Admin role.

Authentication & Authorization
------------------------------
- All endpoints below require HTTP Authorization: Bearer {JWT}
- Role-based authorization: the controller is decorated with [Authorize(Roles = nameof(BoslaPlatform.Domain.Enums.UserRole.Admin))]

Seeding (default)
-----------------
- On application startup in Development environment the database initializer will run and seed default roles and users.
- Default users seeded by ApplicationDbContextInitialiser:
  - admin@localhost — Name: "admin" — Role: Admin
  - specialist@localhost — Name: "specialist" — Role: Specialist
  - user@localhost — Name: "user" — Role: User
- Default password used by the initializer: 0105140@Ma
- Program.cs calls app.InitialiseDatabaseAsync() when running in Development; you can run the app to apply migrations and seed data.

Base route
----------
- All endpoints live under: /api/v1/admin

Endpoints
---------

1) List users
   - Method: GET
   - Route: /api/v1/admin/users
   - Query: page (int, optional, default=1), pageSize (int, optional, default=20)
   - Roles: Admin
   - Response: ApiResponse<List<UserDto>> where UserDto contains Id, Email, FullName, IsActive, Roles[]

2) Get user details
   - Method: GET
   - Route: /api/v1/admin/users/{id}
   - Roles: Admin
   - Response: ApiResponse<UserDetailsDto> (Id, Email, FullName, IsActive, Roles[], CreatedAt)

3) Update user roles
   - Method: PUT
   - Route: /api/v1/admin/users/{id}/roles
   - Body: { "roles": ["admin","specialist","user"] }
   - Roles: Admin
   - Response: ApiResponse (success message) or ProblemDetails on failure

4) Deactivate user
   - Method: POST
   - Route: /api/v1/admin/users/{id}/deactivate
   - Roles: Admin
   - Response: ApiResponse (success message) or ProblemDetails

5) Reactivate user
   - Method: POST
   - Route: /api/v1/admin/users/{id}/reactivate
   - Roles: Admin
   - Response: ApiResponse (success message) or ProblemDetails

6) Verify specialist
   - Method: POST
   - Route: /api/v1/admin/specialists/{id}/verify
   - Body: { "isVerified": true }
   - Roles: Admin
   - Response: ApiResponse (success message) or ProblemDetails

7) Audit logs
   - Method: GET
   - Route: /api/v1/admin/audit-logs
   - Query: page, pageSize
   - Roles: Admin
   - Response: ApiResponse<List<AuditLogDto>> (Id, Action, PerformedBy, PerformedAt, Details)

Error handling
--------------
- The API uses a Result<T> -> ProblemDetails mapping. When operations fail the response will be a ProblemDetails JSON with an appropriate HTTP status code.

Notes for developers
--------------------
- Service interface: BoslaPlatform.Application.Features.Admin.Services.IAdminService
- Implementation: BoslaPlatform.Infrastructure.Services.AdminService
- Seed logic: BoslaPlatform.Infrastructure.Data.ApplicationDbContextInitialiser
- Admin controller: BoslaPlatform.API.Controllers.v1.AdminController

How to test locally
-------------------
1. Run the API in Development mode (dotnet run or via Visual Studio). The initializer will apply migrations and seed default roles/users.
2. Obtain a JWT for the seeded admin (use Auth endpoints, or create a token in the DB using Identity helpers).
3. Call endpoints with Authorization: Bearer {token}.

If you want additional example curl/postman snippets or OpenAPI documentation additions, tell me which endpoints you want examples for.
