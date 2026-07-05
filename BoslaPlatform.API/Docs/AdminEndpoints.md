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

8) Get audit log by id
   - Method: GET
   - Route: /api/v1/admin/audit-logs/{id}
   - Roles: Admin
   - Response: ApiResponse<AuditLogDto> (Id, Action, PerformedBy, PerformedAt, Details)

9) List pending specialists
   - Method: GET
   - Route: /api/v1/admin/specialists/pending
   - Query: page, pageSize
   - Roles: Admin
   - Response: ApiResponse<List<SpecialistDto>> (Id, UserId, Name, Title, HourlyRate, VerificationStatus)

10) Get specialist detail
	- Method: GET
	- Route: /api/v1/admin/specialists/{id}
	- Roles: Admin
	- Response: ApiResponse<SpecialistDetailsDto> (detailed specialist profile)

11) List all appointments
	- Method: GET
	- Route: /api/v1/admin/appointments
	- Query: page, pageSize
	- Roles: Admin
	- Response: ApiResponse<List<AppointmentDto>> (Id, SpecialistId, UserId, Start, End, Status, Price)

12) Cancel appointment (admin)
	- Method: POST
	- Route: /api/v1/admin/appointments/{id}/cancel
	- Body: { "reason": "string" }
	- Roles: Admin
	- Response: ApiResponse (success message) or ProblemDetails
	- Notes: Creates an AuditLog entry recording status and cancellation reason.

13) Reschedule appointment (admin)
	- Method: POST
	- Route: /api/v1/admin/appointments/{id}/reschedule
	- Body: { "newStart": "2026-06-23T13:00:00Z", "newEnd": "2026-06-23T14:00:00Z" }
	- Roles: Admin
	- Response: ApiResponse (success message) or ProblemDetails
	- Notes: Performs a simple overlap check against existing appointments for the same specialist. Creates an AuditLog entry on success.

14) List payments
	- Method: GET
	- Route: /api/v1/admin/payments
	- Query: page, pageSize
	- Roles: Admin
	- Response: ApiResponse<List<PaymentDto>> (Id, AppointmentId, UserId, Amount, Currency, Status, CreatedAt)

15) Refund payment
	- Method: POST
	- Route: /api/v1/admin/payments/{id}/refund
	- Roles: Admin
	- Response: ApiResponse (success message) or ProblemDetails
	- Notes: If payments were created with an ExternalPaymentId and Stripe is configured, the API will attempt a refund via Stripe Refund API. Otherwise the payment record is marked as refunded/failed locally. An AuditLog entry is created.

16) Dashboard (aggregated metrics)
	- Method: GET
	- Route: /api/v1/admin/dashboard
	- Roles: Admin
	- Response: ApiResponse<DashboardDto> (TotalUsers, TotalSpecialists, PendingSpecialists, TotalAppointments, TotalPayments)
	- Notes: The dashboard is backed by a Dapper read model (Infrastructure) for efficient aggregated queries with an EF Core fallback.

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

Configuration
-------------
- Stripe (optional): to enable external refunds the app reads Stripe settings from configuration (appsettings.json or environment variables).
  - Configuration section name: "StripeSettings"
  - Keys:
	- SecretKey (set to your Stripe secret API key)
	- PublishableKey
	- WebhookSecret
	- SuccessUrl (used by PaymentService responses)
	- CancelUrl
  - Example appsettings.json snippet:

```json
"StripeSettings": {
  "SecretKey": "sk_test_...",
  "PublishableKey": "pk_test_...",
  "WebhookSecret": "whsec_...",
  "SuccessUrl": "https://example.com/success/{CHECKOUT_SESSION_ID}",
  "CancelUrl": "https://example.com/cancel"
}
```

Dependency Injection Notes
--------------------------
- Admin service implementation: BoslaPlatform.Infrastructure.Services.AdminService is registered in Infrastructure/DependencyInjection.cs
- Dapper dashboard repository: BoslaPlatform.Infrastructure.Data.DapperDashboardRepository is registered using the application's DefaultConnection string. The repository implements an internal IDashboardRepository used by AdminService.

Audit logging
-------------
- AuditLog entities are created for operations that change state: DeactivateUser, ReactivateUser, VerifySpecialist, CancelAppointment, RescheduleAppointment, RefundPayment. Audit records are persisted into the AuditLogs table via the application's DbContext.

