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
            // Per-operation timeout for WebSocket operations
            using var operationCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

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

            var messageCount = 0;
            var maxMessages = 35;
            var maxTimeSeconds = 90;
            var startTime = DateTime.UtcNow;

            // Use the operation timeout for StreamAsync but handle cancellation gracefully
            try
            {
                await foreach (var message in client.StreamAsync(subscriptionId, operationCts.Token))
                {
                    Console.WriteLine($"[STREAM] Message type: {message.Type}");
                    messageCount++;
                    Console.WriteLine($"Message count: {messageCount}");

                    // Exit conditions: either max messages or max time
                    if (messageCount >= maxMessages)
                    {
                        Console.WriteLine($"Reached maximum messages ({maxMessages}), stopping...");
                        break;
                    }

                    if ((DateTime.UtcNow - startTime).TotalSeconds >= maxTimeSeconds)
                    {
                        Console.WriteLine($"Reached maximum time ({maxTimeSeconds}s), stopping...");
                        break;
                    }

                    // Reset the operation timeout for the next message
                    operationCts.CancelAfter(TimeSpan.FromSeconds(30));
                }
            }
            catch (OperationCanceledException) when (operationCts.Token.IsCancellationRequested)
            {
                Console.WriteLine("Operation timed out waiting for messages (30s timeout)");
            }

            Console.WriteLine("Closing subscription...");

            // Use a fresh timeout for the close operation
            using var closeCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try
            {
                await client.CloseSubscriptionAsync(subscriptionId);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Warning: Close operation timed out");
            }

            Console.WriteLine("CLI session completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            throw;
        }
    }
}
