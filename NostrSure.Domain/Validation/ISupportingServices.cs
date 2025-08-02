using System;
using NostrSure.Domain.Entities;

namespace NostrSure.Domain.Validation;

/// <summary>
/// Service for calculating event IDs according to NIP-01 specification
/// </summary>
public interface IEventIdCalculator
{
    /// <summary>
    /// Calculates the event ID hash for a given NostrEvent
    /// </summary>
    string CalculateEventId(NostrEvent evt);
}

/// <summary>
/// High-performance cryptographic operations for Nostr events
/// </summary>
public interface ICryptographicService
{
    /// <summary>
    /// Verifies a Schnorr signature using BIP-340 standard
    /// </summary>
    bool VerifySchnorrSignature(ReadOnlySpan<byte> signature, ReadOnlySpan<byte> message, ReadOnlySpan<byte> publicKey);
}

/// <summary>
/// Optimized hexadecimal string parsing and formatting
/// </summary>
public interface IHexConverter
{
    /// <summary>
    /// Attempts to parse a hexadecimal string into a byte array
    /// </summary>
    bool TryParseHex(ReadOnlySpan<char> hex, Span<byte> bytes, out int bytesWritten);
    
    /// <summary>
    /// Parses a hexadecimal string into a byte array, throwing on invalid input
    /// </summary>
    byte[] ParseHex(ReadOnlySpan<char> hex);
    
    /// <summary>
    /// Legacy string-based parsing for backward compatibility
    /// </summary>
    byte[] ParseHex(string hex);
}