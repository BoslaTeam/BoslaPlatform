namespace BoslaPlatform.Domain.Common
{
    public interface IAuditableEntity
    {
        DateTimeOffset CreatedAtUtc { get; set; }

        Guid? CreatedBy { get; set; }

        DateTimeOffset? LastModifiedUtc { get; set; }

        Guid? LastModifiedBy { get; set; }
    }
}
