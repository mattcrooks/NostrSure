using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NostrSure.Infrastructure.Client;
using NostrSure.Infrastructure.Client.Abstractions;

namespace NosrtSure.CLI;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("NostrSure CLI - Connect to a Nostr relay and process messages");
        Console.WriteLine("=============================================================");
        Console.WriteLine();

        var relayUrl = args.Length > 0 ? args[0] : "wss://relay.damus.io";
        Console.WriteLine($"Connecting to relay: {relayUrl}");

        // Setup DI
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddNostrClient();
        var serviceProvider = services.BuildServiceProvider();

        using var client = serviceProvider.GetRequiredService<INostrClient>();

        // Event handlers (fix CS1998 warnings by removing async from synchronous handlers)
        client.OnEvent += (eventMsg) =>
        {
            Console.WriteLine($"[EVENT] {eventMsg.SubscriptionId}: {eventMsg.Event.Content}");
            return Task.CompletedTask;
        };
        client.OnEndOfStoredEvents += (eoseMsg) =>
        {
            Console.WriteLine($"[EOSE] {eoseMsg.SubscriptionId}");
            return Task.CompletedTask;
        };
        client.OnNotice += (noticeMsg) =>
        {
            Console.WriteLine($"[NOTICE] {noticeMsg.Message}");
            return Task.CompletedTask;
        };
        client.OnError += (error) =>
        {
            Console.WriteLine($"[ERROR] {error.Message}");
            return Task.CompletedTask;
        };

        try
        {
            await client.ConnectAsync(relayUrl);
            Console.WriteLine("Connected!");

            var filter = new Dictionary<string, object>
            {
                { "kinds", new[] { 1 } },
                { "limit", 10 }
            };
            var subscriptionId = "cli_sub_1";
            Console.WriteLine($"Subscribing to text notes with subscription ID: {subscriptionId}");
            await client.SubscribeAsync(subscriptionId, filter);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var messageCount = 0;
            await foreach (var message in client.StreamAsync(subscriptionId, cts.Token))
            {
                Console.WriteLine($"[STREAM] Message type: {message.Type}");
                messageCount++;
                if (messageCount >= 20)
                    break;
            }

            Console.WriteLine("Closing subscription...");
            await client.CloseSubscriptionAsync(subscriptionId);
            Console.WriteLine("CLI session completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
        }
    }
}
