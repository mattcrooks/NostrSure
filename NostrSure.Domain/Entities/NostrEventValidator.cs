using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using NostrSure.Domain.ValueObjects;
using NBitcoin.Secp256k1;

namespace NostrSure.Domain.Entities;

public sealed class NostrEventValidator : INostrEventValidator
{
    public bool ValidateSignature(NostrEvent evt, out string error)
    {
        // NIP-01: Verify the signature against the provided event ID
        // Note: Event ID validation is separate from signature validation
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

            var eventIdBytes = ParseHex(evt.Id);
            var pubkeyBytes = ParseHex(evt.Pubkey.Value);
            var sigBytes = ParseHex(evt.Sig);

            if (eventIdBytes.Length != 32)
            {
                error = "Invalid event ID length (must be 32 bytes).";
                return false;
            }
            if (pubkeyBytes.Length != 32)
            {
                error = "Invalid pubkey length for x-only pubkey (must be 32 bytes).";
                return false;
            }
            if (sigBytes.Length != 64)
            {
                error = "Invalid signature length for Schnorr signature (must be 64 bytes).";
                return false;
            }

            if (!ECXOnlyPubKey.TryCreate(pubkeyBytes, out var pubkey))
            {
                error = "Invalid pubkey format.";
                return false;
            }
            if (!SecpSchnorrSignature.TryCreate(sigBytes, out var sig))
            {
                error = "Invalid Schnorr signature bytes.";
                return false;
            }

            // Use the provided event ID as the hash for signature verification
            bool valid = pubkey.SigVerifyBIP340(sig, eventIdBytes);
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
        // Validate that the event ID matches the calculated hash
        try
        {
            var calculatedId = CalculateEventId(evt);
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

    private string CalculateEventId(NostrEvent evt)
    {
        // NIP-01: Serialize as [0, pubkey, created_at, kind, tags, content]
        // Manual construction to ensure canonical JSON format

        var tagsJson = "[" + string.Join(",", evt.Tags.Select(tag =>
            "[" + string.Join(",", tag.Select(t => JsonSerializer.Serialize(t))) + "]")) + "]";

        var manualJson = $"[0,{JsonSerializer.Serialize(evt.Pubkey.Value)},{evt.CreatedAt.ToUnixTimeSeconds()},{(int)evt.Kind},{tagsJson},{JsonSerializer.Serialize(evt.Content)}]";

        var utf8Bytes = Encoding.UTF8.GetBytes(manualJson);
        var hash = NBitcoin.Crypto.Hashes.SHA256(utf8Bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
    private static byte[] ParseHex(string hex)
    {
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            hex = hex.Substring(2);
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }

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
            if (tag == null || tag.Count == 0)
            {
                error = "Tag is empty or null.";
                return false;
            }
            if (tag.Any(string.IsNullOrWhiteSpace))
            {
                error = "Tag contains empty value.";
                return false;
            }
        }
        error = string.Empty;
        return true;
    }
}