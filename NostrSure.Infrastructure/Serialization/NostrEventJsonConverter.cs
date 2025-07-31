using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using NostrSure.Domain.Entities;


namespace NostrSure.Infrastructure.Serialization;

public sealed class NostrEventJsonConverter : JsonConverter<NostrEvent>
{
    public override NostrEvent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        string? id = null;
        string? pubkey = null;
        long? createdAt = null;
        int? kindInt = null;
        List<List<string>>? tags = null;
        string? content = null;
        string? sig = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException();

            string propertyName = reader.GetString()!;
            reader.Read();

            switch (propertyName)
            {
                case "id":
                    id = reader.GetString();
                    break;
                case "pubkey":
                    pubkey = reader.GetString();
                    break;
                case "created_at":
                    createdAt = reader.GetInt64();
                    break;
                case "kind":
                    kindInt = reader.GetInt32();
                    break;
                case "tags":
                    tags = JsonSerializer.Deserialize<List<List<string>>>(ref reader, options);
                    break;
                case "content":
                    content = reader.GetString();
                    break;
                case "sig":
                    sig = reader.GetString();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        if (id is null) throw new JsonException("Missing required field: id");
        if (pubkey is null) throw new JsonException("Missing required field: pubkey");
        if (createdAt is null) throw new JsonException("Missing required field: created_at");
        if (kindInt is null) throw new JsonException("Missing required field: kind");
        if (tags is null) throw new JsonException("Missing required field: tags");
        if (content is null) throw new JsonException("Missing required field: content");
        if (sig is null) throw new JsonException("Missing required field: sig");

        if (!Enum.IsDefined(typeof(NostrSure.Domain.ValueObjects.EventKind), kindInt.Value))
            throw new JsonException($"Unknown event kind: {kindInt.Value}");

        return new NostrEvent(
            id,
            new Pubkey(pubkey),
            DateTimeOffset.FromUnixTimeSeconds(createdAt.Value),
            (NostrSure.Domain.ValueObjects.EventKind)kindInt.Value,
            tags,
            content,
            sig
        );
    }

    public override void Write(Utf8JsonWriter writer, NostrEvent value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("id", value.Id);
        writer.WriteString("pubkey", value.Pubkey.Value);
        writer.WriteNumber("created_at", value.CreatedAt.ToUnixTimeSeconds());
        writer.WriteNumber("kind", (int)value.Kind);

        writer.WritePropertyName("tags");
        JsonSerializer.Serialize(writer, value.Tags, options);

        writer.WriteString("content", value.Content);
        writer.WriteString("sig", value.Sig);

        writer.WriteEndObject();
    }
}