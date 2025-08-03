using NostrSure.Domain.Entities;
using NostrSure.Domain.Validation;

namespace NostrSure.Domain.Services;

/// <summary>
/// Validates cryptographic signatures using optimized Schnorr verification
/// </summary>
public sealed class EventSignatureValidator : IEventSignatureValidator
{
    private readonly ICryptographicService _cryptographicService;
    private readonly IHexConverter _hexConverter;

    public EventSignatureValidator(ICryptographicService cryptographicService, IHexConverter hexConverter)
    {
        _cryptographicService = cryptographicService;
        _hexConverter = hexConverter;
    }

    public Task<ValidationResult> ValidateSignatureAsync(NostrEvent evt, CancellationToken cancellationToken = default)
    {
        var result = ValidateSignature(evt);
        return Task.FromResult(result);
    }

    public ValidationResult ValidateSignature(NostrEvent evt)
    {
        // Quick validation of required fields
        if (string.IsNullOrWhiteSpace(evt.Sig))
            return ValidationResult.Failure("Signature is empty", "EMPTY_SIGNATURE");

        if (string.IsNullOrWhiteSpace(evt.Pubkey?.Value))
            return ValidationResult.Failure("Pubkey is empty", "EMPTY_PUBKEY");

        if (string.IsNullOrWhiteSpace(evt.Id))
            return ValidationResult.Failure("Event ID is empty", "EMPTY_EVENT_ID");

        try
        {
            // Parse hex strings using optimized converter
            Span<byte> eventIdBytes = stackalloc byte[32];
            Span<byte> pubkeyBytes = stackalloc byte[32];
            Span<byte> sigBytes = stackalloc byte[64];

            if (!_hexConverter.TryParseHex(evt.Id, eventIdBytes, out var eventIdLength) || eventIdLength != 32)
                return ValidationResult.Failure("Invalid event ID length (must be 32 bytes)", "INVALID_EVENT_ID_LENGTH");

            if (!_hexConverter.TryParseHex(evt.Pubkey.Value, pubkeyBytes, out var pubkeyLength) || pubkeyLength != 32)
                return ValidationResult.Failure("Invalid pubkey length (must be 32 bytes)", "INVALID_PUBKEY_LENGTH");

            if (!_hexConverter.TryParseHex(evt.Sig, sigBytes, out var sigLength) || sigLength != 64)
                return ValidationResult.Failure("Invalid signature length (must be 64 bytes)", "INVALID_SIGNATURE_LENGTH");

            // Verify signature using cryptographic service
            var isValid = _cryptographicService.VerifySchnorrSignature(sigBytes, eventIdBytes, pubkeyBytes);

            return isValid
                ? ValidationResult.Success()
                : ValidationResult.Failure("Signature verification failed", "SIGNATURE_VERIFICATION_FAILED");
        }
        catch (System.Exception ex)
        {
            return ValidationResult.Failure($"Exception during signature validation: {ex.Message}", ex, ValidationSeverity.Critical);
        }
    }
}