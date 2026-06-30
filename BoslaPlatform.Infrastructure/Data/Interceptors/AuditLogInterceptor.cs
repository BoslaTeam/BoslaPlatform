using System.Text.Json;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Domain.Common;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BoslaPlatform.Infrastructure.Data.Interceptors;

public sealed class AuditLogInterceptor(
    IUser user,
    IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    private static readonly HashSet<string> ExcludedEntities = [nameof(AuditLog), "RefreshToken"];

    private static readonly HashSet<string> MetadataProps =
        [nameof(IAuditableEntity.CreatedAtUtc), nameof(IAuditableEntity.CreatedBy),
         nameof(IAuditableEntity.LastModifiedUtc), nameof(IAuditableEntity.LastModifiedBy)];

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            CaptureAuditLogs(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CaptureAuditLogs(DbContext context)
    {
        var entries = context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => !ExcludedEntities.Contains(e.Entity.GetType().Name))
            .ToList();

        if (entries.Count == 0)
            return;

        var utcNow = DateTime.UtcNow;
        var userId = user.Id;
        var ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        var serializedOptions = new JsonSerializerOptions { WriteIndented = false, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };

        foreach (var entry in entries)
        {
            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Deleted => AuditAction.Deleted,
                _ => AuditAction.Updated,
            };

            string? oldValues = null;
            string? newValues = null;

            if (entry.State == EntityState.Modified)
            {
                var oldDict = new Dictionary<string, object?>();
                var newDict = new Dictionary<string, object?>();

                foreach (var prop in entry.Properties)
                {
                    if (prop.Metadata.IsPrimaryKey() || prop.Metadata.IsShadowProperty())
                        continue;
                    if (MetadataProps.Contains(prop.Metadata.Name))
                        continue;

                    var original = prop.OriginalValue;
                    var current = prop.CurrentValue;

                    if (!Equals(original, current))
                    {
                        oldDict[prop.Metadata.Name] = original;
                        newDict[prop.Metadata.Name] = current;
                    }
                }

                if (oldDict.Count > 0)
                    oldValues = JsonSerializer.Serialize(oldDict, serializedOptions);
                if (newDict.Count > 0)
                    newValues = JsonSerializer.Serialize(newDict, serializedOptions);
            }
            else if (entry.State == EntityState.Added)
            {
                var dict = new Dictionary<string, object?>();
                foreach (var prop in entry.Properties)
                {
                    if (prop.Metadata.IsPrimaryKey() || prop.Metadata.IsShadowProperty())
                        continue;
                    if (MetadataProps.Contains(prop.Metadata.Name))
                        continue;
                    dict[prop.Metadata.Name] = prop.CurrentValue;
                }
                newValues = JsonSerializer.Serialize(dict, serializedOptions);
            }
            else if (entry.State == EntityState.Deleted)
            {
                var dict = new Dictionary<string, object?>();
                foreach (var prop in entry.Properties)
                {
                    if (prop.Metadata.IsPrimaryKey() || prop.Metadata.IsShadowProperty())
                        continue;
                    if (MetadataProps.Contains(prop.Metadata.Name))
                        continue;
                    dict[prop.Metadata.Name] = prop.OriginalValue;
                }
                oldValues = JsonSerializer.Serialize(dict, serializedOptions);
            }

            var log = new AuditLog
            {
                EntityType = entry.Entity.GetType().Name,
                EntityId = entry.Entity.Id.ToString(),
                Action = action,
                OldValues = oldValues,
                NewValues = newValues,
                Timestamp = utcNow,
                IpAddress = ipAddress,
                LastModifiedBy = userId,
                CreatedBy = userId,
                CreatedAtUtc = utcNow,
                LastModifiedUtc = utcNow,
            };

            context.Entry(log).State = EntityState.Added;
        }
    }
}
