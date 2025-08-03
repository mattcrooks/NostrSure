using NostrSure.Domain.Entities;
using NostrSure.Infrastructure.Client.Messages;

namespace NostrSure.Infrastructure.Client.Abstractions;

/// <summary>
/// Main interface for interacting with Nostr relays
/// </summary>
public interface INostrClient : IDisposable
{
    /// <summary>
    /// Connect to a relay
    /// </summary>
    Task ConnectAsync(string relayUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribe to events matching the given filter
    /// </summary>
    Task SubscribeAsync(string subscriptionId, Dictionary<string, object> filter,
                       CancellationToken cancellationToken = default);

    /// <summary>
    /// Close a subscription
    /// </summary>
    Task CloseSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish an event to the relay
    /// </summary>
    Task PublishAsync(NostrEvent nostrEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream messages for a specific subscription
    /// </summary>
    IAsyncEnumerable<NostrMessage> StreamAsync(string? subscriptionId = null,
                                             CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if connected to relay
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Current relay URL
    /// </summary>
    string? RelayUrl { get; }

    /// <summary>
    /// Event handlers
    /// </summary>
    event Func<RelayEventMessage, Task>? OnEvent;
    event Func<EoseMessage, Task>? OnEndOfStoredEvents;
    event Func<NoticeMessage, Task>? OnNotice;
    event Func<ClosedMessage, Task>? OnClosed;
    event Func<OkMessage, Task>? OnOk;
    event Func<Exception, Task>? OnError;
}