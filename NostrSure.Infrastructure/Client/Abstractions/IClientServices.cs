using NostrSure.Infrastructure.Client.Messages;

namespace NostrSure.Infrastructure.Client.Abstractions;

/// <summary>
/// Serializes and deserializes Nostr protocol messages
/// </summary>
public interface IMessageSerializer
{
    string Serialize(object[] message);
    NostrMessage Deserialize(string json);
}

/// <summary>
/// Manages subscription IDs and state
/// </summary>
public interface ISubscriptionManager
{
    string NewSubscriptionId();
    void AddSubscription(string subscriptionId);
    void RemoveSubscription(string subscriptionId);
    bool HasSubscription(string subscriptionId);
    IEnumerable<string> GetActiveSubscriptions();
}

/// <summary>
/// Dispatches parsed messages to handlers
/// </summary>
public interface IEventDispatcher
{
    void Dispatch(NostrMessage message);
    event Func<RelayEventMessage, Task>? OnEvent;
    event Func<EoseMessage, Task>? OnEndOfStoredEvents;
    event Func<NoticeMessage, Task>? OnNotice;
    event Func<ClosedMessage, Task>? OnClosed;
    event Func<OkMessage, Task>? OnOk;
}

/// <summary>
/// Handles reconnection policies and backoff
/// </summary>
public interface IHealthPolicy
{
    Task DelayAsync(int attempt, CancellationToken cancellationToken = default);
    bool ShouldRetry(int attempt);
    TimeSpan GetDelay(int attempt);
}