using NBitcoin.Secp256k1;
using NostrSure.Domain.Validation;

namespace NostrSure.Domain.Services;

/// <summary>
/// High-performance cryptographic service optimized for Nostr operations
/// </summary>
public sealed class OptimizedCryptographicService : ICryptographicService
{
    public bool VerifySchnorrSignature(ReadOnlySpan<byte> signature, ReadOnlySpan<byte> message, ReadOnlySpan<byte> publicKey)
    {
        // Validate input parameters
        if (signature.Length != 64 || message.Length != 32 || publicKey.Length != 32)
            return false;

        // Create pubkey and signature objects using NBitcoin
        if (!ECXOnlyPubKey.TryCreate(publicKey, out var pubkey))
            return false;

        if (!SecpSchnorrSignature.TryCreate(signature, out var sig))
            return false;

        // Verify signature using BIP-340 standard
        return pubkey.SigVerifyBIP340(sig, message);
    }
}