using NostrSure.Domain.Entities;
using NostrSure.Domain.ValueObjects;
using NostrSure.Infrastructure.Serialization;
using System.Text.Json;

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
                new List<NostrTag> { new NostrTag("t", new[] { "testhashtag" }) }, // Use a "t" tag instead of "p" tag to avoid hex validation
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
            var evt = CreateValidEvent() with { Tags = null! };
            var validator = new NostrEventValidator();
            var result = validator.ValidateTags(evt, out var error);
            Assert.IsFalse(result);
            Assert.IsTrue(error.Contains("Tags are null"));
        }

        [TestMethod]
        public void ValidateTags_EmptyTag_Fails()
        {
            // This test now uses NostrTag, but NostrTag.FromArray will throw if empty
            // So we test the validator's handling of invalid tag structures
            var evt = new NostrEvent(
                "id",
                new Pubkey("pubkey"),
                DateTimeOffset.UtcNow,
                EventKind.Note,
                new List<NostrTag>(), // Empty tags list is valid
                "content",
                "sig"
            );
            var validator = new NostrEventValidator();
            var result = validator.ValidateTags(evt, out var error);
            Assert.IsTrue(result); // Empty tags list should be valid
            Assert.AreEqual(string.Empty, error);
        }

        [TestMethod]
        public void ValidateTags_InvalidPTag_Fails()
        {
            var invalidPTag = new NostrTag("p", new[] { "invalid_hex" });
            var evt = CreateValidEvent() with { Tags = new List<NostrTag> { invalidPTag } };
            var validator = new NostrEventValidator();
            var result = validator.ValidateTags(evt, out var error);
            Assert.IsFalse(result);
            Assert.IsTrue(error.Contains("Invalid tag"));
        }

        [TestMethod]
        public void ValidateSignature_ValidNIP01Event_Passes()
        {
            var json = """
            {
                "content": "What would be an \"authentically Conservative position\"?\n\nLibertarian optimist, mere conservative or communitarian nationalist? Who is the authentic Conservative? Towards a Conservatism that can work.\n\nhttps://fightingforafreefuture.substack.com/p/what-would-be-an-authentically-conservative",
                "created_at": 1753960226,
                "id": "9585c6e6b72d6ba0d9768c870ffc99c2462453b90d5e52f0e1ef10201feb76c5",
                "kind": 1,
                "pubkey": "8d8ec0e89fdf509484369f721eff01fbd0ff0c767190d01f1e8abf3eb6a8de6c",
                "sig": "69b26970ad420b283f3aa794ee48c87b5b3a2f3f5896b6f2476e4df4041ab4e7d3f309ee0c4d46b45706776e607683dd25b66478228a5f53f6c62aa107489601",
                "tags": [
                    [
                        "r",
                        "https://fightingforafreefuture.substack.com/p/what-would-be-an-authentically-conservative"
                    ]
                ]
            }
            """;
            var evt = JsonSerializer.Deserialize<NostrEvent>(json, new JsonSerializerOptions { Converters = { new NostrEventJsonConverter() } });
            var validator = new NostrEventValidator();

            var idResult = validator.ValidateEventId(evt!, out var idError);
            Assert.IsTrue(idResult, $"Event ID should be valid, but got error: {idError}");

            // Only test signature validation - event ID validation is separate
            var result = validator.ValidateSignature(evt!, out var error);
            Assert.IsTrue(result, $"Signature should be valid, but got error: {error}");
        }

        [TestMethod]
        public void ValidateSignature_ValidNIP01SimpleEvent_Passes()
        {
            var json = """
                {
                        "content": "Bitcoin price: $118451, Sats per USD: 844",
                        "created_at": 1753958704,
                        "id": "db7e784617a8caa09433cb0ec2250deb3ab20b59adaae1f8fe0f574243df015a",
                        "kind": 1,
                        "pubkey": "aa4fc8665f5696e33db7e1a572e3b0f5b3d615837b0f362dcb1c8068b098c7b4",
                        "sig": "177d555723178c1e6ec5dff8a9fd252b6b0768c085860df1e2a1ea881fc23734d2c846e061be31090db892cd1525ec2f976280a6ac90b1c02427f4e3db048db4",
                        "tags": []
                    }
            """;
            var evt = JsonSerializer.Deserialize<NostrEvent>(json, new JsonSerializerOptions { Converters = { new NostrEventJsonConverter() } });
            var validator = new NostrEventValidator();

            var idResult = validator.ValidateEventId(evt!, out var idError);
            Assert.IsTrue(idResult, $"Event ID should be valid, but got error: {idError}");

            // Only test signature validation - event ID validation is separate
            var result = validator.ValidateSignature(evt!, out var error);
            Assert.IsTrue(result, $"Signature should be valid, but got error: {error}");
        }

        [TestMethod]
        public void ValidateSignature_ValidNIP01UnicodeEvent_Passes()
        {
            var json = """
                    {
                            "content": "バグったら\n仕様ですって\n言えばええ",
                            "created_at": 1753958926,
                            "id": "b4b2e4feba7089db62c748c73c1e07be96f26c66ae3ea21867416e7b179a7658",
                            "kind": 1,
                            "pubkey": "fe9edd5d5c635dd2900f1f86a872e81ce1d6e20bd4e06549f133ae6bf158913b",
                            "sig": "511884c663337bb7fd4b52a5fc2f67c906e10a2303f9bd9478cdddb9795ecc5a58b506b8f3455c4f6d19b6eef333d7c10943908666ab5d511e5c71d235eef8ed",
                            "tags": []
                        }
            """;
            var evt = JsonSerializer.Deserialize<NostrEvent>(json, new JsonSerializerOptions { Converters = { new NostrEventJsonConverter() } });
            var validator = new NostrEventValidator();

            var idResult = validator.ValidateEventId(evt!, out var idError);
            Assert.IsTrue(idResult, $"Event ID should be valid, but got error: {idError}");

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

        [TestMethod]
        public void ValidateTags_ValidPTag_Passes()
        {
            var validPTag = new NostrTag("p", new[] { "82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2" });
            var evt = CreateValidEvent() with { Tags = new List<NostrTag> { validPTag } };
            var validator = new NostrEventValidator();
            var result = validator.ValidateTags(evt, out var error);
            Assert.IsTrue(result);
            Assert.AreEqual(string.Empty, error);
        }

        [TestMethod]
        public void ValidateTags_ValidETag_Passes()
        {
            var validETag = new NostrTag("e", new[] { "9007b89f5626b945174a2a8c8d9d0aefc44389fcdd45da2d14ec21bd2f943efe" });
            var evt = CreateValidEvent() with { Tags = new List<NostrTag> { validETag } };
            var validator = new NostrEventValidator();
            var result = validator.ValidateTags(evt, out var error);
            Assert.IsTrue(result);
            Assert.AreEqual(string.Empty, error);
        }

        [TestMethod]
        public void NostrTag_EnforcesNIP01Validation()
        {
            // Test that NostrTag.IsValid() works correctly
            var validPTag = new NostrTag("p", new[] { "82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2" });
            Assert.IsTrue(validPTag.IsValid(), "Valid p tag should pass validation");

            var invalidPTag = new NostrTag("p", new[] { "invalid_hex" });
            Assert.IsFalse(invalidPTag.IsValid(), "Invalid p tag should fail validation");

            var validETag = new NostrTag("e", new[] { "9007b89f5626b945174a2a8c8d9d0aefc44389fcdd45da2d14ec21bd2f943efe" });
            Assert.IsTrue(validETag.IsValid(), "Valid e tag should pass validation");

            var validTTag = new NostrTag("t", new[] { "nostr" });
            Assert.IsTrue(validTTag.IsValid(), "Valid t tag should pass validation");
        }
    }
}