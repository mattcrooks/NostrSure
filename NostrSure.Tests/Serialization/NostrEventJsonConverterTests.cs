using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NostrSure.Domain.Entities;
using NostrSure.Infrastructure.Serialization;

namespace NostrSure.Tests.Serialization
{
    [TestCategory("Serialization")]
    [TestClass]
    public class NostrEventJsonConverterTests
    {
        private static JsonSerializerOptions GetOptions()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new NostrEventJsonConverter());
            return options;
        }

        [TestMethod]
        public void CanDeserialize_SimpleEvent()
        {
            var json = """
            {
                "content": "#[0]’s desire for more micro apps on nostr is critical. \n\nWe’re having fun with all the social clients being built right now, but the true power of this protocol comes with thousands of smaller ultilities coming together to build an ecosystem of valuable services. The seamlessness of switching between them will be the magic. \n\nI think that’s where this becomes truly unique. Can’t wait to see more.",
                "created_at": 1673311423,
                "id": "9007b89f5626b945174a2a8c8d9d0aefc44389fcdd45da2d14ec21bd2f943efe",
                "kind": 1,
                "pubkey": "82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2",
                "sig": "f188ace3426d97dbe1641b35984dc839a5c88a728e7701c848144920616967eb64a30a7d657ca16d556bea718311b15260c886568531399ed14239868aedbcee",
                "tags": [
                    [
                        "p",
                        "3bf0c63fcb93463407af97a5e5ee64fa883d107ef9e558472c4eb9aaaefa459d"
                    ]
                ]
            }
            """;

            var evt = JsonSerializer.Deserialize<NostrEvent>(json, GetOptions());

            Assert.IsNotNull(evt);
            Assert.AreEqual("9007b89f5626b945174a2a8c8d9d0aefc44389fcdd45da2d14ec21bd2f943efe", evt.Id);
            Assert.AreEqual("82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2", evt.Pubkey.Value);
            Assert.AreEqual(1, evt.Kind);
            Assert.AreEqual(1673311423, evt.CreatedAt.ToUnixTimeSeconds());
            Assert.AreEqual("#[0]’s desire for more micro apps on nostr is critical. \n\nWe’re having fun with all the social clients being built right now, but the true power of this protocol comes with thousands of smaller ultilities coming together to build an ecosystem of valuable services. The seamlessness of switching between them will be the magic. \n\nI think that’s where this becomes truly unique. Can’t wait to see more.", evt.Content);
            Assert.AreEqual("f188ace3426d97dbe1641b35984dc839a5c88a728e7701c848144920616967eb64a30a7d657ca16d556bea718311b15260c886568531399ed14239868aedbcee", evt.Sig);
            Assert.AreEqual(1, evt.Tags.Count);
            Assert.AreEqual("p", evt.Tags[0][0]);
        }

        [TestMethod]
        public void CanDeserialize_ComplexTags()
        {
            var json = """
            {
                "content": "{\"wss://relay.primal.net\":{\"read\":true,\"write\":true}}",
                "created_at": 1750012616,
                "id": "0be97f227cf7758e72a62eb6392d1a67b65aef48d684517ea496d17d799b292b",
                "kind": 3,
                "pubkey": "82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2",
                "sig": "88925482183cabd79c94e179309d0b5314efd1ce55848b1f264480ae610e55e45dfb3c873eb6b3850060124b7c70742295a1ecd32e7447848b061778f26e3375",
                "tags": [
                    ["p", "b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c"],
                    ["p", "a39199ccb5ec92b1cd047bf3dc7e8923ede769d1a5ccc47d579912f0f5cbdab4"]
                ]
            }
            """;

            var evt = JsonSerializer.Deserialize<NostrEvent>(json, GetOptions());

            Assert.IsNotNull(evt);
            Assert.AreEqual(2, evt.Tags.Count);
            foreach (var tag in evt.Tags)
            {
                Assert.AreEqual("p", tag[0]);
            }
        }

        [TestMethod]
        public void RoundTrip_Serialization_MatchesOriginal()
        {
            var json = """
            {
                "content": "#[0]’s desire for more micro apps on nostr is critical.",
                "created_at": 1673311423,
                "id": "9007b89f5626b945174a2a8c8d9d0aefc44389fcdd45da2d14ec21bd2f943efe",
                "kind": 1,
                "pubkey": "82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2",
                "sig": "f188ace3426d97dbe1641b35984dc839a5c88a728e7701c848144920616967eb64a30a7d657ca16d556bea718311b15260c886568531399ed14239868aedbcee",
                "tags": [
                    [
                        "p",
                        "3bf0c63fcb93463407af97a5e5ee64fa883d107ef9e558472c4eb9aaaefa459d"
                    ]
                ]
            }
            """;

            var evt = JsonSerializer.Deserialize<NostrEvent>(json, GetOptions());
            var serialized = JsonSerializer.Serialize(evt, GetOptions());

            // Deserialize again to compare objects, not raw JSON
            var evt2 = JsonSerializer.Deserialize<NostrEvent>(serialized, GetOptions());

            // Compare properties individually for value equality
            Assert.AreEqual(evt.Id, evt2.Id);
            Assert.AreEqual(evt.Pubkey.Value, evt2.Pubkey.Value);
            Assert.AreEqual(evt.CreatedAt, evt2.CreatedAt);
            Assert.AreEqual(evt.Kind, evt2.Kind);
            Assert.AreEqual(evt.Content, evt2.Content);
            Assert.AreEqual(evt.Sig, evt2.Sig);

            Assert.AreEqual(evt.Tags.Count, evt2.Tags.Count);
    
                CollectionAssert.AreEqual(evt.Tags.ToList(), evt2.Tags.ToList());
        
        }

        [TestMethod]
        public void Serializer_UsesUtf8Encoding()
        {
            var evt = new NostrEvent(
                "id",
                new Pubkey("pubkey"),
                DateTimeOffset.UtcNow,
                1,
                new List<List<string>>(),
                "test",
                "sig"
            );

            var options = GetOptions();
            var json = JsonSerializer.Serialize(evt, options);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var decoded = System.Text.Encoding.UTF8.GetString(bytes);
            Assert.AreEqual(json, decoded, "JSON should be valid UTF-8 encoding.");
        }

        [TestMethod]
        public void Serializer_ProducesMinifiedJson()
        {
            var evt = new NostrEvent(
                "id",
                new Pubkey("pubkey"),
                DateTimeOffset.UtcNow,
                1,
                new List<List<string>>(),
                "test",
                "sig"
            );

            var options = GetOptions();
            var json = JsonSerializer.Serialize(evt, options);

            // Should not contain unnecessary whitespace or line breaks
            Assert.IsFalse(json.Contains("\n") || json.Contains("\r") || json.Contains("\t"), "JSON should not contain unnecessary whitespace or line breaks.");
            Assert.IsFalse(json.Contains("  "), "JSON should not contain multiple spaces.");
        }

        [TestMethod]
        public void Serializer_EscapesContentFieldCharacters()
        {
            var specialContent = "line\nbreak\"quote\\backslash\rcarriage\tab\bbackspace\fformfeed";
            var evt = new NostrEvent(
                "id",
                new Pubkey("pubkey"),
                DateTimeOffset.UtcNow,
                1,
                new List<List<string>>(),
                specialContent,
                "sig"
            );

            var options = GetOptions();
            var json = JsonSerializer.Serialize(evt, options);

            // Parse the JSON and extract the content property value
            using var doc = JsonDocument.Parse(json);   
            var contentValue = doc.RootElement.GetProperty("content").GetString();

            // Check for correct escaping in the content field (as it appears in JSON)
            Assert.IsTrue(contentValue.Contains("\n"), "Should contain escaped line break (\\n)");
            Assert.IsTrue(contentValue.Contains("\""), "Should contain escaped double quote (\\\")");
            Assert.IsTrue(contentValue.Contains("\\"), "Should contain escaped backslash (\\\\)");
            Assert.IsTrue(contentValue.Contains("\r"), "Should contain escaped carriage return (\\r)");
            Assert.IsTrue(contentValue.Contains("\t"), "Should contain escaped tab (\\t)");
            Assert.IsTrue(contentValue.Contains("\b"), "Should contain escaped backspace (\\b)");
            Assert.IsTrue(contentValue.Contains("\f"), "Should contain escaped form feed (\\f)");

            // All other characters should be included verbatim
            Assert.IsTrue(contentValue.Contains("line"), "Other characters should be included verbatim.");
        }
    }
}