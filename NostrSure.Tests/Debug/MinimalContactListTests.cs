using NostrSure.Domain.Entities;
using NostrSure.Domain.ValueObjects;
using NostrSure.Infrastructure.Serialization;
using System.Text.Json;

namespace NostrSure.Tests.Debug
{
    [TestCategory("Debug")]
    [TestClass]
    public class MinimalContactListTests
    {
        [TestMethod]
        public void Minimal_ContactList_Deserialization()
        {
            var json = """
            {
                "id": "test_id",
                "pubkey": "82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2",
                "created_at": 1673311423,
                "kind": 3,
                "tags": [
                    ["p", "b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c", "alice"]
                ],
                "content": "test content",
                "sig": "test_sig"
            }
            """;

            var options = new JsonSerializerOptions();
            options.Converters.Add(new NostrEventJsonConverter());

            Console.WriteLine("Hardcoded Contact List JSON:");
            Console.WriteLine(json);

            var deserializedEvent = JsonSerializer.Deserialize<NostrEvent>(json, options);

            Assert.IsNotNull(deserializedEvent);
            Assert.AreEqual(EventKind.ContactList, deserializedEvent.Kind);
            Assert.IsInstanceOfType(deserializedEvent, typeof(ContactListEvent));

            var contactList = (ContactListEvent)deserializedEvent;
            Assert.AreEqual(1, contactList.Contacts.Count);
            Assert.AreEqual("alice", contactList.Contacts[0].Petname);
        }
    }
}
