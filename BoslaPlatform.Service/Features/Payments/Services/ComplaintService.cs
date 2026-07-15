using BoslaPlatform.Application.Features.Payments.Dtos;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Payments;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Domain.Entities.Payments;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace BoslaPlatform.Application.Features.Payments.Services;

public class ComplaintService : IComplaintService
{
    private readonly IAppDbContext _context;

    public ComplaintService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ComplaintDto>> FileDisputeAsync(Guid userId, FileDisputeRequest request, CancellationToken ct = default)
    {
        var payment = await _context.Payments
            .Include(p => p.Appointment)
            .Include(p => p.Complaint)
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, ct);

        if (payment is null)
            return Error.NotFound("Payment.NotFound", "Payment not found.");

        if (payment.EscrowStatus != EscrowStatus.Held)
            return Error.Validation("Payment.NotInEscrow",
                "Only held payments can be disputed.");

        if (payment.Complaint is not null)
            return Error.Conflict("Payment.AlreadyDisputed",
                "A dispute has already been filed for this payment.");

        var complaint = PaymentComplaint.File(request.PaymentId, userId, request.Reason, request.Description);
        payment.FileDispute(request.Reason);

        _context.Set<PaymentComplaint>().Add(complaint);
        await _context.SaveChangesAsync(ct);

        return new ComplaintDto
        {
            Id = complaint.Id,
            PaymentId = complaint.PaymentId,
            Reason = complaint.Reason,
            Description = complaint.Description,
            Status = complaint.Status,
            CreatedAtUtc = complaint.CreatedAtUtc.DateTime,
            AdminNotes = complaint.AdminNotes,
            ResolvedAt = complaint.ResolvedAt
        };
    }

    public async Task<Result<ComplaintDetailDto>> GetComplaintByIdAsync(Guid id, CancellationToken ct = default)
    {
        var complaint = await _context.Set<PaymentComplaint>()
            .Include(c => c.Payment)
                .ThenInclude(p => p.Appointment)
                    .ThenInclude(a => a.Specialist)
                        .ThenInclude(s => s.User)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (complaint is null)
            return Error.NotFound("Complaint.NotFound", "Complaint not found.");

        return new ComplaintDetailDto
        {
            Id = complaint.Id,
            PaymentId = complaint.PaymentId,
            AppointmentId = complaint.Payment.AppointmentId,
            UserId = complaint.UserId,
            UserName = complaint.User.Name,
            UserAvatarUrl = complaint.User.ProfileImageUrl,
            Reason = complaint.Reason,
            Description = complaint.Description,
            Status = complaint.Status,
            Amount = complaint.Payment.Amount,
            Currency = complaint.Payment.Currency,
            SpecialistName = complaint.Payment.Appointment.Specialist?.User?.Name,
            CreatedAtUtc = complaint.CreatedAtUtc.DateTime,
            AdminNotes = complaint.AdminNotes,
            ResolvedAt = complaint.ResolvedAt
        };
    }

    public async Task<Result<List<ComplaintListItemDto>>> GetAllComplaintsAsync(ComplaintStatus? status = null, CancellationToken ct = default)
    {
        var query = _context.Set<PaymentComplaint>()
            .Include(c => c.Payment)
            .Include(c => c.User)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        var complaints = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new ComplaintListItemDto
            {
                Id = c.Id,
                PaymentId = c.PaymentId,
                AppointmentId = c.Payment.AppointmentId,
                Reason = c.Reason,
                Description = c.Description,
                Status = c.Status,
                CreatedAtUtc = c.CreatedAtUtc.DateTime,
                AdminNotes = c.AdminNotes,
                ResolvedAt = c.ResolvedAt,
                UserName = c.User.Name,
                UserAvatarUrl = c.User.ProfileImageUrl,
                Amount = c.Payment.Amount,
                Currency = c.Payment.Currency
            })
            .ToListAsync(ct);

        return complaints;
    }

    public async Task<Result<ComplaintDto?>> GetComplaintByAppointmentAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var payment = await _context.Payments
            .Include(p => p.Complaint)
            .FirstOrDefaultAsync(p => p.AppointmentId == appointmentId, ct);

        if (payment?.Complaint is null)
            return Result<ComplaintDto?>.Success(null);

        var c = payment.Complaint;
        return new ComplaintDto
        {
            Id = c.Id,
            PaymentId = c.PaymentId,
            Reason = c.Reason,
            Description = c.Description,
            Status = c.Status,
            CreatedAtUtc = c.CreatedAtUtc.DateTime,
            AdminNotes = c.AdminNotes,
            ResolvedAt = c.ResolvedAt
        };
    }

    public async Task<Result<List<ComplaintDto>>> GetMyComplaintsAsync(Guid userId, CancellationToken ct = default)
    {
        var complaints = await _context.Set<PaymentComplaint>()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new ComplaintDto
            {
                Id = c.Id,
                PaymentId = c.PaymentId,
                Reason = c.Reason,
                Description = c.Description,
                Status = c.Status,
                CreatedAtUtc = c.CreatedAtUtc.DateTime,
                AdminNotes = c.AdminNotes,
                ResolvedAt = c.ResolvedAt
            })
            .ToListAsync(ct);

        return complaints;
    }

    public async Task<Result<List<ComplaintDto>>> GetPendingComplaintsAsync(CancellationToken ct = default)
    {
        var complaints = await _context.Set<PaymentComplaint>()
            .Where(c => c.Status == ComplaintStatus.Pending)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new ComplaintDto
            {
                Id = c.Id,
                PaymentId = c.PaymentId,
                Reason = c.Reason,
                Description = c.Description,
                Status = c.Status,
                CreatedAtUtc = c.CreatedAtUtc.DateTime,
                AdminNotes = c.AdminNotes,
                ResolvedAt = c.ResolvedAt
            })
            .ToListAsync(ct);

        return complaints;
    }

    public async Task<Result> ResolveDisputeAsync(Guid complaintId, Guid adminId, ResolveDisputeRequest request, CancellationToken ct = default)
    {
        var complaint = await _context.Set<PaymentComplaint>()
            .Include(c => c.Payment)
            .ThenInclude(p => p.Appointment)
            .FirstOrDefaultAsync(c => c.Id == complaintId, ct);

        if (complaint is null)
            return Error.NotFound("Complaint.NotFound", "Complaint not found.");

        if (complaint.Status != ComplaintStatus.Pending)
            return Error.Conflict("Complaint.AlreadyResolved",
                "This complaint has already been resolved.");

        var payment = complaint.Payment;

        if (request.ApproveRefund)
        {
            // Process Stripe refund
            try
            {
                var options = new RefundCreateOptions
                {
                    PaymentIntent = payment.ExternalPaymentId,
                    Reason = RefundReasons.RequestedByCustomer
                };

                var refundService = new RefundService();
                await refundService.CreateAsync(options, cancellationToken: ct);

                payment.RefundAfterDispute(request.AdminNotes);
                payment.Appointment.Cancel(adminId, "تم إلغاء الحجز بعد الموافقة على استرجاع المبلغ.");
                complaint.ResolveRefunded(adminId, request.AdminNotes);
            }
            catch (StripeException ex)
            {
                return Error.Unexpected("Stripe.RefundFailed",
                    $"Refund failed: {ex.Message}");
            }
        }
        else
        {
            payment.RejectDispute();
            complaint.ResolveRejected(adminId, request.AdminNotes);
        }

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
