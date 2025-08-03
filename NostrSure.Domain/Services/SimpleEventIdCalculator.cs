using NostrSure.Domain.Entities;
using NostrSure.Domain.Validation;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace NostrSure.Domain.Services;

/// <summary>
/// Simple event ID calculator without caching for scenarios where caching is not desired
/// This implementation closely matches the original legacy implementation to ensure compatibility
/// </summary>
public sealed class SimpleEventIdCalculator : IEventIdCalculator
{
    private readonly JsonSerializerOptions _jsonOptions;
    private static readonly SHA256 _sha256 = SHA256.Create();

    public SimpleEventIdCalculator()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    public string CalculateEventId(NostrEvent evt)
    {
        // Match the exact legacy implementation format for compatibility
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

        var serialized = JsonSerializer.Serialize(eventArray, _jsonOptions);

        var utf8Bytes = Encoding.UTF8.GetBytes(serialized);
        var hash = NBitcoin.Crypto.Hashes.SHA256(utf8Bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}