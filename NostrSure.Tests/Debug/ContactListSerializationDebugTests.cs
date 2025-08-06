using NostrSure.Domain.Entities;
using NostrSure.Domain.ValueObjects;
using NostrSure.Infrastructure.Serialization;
using System.Text.Json;

namespace NostrSure.Tests.Debug
{
    [TestCategory("Debug")]
    [TestClass]
    public class ContactListSerializationDebugTests
    {
        private static JsonSerializerOptions GetOptions()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new NostrEventJsonConverter());
            return options;
        }

        [TestMethod]
        public void Debug_ContactListEvent_Serialization()
        {
            var contacts = new List<ContactEntry>
            {
                new ContactEntry(new Pubkey("b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c"), "alice")
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

            Console.WriteLine("Generated JSON:");
            Console.WriteLine(json);

            // Let's also check the properties of the original event
            Console.WriteLine($"Original Event Kind: {originalEvent.Kind}");
            Console.WriteLine($"Original Event Tags Count: {originalEvent.Tags.Count}");
            Console.WriteLine($"Original Event Contacts Count: {originalEvent.Contacts.Count}");

            // Now try to deserialize
            try
            {
                var deserializedEvent = JsonSerializer.Deserialize<NostrEvent>(json, options);
                Console.WriteLine("Deserialization successful!");
                Console.WriteLine($"Deserialized Event Type: {deserializedEvent?.GetType().Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Deserialization failed: {ex.Message}");
                throw;
            }

            Assert.IsNotNull(json);
        }

        [TestMethod]
        public void Debug_RoundTrip_Exact_Replica()
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
            Console.WriteLine("JSON for round-trip test:");
            Console.WriteLine(json);

            var deserializedEvent = JsonSerializer.Deserialize<NostrEvent>(json, options);

            Assert.IsNotNull(deserializedEvent);
            Assert.IsInstanceOfType(deserializedEvent, typeof(ContactListEvent));

            var contactList = (ContactListEvent)deserializedEvent;
            Assert.AreEqual(3, contactList.Contacts.Count);
        }
    }
}
