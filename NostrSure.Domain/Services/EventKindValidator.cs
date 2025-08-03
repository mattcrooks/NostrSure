using NostrSure.Domain.Entities;
using NostrSure.Domain.Validation;
using NostrSure.Domain.ValueObjects;

namespace NostrSure.Domain.Services;

/// <summary>
/// Validates event kinds according to supported EventKind enum values
/// </summary>
public sealed class EventKindValidator : IEventKindValidator
{
    public ValidationResult ValidateKind(NostrEvent evt)
    {
        if (!Enum.IsDefined(typeof(EventKind), evt.Kind))
        {
            return ValidationResult.Failure(
                $"Unknown event kind: {evt.Kind}",
                "UNKNOWN_EVENT_KIND");
        }

        return ValidationResult.Success();
    }
}