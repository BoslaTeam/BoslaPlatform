namespace BoslaPlatform.Domain.Common
{
    public abstract class AuditableEntity : BaseEntity, IAuditableEntity
    {
        protected AuditableEntity() { }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public Guid? CreatedBy { get; set; }

        public DateTimeOffset? LastModifiedUtc { get; set; }

        public Guid? LastModifiedBy { get; set; }
    }

}
