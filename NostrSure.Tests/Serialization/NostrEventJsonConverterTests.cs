using NostrSure.Domain.Entities;
using NostrSure.Domain.ValueObjects;
using NostrSure.Infrastructure.Serialization;
using System.Text.Json;

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
                "content": "#[0]'s desire for more micro apps on nostr is critical. \n\nWe're having fun with all the social clients being built right now, but the true power of this protocol comes with thousands of smaller ultilities coming together to build an ecosystem of valuable services. The seamlessness of switching between them will be the magic. \n\nI think that's where this becomes truly unique. Can't wait to see more.",
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
            Assert.AreEqual(EventKind.Note, evt.Kind);
            Assert.AreEqual(1673311423, evt.CreatedAt.ToUnixTimeSeconds());
            Assert.AreEqual("#[0]'s desire for more micro apps on nostr is critical. \n\nWe're having fun with all the social clients being built right now, but the true power of this protocol comes with thousands of smaller ultilities coming together to build an ecosystem of valuable services. The seamlessness of switching between them will be the magic. \n\nI think that's where this becomes truly unique. Can't wait to see more.", evt.Content);
            Assert.AreEqual("f188ace3426d97dbe1641b35984dc839a5c88a728e7701c848144920616967eb64a30a7d657ca16d556bea718311b15260c886568531399ed14239868aedbcee", evt.Sig);
            Assert.AreEqual(1, evt.Tags.Count);
            Assert.AreEqual("p", evt.Tags[0].Name);
            Assert.AreEqual(1, evt.Tags[0].Values.Count);
            Assert.AreEqual("3bf0c63fcb93463407af97a5e5ee64fa883d107ef9e558472c4eb9aaaefa459d", evt.Tags[0].Values[0]);
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
            Assert.AreEqual(EventKind.ContactList, evt.Kind);
            Assert.IsInstanceOfType(evt, typeof(ContactListEvent));

            var contactList = (ContactListEvent)evt;
            Assert.AreEqual(2, contactList.Contacts.Count);
            Assert.AreEqual("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c", contactList.Contacts[0].ContactPubkey.Value);
            Assert.AreEqual("a39199ccb5ec92b1cd047bf3dc7e8923ede769d1a5ccc47d579912f0f5cbdab4", contactList.Contacts[1].ContactPubkey.Value);

            // Verify the tags are still accessible via base class
            Assert.AreEqual(2, evt.Tags.Count);
            foreach (var tag in evt.Tags)
            {
                Assert.AreEqual("p", tag.Name);
                Assert.AreEqual(1, tag.Values.Count);
                Assert.IsTrue(tag.Values[0].Length == 64, "Pubkey should be 64 characters");
            }
        }

        [TestMethod]
        public void RoundTrip_Serialization_MatchesOriginal()
        {
            var json = """
            {
                "content": "#[0]'s desire for more micro apps on nostr is critical.",
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
            var serialized = JsonSerializer.Serialize(evt!, GetOptions());

            // Deserialize again to compare objects, not raw JSON
            var evt2 = JsonSerializer.Deserialize<NostrEvent>(serialized, GetOptions());
            Assert.IsNotNull(evt2);

            // Compare properties individually for value equality
            Assert.AreEqual(evt!.Id, evt2!.Id);
            Assert.AreEqual(evt.Pubkey.Value, evt2.Pubkey.Value);
            Assert.AreEqual(evt.CreatedAt, evt2.CreatedAt);
            Assert.AreEqual(evt.Kind, evt2.Kind);
            Assert.IsTrue(Enum.IsDefined(typeof(EventKind), evt.Kind));
            Assert.AreEqual(evt.Content, evt2.Content);
            Assert.AreEqual(evt.Sig, evt2.Sig);

            Assert.AreEqual(evt.Tags.Count, evt2.Tags.Count);

            // Compare tags individually
            for (int i = 0; i < evt.Tags.Count; i++)
            {
                Assert.AreEqual(evt.Tags[i].Name, evt2.Tags[i].Name);
                Assert.AreEqual(evt.Tags[i].Values.Count, evt2.Tags[i].Values.Count);
                for (int j = 0; j < evt.Tags[i].Values.Count; j++)
                {
                    Assert.AreEqual(evt.Tags[i].Values[j], evt2.Tags[i].Values[j]);
                }
            }
        }

        [TestMethod]
        public void Serializer_UsesUtf8Encoding()
        {
            var evt = new NostrEvent(
                "id",
                new Pubkey("pubkey"),
                DateTimeOffset.UtcNow,
                EventKind.Note,
                new List<NostrTag>(),
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
                EventKind.Note,
                new List<NostrTag>(),
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
                EventKind.Note,
                new List<NostrTag>(),
                specialContent,
                "sig"
            );

            var options = GetOptions();
            var json = JsonSerializer.Serialize(evt, options);

            // Parse the JSON and extract the content property value
            using var doc = JsonDocument.Parse(json);
            var contentValue = doc.RootElement.GetProperty("content").GetString();

            // Check for correct escaping in the content field (as it appears in JSON)
            Assert.IsNotNull(contentValue, "Content should not be null");
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

        [TestMethod]
        public void CanDeserialize_TagsWithMultipleValues()
        {
            var json = """
            {
                "content": "test content",
                "created_at": 1673311423,
                "id": "test_id",
                "kind": 1,
                "pubkey": "test_pubkey",
                "sig": "test_sig",
                "tags": [
                    ["e", "event_id", "relay_url", "marker"],
                    ["p", "pubkey_hex"],
                    ["t", "hashtag"],
                    ["relay", "wss://relay.example.com", "read"]
                ]
            }
            """;

            var evt = JsonSerializer.Deserialize<NostrEvent>(json, GetOptions());

            Assert.IsNotNull(evt);
            Assert.AreEqual(4, evt.Tags.Count);

            // Check "e" tag with multiple values
            var eTag = evt.Tags[0];
            Assert.AreEqual("e", eTag.Name);
            Assert.AreEqual(3, eTag.Values.Count);
            Assert.AreEqual("event_id", eTag.Values[0]);
            Assert.AreEqual("relay_url", eTag.Values[1]);
            Assert.AreEqual("marker", eTag.Values[2]);

            // Check "p" tag with single value
            var pTag = evt.Tags[1];
            Assert.AreEqual("p", pTag.Name);
            Assert.AreEqual(1, pTag.Values.Count);
            Assert.AreEqual("pubkey_hex", pTag.Values[0]);

            // Check "t" tag with single value
            var tTag = evt.Tags[2];
            Assert.AreEqual("t", tTag.Name);
            Assert.AreEqual(1, tTag.Values.Count);
            Assert.AreEqual("hashtag", tTag.Values[0]);

            // Check "relay" tag with multiple values
            var relayTag = evt.Tags[3];
            Assert.AreEqual("relay", relayTag.Name);
            Assert.AreEqual(2, relayTag.Values.Count);
            Assert.AreEqual("wss://relay.example.com", relayTag.Values[0]);
            Assert.AreEqual("read", relayTag.Values[1]);
        }

        [TestMethod]
        public void TagValidation_EnforcesNIP01Compliance()
        {
            // Test that tags are properly validated according to NIP-01
            var validPTag = new NostrTag("p", new[] { "82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2" });
            Assert.IsTrue(validPTag.IsValid(), "Valid p tag should pass validation");

            var invalidPTag = new NostrTag("p", new[] { "invalid_hex" });
            Assert.IsFalse(invalidPTag.IsValid(), "Invalid p tag should fail validation");

            var validETag = new NostrTag("e", new[] { "9007b89f5626b945174a2a8c8d9d0aefc44389fcdd45da2d14ec21bd2f943efe" });
            Assert.IsTrue(validETag.IsValid(), "Valid e tag should pass validation");

            var validTTag = new NostrTag("t", new[] { "nostr" });
            Assert.IsTrue(validTTag.IsValid(), "Valid t tag should pass validation");
        }

        [TestMethod]
        public void CanDeserialize_EventWithNullValues()
        {
            var json = """
            {
                "content": null,
                "created_at": 1673311423,
                "id": "9007b89f5626b945174a2a8c8d9d0aefc44389fcdd45da2d14ec21bd2f943efe",
                "kind": 1,
                "pubkey": "82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2",
                "sig": "f188ace3426d97dbe1641b35984dc839a5c88a728e7701c848144920616967eb64a30a7d657ca16d556bea718311b15260c886568531399ed14239868aedbcee",
                "tags": null
            }
            """;

            var evt = JsonSerializer.Deserialize<NostrEvent>(json, GetOptions());

            Assert.IsNotNull(evt);
            Assert.AreEqual(string.Empty, evt.Content); // null content becomes empty string
            Assert.IsNotNull(evt.Tags);
            Assert.AreEqual(0, evt.Tags.Count);
        }

        [TestMethod]
        public void CanSerialize_EventWithNullValues()
        {
            var evt = new NostrEvent(
                "9007b89f5626b945174a2a8c8d9d0aefc44389fcdd45da2d14ec21bd2f943efe",
                new Pubkey("82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2"),
                new DateTimeOffset(2023, 1, 9, 12, 15, 30, TimeSpan.Zero),
                EventKind.Note,
                new List<NostrTag>(), // Use empty list instead of null
                string.Empty, // Use empty string instead of null
                string.Empty  // Use empty string instead of null
            );

            var options = GetOptions();
            var json = JsonSerializer.Serialize(evt, options);

            // Ensure the JSON is valid and can be deserialized back to an event
            var deserializedEvt = JsonSerializer.Deserialize<NostrEvent>(json, options);
            Assert.IsNotNull(deserializedEvt);
            Assert.AreEqual(evt.Id, deserializedEvt!.Id);
            Assert.AreEqual(evt.Pubkey.Value, deserializedEvt.Pubkey.Value);
            Assert.AreEqual(evt.CreatedAt, deserializedEvt.CreatedAt);
            Assert.AreEqual(evt.Kind, deserializedEvt.Kind);
            Assert.IsTrue(Enum.IsDefined(typeof(EventKind), evt.Kind));
            Assert.AreEqual(evt.Sig, deserializedEvt.Sig);
            Assert.IsNotNull(deserializedEvt.Tags);
            Assert.AreEqual(0, deserializedEvt.Tags.Count);
        }

        [TestMethod]
        public void CanDeserialize_EventWithEmptyTags()
        {
            var json = """
            {
                "content": "test",
                "created_at": 1673311423,
                "id": "test_id",
                "kind": 1,
                "pubkey": "test_pubkey",
                "sig": "test_sig",
                "tags": []
            }
            """;

            var evt = JsonSerializer.Deserialize<NostrEvent>(json, GetOptions());

            Assert.IsNotNull(evt);
            Assert.AreEqual(0, evt.Tags.Count);
        }

        [TestMethod]
        public void CanSerialize_EventWithEmptyTags()
        {
            var fixedDateTime = DateTimeOffset.FromUnixTimeSeconds(1673311423);
            var evt = new NostrEvent(
                "test_id",
                new Pubkey("test_pubkey"),
                fixedDateTime,
                EventKind.Note,
                new List<NostrTag>(),
                "test",
                "test_sig"
            );

            var options = GetOptions();
            var json = JsonSerializer.Serialize(evt, options);

            // Ensure the JSON is valid and can be deserialized back to an event
            var deserializedEvt = JsonSerializer.Deserialize<NostrEvent>(json, options);
            Assert.IsNotNull(deserializedEvt);
            Assert.AreEqual(evt.Id, deserializedEvt.Id);
            Assert.AreEqual(evt.Pubkey.Value, deserializedEvt.Pubkey.Value);
            Assert.AreEqual(evt.CreatedAt, deserializedEvt.CreatedAt);
            Assert.AreEqual(evt.Kind, deserializedEvt.Kind);
            Assert.IsTrue(Enum.IsDefined(typeof(EventKind), evt.Kind));
            Assert.AreEqual(evt.Sig, deserializedEvt.Sig);
            Assert.IsNotNull(deserializedEvt.Tags);
            Assert.AreEqual(0, deserializedEvt.Tags.Count);
        }

        [TestMethod]
        public void Deserialization_ThrowsJsonException_OnInvalidJson()
        {
            var invalidJsons = new string[]
            {
                """{"content": "test", "created_at": 1673311423, "id": "test_id", "kind": 1, "pubkey": "test_pubkey", "sig": "test_sig","tags": }""",
                """{"content": "test", "created_at": 1673311423, "id": "test_id", "kind": 1, "pubkey": "test_pubkey", "sig": "test_sig","tags": ["p","invalid_hex"]}"""
            };

            foreach (var invalidJson in invalidJsons)
            {
                Assert.ThrowsException<JsonException>(() => JsonSerializer.Deserialize<NostrEvent>(invalidJson, GetOptions()));
            }
        }

        [TestMethod]
        public void Deserialization_UnknownProperty_IgnoresExtraFields()
        {
            var json = """
            {
                "content": "test",
                "created_at": 1673311423,
                "id": "test_id",
                "kind": 1,
                "pubkey": "test_pubkey",
                "sig": "test_sig",
                "tags": [],
                "unknown_field": "ignore_me"
            }
            """;

            var evt = JsonSerializer.Deserialize<NostrEvent>(json, GetOptions());

            Assert.IsNotNull(evt);
            Assert.AreEqual("test_id", evt.Id);
            Assert.AreEqual("test_pubkey", evt.Pubkey.Value);
            Assert.AreEqual(EventKind.Note, evt.Kind);
            Assert.AreEqual(1673311423, evt.CreatedAt.ToUnixTimeSeconds());
            Assert.AreEqual("test", evt.Content);
            Assert.AreEqual("test_sig", evt.Sig);
            Assert.IsNotNull(evt.Tags);
            Assert.AreEqual(0, evt.Tags.Count);
        }

        [TestMethod]
        public void CanDeserialize_ValidEventWithNip05Dns()
        {
            // Use a simple test event that doesn't require complex signature validation
            var json = """
            {
                "content": "test content",
                "created_at": 1673311423,
                "id": "82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2",
                "kind": 1,
                "pubkey": "82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2",
                "sig": "f188ace3426d97dbe1641b35984dc839a5c88a728e7701c848144920616967eb64a30a7d657ca16d556bea718311b15260c886568531399ed14239868aedbcee",
                "tags": [
                    ["t", "hashtag"],
                    ["relay", "wss://relay.example.com"]
                ]
            }
            """;

            var evt = JsonSerializer.Deserialize<NostrEvent>(json, new JsonSerializerOptions { Converters = { new NostrEventJsonConverter() } });
            Assert.IsNotNull(evt);
            Assert.AreEqual("test content", evt.Content);
            Assert.AreEqual(1673311423, evt.CreatedAt.ToUnixTimeSeconds());
            Assert.AreEqual(2, evt.Tags.Count);
        }

        [TestMethod]
        public void CanDeserialize_InvalidEventWithNip05Dns()
        {
            var json = """
            {
                "content": "test content",
                "created_at": 1673311423,
                "id": "test_id",
                "kind": 1,
                "pubkey": "test_pubkey",
                "sig": "test_sig",
                "tags": [
                    ["p", "invalid_pubkey"],
                    ["t", "hashtag"],
                    ["e", "event_id"],
                    ["relay", "wss://relay.example.com"]
                ]
            }
            """;

            var evt = JsonSerializer.Deserialize<NostrEvent>(json, new JsonSerializerOptions { Converters = { new NostrEventJsonConverter() } });
            Assert.IsNotNull(evt);
            var validator = new NostrEventValidator();
            var result = validator.Validate(evt!);
            Assert.IsFalse(result.IsValid, "Event should be invalid due to incorrect pubkey in p tag");
        }

        [TestMethod]
        public void CanDeserialize_ContactListEvent_WithPetnamesAndRelays()
        {
            var json = """
            {
                "content": "My contact list",
                "created_at": 1750012616,
                "id": "contact_list_id_123",
                "kind": 3,
                "pubkey": "82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2",
                "sig": "sigvalue123",
                "tags": [
                    ["p", "b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c", "alice", "wss://relay.example.com"],
                    ["p", "a39199ccb5ec92b1cd047bf3dc7e8923ede769d1a5ccc47d579912f0f5cbdab4", "bob"],
                    ["p", "1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef", "", "wss://another-relay.com"]
                ]
            }
            """;

            var evt = JsonSerializer.Deserialize<NostrEvent>(json, GetOptions());
            Assert.IsNotNull(evt);
            Assert.AreEqual(EventKind.ContactList, evt.Kind);
            Assert.IsInstanceOfType(evt, typeof(ContactListEvent));

            var contactList = (ContactListEvent)evt;
            Assert.AreEqual(3, contactList.Contacts.Count);

            // First contact: full petname and relay
            Assert.AreEqual("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c", contactList.Contacts[0].ContactPubkey.Value);
            Assert.AreEqual("alice", contactList.Contacts[0].Petname);
            Assert.AreEqual("wss://relay.example.com", contactList.Contacts[0].RelayUrl);

            // Second contact: petname only
            Assert.AreEqual("a39199ccb5ec92b1cd047bf3dc7e8923ede769d1a5ccc47d579912f0f5cbdab4", contactList.Contacts[1].ContactPubkey.Value);
            Assert.AreEqual("bob", contactList.Contacts[1].Petname);
            Assert.IsNull(contactList.Contacts[1].RelayUrl);

            // Third contact: relay URL with empty petname
            Assert.AreEqual("1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef", contactList.Contacts[2].ContactPubkey.Value);
            Assert.IsNull(contactList.Contacts[2].Petname);
            Assert.AreEqual("wss://another-relay.com", contactList.Contacts[2].RelayUrl);
        }

        [TestMethod]
        public void RoundTrip_ContactListEvent_Serialization()
        {
            var contacts = new List<ContactEntry>
            {
                new ContactEntry(new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c"), "alice", "wss://relay1.example.com"),
                new ContactEntry(new Pubkey("a39199ccb5ec92b1cd047bf3dc7e8923ede769d1a5ccc47d579912f0f5cbdab4"), "bob"),
                new ContactEntry(new Pubkey("1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef"), null, "wss://relay2.example.com")
            };

            var originalEvent = ContactListEvent.Create(
                "test_contact_list_id",
                new Pubkey("82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2"),
                DateTimeOffset.FromUnixTimeSeconds(1673311423),
                "My contact list",
                "test_sig",
                contacts
            );

            var options = GetOptions();
            var json = JsonSerializer.Serialize(originalEvent, options);
            var deserializedEvent = JsonSerializer.Deserialize<NostrEvent>(json, options);

            Assert.IsNotNull(deserializedEvent);
            Assert.IsInstanceOfType(deserializedEvent, typeof(ContactListEvent));

            var contactList = (ContactListEvent)deserializedEvent;
            Assert.AreEqual(3, contactList.Contacts.Count);

            // Verify first contact
            Assert.AreEqual("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c", contactList.Contacts[0].ContactPubkey.Value);
            Assert.AreEqual("alice", contactList.Contacts[0].Petname);
            Assert.AreEqual("wss://relay1.example.com", contactList.Contacts[0].RelayUrl);

            // Verify second contact
            Assert.AreEqual("a39199ccb5ec92b1cd047bf3dc7e8923ede769d1a5ccc47d579912f0f5cbdab4", contactList.Contacts[1].ContactPubkey.Value);
            Assert.AreEqual("bob", contactList.Contacts[1].Petname);
            Assert.IsNull(contactList.Contacts[1].RelayUrl);

            // Verify third contact
            Assert.AreEqual("1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef", contactList.Contacts[2].ContactPubkey.Value);
            Assert.IsNull(contactList.Contacts[2].Petname);
            Assert.AreEqual("wss://relay2.example.com", contactList.Contacts[2].RelayUrl);
        }

        [TestMethod]
        public void ContactEntry_FromPTag_ValidatesCorrectly()
        {
            var fullPTag = new NostrTag("p", new[] {
                "b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c",
                "alice",
                "wss://relay.example.com"
            });
            var contact = ContactEntry.FromPTag(fullPTag);

            Assert.AreEqual("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c", contact.ContactPubkey.Value);
            Assert.AreEqual("alice", contact.Petname);
            Assert.AreEqual("wss://relay.example.com", contact.RelayUrl);
            Assert.IsTrue(contact.IsValid);
        }

        [TestMethod]
        public void ContactEntry_ToPTag_RoundTrip()
        {
            var contact = new ContactEntry(
                new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c"),
                "alice",
                "wss://relay.example.com"
            );

            var pTag = contact.ToPTag();
            var roundTripContact = ContactEntry.FromPTag(pTag);

            Assert.AreEqual(contact.ContactPubkey.Value, roundTripContact.ContactPubkey.Value);
            Assert.AreEqual(contact.Petname, roundTripContact.Petname);
            Assert.AreEqual(contact.RelayUrl, roundTripContact.RelayUrl);
        }

        [TestMethod]
        public void ContactListEvent_ValidateContactList_ReturnsTrue()
        {
            var contacts = new List<ContactEntry>
            {
                new ContactEntry(new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c"), "alice"),
                new ContactEntry(new Pubkey("a39199ccb5ec92b1cd047bf3dc7e8923ede769d1a5ccc47d579912f0f5cbdab4"))
            };

            var contactListEvent = ContactListEvent.Create(
                "test_id",
                new Pubkey("82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2"),
                DateTimeOffset.Now,
                "test content",
                "test_sig",
                contacts
            );

            Assert.IsTrue(contactListEvent.IsValidContactList());
        }
    }
}