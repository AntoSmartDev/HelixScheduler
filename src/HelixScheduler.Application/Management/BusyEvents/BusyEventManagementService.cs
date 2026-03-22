using HelixScheduler.Application.Abstractions;
using HelixScheduler.Application.Management;
using HelixScheduler.Application.Management.ResourceCatalog;

namespace HelixScheduler.Application.Management.BusyEvents;

public sealed class BusyEventManagementService : IBusyEventManagementService
{
    private const int TitleMaxLength = 300;
    private const int EventTypeMaxLength = 50;
    private const int ExternalKeyMaxLength = 200;

    private readonly IBusyEventManagementStore _store;
    private readonly IResourceManagementStore _resourceStore;
    private readonly ITenantContext _tenantContext;
    private readonly IClock _clock;

    public BusyEventManagementService(
        IBusyEventManagementStore store,
        IResourceManagementStore resourceStore,
        ITenantContext tenantContext,
        IClock clock)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _resourceStore = resourceStore ?? throw new ArgumentNullException(nameof(resourceStore));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<ManagementResult<BusyEventManagementDto>> RegisterBusyEventAsync(
        RegisterBusyEventCommand command,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<BusyEventManagementDto>.Failure(tenantError);
        }

        var normalized = NormalizeDefinition(command.Definition);
        var errors = ValidateDefinition(normalized);
        if (errors.Count > 0)
        {
            return ManagementResult<BusyEventManagementDto>.Failure(errors);
        }

        var resourceErrors = await ValidateResourcesAsync(normalized.ResourceIds, ct).ConfigureAwait(false);
        if (resourceErrors.Count > 0)
        {
            return ManagementResult<BusyEventManagementDto>.Failure(resourceErrors);
        }

        if (normalized.ExternalKey != null)
        {
            var existingByExternalKey = await _store.FindByExternalKeyAsync(normalized.ExternalKey, ct).ConfigureAwait(false);
            if (existingByExternalKey != null)
            {
                return ManagementResult<BusyEventManagementDto>.Failure(
                    CreateError(
                        "busy-event.external-key.duplicate",
                        ManagementErrorCategory.Conflict,
                        $"Busy event with external key '{normalized.ExternalKey}' already exists.",
                        "externalKey"));
            }
        }

        var created = await _store.CreateAsync(_tenantContext.TenantId, normalized, _clock.UtcNow, ct).ConfigureAwait(false);
        return ManagementResult<BusyEventManagementDto>.Success(created);
    }

    public async Task<ManagementResult<IReadOnlyList<BusyEventManagementDto>>> RegisterBusyEventsAsync(
        RegisterBusyEventsCommand command,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<IReadOnlyList<BusyEventManagementDto>>.Failure(tenantError);
        }

        if (command.Definitions == null || command.Definitions.Count == 0)
        {
            return ManagementResult<IReadOnlyList<BusyEventManagementDto>>.Failure(
                CreateError(
                    "busy-event.bulk.required",
                    ManagementErrorCategory.Validation,
                    "At least one busy event definition is required.",
                    "definitions"));
        }

        var normalizedDefinitions = command.Definitions.Select(NormalizeDefinition).ToList();
        var errors = new List<ManagementError>();
        var resourceIds = new HashSet<int>();
        var externalKeys = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < normalizedDefinitions.Count; i++)
        {
            var definition = normalizedDefinitions[i];
            foreach (var error in ValidateDefinition(definition))
            {
                errors.Add(error with { Target = $"definitions[{i}].{error.Target}" });
            }

            for (var j = 0; j < definition.ResourceIds.Count; j++)
            {
                resourceIds.Add(definition.ResourceIds[j]);
            }

            if (definition.ExternalKey == null)
            {
                continue;
            }

            if (!externalKeys.Add(definition.ExternalKey))
            {
                errors.Add(CreateError(
                    "busy-event.external-key.duplicate-in-batch",
                    ManagementErrorCategory.Conflict,
                    $"External key '{definition.ExternalKey}' appears more than once in the batch.",
                    $"definitions[{i}].externalKey"));
            }
        }

        if (errors.Count > 0)
        {
            return ManagementResult<IReadOnlyList<BusyEventManagementDto>>.Failure(errors);
        }

        var resourceErrors = await ValidateResourcesAsync(resourceIds.ToList(), ct).ConfigureAwait(false);
        if (resourceErrors.Count > 0)
        {
            return ManagementResult<IReadOnlyList<BusyEventManagementDto>>.Failure(resourceErrors);
        }

        foreach (var definition in normalizedDefinitions.Where(item => item.ExternalKey != null))
        {
            var existingByExternalKey = await _store.FindByExternalKeyAsync(definition.ExternalKey!, ct).ConfigureAwait(false);
            if (existingByExternalKey != null)
            {
                return ManagementResult<IReadOnlyList<BusyEventManagementDto>>.Failure(
                    CreateError(
                        "busy-event.external-key.duplicate",
                        ManagementErrorCategory.Conflict,
                        $"Busy event with external key '{definition.ExternalKey}' already exists.",
                        "externalKey"));
            }
        }

        var created = await _store.CreateManyAsync(_tenantContext.TenantId, normalizedDefinitions, _clock.UtcNow, ct).ConfigureAwait(false);
        return ManagementResult<IReadOnlyList<BusyEventManagementDto>>.Success(created);
    }

    public async Task<ManagementResult<BusyEventManagementDto>> UpsertBusyEventByExternalKeyAsync(
        UpsertBusyEventByExternalKeyCommand command,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<BusyEventManagementDto>.Failure(tenantError);
        }

        var normalizedExternalKey = NormalizeExternalKey(command.ExternalKey);
        var normalized = NormalizeDefinition(command.Definition) with { ExternalKey = normalizedExternalKey };
        var errors = ValidateDefinition(normalized);
        if (normalizedExternalKey == null)
        {
            errors.Add(CreateError(
                "busy-event.external-key.required",
                ManagementErrorCategory.Validation,
                "ExternalKey is required for idempotent upsert.",
                "externalKey"));
        }

        if (errors.Count > 0)
        {
            return ManagementResult<BusyEventManagementDto>.Failure(errors);
        }

        var resourceErrors = await ValidateResourcesAsync(normalized.ResourceIds, ct).ConfigureAwait(false);
        if (resourceErrors.Count > 0)
        {
            return ManagementResult<BusyEventManagementDto>.Failure(resourceErrors);
        }

        var existing = await _store.FindByExternalKeyAsync(normalizedExternalKey!, ct).ConfigureAwait(false);
        if (existing == null)
        {
            var created = await _store.CreateAsync(_tenantContext.TenantId, normalized, _clock.UtcNow, ct).ConfigureAwait(false);
            return ManagementResult<BusyEventManagementDto>.Success(created);
        }

        var updated = await _store.UpdateAsync(existing.Id, normalized, ct).ConfigureAwait(false);
        return ManagementResult<BusyEventManagementDto>.Success(updated);
    }

    public async Task<ManagementResult<BusyEventManagementDto>> UpdateBusyEventAsync(
        UpdateBusyEventCommand command,
        CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<BusyEventManagementDto>.Failure(tenantError);
        }

        var normalized = NormalizeDefinition(command.Definition);
        var errors = ValidateDefinition(normalized);
        if (command.BusyEventId <= 0)
        {
            errors.Add(CreateError("busy-event.id.required", ManagementErrorCategory.Validation, "BusyEventId must be positive.", "busyEventId"));
        }

        if (errors.Count > 0)
        {
            return ManagementResult<BusyEventManagementDto>.Failure(errors);
        }

        var existing = await _store.FindByIdAsync(command.BusyEventId, ct).ConfigureAwait(false);
        if (existing == null)
        {
            return NotFound(command.BusyEventId);
        }

        if (!existing.IsActive)
        {
            return ManagementResult<BusyEventManagementDto>.Failure(
                CreateError("busy-event.lifecycle.inactive", ManagementErrorCategory.InvalidOperation, "Inactive busy events cannot be updated.", "busyEventId"));
        }

        if (normalized.ExternalKey != null)
        {
            var existingByExternalKey = await _store.FindByExternalKeyAsync(normalized.ExternalKey, ct).ConfigureAwait(false);
            if (existingByExternalKey != null && existingByExternalKey.Id != command.BusyEventId)
            {
                return ManagementResult<BusyEventManagementDto>.Failure(
                    CreateError(
                        "busy-event.external-key.duplicate",
                        ManagementErrorCategory.Conflict,
                        $"Busy event with external key '{normalized.ExternalKey}' already exists.",
                        "externalKey"));
            }
        }

        var resourceErrors = await ValidateResourcesAsync(normalized.ResourceIds, ct).ConfigureAwait(false);
        if (resourceErrors.Count > 0)
        {
            return ManagementResult<BusyEventManagementDto>.Failure(resourceErrors);
        }

        var updated = await _store.UpdateAsync(command.BusyEventId, normalized, ct).ConfigureAwait(false);
        return ManagementResult<BusyEventManagementDto>.Success(updated);
    }

    public async Task<ManagementResult<BusyEventManagementDto>> GetBusyEventAsync(long busyEventId, CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<BusyEventManagementDto>.Failure(tenantError);
        }

        if (busyEventId <= 0)
        {
            return ManagementResult<BusyEventManagementDto>.Failure(
                CreateError("busy-event.id.required", ManagementErrorCategory.Validation, "BusyEventId must be positive.", "busyEventId"));
        }

        var existing = await _store.FindByIdAsync(busyEventId, ct).ConfigureAwait(false);
        return existing == null
            ? NotFound(busyEventId)
            : ManagementResult<BusyEventManagementDto>.Success(existing);
    }

    public async Task<IReadOnlyList<BusyEventManagementDto>> ListBusyEventsAsync(CancellationToken ct)
    {
        if (ValidateTenantContext() != null)
        {
            return Array.Empty<BusyEventManagementDto>();
        }

        return await _store.ListAsync(ct).ConfigureAwait(false);
    }

    public async Task<ManagementResult<BusyEventManagementDto>> CancelBusyEventAsync(long busyEventId, CancellationToken ct)
    {
        var tenantError = ValidateTenantContext();
        if (tenantError != null)
        {
            return ManagementResult<BusyEventManagementDto>.Failure(tenantError);
        }

        if (busyEventId <= 0)
        {
            return ManagementResult<BusyEventManagementDto>.Failure(
                CreateError("busy-event.id.required", ManagementErrorCategory.Validation, "BusyEventId must be positive.", "busyEventId"));
        }

        var existing = await _store.FindByIdAsync(busyEventId, ct).ConfigureAwait(false);
        if (existing == null)
        {
            return NotFound(busyEventId);
        }

        if (!existing.IsActive)
        {
            return ManagementResult<BusyEventManagementDto>.Failure(
                CreateError("busy-event.lifecycle.already-inactive", ManagementErrorCategory.InvalidOperation, "Busy event is already inactive.", "busyEventId"));
        }

        var updated = await _store.SetActiveAsync(busyEventId, false, ct).ConfigureAwait(false);
        return ManagementResult<BusyEventManagementDto>.Success(updated);
    }

    private async Task<List<ManagementError>> ValidateResourcesAsync(
        IReadOnlyList<int> resourceIds,
        CancellationToken ct)
    {
        var errors = new List<ManagementError>();

        for (var i = 0; i < resourceIds.Count; i++)
        {
            var resourceId = resourceIds[i];
            var resource = await _resourceStore.FindByIdAsync(resourceId, ct).ConfigureAwait(false);
            if (resource == null)
            {
                errors.Add(CreateError("busy-event.resource.not-found", ManagementErrorCategory.NotFound, $"Resource '{resourceId}' was not found.", "resourceIds"));
                continue;
            }

            if (!resource.IsActive || resource.IsArchived)
            {
                errors.Add(CreateError("busy-event.resource.inactive", ManagementErrorCategory.InvalidOperation, $"Resource '{resourceId}' must be active and not archived.", "resourceIds"));
            }
        }

        return errors;
    }

    private ManagementError? ValidateTenantContext()
    {
        return _tenantContext.TenantId == Guid.Empty
            ? CreateError("tenant.context.unresolved", ManagementErrorCategory.InvalidOperation, "Tenant context is not resolved.", "tenant")
            : null;
    }

    private static BusyEventDefinition NormalizeDefinition(BusyEventDefinition definition)
    {
        return definition with
        {
            ResourceIds = definition.ResourceIds.Where(id => id > 0).Distinct().OrderBy(id => id).ToList(),
            Title = string.IsNullOrWhiteSpace(definition.Title) ? null : definition.Title.Trim(),
            EventType = string.IsNullOrWhiteSpace(definition.EventType) ? null : definition.EventType.Trim(),
            ExternalKey = NormalizeExternalKey(definition.ExternalKey),
            StartUtc = DateTime.SpecifyKind(definition.StartUtc, definition.StartUtc.Kind),
            EndUtc = DateTime.SpecifyKind(definition.EndUtc, definition.EndUtc.Kind)
        };
    }

    private static List<ManagementError> ValidateDefinition(BusyEventDefinition definition)
    {
        var errors = new List<ManagementError>();

        if (definition.ResourceIds.Count == 0)
        {
            errors.Add(CreateError("busy-event.resources.required", ManagementErrorCategory.Validation, "At least one target resource is required.", "resourceIds"));
        }

        if (definition.StartUtc.Kind != DateTimeKind.Utc || definition.EndUtc.Kind != DateTimeKind.Utc)
        {
            errors.Add(CreateError("busy-event.utc.required", ManagementErrorCategory.Validation, "StartUtc and EndUtc must be UTC.", "timeRange"));
        }

        if (definition.EndUtc <= definition.StartUtc)
        {
            errors.Add(CreateError("busy-event.time-range.invalid", ManagementErrorCategory.Validation, "EndUtc must be greater than StartUtc.", "timeRange"));
        }

        if (definition.Title != null && definition.Title.Length > TitleMaxLength)
        {
            errors.Add(CreateError("busy-event.title.too-long", ManagementErrorCategory.Validation, $"Busy event title must be at most {TitleMaxLength} characters.", "title"));
        }

        if (definition.EventType != null && definition.EventType.Length > EventTypeMaxLength)
        {
            errors.Add(CreateError("busy-event.event-type.too-long", ManagementErrorCategory.Validation, $"Busy event type must be at most {EventTypeMaxLength} characters.", "eventType"));
        }

        if (definition.ExternalKey != null && definition.ExternalKey.Length > ExternalKeyMaxLength)
        {
            errors.Add(CreateError("busy-event.external-key.too-long", ManagementErrorCategory.Validation, $"Busy event external key must be at most {ExternalKeyMaxLength} characters.", "externalKey"));
        }

        return errors;
    }

    private static string? NormalizeExternalKey(string? externalKey)
    {
        return string.IsNullOrWhiteSpace(externalKey) ? null : externalKey.Trim();
    }

    private static ManagementResult<BusyEventManagementDto> NotFound(long busyEventId)
    {
        return ManagementResult<BusyEventManagementDto>.Failure(
            CreateError("busy-event.not-found", ManagementErrorCategory.NotFound, $"Busy event '{busyEventId}' was not found.", "busyEventId"));
    }

    private static ManagementError CreateError(string code, ManagementErrorCategory category, string message, string target)
    {
        return new ManagementError(code, category, message, target);
    }
}
