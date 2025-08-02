using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NostrSure.Infrastructure.Client;
using NostrSure.Infrastructure.Client.Abstractions;

namespace NostrSure.Examples;

/// <summary>
/// Example demonstrating basic usage of the NostrClient
/// </summary>
public class BasicClientExample
{
    /// <summary>
    /// Example demonstrating how to set up and use the NostrClient
    /// </summary>
    public static async Task RunExample()
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddNostrClient();
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Get the client from DI container
        using var client = serviceProvider.GetRequiredService<INostrClient>();
        
        // Set up event handlers
        client.OnEvent += async (eventMsg) =>
        {
            Console.WriteLine($"Received event from subscription {eventMsg.SubscriptionId}: {eventMsg.Event.Content}");
        };
        
        client.OnEndOfStoredEvents += async (eoseMsg) =>
        {
            Console.WriteLine($"End of stored events for subscription: {eoseMsg.SubscriptionId}");
        };
        
        client.OnNotice += async (noticeMsg) =>
        {
            Console.WriteLine($"Notice from relay: {noticeMsg.Message}");
        };
        
        client.OnError += async (error) =>
        {
            Console.WriteLine($"Client error: {error.Message}");
        };
        
        try
        {
            // Connect to a Nostr relay
            Console.WriteLine("Connecting to relay...");
            await client.ConnectAsync("wss://relay.damus.io");
            Console.WriteLine("Connected!");
            
            // Subscribe to text notes (kind 1)
            var filter = new Dictionary<string, object>
            {
                { "kinds", new[] { 1 } },
                { "limit", 10 }
            };
            
            var subscriptionId = "example_sub_1";
            Console.WriteLine($"Subscribing to text notes with subscription ID: {subscriptionId}");
            await client.SubscribeAsync(subscriptionId, filter);
            
            // Stream messages for a short time
            Console.WriteLine("Streaming messages for 30 seconds...");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            
            var messageCount = 0;
            await foreach (var message in client.StreamAsync(subscriptionId, cts.Token))
            {
                Console.WriteLine($"Received message type: {message.Type}");
                messageCount++;
                
                if (messageCount >= 20) // Limit to first 20 messages
                    break;
            }
            
            // Close the subscription
            Console.WriteLine("Closing subscription...");
            await client.CloseSubscriptionAsync(subscriptionId);
            
            Console.WriteLine("Example completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

/// <summary>
/// Example demonstrating publishing events
/// </summary>
public class PublishingExample
{
    public static void ShowPublishingPattern()
    {
        Console.WriteLine(@"
Example: Publishing a note to Nostr

// Setup client (same as above)
using var client = serviceProvider.GetRequiredService<INostrClient>();
await client.ConnectAsync(""wss://relay.damus.io"");

// Create a NostrEvent (in real usage, you'd properly sign this)
var noteEvent = new NostrEvent(
    Id: ComputeEventId(...),           // Proper event ID computation
    Pubkey: new Pubkey(userPubkey),    // Your public key
    CreatedAt: DateTimeOffset.UtcNow,
    Kind: EventKind.Note,
    Tags: new List<IReadOnlyList<string>>(),
    Content: ""Hello Nostr!"",
    Sig: ComputeSignature(...)         // Proper signature
);

// Publish the event
await client.PublishAsync(noteEvent);

// Handle OK response
client.OnOk += async (okMsg) =>
{
    if (okMsg.Accepted)
        Console.WriteLine($""Event {okMsg.EventId} was accepted"");
    else
        Console.WriteLine($""Event {okMsg.EventId} was rejected: {okMsg.Message}"");
};
");
    }
}

/// <summary>
/// Entry point for running examples
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("NostrSure Client Examples");
        Console.WriteLine("========================");
        Console.WriteLine();
        
        if (args.Length > 0 && args[0] == "--demo")
        {
            Console.WriteLine("Running live demo...");
            await BasicClientExample.RunExample();
        }
        else
        {
            Console.WriteLine("Usage patterns:");
            PublishingExample.ShowPublishingPattern();
            
            Console.WriteLine();
            Console.WriteLine("To run a live demo (connects to real relay):");
            Console.WriteLine("dotnet run --demo");
        }
    }
}