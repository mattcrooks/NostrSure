using System.Text.Json;
using NostrSure.Domain.Entities;
using NostrSure.Infrastructure.Client.Abstractions;
using NostrSure.Infrastructure.Client.Messages;
using NostrSure.Infrastructure.Serialization;

namespace NostrSure.Infrastructure.Client.Implementation;

/// <summary>
/// JSON serializer for Nostr protocol messages
/// </summary>
public class JsonMessageSerializer : IMessageSerializer
{
    private readonly JsonSerializerOptions _options;

    public JsonMessageSerializer()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Converters = { new NostrEventJsonConverter() }
        };
    }

    public string Serialize(object[] message)
    {
        if (message == null || message.Length == 0)
            throw new ArgumentException("Message cannot be null or empty", nameof(message));

        // All outbound messages are valid JSON arrays (requirement R2) since we serialize object arrays
        return JsonSerializer.Serialize(message, _options);
    }

    public NostrMessage Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON cannot be null or empty", nameof(json));

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
                throw new ArgumentException("Nostr messages must be JSON arrays");

            var arrayLength = root.GetArrayLength();
            if (arrayLength == 0)
                throw new ArgumentException("Nostr message array cannot be empty");

            var messageType = root[0].GetString();
            if (string.IsNullOrEmpty(messageType))
                throw new ArgumentException("Message type cannot be null or empty");

            return messageType switch
            {
                "EVENT" when arrayLength == 3 => ParseRelayEventMessage(root),
                "EOSE" when arrayLength == 2 => new EoseMessage(root[1].GetString()!),
                "NOTICE" when arrayLength == 2 => new NoticeMessage(root[1].GetString()!),
                "CLOSED" when arrayLength >= 2 => new ClosedMessage(
                    root[1].GetString()!,
                    arrayLength > 2 ? root[2].GetString()! : ""),
                "OK" when arrayLength >= 3 => new OkMessage(
                    root[1].GetString()!,
                    root[2].GetBoolean(),
                    arrayLength > 3 ? root[3].GetString()! : ""),
                _ => throw new ArgumentException($"Unknown or malformed message type: {messageType}")
            };
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON: {ex.Message}", ex);
        }
    }

    // Removed IsValidJsonArray method; validation is handled in Serialize.

    private RelayEventMessage ParseRelayEventMessage(JsonElement root)
    {
        var subscriptionId = root[1].GetString()!;
        var eventJson = root[2].GetRawText();
        
        var nostrEvent = JsonSerializer.Deserialize<NostrEvent>(eventJson, _options);
        if (nostrEvent == null)
            throw new ArgumentException("Failed to parse NostrEvent from relay EVENT message");

        return new RelayEventMessage(subscriptionId, nostrEvent);
    }
}