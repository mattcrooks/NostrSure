using NostrSure.Domain.Entities;
using NostrSure.Domain.ValueObjects;
using NostrSure.Infrastructure.Serialization;
using System.Text.Json;

namespace NostrSure.Tests.Debug
{
    [TestCategory("Debug")]
    [TestClass]
    public class ContactListEventCreationTests
    {
        [TestMethod]
        public void Debug_ContactListEvent_Creation()
        {
            var contacts = new List<ContactEntry>
            {
                new ContactEntry(new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c"), "alice")
            };

            var contactListEvent = ContactListEvent.Create(
                "test_contact_list_id",
                new Pubkey("82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2"),
                DateTimeOffset.FromUnixTimeSeconds(1673311423),
                "My contact list",
                "test_sig",
                contacts
            );

            Console.WriteLine($"Event ID: {contactListEvent.Id}");
            Console.WriteLine($"Event Pubkey: {contactListEvent.Pubkey.Value}");
            Console.WriteLine($"Event Kind: {contactListEvent.Kind}");
            Console.WriteLine($"Event Content: {contactListEvent.Content}");
            Console.WriteLine($"Event Sig: {contactListEvent.Sig}");
            Console.WriteLine($"Event CreatedAt: {contactListEvent.CreatedAt}");
            Console.WriteLine($"Event Tags Count: {contactListEvent.Tags.Count}");
            Console.WriteLine($"Event Contacts Count: {contactListEvent.Contacts.Count}");

            foreach (var tag in contactListEvent.Tags)
            {
                Console.WriteLine($"Tag: {tag.Name}, Values: [{string.Join(", ", tag.Values)}]");
            }

            Assert.AreEqual(EventKind.ContactList, contactListEvent.Kind);
            Assert.AreEqual(1, contactListEvent.Contacts.Count);
            Assert.AreEqual(1, contactListEvent.Tags.Count);
        }

        [TestMethod]
        public void Debug_ContactListEvent_SerializationOnly()
        {
            var contacts = new List<ContactEntry>
            {
                new ContactEntry(new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c"), "alice")
            };

            var contactListEvent = ContactListEvent.Create(
                "test_contact_list_id",
                new Pubkey("82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2"),
                DateTimeOffset.FromUnixTimeSeconds(1673311423),
                "My contact list",
                "test_sig",
                contacts
            );

            var options = new JsonSerializerOptions();
            options.Converters.Add(new NostrEventJsonConverter());

            var json = JsonSerializer.Serialize(contactListEvent, options);

            Console.WriteLine("Generated JSON (serialize only):");
            Console.WriteLine(json);

            // Check if it has all required fields
            Assert.IsTrue(json.Contains("\"id\":"));
            Assert.IsTrue(json.Contains("\"pubkey\":"));
            Assert.IsTrue(json.Contains("\"created_at\":"));
            Assert.IsTrue(json.Contains("\"kind\":"));
            Assert.IsTrue(json.Contains("\"tags\":"));
            Assert.IsTrue(json.Contains("\"content\":"));
            Assert.IsTrue(json.Contains("\"sig\":"));
        }
    }
}
