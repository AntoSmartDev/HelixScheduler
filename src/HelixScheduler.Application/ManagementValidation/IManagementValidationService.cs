namespace HelixScheduler.Application.ManagementValidation;

public interface IManagementValidationService
{
    Task<ManagementValidationResult> ValidateTenantModelAsync(CancellationToken ct);
    Task<ManagementValidationResult> ValidateResourceConfigurationAsync(int resourceId, CancellationToken ct);
}
