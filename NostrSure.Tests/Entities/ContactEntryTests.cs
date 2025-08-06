using NostrSure.Domain.Entities;
using NostrSure.Domain.ValueObjects;

namespace NostrSure.Tests.Entities
{
    [TestCategory("Domain")]
    [TestClass]
    public class ContactEntryTests
    {
        [TestMethod]
        public void Constructor_ValidParameters_CreatesInstance()
        {
            var pubkey = new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c");
            var petname = "alice";
            var relayUrl = "wss://relay.example.com";

            var contact = new ContactEntry(pubkey, petname, relayUrl);

            Assert.AreEqual(pubkey, contact.ContactPubkey);
            Assert.AreEqual(petname, contact.Petname);
            Assert.AreEqual(relayUrl, contact.RelayUrl);
        }

        [TestMethod]
        public void Constructor_OptionalParameters_CreatesInstance()
        {
            var pubkey = new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c");

            var contact = new ContactEntry(pubkey);

            Assert.AreEqual(pubkey, contact.ContactPubkey);
            Assert.IsNull(contact.Petname);
            Assert.IsNull(contact.RelayUrl);
        }

        [TestMethod]
        public void IsValid_ValidPubkeyAndRelay_ReturnsTrue()
        {
            var pubkey = new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c");
            var contact = new ContactEntry(pubkey, "alice", "wss://relay.example.com");

            Assert.IsTrue(contact.IsValid);
        }

        [TestMethod]
        public void IsValid_ValidPubkeyNoRelay_ReturnsTrue()
        {
            var pubkey = new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c");
            var contact = new ContactEntry(pubkey, "alice");

            Assert.IsTrue(contact.IsValid);
        }

        [TestMethod]
        public void IsValid_InvalidRelayUrl_ReturnsFalse()
        {
            var pubkey = new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c");
            var contact = new ContactEntry(pubkey, "alice", "not-a-url");

            Assert.IsFalse(contact.IsValid);
        }

        [TestMethod]
        public void FromPTag_ValidFullTag_CreatesContact()
        {
            var pTag = new NostrTag("p", new[] {
                "b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c",
                "alice",
                "wss://relay.example.com"
            });

            var contact = ContactEntry.FromPTag(pTag);

            Assert.AreEqual("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c", contact.ContactPubkey.Value);
            Assert.AreEqual("alice", contact.Petname);
            Assert.AreEqual("wss://relay.example.com", contact.RelayUrl);
        }

        [TestMethod]
        public void FromPTag_PubkeyOnly_CreatesContact()
        {
            var pTag = new NostrTag("p", new[] { "b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c" });

            var contact = ContactEntry.FromPTag(pTag);

            Assert.AreEqual("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c", contact.ContactPubkey.Value);
            Assert.IsNull(contact.Petname);
            Assert.IsNull(contact.RelayUrl);
        }

        [TestMethod]
        public void FromPTag_PubkeyAndPetname_CreatesContact()
        {
            var pTag = new NostrTag("p", new[] {
                "b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c",
                "alice"
            });

            var contact = ContactEntry.FromPTag(pTag);

            Assert.AreEqual("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c", contact.ContactPubkey.Value);
            Assert.AreEqual("alice", contact.Petname);
            Assert.IsNull(contact.RelayUrl);
        }

        [TestMethod]
        public void FromPTag_EmptyPetname_CreatesContactWithNullPetname()
        {
            var pTag = new NostrTag("p", new[] {
                "b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c",
                "",
                "wss://relay.example.com"
            });

            var contact = ContactEntry.FromPTag(pTag);

            Assert.AreEqual("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c", contact.ContactPubkey.Value);
            Assert.IsNull(contact.Petname);
            Assert.AreEqual("wss://relay.example.com", contact.RelayUrl);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FromPTag_InvalidTagName_ThrowsException()
        {
            var invalidTag = new NostrTag("e", new[] { "some_value" });

            ContactEntry.FromPTag(invalidTag);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FromPTag_EmptyValues_ThrowsException()
        {
            var invalidTag = new NostrTag("p", new string[0]);

            ContactEntry.FromPTag(invalidTag);
        }

        [TestMethod]
        public void ToPTag_FullContact_CreatesCorrectTag()
        {
            var contact = new ContactEntry(
                new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c"),
                "alice",
                "wss://relay.example.com"
            );

            var pTag = contact.ToPTag();

            Assert.AreEqual("p", pTag.Name);
            Assert.AreEqual(3, pTag.Values.Count);
            Assert.AreEqual("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c", pTag.Values[0]);
            Assert.AreEqual("alice", pTag.Values[1]);
            Assert.AreEqual("wss://relay.example.com", pTag.Values[2]);
        }

        [TestMethod]
        public void ToPTag_PubkeyOnly_CreatesCorrectTag()
        {
            var contact = new ContactEntry(new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c"));

            var pTag = contact.ToPTag();

            Assert.AreEqual("p", pTag.Name);
            Assert.AreEqual(1, pTag.Values.Count);
            Assert.AreEqual("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c", pTag.Values[0]);
        }

        [TestMethod]
        public void ToPTag_PubkeyAndPetname_CreatesCorrectTag()
        {
            var contact = new ContactEntry(
                new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c"),
                "alice"
            );

            var pTag = contact.ToPTag();

            Assert.AreEqual("p", pTag.Name);
            Assert.AreEqual(2, pTag.Values.Count);
            Assert.AreEqual("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c", pTag.Values[0]);
            Assert.AreEqual("alice", pTag.Values[1]);
        }

        [TestMethod]
        public void ToPTag_RelayWithoutPetname_CreatesCorrectTag()
        {
            var contact = new ContactEntry(
                new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c"),
                null,
                "wss://relay.example.com"
            );

            var pTag = contact.ToPTag();

            Assert.AreEqual("p", pTag.Name);
            Assert.AreEqual(3, pTag.Values.Count);
            Assert.AreEqual("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c", pTag.Values[0]);
            Assert.AreEqual("", pTag.Values[1]); // Empty string for missing petname
            Assert.AreEqual("wss://relay.example.com", pTag.Values[2]);
        }

        [TestMethod]
        public void RoundTrip_ToPTagFromPTag_MaintainsData()
        {
            var originalContact = new ContactEntry(
                new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c"),
                "alice",
                "wss://relay.example.com"
            );

            var pTag = originalContact.ToPTag();
            var roundTripContact = ContactEntry.FromPTag(pTag);

            Assert.AreEqual(originalContact.ContactPubkey.Value, roundTripContact.ContactPubkey.Value);
            Assert.AreEqual(originalContact.Petname, roundTripContact.Petname);
            Assert.AreEqual(originalContact.RelayUrl, roundTripContact.RelayUrl);
        }
    }
}
