using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NostrSure.Domain.Entities;
using NostrSure.Domain.Interfaces;
using NostrSure.Domain.Validation;

namespace NostrSure.Domain.Services;

/// <summary>
/// Main validator that orchestrates the validation pipeline with high performance and backward compatibility
/// </summary>
public sealed class ModularNostrEventValidator : INostrEventValidator
{
    private readonly IEventSignatureValidator _signatureValidator;
    private readonly IEventIdValidator _eventIdValidator;
    private readonly IEventKindValidator _kindValidator;
    private readonly IEventTagValidator _tagValidator;
    private readonly ILogger<ModularNostrEventValidator>? _logger;

    public ModularNostrEventValidator(
        IEventSignatureValidator signatureValidator,
        IEventIdValidator eventIdValidator,
        IEventKindValidator kindValidator,
        IEventTagValidator tagValidator,
        ILogger<ModularNostrEventValidator>? logger = null)
    {
        _signatureValidator = signatureValidator;
        _eventIdValidator = eventIdValidator;
        _kindValidator = kindValidator;
        _tagValidator = tagValidator;
        _logger = logger;
    }

    public async Task<ValidationResult> ValidateAsync(NostrEvent evt, CancellationToken cancellationToken = default)
    {
        // Fast path: validate cheap operations first
        var kindResult = _kindValidator.ValidateKind(evt);
        if (!kindResult.IsValid) 
        {
            _logger?.LogWarning("Event kind validation failed: {Error}", kindResult.Error?.Message);
            return kindResult;
        }

        var tagResult = _tagValidator.ValidateTags(evt);
        if (!tagResult.IsValid) 
        {
            _logger?.LogWarning("Event tag validation failed: {Error}", tagResult.Error?.Message);
            return tagResult;
        }

        // Expensive operations with async support for potential parallelization
        var eventIdTask = _eventIdValidator.ValidateEventIdAsync(evt, cancellationToken);
        var signatureTask = _signatureValidator.ValidateSignatureAsync(evt, cancellationToken);

        // Wait for both expensive operations to complete
        await Task.WhenAll(eventIdTask, signatureTask);

        var eventIdResult = eventIdTask.Result;
        if (!eventIdResult.IsValid) 
        {
            _logger?.LogWarning("Event ID validation failed: {Error}", eventIdResult.Error?.Message);
            return eventIdResult;
        }

        var signatureResult = signatureTask.Result;
        if (!signatureResult.IsValid)
        {
            _logger?.LogWarning("Signature validation failed: {Error}", signatureResult.Error?.Message);
            return signatureResult;
        }

        _logger?.LogDebug("Event validation completed successfully for event {EventId}", evt.Id);
        return ValidationResult.Success();
    }

    public ValidationResult Validate(NostrEvent evt) 
    {
        return ValidateAsync(evt).GetAwaiter().GetResult();
    }

    #region Legacy Compatibility Methods

    public bool ValidateSignature(NostrEvent evt, out string error)
    {
        var result = _signatureValidator.ValidateSignature(evt);
        error = result.Error?.Message ?? string.Empty;
        return result.IsValid;
    }

    public bool ValidateKind(NostrEvent evt, out string error)
    {
        var result = _kindValidator.ValidateKind(evt);
        error = result.Error?.Message ?? string.Empty;
        return result.IsValid;
    }

    public bool ValidateTags(NostrEvent evt, out string error)
    {
        var result = _tagValidator.ValidateTags(evt);
        error = result.Error?.Message ?? string.Empty;
        return result.IsValid;
    }

    public bool ValidateEventId(NostrEvent evt, out string error)
    {
        var result = _eventIdValidator.ValidateEventId(evt);
        error = result.Error?.Message ?? string.Empty;
        return result.IsValid;
    }

    #endregion
}