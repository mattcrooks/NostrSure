using Microsoft.Extensions.Caching.Memory;
using NostrSure.Domain.Entities;
using NostrSure.Domain.Validation;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace NostrSure.Domain.Services;

/// <summary>
/// High-performance event ID calculator with caching support
/// This implementation closely matches the original legacy implementation to ensure compatibility
/// </summary>
public sealed class CachedEventIdCalculator : IEventIdCalculator
{
    private readonly IMemoryCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;
    private static readonly SHA256 _sha256 = SHA256.Create();

    public CachedEventIdCalculator(IMemoryCache cache)
    {
        _cache = cache;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    public string CalculateEventId(NostrEvent evt)
    {
        // Create cache key based on event content that affects the hash
        var cacheKey = CreateCacheKey(evt);

        if (_cache.TryGetValue(cacheKey, out string? cachedId))
            return cachedId!;

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
        var eventId = Convert.ToHexString(hash).ToLowerInvariant();

        // Cache for 5 minutes to balance memory usage and performance
        _cache.Set(cacheKey, eventId, TimeSpan.FromMinutes(5));
        return eventId;
    }

    private string CreateCacheKey(NostrEvent evt)
    {
        // Create a hash-based cache key that includes all fields that affect the event ID
        var keyBuilder = new StringBuilder(200);
        keyBuilder.Append("eventid:");
        keyBuilder.Append(evt.Pubkey.Value);
        keyBuilder.Append(':');
        keyBuilder.Append(evt.CreatedAt.ToUnixTimeSeconds());
        keyBuilder.Append(':');
        keyBuilder.Append((int)evt.Kind);
        keyBuilder.Append(':');
        keyBuilder.Append(evt.Content.GetHashCode());
        keyBuilder.Append(':');

        // Include a hash of tags to handle tag changes
        if (evt.Tags.Count > 0)
        {
            var tagsHash = 0;
            foreach (var tag in evt.Tags)
            {
                tagsHash = HashCode.Combine(tagsHash, tag.Name.GetHashCode());
                foreach (var value in tag.Values)
                {
                    tagsHash = HashCode.Combine(tagsHash, value.GetHashCode());
                }
            }
            keyBuilder.Append(tagsHash);
        }

        return keyBuilder.ToString();
    }
}