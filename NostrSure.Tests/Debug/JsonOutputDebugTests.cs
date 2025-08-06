using NostrSure.Domain.Entities;
using NostrSure.Domain.ValueObjects;
using NostrSure.Infrastructure.Serialization;
using System.Text.Json;

namespace NostrSure.Tests.Debug
{
    [TestCategory("Debug")]
    [TestClass]
    public class JsonOutputDebugTests
    {
        [TestMethod]
        public void Debug_BasicEvent_JsonOutput()
        {
            var basicEvent = new NostrEvent(
                "test_id",
                new Pubkey("82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2"),
                DateTimeOffset.FromUnixTimeSeconds(1673311423),
                EventKind.Note,
                new List<NostrTag>(),
                "test content",
                "test_sig"
            );

            var options = new JsonSerializerOptions();
            options.Converters.Add(new NostrEventJsonConverter());

            var json = JsonSerializer.Serialize(basicEvent, options);
            Console.WriteLine("Basic Event JSON:");
            Console.WriteLine(json);

            // Test each field
            Assert.IsTrue(json.Contains("\"id\":"), "Missing id field");
            Assert.IsTrue(json.Contains("\"pubkey\":"), "Missing pubkey field");
            Assert.IsTrue(json.Contains("\"created_at\":"), "Missing created_at field");
            Assert.IsTrue(json.Contains("\"kind\":"), "Missing kind field");
            Assert.IsTrue(json.Contains("\"tags\":"), "Missing tags field");
            Assert.IsTrue(json.Contains("\"content\":"), "Missing content field");
            Assert.IsTrue(json.Contains("\"sig\":"), "Missing sig field");
        }

        [TestMethod]
        public void Debug_ContactListEvent_JsonOutput()
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
            Console.WriteLine("ContactListEvent JSON:");
            Console.WriteLine(json);

            // Test each field
            Assert.IsTrue(json.Contains("\"id\":"), "Missing id field");
            Assert.IsTrue(json.Contains("\"pubkey\":"), "Missing pubkey field"); 
            Assert.IsTrue(json.Contains("\"created_at\":"), "Missing created_at field");
            Assert.IsTrue(json.Contains("\"kind\":"), "Missing kind field");
            Assert.IsTrue(json.Contains("\"tags\":"), "Missing tags field");
            Assert.IsTrue(json.Contains("\"content\":"), "Missing content field");
            Assert.IsTrue(json.Contains("\"sig\":"), "Missing sig field");
        }
    }
}
