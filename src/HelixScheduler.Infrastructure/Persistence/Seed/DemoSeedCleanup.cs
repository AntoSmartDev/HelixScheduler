using HelixScheduler.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HelixScheduler.Infrastructure.Persistence.Seed;

internal sealed class DemoSeedCleanup
{
    private readonly SchedulerDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public DemoSeedCleanup(SchedulerDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task CleanupAsync(CancellationToken ct)
    {
        await CleanupRulesAndBusyAsync(ct).ConfigureAwait(false);
        await CleanupPropertyTreeAsync(ct).ConfigureAwait(false);
        await CleanupPropertyLinksAsync(ct).ConfigureAwait(false);
        await CleanupTypeMappingsAsync(ct).ConfigureAwait(false);
        await CleanupRelationsAsync(ct).ConfigureAwait(false);
    }

    private Task CleanupRulesAndBusyAsync(CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        return _dbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE rr
            FROM RuleResources rr
            INNER JOIN Rules r ON rr.RuleId = r.Id
            WHERE r.Title LIKE 'Demo:%' AND r.TenantId = {0};

            DELETE FROM Rules WHERE Title LIKE 'Demo:%' AND TenantId = {0};

            DELETE ber
            FROM BusyEventResources ber
            INNER JOIN BusyEvents b ON ber.BusyEventId = b.Id
            WHERE b.Title LIKE 'Demo:%' AND b.TenantId = {0};

            DELETE FROM BusyEvents WHERE Title LIKE 'Demo:%' AND TenantId = {0};
            """,
            new object[] { tenantId },
            ct);
    }

    private Task CleanupPropertyTreeAsync(CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        return _dbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE rpl
            FROM ResourcePropertyLinks rpl
            INNER JOIN ResourceProperties p ON rpl.PropertyId = p.Id
            WHERE p.TenantId = {0}
              AND (p.[Key] IN ('Specialization', 'RoomFeature', 'Location', 'Accreditation')
               OR EXISTS (
                    SELECT 1
                    FROM ResourceProperties parent
                    WHERE parent.Id = p.ParentId
                      AND parent.[Key] IN ('Specialization', 'RoomFeature', 'Location', 'Accreditation')
               ));

            DELETE p
            FROM ResourceProperties p
            WHERE p.TenantId = {0}
              AND (p.[Key] IN ('Specialization', 'RoomFeature', 'Location', 'Accreditation')
               OR EXISTS (
                    SELECT 1
                    FROM ResourceProperties parent
                    WHERE parent.Id = p.ParentId
                      AND parent.[Key] IN ('Specialization', 'RoomFeature', 'Location', 'Accreditation')
               ));
            """,
            new object[] { tenantId },
            ct);
    }

    private Task CleanupTypeMappingsAsync(CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        return _dbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE rtp
            FROM ResourceTypeProperties rtp
            INNER JOIN ResourceTypes rt ON rtp.ResourceTypeId = rt.Id
            WHERE rt.TenantId = {0}
              AND rt.[Key] IN ('Doctor', 'Room', 'Site', 'Floor');
            """,
            new object[] { tenantId },
            ct);
    }

    private Task CleanupPropertyLinksAsync(CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        return _dbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE rpl
            FROM ResourcePropertyLinks rpl
            INNER JOIN Resources r ON rpl.ResourceId = r.Id
            WHERE r.TenantId = {0}
              AND (r.Code IN ('SITE-A', 'SITE-B', 'FLOOR-A1', 'FLOOR-B1', 'ROOM-1', 'ROOM-2', 'ROOM-3', 'ROOM-4', 'DOC-7', 'DOC-8', 'DOC-9')
               OR r.Name IN ('Site A', 'Site B', 'Floor A1', 'Floor B1', 'Room 1', 'Room 2', 'Room 3', 'Room 4', 'Doctor 7', 'Doctor 8', 'Doctor 9'));
            """,
            new object[] { tenantId },
            ct);
    }

    private Task CleanupRelationsAsync(CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        return _dbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM ResourceRelations
            WHERE TenantId = {0}
              AND (
                ParentResourceId IN (
                  SELECT Id FROM Resources
                  WHERE TenantId = {0}
                    AND (Code IN ('SITE-A', 'SITE-B', 'FLOOR-A1', 'FLOOR-B1', 'ROOM-1', 'ROOM-2', 'ROOM-3', 'ROOM-4', 'DOC-7', 'DOC-8', 'DOC-9')
                     OR Name IN ('Site A', 'Site B', 'Floor A1', 'Floor B1', 'Room 1', 'Room 2', 'Room 3', 'Room 4', 'Doctor 7', 'Doctor 8', 'Doctor 9')
                    )
                )
                OR ChildResourceId IN (
                  SELECT Id FROM Resources
                  WHERE TenantId = {0}
                    AND (Code IN ('SITE-A', 'SITE-B', 'FLOOR-A1', 'FLOOR-B1', 'ROOM-1', 'ROOM-2', 'ROOM-3', 'ROOM-4', 'DOC-7', 'DOC-8', 'DOC-9')
                     OR Name IN ('Site A', 'Site B', 'Floor A1', 'Floor B1', 'Room 1', 'Room 2', 'Room 3', 'Room 4', 'Doctor 7', 'Doctor 8', 'Doctor 9')
                    )
                )
              );
            """,
            new object[] { tenantId },
            ct);
    }
}
