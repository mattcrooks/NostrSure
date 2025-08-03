using NostrSure.Domain.Entities;

namespace NostrSure.Domain.Validation;

/// <summary>
/// Validates cryptographic signatures using Schnorr signatures on secp256k1
/// </summary>
public interface IEventSignatureValidator
{
    Task<ValidationResult> ValidateSignatureAsync(NostrEvent evt, CancellationToken cancellationToken = default);
    ValidationResult ValidateSignature(NostrEvent evt);
}

/// <summary>
/// Validates that event IDs match the calculated hash according to NIP-01
/// </summary>
public interface IEventIdValidator
{
    Task<ValidationResult> ValidateEventIdAsync(NostrEvent evt, CancellationToken cancellationToken = default);
    ValidationResult ValidateEventId(NostrEvent evt);
}

/// <summary>
/// Validates event kinds according to supported EventKind enum values
/// </summary>
public interface IEventKindValidator
{
    ValidationResult ValidateKind(NostrEvent evt);
}

/// <summary>
/// Validates tag structure and content according to NIP-01 rules
/// </summary>
public interface IEventTagValidator
{
    ValidationResult ValidateTags(NostrEvent evt);
}