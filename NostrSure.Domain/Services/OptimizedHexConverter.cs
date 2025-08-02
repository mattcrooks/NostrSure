using System;
using NostrSure.Domain.Validation;

namespace NostrSure.Domain.Services;

/// <summary>
/// High-performance hexadecimal converter using lookup tables for optimal speed
/// </summary>
public sealed class OptimizedHexConverter : IHexConverter
{
    // Precomputed lookup table for hex character to byte conversion
    private static readonly byte[] HexLookup = new byte[128];
    
    static OptimizedHexConverter()
    {
        // Initialize lookup table
        for (int i = 0; i < HexLookup.Length; i++)
            HexLookup[i] = 255; // Invalid marker
            
        // Set valid hex characters
        for (int i = 0; i <= 9; i++)
            HexLookup['0' + i] = (byte)i;
            
        for (int i = 0; i <= 5; i++)
        {
            HexLookup['A' + i] = (byte)(10 + i);
            HexLookup['a' + i] = (byte)(10 + i);
        }
    }

    public bool TryParseHex(ReadOnlySpan<char> hex, Span<byte> bytes, out int bytesWritten)
    {
        bytesWritten = 0;
        
        // Handle 0x prefix
        if (hex.Length >= 2 && hex[0] == '0' && (hex[1] == 'x' || hex[1] == 'X'))
            hex = hex[2..];
            
        if (hex.Length % 2 != 0) 
            return false;
            
        if (bytes.Length < hex.Length / 2) 
            return false;

        for (int i = 0; i < hex.Length; i += 2)
        {
            var c1 = (int)hex[i];
            var c2 = (int)hex[i + 1];
            
            if (c1 >= HexLookup.Length || c2 >= HexLookup.Length) 
                return false;
            
            var b1 = HexLookup[c1];
            var b2 = HexLookup[c2];
            
            if (b1 == 255 || b2 == 255) 
                return false;
            
            bytes[bytesWritten++] = (byte)((b1 << 4) | b2);
        }

        return true;
    }

    public byte[] ParseHex(ReadOnlySpan<char> hex)
    {
        var expectedLength = hex.Length;
        
        // Handle 0x prefix
        if (hex.Length >= 2 && hex[0] == '0' && (hex[1] == 'x' || hex[1] == 'X'))
        {
            hex = hex[2..];
            expectedLength -= 2;
        }
        
        var bytes = new byte[expectedLength / 2];
        if (!TryParseHex(hex, bytes, out _))
            throw new ArgumentException("Invalid hex string", nameof(hex));
        return bytes;
    }

    public byte[] ParseHex(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            return Array.Empty<byte>();
            
        return ParseHex(hex.AsSpan());
    }
}