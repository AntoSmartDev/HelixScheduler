namespace HelixScheduler.Application.Management.Validation;

public interface IManagementValidationService
{
    Task<ManagementValidationResult> ValidateTenantModelAsync(CancellationToken ct);
    Task<ManagementValidationResult> ValidateResourceConfigurationAsync(int resourceId, CancellationToken ct);
}
