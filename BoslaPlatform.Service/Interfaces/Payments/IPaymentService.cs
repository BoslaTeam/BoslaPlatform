using BoslaPlatform.Application.Features.Payments.Dtos;
using BoslaPlatform.Application.Features.Payments.Requests;
using BoslaPlatform.Shared;

namespace BoslaPlatform.Application.Interfaces.Payments
{
    public interface IPaymentService
    {
        Task<Result<PaymentResponseDto>> InitiateAsync(InitiatePaymentRequest request, CancellationToken ct = default);
        Task<Result<PaymentResponseDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Result<PaymentResponseDto>> GetByAppointmentAsync(Guid appointmentId, CancellationToken ct = default);
    }
}
