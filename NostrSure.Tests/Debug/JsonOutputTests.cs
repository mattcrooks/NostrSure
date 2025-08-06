using NostrSure.Domain.Entities;
using NostrSure.Domain.ValueObjects;
using NostrSure.Infrastructure.Serialization;
using System.Text.Json;

namespace NostrSure.Tests.Debug
{
    [TestCategory("Debug")]
    [TestClass]
    public class JsonOutputTests
    {
        [TestMethod]
        public void OutputActualJson()
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

            // Let's see the exact JSON
            File.WriteAllText("debug_output.json", json);

            Console.WriteLine("=== ACTUAL JSON START ===");
            Console.WriteLine(json);
            Console.WriteLine("=== ACTUAL JSON END ===");

            Console.WriteLine($"JSON Length: {json.Length}");
            Console.WriteLine($"Contains id: {json.Contains("\"id\":")}");
            Console.WriteLine($"Contains pubkey: {json.Contains("\"pubkey\":")}");
            Console.WriteLine($"Contains created_at: {json.Contains("\"created_at\":")}");
            Console.WriteLine($"Contains kind: {json.Contains("\"kind\":")}");
            Console.WriteLine($"Contains tags: {json.Contains("\"tags\":")}");
            Console.WriteLine($"Contains content: {json.Contains("\"content\":")}");
            Console.WriteLine($"Contains sig: {json.Contains("\"sig\":")}");

            Assert.IsNotNull(json);
        }
    }
}
