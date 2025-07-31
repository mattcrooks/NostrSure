using System.Text.Json;
using NostrSure.Infrastructure.Serialization;
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NostrSure.Domain.Entities;
using NostrSure.Domain.ValueObjects;

namespace NostrSure.Tests.Entities
{
    [TestCategory("Entities")]
    [TestClass]
    public class NostrEventValidatorTests
    {
        private static NostrEvent CreateValidEvent()
        {
            return new NostrEvent(
                "id",
                new Pubkey("pubkey"),
                DateTimeOffset.UtcNow,
                EventKind.Note,
                new List<List<string>> { new List<string> { "p", "abc" } },
                "content",
                "sig"
            );
        }

        [TestMethod]
        public void ValidateSignature_EmptySig_Fails()
        {
            var evt = CreateValidEvent() with { Sig = "" };
            var validator = new NostrEventValidator();
            var result = validator.ValidateSignature(evt, out var error);
            Assert.IsFalse(result);
            Assert.IsTrue(error.Contains("Signature is empty"));
        }



        [TestMethod]
        public void ValidateKind_UnknownKind_Fails()
        {
            var evt = CreateValidEvent() with { Kind = (EventKind)9999 };
            var validator = new NostrEventValidator();
            var result = validator.ValidateKind(evt, out var error);
            Assert.IsFalse(result);
            Assert.IsTrue(error.Contains("Unknown event kind"));
        }

        [TestMethod]
        public void ValidateKind_KnownKind_Passes()
        {
            var evt = CreateValidEvent();
            var validator = new NostrEventValidator();
            var result = validator.ValidateKind(evt, out var error);
            Assert.IsTrue(result);
            Assert.AreEqual(string.Empty, error);
        }

        [TestMethod]
        public void ValidateTags_NullTags_Fails()
        {
            var evt = CreateValidEvent() with { Tags = null };
            var validator = new NostrEventValidator();
            var result = validator.ValidateTags(evt, out var error);
            Assert.IsFalse(result);
            Assert.IsTrue(error.Contains("Tags are null"));
        }

        [TestMethod]
        public void ValidateTags_EmptyTag_Fails()
        {
            var evt = CreateValidEvent() with { Tags = new List<List<string>> { new List<string>() } };
            var validator = new NostrEventValidator();
            var result = validator.ValidateTags(evt, out var error);
            Assert.IsFalse(result);
            Assert.IsTrue(error.Contains("Tag is empty"));
        }

        [TestMethod]
        public void ValidateTags_TagWithEmptyValue_Fails()
        {
            var evt = CreateValidEvent() with { Tags = new List<List<string>> { new List<string> { "" } } };
            var validator = new NostrEventValidator();
            var result = validator.ValidateTags(evt, out var error);
            Assert.IsFalse(result);
            Assert.IsTrue(error.Contains("Tag contains empty value"));
        }

        [TestMethod]
        public void ValidateSignature_ValidNIP01Event_Passes()
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
            var evt = JsonSerializer.Deserialize<NostrEvent>(json, new JsonSerializerOptions { Converters = { new NostrEventJsonConverter() } });
            var validator = new NostrEventValidator();

            // Only test signature validation - event ID validation is separate
            var result = validator.ValidateSignature(evt!, out var error);
            Assert.IsTrue(result, $"Signature should be valid, but got error: {error}");
        }

        [TestMethod]
        public void ValidateSignature_ValidNIP01Event_Failed()
        {
            var json = """
            {
                "content": "{\"Wss://relay.primal.net\":{\"read\":true,\"write\":true}}",
                "created_at": 1750012616,
                "id": "11c71efa0d93768021dfff23326af6ce8553fcf5113970a285be0202db95d66e",
                "kind": 3,
                "pubkey": "82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2",
                "sig": "88925482183cabd79c94e179309d0b5314efd1ce55848b1f264480ae610e55e45dfb3c873eb6b3850060124b7c70742295a1ecd32e7447848b061778f26e3375",
                "tags": [
                    ["p", "b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c"],
                    ["p", "a39199ccb5ec92b1cd047bf3dc7e8923ede769d1a5ccc47d579912f0f5cbdab4"]
                ]
            }
            """;
            var evt = JsonSerializer.Deserialize<NostrEvent>(json, new JsonSerializerOptions { Converters = { new NostrEventJsonConverter() } });
            var validator = new NostrEventValidator();
            var result = validator.ValidateEventId(evt!, out var error);
            Assert.IsFalse(result, $"Event ID should be invalid because content was modified, but validation passed");
            Assert.IsTrue(error.Contains("Event ID mismatch"), $"Expected event ID mismatch error, but got: {error}");
        }

        [TestMethod]
        public void ValidateEventId_CorrectHash_Passes()
        {
            var json = """
            {
                "content": "{\"wss://relay.primal.net\":{\"read\":true,\"write\":true}}",
                "created_at": 1750012616,
                "id": "8f24947b68b7f5306d22142490b921dabde65f7caa4bf5d994568924367bc3b6",
                "kind": 3,
                "pubkey": "82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2",
                "sig": "88925482183cabd79c94e179309d0b5314efd1ce55848b1f264480ae610e55e45dfb3c873eb6b3850060124b7c70742295a1ecd32e7447848b061778f26e3375",
                "tags": [
                    ["p", "b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c"],
                    ["p", "a39199ccb5ec92b1cd047bf3dc7e8923ede769d1a5ccc47d579912f0f5cbdab4"]
                ]
            }
            """;
            var evt = JsonSerializer.Deserialize<NostrEvent>(json, new JsonSerializerOptions { Converters = { new NostrEventJsonConverter() } });
            var validator = new NostrEventValidator();
            var result = validator.ValidateEventId(evt!, out var error);
            Assert.IsTrue(result, $"Event ID should be valid for our canonical serialization, but got error: {error}");
        }

        [TestMethod]
        public void ValidateTags_ValidTags_Passes()
        {
            var evt = CreateValidEvent();
            var validator = new NostrEventValidator();
            var result = validator.ValidateTags(evt, out var error);
            Assert.IsTrue(result);
            Assert.AreEqual(string.Empty, error);
        }
    }
}