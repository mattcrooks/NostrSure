using System.Threading;
using System.Threading.Tasks;
using NostrSure.Domain.Entities;
using NostrSure.Domain.Validation;

namespace NostrSure.Domain.Services;

/// <summary>
/// Validates event IDs against calculated hashes according to NIP-01
/// </summary>
public sealed class EventIdValidator : IEventIdValidator
{
    private readonly IEventIdCalculator _eventIdCalculator;

    public EventIdValidator(IEventIdCalculator eventIdCalculator)
    {
        _eventIdCalculator = eventIdCalculator;
    }

    public Task<ValidationResult> ValidateEventIdAsync(NostrEvent evt, CancellationToken cancellationToken = default)
    {
        var result = ValidateEventId(evt);
        return Task.FromResult(result);
    }

    public ValidationResult ValidateEventId(NostrEvent evt)
    {
        try
        {
            var calculatedId = _eventIdCalculator.CalculateEventId(evt);
            
            if (evt.Id != calculatedId)
            {
                return ValidationResult.Failure(
                    $"Event ID mismatch. Expected: {calculatedId}, Got: {evt.Id}", 
                    "EVENT_ID_MISMATCH");
            }
            
            return ValidationResult.Success();
        }
        catch (System.Exception ex)
        {
            return ValidationResult.Failure(
                $"Exception during event ID validation: {ex.Message}", 
                ex, 
                ValidationSeverity.Critical);
        }
    }
}