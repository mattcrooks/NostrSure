using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NostrSure.Domain.ValueObjects;
using NBitcoin.Secp256k1;
using NostrSure.Domain.Interfaces;
using NostrSure.Domain.Services;
using NostrSure.Domain.Validation;

namespace NostrSure.Domain.Entities;

/// <summary>
/// Legacy NostrEventValidator maintained for backward compatibility.
/// Consider migrating to ModularNostrEventValidator for better performance and testability.
/// </summary>
public sealed class NostrEventValidator : INostrEventValidator
{
    // Internal services for improved performance while maintaining compatibility
    private readonly IHexConverter _hexConverter;
    private readonly ICryptographicService _cryptographicService;
    private readonly IEventIdCalculator _eventIdCalculator;

    public NostrEventValidator()
    {
        // Use optimized services internally for better performance
        _hexConverter = new OptimizedHexConverter();
        _cryptographicService = new OptimizedCryptographicService();
        _eventIdCalculator = new SimpleEventIdCalculator();
    }

    #region New Async Methods

    public Task<ValidationResult> ValidateAsync(NostrEvent evt, CancellationToken cancellationToken = default)
    {
        var result = Validate(evt);
        return Task.FromResult(result);
    }

    public ValidationResult Validate(NostrEvent evt)
    {
        // Validate all components and return the first failure or success
        if (!ValidateKind(evt, out var kindError))
            return ValidationResult.Failure(kindError, "INVALID_KIND");

        if (!ValidateTags(evt, out var tagError))
            return ValidationResult.Failure(tagError, "INVALID_TAGS");

        if (!ValidateEventId(evt, out var eventIdError))
            return ValidationResult.Failure(eventIdError, "INVALID_EVENT_ID");

        if (!ValidateSignature(evt, out var signatureError))
            return ValidationResult.Failure(signatureError, "INVALID_SIGNATURE");

        return ValidationResult.Success();
    }

    #endregion

    #region Legacy Methods

    public bool ValidateSignature(NostrEvent evt, out string error)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(evt.Sig))
            {
                error = "Signature is empty.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(evt.Pubkey?.Value))
            {
                error = "Pubkey is empty.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(evt.Id))
            {
                error = "Event ID is empty.";
                return false;
            }

            // Use optimized hex converter with stack allocation
            Span<byte> eventIdBytes = stackalloc byte[32];
            Span<byte> pubkeyBytes = stackalloc byte[32];
            Span<byte> sigBytes = stackalloc byte[64];

            if (!_hexConverter.TryParseHex(evt.Id, eventIdBytes, out var eventIdLength) || eventIdLength != 32)
            {
                error = "Invalid event ID length (must be 32 bytes).";
                return false;
            }
            if (!_hexConverter.TryParseHex(evt.Pubkey.Value, pubkeyBytes, out var pubkeyLength) || pubkeyLength != 32)
            {
                error = "Invalid pubkey length for x-only pubkey (must be 32 bytes).";
                return false;
            }
            if (!_hexConverter.TryParseHex(evt.Sig, sigBytes, out var sigLength) || sigLength != 64)
            {
                error = "Invalid signature length for Schnorr signature (must be 64 bytes).";
                return false;
            }

            // Use optimized cryptographic service
            bool valid = _cryptographicService.VerifySchnorrSignature(sigBytes, eventIdBytes, pubkeyBytes);
            if (!valid)
            {
                error = "Signature verification failed.";
                return false;
            }
            
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            error = $"Exception during signature validation: {ex.Message}";
            return false;
        }
    }

    public bool ValidateEventId(NostrEvent evt, out string error)
    {
        try
        {
            var calculatedId = _eventIdCalculator.CalculateEventId(evt);
            if (evt.Id != calculatedId)
            {
                error = $"Event ID mismatch. Expected: {calculatedId}, Got: {evt.Id}";
                return false;
            }
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            error = $"Exception during event ID validation: {ex.Message}";
            return false;
        }
    }

    public bool ValidateKind(NostrEvent evt, out string error)
    {
        if (!Enum.IsDefined(typeof(EventKind), evt.Kind))
        {
            error = $"Unknown event kind: {evt.Kind}";
            return false;
        }
        error = string.Empty;
        return true;
    }

    public bool ValidateTags(NostrEvent evt, out string error)
    {
        if (evt.Tags == null)
        {
            error = "Tags are null.";
            return false;
        }
        
        foreach (var tag in evt.Tags)
        {
            if (tag == null)
            {
                error = "Tag is null.";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(tag.Name))
            {
                error = "Tag name is empty or null.";
                return false;
            }
            
            if (!tag.IsValid())
            {
                error = $"Invalid tag: {tag.Name}";
                return false;
            }
            
            if (tag.Values.Any(string.IsNullOrWhiteSpace))
            {
                error = "Tag contains empty value.";
                return false;
            }
        }
        
        error = string.Empty;
        return true;
    }

    #endregion

    #region Legacy Helper Methods (Deprecated)

    [Obsolete("This method uses legacy hex parsing. Consider using ModularNostrEventValidator for better performance.")]
    private string CalculateEventId(NostrEvent evt)
    {
        // Keep legacy implementation for backward compatibility
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        
        var tagsArrays = evt.Tags.Select(tag => 
        {
            var array = new List<string> { tag.Name };
            array.AddRange(tag.Values);
            return array.ToArray();
        }).ToArray();
        
        var eventArray = new object[]
        {
            0,
            evt.Pubkey.Value,
            evt.CreatedAt.ToUnixTimeSeconds(),
            (int)evt.Kind,
            tagsArrays,
            evt.Content
        };
        
        var serialized = JsonSerializer.Serialize(eventArray, options);
        var utf8Bytes = Encoding.UTF8.GetBytes(serialized);
        var hash = NBitcoin.Crypto.Hashes.SHA256(utf8Bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
    
    [Obsolete("This method uses legacy hex parsing. Consider using OptimizedHexConverter for better performance.")]
    private static byte[] ParseHex(string hex)
    {
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            hex = hex.Substring(2);
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }

    [Obsolete("This method is unused. Consider using OptimizedHexConverter.TryParseHex instead.")]
    private static bool TryParseHex(string hex, out byte[] bytes)
    {
        try
        {
            bytes = ParseHex(hex);
            return true;
        }
        catch
        {
            bytes = Array.Empty<byte>();
            return false;
        }
    }

    #endregion
}