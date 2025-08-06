using NostrSure.Domain.Entities;
using NostrSure.Domain.ValueObjects;
using NostrSure.Infrastructure.Serialization;
using System.Text.Json;

namespace NostrSure.Tests.Debug
{
    [TestCategory("Debug")]
    [TestClass]
    public class StepByStepDebugTests
    {
        [TestMethod]
        public void StepByStep_SerializeAndDeserialize()
        {
            // Step 1: Create a simple ContactListEvent
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

            Console.WriteLine("=== ORIGINAL EVENT DETAILS ===");
            Console.WriteLine($"Type: {originalEvent.GetType().Name}");
            Console.WriteLine($"Id: '{originalEvent.Id}'");
            Console.WriteLine($"Pubkey: '{originalEvent.Pubkey.Value}'");
            Console.WriteLine($"CreatedAt: {originalEvent.CreatedAt.ToUnixTimeSeconds()}");
            Console.WriteLine($"Kind: {(int)originalEvent.Kind}");
            Console.WriteLine($"Content: '{originalEvent.Content}'");
            Console.WriteLine($"Sig: '{originalEvent.Sig}'");
            Console.WriteLine($"Tags count: {originalEvent.Tags.Count}");
            Console.WriteLine($"Contacts count: {originalEvent.Contacts.Count}");

            // Step 2: Serialize to JSON
            var options = new JsonSerializerOptions();
            options.Converters.Add(new NostrEventJsonConverter());

            var json = JsonSerializer.Serialize(originalEvent, options);

            Console.WriteLine("\n=== SERIALIZED JSON ===");
            Console.WriteLine(json);

            // Step 3: Try to deserialize
            Console.WriteLine("\n=== ATTEMPTING DESERIALIZATION ===");
            try
            {
                var deserializedEvent = JsonSerializer.Deserialize<NostrEvent>(json, options);
                Console.WriteLine("SUCCESS: Deserialization completed");
                Console.WriteLine($"Deserialized type: {deserializedEvent?.GetType().Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // Let's manually check the JSON structure
                Console.WriteLine("\n=== MANUAL JSON ANALYSIS ===");
                var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;

                Console.WriteLine($"Has id: {root.TryGetProperty("id", out _)}");
                Console.WriteLine($"Has pubkey: {root.TryGetProperty("pubkey", out _)}");
                Console.WriteLine($"Has created_at: {root.TryGetProperty("created_at", out _)}");
                Console.WriteLine($"Has kind: {root.TryGetProperty("kind", out _)}");
                Console.WriteLine($"Has tags: {root.TryGetProperty("tags", out _)}");
                Console.WriteLine($"Has content: {root.TryGetProperty("content", out _)}");
                Console.WriteLine($"Has sig: {root.TryGetProperty("sig", out _)}");

                throw;
            }

            Assert.IsNotNull(originalEvent);
        }
    }
}
