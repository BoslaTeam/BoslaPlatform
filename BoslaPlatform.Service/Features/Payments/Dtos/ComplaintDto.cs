using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Application.Features.Payments.Dtos;

public class ComplaintDto
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ComplaintStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class ComplaintDetailDto
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ComplaintStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class FileDisputeRequest
{
    public Guid PaymentId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ResolveDisputeRequest
{
    public bool ApproveRefund { get; set; }
    public string? AdminNotes { get; set; }
}
