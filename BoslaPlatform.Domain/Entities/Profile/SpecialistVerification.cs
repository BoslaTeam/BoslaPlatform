using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Specialists;

namespace BoslaPlatform.Domain.Entities.Profile
{
    public class SpecialistVerification : AuditableEntity
    {
        public Guid SpecialistId { get; set; }
        public VerificationStatus Status { get; set; }
        public bool IsSubmitted { get; private set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime? LastUpdatedAt { get; private set; }
        public Guid? ReviewedBy { get; set; }
        public string? AdminNotes { get; set; }

        public Specialist Specialist { get; set; } = null!;

        public void Submit()
        {
            Status = VerificationStatus.Pending;
            IsSubmitted = true;
            SubmittedAt ??= DateTime.UtcNow;
            LastUpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new SpecialistVerificationSubmittedEvent(SpecialistId));
        }

        public void Approve(Guid adminId)
        {
            Status = VerificationStatus.Approved;
            ReviewedBy = adminId;
            ReviewedAt = DateTime.UtcNow;
            LastUpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new SpecialistVerificationApprovedEvent(SpecialistId));
        }

        public void Reject(Guid adminId, string? notes)
        {
            Status = VerificationStatus.Rejected;
            ReviewedBy = adminId;
            ReviewedAt = DateTime.UtcNow;
            AdminNotes = notes;
            LastUpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new SpecialistVerificationRejectedEvent(SpecialistId));
        }
    }
}
