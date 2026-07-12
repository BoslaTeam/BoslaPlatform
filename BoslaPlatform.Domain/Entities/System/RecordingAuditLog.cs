using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Domain.Entities.System
{
    /// <summary>
    /// Immutable audit record for recording-related user actions.
    /// Never stores presigned URLs or other sensitive content.
    /// </summary>
    public sealed class RecordingAuditLog : BaseEntity
    {
        private RecordingAuditLog() { }

        /// <summary>The video session this audit entry relates to.</summary>
        public Guid VideoSessionId { get; private set; }

        /// <summary>The user who performed the action. Null for system-initiated actions.</summary>
        public Guid? UserId { get; private set; }

        /// <summary>The action that was performed.</summary>
        public RecordingAuditAction Action { get; private set; }

        /// <summary>UTC timestamp when the action occurred.</summary>
        public DateTime OccurredAtUtc { get; private set; }

        /// <summary>
        /// Creates a new audit log entry. This is the only factory — it enforces
        /// immutability and guarantees a timestamp is always set.
        /// </summary>
        public static RecordingAuditLog Create(
            Guid videoSessionId,
            Guid? userId,
            RecordingAuditAction action)
        {
            return new RecordingAuditLog
            {
                Id = Guid.NewGuid(),
                VideoSessionId = videoSessionId,
                UserId = userId,
                Action = action,
                OccurredAtUtc = DateTime.UtcNow
            };
        }
    }
}
