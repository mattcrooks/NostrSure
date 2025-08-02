using System.Threading;
using System.Threading.Tasks;
using NostrSure.Domain.Entities;
using NostrSure.Domain.Validation;

namespace NostrSure.Domain.Interfaces;

/// <summary>
/// Interface for validating Nostr events according to NIP-01 specifications.
/// Supports both legacy synchronous methods and new asynchronous validation pipeline.
/// </summary>
public interface INostrEventValidator
{
    /// <summary>
    /// Validates a Nostr event asynchronously using the full validation pipeline (new)
    /// </summary>
    Task<ValidationResult> ValidateAsync(NostrEvent evt, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates a Nostr event synchronously using the full validation pipeline (new)
    /// </summary>
    ValidationResult Validate(NostrEvent evt);
    
    /// <summary>
    /// Legacy method: Validates the cryptographic signature of an event
    /// </summary>
    bool ValidateSignature(NostrEvent evt, out string error);
    
    /// <summary>
    /// Legacy method: Validates the event kind according to supported types
    /// </summary>
    bool ValidateKind(NostrEvent evt, out string error);
    
    /// <summary>
    /// Legacy method: Validates the tag structure and content
    /// </summary>
    bool ValidateTags(NostrEvent evt, out string error);
    
    /// <summary>
    /// Legacy method: Validates the event ID matches the calculated hash
    /// </summary>
    bool ValidateEventId(NostrEvent evt, out string error);
}