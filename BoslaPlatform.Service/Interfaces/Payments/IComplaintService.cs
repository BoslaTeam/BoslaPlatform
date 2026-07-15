using BoslaPlatform.Application.Features.Payments.Dtos;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.Payments;

public interface IComplaintService
{
    Task<Result<ComplaintDto>> FileDisputeAsync(Guid userId, FileDisputeRequest request, CancellationToken ct = default);
    Task<Result<ComplaintDetailDto>> GetComplaintByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<ComplaintDto?>> GetComplaintByAppointmentAsync(Guid appointmentId, CancellationToken ct = default);
    Task<Result<List<ComplaintDto>>> GetMyComplaintsAsync(Guid userId, CancellationToken ct = default);
    Task<Result<List<ComplaintDto>>> GetPendingComplaintsAsync(CancellationToken ct = default);
    Task<Result<List<ComplaintListItemDto>>> GetAllComplaintsAsync(ComplaintStatus? status = null, CancellationToken ct = default);
    Task<Result> ResolveDisputeAsync(Guid complaintId, Guid adminId, ResolveDisputeRequest request, CancellationToken ct = default);
}
