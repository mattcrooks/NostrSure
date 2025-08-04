using NostrSure.Domain.Entities;
using NostrSure.Domain.ValueObjects;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NostrSure.Infrastructure.Serialization;

public sealed class NostrEventJsonConverter : JsonConverter<NostrEvent>
{
    // Pre-allocated property name spans for faster comparison
    private static ReadOnlySpan<byte> IdPropertyName => "id"u8;
    private static ReadOnlySpan<byte> PubkeyPropertyName => "pubkey"u8;
    private static ReadOnlySpan<byte> CreatedAtPropertyName => "created_at"u8;
    private static ReadOnlySpan<byte> KindPropertyName => "kind"u8;
    private static ReadOnlySpan<byte> TagsPropertyName => "tags"u8;
    private static ReadOnlySpan<byte> ContentPropertyName => "content"u8;
    private static ReadOnlySpan<byte> SigPropertyName => "sig"u8;

    // Valid EventKind values for fast lookup
    private static readonly HashSet<int> ValidEventKinds = new()
    {
        (int)EventKind.Note,
        (int)EventKind.ContactList,
        (int)EventKind.Zap
    };

    public override NostrEvent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            ThrowInvalidJsonException();

        string? id = null;
        string? pubkey = null;
        long? createdAt = null;
        int? kindInt = null;
        List<NostrTag>? tags = null;
        string? content = null;
        string? sig = null;

        var fieldsFound = 0;
        const int requiredFields = 7;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                ThrowInvalidJsonException();

            // Use ValueSpan for zero-allocation property name comparison
            var propertyNameSpan = reader.ValueSpan;
            reader.Read();

            if (propertyNameSpan.SequenceEqual(IdPropertyName))
            {
                id = reader.GetString();
                fieldsFound++;
            }
            else if (propertyNameSpan.SequenceEqual(PubkeyPropertyName))
            {
                pubkey = reader.GetString();
                fieldsFound++;
            }
            else if (propertyNameSpan.SequenceEqual(CreatedAtPropertyName))
            {
                createdAt = reader.GetInt64();
                fieldsFound++;
            }
            else if (propertyNameSpan.SequenceEqual(KindPropertyName))
            {
                kindInt = reader.GetInt32();
                fieldsFound++;
            }
            else if (propertyNameSpan.SequenceEqual(TagsPropertyName))
            {
                tags = ReadTagsOptimized(ref reader);
                fieldsFound++;
            }
            else if (propertyNameSpan.SequenceEqual(ContentPropertyName))
            {
                content = reader.GetString();
                fieldsFound++;
            }
            else if (propertyNameSpan.SequenceEqual(SigPropertyName))
            {
                sig = reader.GetString();
                fieldsFound++;
            }
            else
            {
                reader.Skip();
            }
        }

        // Validate all required fields are present and handle nullable types properly
        if (fieldsFound != requiredFields ||
            id is null || pubkey is null || createdAt is null ||
            kindInt is null || tags is null)
        {
            ThrowMissingRequiredFieldsException();
        }

        // Fast EventKind validation using HashSet lookup
        if (!ValidEventKinds.Contains(kindInt.Value))
            ThrowUnknownEventKindException(kindInt.Value);

        return new NostrEvent(
            id ?? string.Empty,
            new Pubkey(pubkey ?? string.Empty),
            DateTimeOffset.FromUnixTimeSeconds(createdAt.Value),
            (EventKind)kindInt.Value,
            tags ?? new List<NostrTag>(),
            content ?? string.Empty,
            sig ?? string.Empty
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static List<NostrTag> ReadTagsOptimized(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            ThrowInvalidJsonException();

        var tags = new List<NostrTag>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            if (reader.TokenType != JsonTokenType.StartArray)
                ThrowInvalidJsonException();

            // Pre-allocate list for tag elements
            var tagElements = new List<string>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;

                if (reader.TokenType != JsonTokenType.String)
                    ThrowInvalidJsonException();

                var value = reader.GetString();
                if (value is not null)
                    tagElements.Add(value);
            }

            // Only create NostrTag if we have at least one element (the tag name)
            if (tagElements.Count > 0)
            {
                // Use NostrTag.FromArray for proper validation and construction
                tags.Add(NostrTag.FromArray(tagElements));
            }
        }

        return tags;
    }

    public override void Write(Utf8JsonWriter writer, NostrEvent value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Write properties in optimal order (most common lookups first)
        writer.WriteString(IdPropertyName, value.Id);
        writer.WriteString(PubkeyPropertyName, value.Pubkey.Value);
        writer.WriteNumber(CreatedAtPropertyName, value.CreatedAt.ToUnixTimeSeconds());
        writer.WriteNumber(KindPropertyName, (int)value.Kind);

        // Inline tags serialization for better performance
        writer.WritePropertyName(TagsPropertyName);
        WriteTagsOptimized(writer, value.Tags);

        writer.WriteString(ContentPropertyName, value.Content);
        writer.WriteString(SigPropertyName, value.Sig);

        writer.WriteEndObject();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteTagsOptimized(Utf8JsonWriter writer, IReadOnlyList<NostrTag> tags)
    {
        writer.WriteStartArray();

        foreach (var tag in tags)
        {
            writer.WriteStartArray();

            // Write tag name first
            writer.WriteStringValue(tag.Name);

            // Write tag values
            foreach (var value in tag.Values)
            {
                writer.WriteStringValue(value);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndArray();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInvalidJsonException() =>
        throw new JsonException("Invalid JSON format for NostrEvent");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowMissingRequiredFieldsException() =>
        throw new JsonException("Missing required NostrEvent fields");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowUnknownEventKindException(int kind) =>
        throw new JsonException($"Unknown event kind: {kind}");
}