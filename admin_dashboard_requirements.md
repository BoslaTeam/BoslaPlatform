# Admin Dashboard API Endpoint Requirements

The frontend requires a new endpoint to provide statistical data for the Admin Dashboard. Below are the exact details needed to implement this in the backend (`AdminController`).

## Endpoint Details

- **Route:** `GET /api/v1/admin/dashboard`
- **Controller:** `AdminController`
- **Authorization:** `[Authorize(Roles = nameof(UserRole.Admin))]`

## Expected Response JSON

The frontend expects a successful response wrapped in your standard `ApiResponse<T>`, where `T` is the `AdminDashboardDto`.

```json
{
  "success": true,
  "message": "Dashboard stats retrieved successfully",
  "data": {
    "totalUsers": 1500,
    "totalSpecialists": 120,
    "totalAppointments": 850,
    "totalRevenue": 45000.50,
    "pendingVerifications": 15,
    "activeAppointments": 40,
    "userGrowthPercentage": 12.5,
    "revenueGrowthPercentage": 8.4,
    "appointmentGrowthPercentage": 15.2,
    "specialistGrowthPercentage": 5.1,
    "recentUsers": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "fullName": "Ahmed Ali",
        "email": "ahmed@example.com",
        "role": 0,
        "isActive": true,
        "createdAt": "2024-03-10T15:30:00Z",
        "avatarUrl": "https://example.com/avatar.png"
      }
    ],
    "recentAppointments": [
      {
        "id": "1fa85f64-5717-4562-b3fc-2c963f66afa6",
        "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "userName": "Ahmed Ali",
        "specialistId": "2fa85f64-5717-4562-b3fc-2c963f66afa6",
        "specialistName": "Dr. Mahmoud",
        "scheduledAt": "2024-03-20T10:00:00Z",
        "durationMinutes": 60,
        "status": 1, 
        "totalAmount": 500,
        "createdAt": "2024-03-10T15:30:00Z"
      }
    ]
  },
  "errors": null
}
```

## DTO Definitions (C#)

You will need to create the following DTOs in `Bosla.Application.Features.Admin.DTOs`:

### 1. `AdminDashboardDto`
```csharp
public class AdminDashboardDto
{
    public int TotalUsers { get; set; }
    public int TotalSpecialists { get; set; }
    public int TotalAppointments { get; set; }
    public decimal TotalRevenue { get; set; }
    public int PendingVerifications { get; set; }
    public int ActiveAppointments { get; set; }
    
    // Percentages (Growth from last month)
    public double UserGrowthPercentage { get; set; }
    public double RevenueGrowthPercentage { get; set; }
    public double AppointmentGrowthPercentage { get; set; }
    public double SpecialistGrowthPercentage { get; set; }
    
    // Lists for recent activities
    public List<UserDto> RecentUsers { get; set; } = new();
    public List<AdminAppointmentDto> RecentAppointments { get; set; } = new();
}
```

### 2. `AdminAppointmentDto`
*(Note: You might already have a similar DTO, but ensure it has these fields)*
```csharp
public class AdminAppointmentDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public Guid SpecialistId { get; set; }
    public string SpecialistName { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public int Status { get; set; } // e.g. 0: Pending, 1: Confirmed, 2: Completed
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

## Implementation Steps

1. Add `Task<Result<AdminDashboardDto>> GetDashboardStatsAsync(CancellationToken cancellationToken);` to `IAdminService.cs`.
2. Implement the logic in `AdminService.cs` (query the DB counts, sum the revenue, fetch the last 5 users, and last 5 appointments).
3. Add the `[HttpGet("dashboard")]` endpoint inside `AdminController.cs` returning `ApiResponse<AdminDashboardDto>.SuccessResponse(value)`.
