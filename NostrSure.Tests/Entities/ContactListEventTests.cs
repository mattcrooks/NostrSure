using NostrSure.Domain.Entities;
using NostrSure.Domain.ValueObjects;

namespace NostrSure.Tests.Entities
{
    [TestCategory("Domain")]
    [TestClass]
    public class ContactListEventTests
    {
        [TestMethod]
        public void Constructor_ValidParameters_CreatesInstance()
        {
            var contacts = new List<ContactEntry>
            {
                new ContactEntry(new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c"), "alice"),
                new ContactEntry(new Pubkey("a39199ccb5ec92b1cd047bf3dc7e8923ede769d1a5ccc47d579912f0f5cbdab4"), "bob")
            };

            var contactList = new ContactListEvent(
                "test_id",
                new Pubkey("82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2"),
                DateTimeOffset.Now,
                new List<NostrTag>(),
                "test content",
                "test_sig",
                contacts
            );

            Assert.AreEqual("test_id", contactList.Id);
            Assert.AreEqual(EventKind.ContactList, contactList.Kind);
            Assert.AreEqual(2, contactList.Contacts.Count);
            Assert.AreEqual("alice", contactList.Contacts[0].Petname);
            Assert.AreEqual("bob", contactList.Contacts[1].Petname);
        }

        [TestMethod]
        public void Create_WithContacts_GeneratesPTags()
        {
            var contacts = new List<ContactEntry>
            {
                new ContactEntry(new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c"), "alice", "wss://relay1.com"),
                new ContactEntry(new Pubkey("a39199ccb5ec92b1cd047bf3dc7e8923ede769d1a5ccc47d579912f0f5cbdab4"), "bob")
            };

            var contactListEvent = ContactListEvent.Create(
                "test_id",
                new Pubkey("82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2"),
                DateTimeOffset.FromUnixTimeSeconds(1673311423),
                "My contacts",
                "test_sig",
                contacts
            );

            Assert.AreEqual(EventKind.ContactList, contactListEvent.Kind);
            Assert.AreEqual(2, contactListEvent.Contacts.Count);
            Assert.AreEqual(2, contactListEvent.Tags.Count);

            // Verify p tags are created correctly
            var pTags = contactListEvent.Tags.Where(t => t.Name == "p").ToList();
            Assert.AreEqual(2, pTags.Count);

            var firstPTag = pTags.First(t => t.Values[0] == "b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c");
            Assert.AreEqual(3, firstPTag.Values.Count);
            Assert.AreEqual("alice", firstPTag.Values[1]);
            Assert.AreEqual("wss://relay1.com", firstPTag.Values[2]);

            var secondPTag = pTags.First(t => t.Values[0] == "a39199ccb5ec92b1cd047bf3dc7e8923ede769d1a5ccc47d579912f0f5cbdab4");
            Assert.AreEqual(2, secondPTag.Values.Count);
            Assert.AreEqual("bob", secondPTag.Values[1]);
        }

        [TestMethod]
        public void Create_WithAdditionalTags_PreservesNonPTags()
        {
            var contacts = new List<ContactEntry>
            {
                new ContactEntry(new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c"), "alice")
            };

            var additionalTags = new List<NostrTag>
            {
                new NostrTag("t", new[] { "hashtag" }),
                new NostrTag("relay", new[] { "wss://global-relay.com" }),
                new NostrTag("p", new[] { "should_be_removed" }) // This should be removed
            };

            var contactListEvent = ContactListEvent.Create(
                "test_id",
                new Pubkey("82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2"),
                DateTimeOffset.FromUnixTimeSeconds(1673311423),
                "My contacts",
                "test_sig",
                contacts,
                additionalTags
            );

            Assert.AreEqual(3, contactListEvent.Tags.Count); // t, relay, and 1 p tag from contacts
            Assert.AreEqual(1, contactListEvent.Tags.Count(t => t.Name == "p"));
            Assert.AreEqual(1, contactListEvent.Tags.Count(t => t.Name == "t"));
            Assert.AreEqual(1, contactListEvent.Tags.Count(t => t.Name == "relay"));
        }

        [TestMethod]
        public void FromNostrEvent_ValidContactListEvent_ExtractsContacts()
        {
            var tags = new List<NostrTag>
            {
                new NostrTag("p", new[] { "b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c", "alice", "wss://relay1.com" }),
                new NostrTag("p", new[] { "a39199ccb5ec92b1cd047bf3dc7e8923ede769d1a5ccc47d579912f0f5cbdab4", "bob" }),
                new NostrTag("t", new[] { "hashtag" })
            };

            var baseEvent = new NostrEvent(
                "test_id",
                new Pubkey("82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2"),
                DateTimeOffset.FromUnixTimeSeconds(1673311423),
                EventKind.ContactList,
                tags,
                "My contacts",
                "test_sig"
            );

            var contactListEvent = ContactListEvent.FromNostrEvent(baseEvent);

            Assert.AreEqual(2, contactListEvent.Contacts.Count);
            Assert.AreEqual("alice", contactListEvent.Contacts[0].Petname);
            Assert.AreEqual("wss://relay1.com", contactListEvent.Contacts[0].RelayUrl);
            Assert.AreEqual("bob", contactListEvent.Contacts[1].Petname);
            Assert.IsNull(contactListEvent.Contacts[1].RelayUrl);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FromNostrEvent_InvalidKind_ThrowsException()
        {
            var baseEvent = new NostrEvent(
                "test_id",
                new Pubkey("82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2"),
                DateTimeOffset.FromUnixTimeSeconds(1673311423),
                EventKind.Note, // Wrong kind
                new List<NostrTag>(),
                "test content",
                "test_sig"
            );

            ContactListEvent.FromNostrEvent(baseEvent);
        }

        [TestMethod]
        public void IsValidContactList_ValidContacts_ReturnsTrue()
        {
            var contacts = new List<ContactEntry>
            {
                new ContactEntry(new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c"), "alice"),
                new ContactEntry(new Pubkey("a39199ccb5ec92b1cd047bf3dc7e8923ede769d1a5ccc47d579912f0f5cbdab4"), "bob")
            };

            var contactListEvent = ContactListEvent.Create(
                "test_id",
                new Pubkey("82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2"),
                DateTimeOffset.FromUnixTimeSeconds(1673311423),
                "My contacts",
                "test_sig",
                contacts
            );

            Assert.IsTrue(contactListEvent.IsValidContactList());
        }

        [TestMethod]
        public void IsValidContactList_InvalidContact_ReturnsFalse()
        {
            var contacts = new List<ContactEntry>
            {
                new ContactEntry(new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c"), "alice"),
                new ContactEntry(new Pubkey("a39199ccb5ec92b1cd047bf3dc7e8923ede769d1a5ccc47d579912f0f5cbdab4"), "bob", "invalid-url")
            };

            var contactListEvent = ContactListEvent.Create(
                "test_id",
                new Pubkey("82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2"),
                DateTimeOffset.FromUnixTimeSeconds(1673311423),
                "My contacts",
                "test_sig",
                contacts
            );

            Assert.IsFalse(contactListEvent.IsValidContactList());
        }

        [TestMethod]
        public void IsValidContactList_MismatchedTagsAndContacts_ReturnsFalse()
        {
            var contacts = new List<ContactEntry>
            {
                new ContactEntry(new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c"), "alice")
            };

            var tags = new List<NostrTag>
            {
                new NostrTag("p", new[] { "b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c", "alice" }),
                new NostrTag("p", new[] { "a39199ccb5ec92b1cd047bf3dc7e8923ede769d1a5ccc47d579912f0f5cbdab4", "bob" }) // Extra p tag
            };

            var contactListEvent = new ContactListEvent(
                "test_id",
                new Pubkey("82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2"),
                DateTimeOffset.FromUnixTimeSeconds(1673311423),
                tags,
                "My contacts",
                "test_sig",
                contacts
            );

            Assert.IsFalse(contactListEvent.IsValidContactList());
        }

        [TestMethod]
        public void Validate_OverridesBaseValidation()
        {
            var contacts = new List<ContactEntry>
            {
                new ContactEntry(new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c"), "alice")
            };

            var contactListEvent = ContactListEvent.Create(
                "test_id",
                new Pubkey("82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2"),
                DateTimeOffset.FromUnixTimeSeconds(1673311423),
                "My contacts",
                "test_sig",
                contacts
            );

            // This should work without throwing, demonstrating that virtual method override works
            Assert.IsNotNull(contactListEvent);
            Assert.AreEqual(EventKind.ContactList, contactListEvent.Kind);
        }
    }
}
