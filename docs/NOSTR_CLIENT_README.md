# NostrSure Client Library

A complete, production-ready Nostr client library for .NET 8, implementing all NIP-01 requirements with comprehensive test coverage.

## Features

- **Full NIP-01 Compliance**: Implements all 12 requirements (R1-R12) from the Nostr protocol specification
- **SOLID Architecture**: Clean separation of concerns with dependency injection support
- **Comprehensive Testing**: 87 unit tests covering all components and edge cases
- **Async Streaming**: `IAsyncEnumerable` support for real-time event streams
- **Resilient Connection**: Automatic reconnection with exponential backoff
- **Type Safety**: Strong typing for all Nostr protocol messages

## Quick Start

### 1. Register Services

```csharp
using Microsoft.Extensions.DependencyInjection;
using NostrSure.Infrastructure.Client;

var services = new ServiceCollection();
services.AddLogging();
services.AddNostrClient();

var serviceProvider = services.BuildServiceProvider();
```

### 2. Create and Connect Client

```csharp
using var client = serviceProvider.GetRequiredService<INostrClient>();

await client.ConnectAsync("wss://relay.damus.io");
```

### 3. Subscribe to Events

```csharp
// Subscribe to text notes (kind 1)
var filter = new Dictionary<string, object>
{
    { "kinds", new[] { 1 } },
    { "limit", 10 }
};

var subscriptionId = "my_subscription";
await client.SubscribeAsync(subscriptionId, filter);
```

### 4. Handle Events

```csharp
client.OnEvent += async (eventMsg) =>
{
    Console.WriteLine($"Received: {eventMsg.Event.Content}");
};

client.OnNotice += async (noticeMsg) =>
{
    Console.WriteLine($"Notice: {noticeMsg.Message}");
};
```

### 5. Stream Messages

```csharp
await foreach (var message in client.StreamAsync(subscriptionId))
{
    Console.WriteLine($"Message type: {message.Type}");
    
    if (message is RelayEventMessage eventMsg)
    {
        // Process the event
        Console.WriteLine($"Event content: {eventMsg.Event.Content}");
    }
}
```

### 6. Publish Events

```csharp
// Create a properly signed event (using existing domain logic)
var noteEvent = new NostrEvent(
    Id: computedEventId,
    Pubkey: new Pubkey(userPubkey),
    CreatedAt: DateTimeOffset.UtcNow,
    Kind: EventKind.Note,
    Tags: new List<IReadOnlyList<string>>(),
    Content: "Hello Nostr!",
    Sig: computedSignature
);

await client.PublishAsync(noteEvent);
```

## Architecture

### Core Interfaces

- **`INostrClient`**: Main client interface for connecting and communicating
- **`IWebSocketConnection`**: WebSocket abstraction for testability
- **`IMessageSerializer`**: JSON serialization for Nostr protocol messages
- **`ISubscriptionManager`**: Tracks active subscriptions
- **`IEventDispatcher`**: Routes incoming messages to handlers
- **`IHealthPolicy`**: Manages reconnection and backoff logic

### Message Types

All Nostr protocol messages are strongly typed:

- **`ReqMessage`**: Subscription requests
- **`CloseMessage`**: Subscription closures
- **`EventMessage`**: Event publishing
- **`RelayEventMessage`**: Events from relay
- **`EoseMessage`**: End of stored events
- **`NoticeMessage`**: Relay notices
- **`ClosedMessage`**: Server-side closures
- **`OkMessage`**: Event acceptance/rejection

## Configuration

### Custom Health Policy

```csharp
services.AddNostrClient(
    baseDelay: TimeSpan.FromSeconds(1),
    maxDelay: TimeSpan.FromMinutes(2),
    maxRetries: 5
);
```

### Custom Implementations

Replace any component with your own implementation:

```csharp
services.AddSingleton<IHealthPolicy, MyCustomHealthPolicy>();
services.AddSingleton<IWebSocketFactory, MyCustomWebSocketFactory>();
```

## NIP-01 Compliance

This library implements all NIP-01 requirements:

| Requirement | Description | Implementation |
|-------------|-------------|----------------|
| **R1** | WebSocket connection to relay | `WebSocketConnection` class |
| **R2** | JSON array messages only | `JsonMessageSerializer` validation |
| **R3** | REQ subscription messages | `SubscribeAsync()` method |
| **R4** | CLOSE subscription messages | `CloseSubscriptionAsync()` method |
| **R5** | EVENT publishing messages | `PublishAsync()` method |
| **R6** | Parse relay EVENT messages | `RelayEventMessage` parsing |
| **R7** | Handle EOSE messages | `EoseMessage` handling |
| **R8** | Handle NOTICE messages | `NoticeMessage` handling |
| **R9** | Handle CLOSED messages | `ClosedMessage` handling |
| **R10** | Event ID and signature validation | Integration with existing `NostrEventValidator` |
| **R11** | Reconnection on errors | `RetryBackoffPolicy` with exponential backoff |
| **R12** | Handle OK responses | `OkMessage` handling with retry logic |

## Testing

The library includes comprehensive test coverage:

- **Unit Tests**: 87 tests covering all components
- **Integration Tests**: NIP-01 compliance verification
- **Mock Support**: Full mock implementations for testing

Run tests:

```bash
dotnet test
```

Run only NIP-01 compliance tests:

```bash
dotnet test --filter "TestCategory=NIP-01"
```

## Error Handling

The client handles errors gracefully:

- **Connection Failures**: Automatic retry with exponential backoff
- **Message Parsing Errors**: Graceful degradation with error events
- **Relay Disconnections**: Automatic reconnection attempts
- **Protocol Violations**: Clear exception messages

```csharp
client.OnError += async (error) =>
{
    Console.WriteLine($"Client error: {error.Message}");
    // Handle error appropriately
};
```

## Thread Safety

All public methods are thread-safe and can be called concurrently. The client uses:

- **ConcurrentQueue**: For thread-safe message buffering
- **CancellationToken**: For proper async cancellation
- **Atomic Operations**: For state management

## Performance

The client is designed for high throughput:

- **Non-blocking I/O**: Async/await throughout
- **Streaming Support**: `IAsyncEnumerable` for backpressure
- **Efficient Serialization**: Minimal allocations
- **Connection Pooling**: Ready for multi-relay scenarios

## Examples

See `NostrSure.Infrastructure/Examples/BasicExample.cs` for complete usage examples.

## Dependencies

- **.NET 8.0**: Target framework
- **Microsoft.Extensions.DependencyInjection**: Dependency injection
- **Microsoft.Extensions.Logging**: Logging abstractions
- **NBitcoin**: Cryptographic operations (existing dependency)

## Contributing

This library follows SOLID principles and is designed for extensibility. To add new features:

1. Create interfaces for new components
2. Implement concrete classes
3. Add comprehensive unit tests
4. Update dependency injection configuration
5. Document the new functionality

## License

MIT License - see LICENSE.txt for details.