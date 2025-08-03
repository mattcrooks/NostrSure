using NostrSure.Infrastructure.Client.Abstractions;
using System.Collections.Concurrent;

namespace NostrSure.Infrastructure.Client.Implementation;

/// <summary>
/// In-memory subscription manager
/// </summary>
public class InMemorySubscriptionManager : ISubscriptionManager
{
    private readonly ConcurrentDictionary<string, DateTime> _subscriptions = new();
    private int _subscriptionCounter = 0;

    public string NewSubscriptionId()
    {
        var id = $"sub_{Interlocked.Increment(ref _subscriptionCounter)}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        return id;
    }

    public void AddSubscription(string subscriptionId)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
            throw new ArgumentException("Subscription ID cannot be null or empty", nameof(subscriptionId));

        _subscriptions.TryAdd(subscriptionId, DateTime.UtcNow);
    }

    public void RemoveSubscription(string subscriptionId)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
            return;

        _subscriptions.TryRemove(subscriptionId, out _);
    }

    public bool HasSubscription(string subscriptionId)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
            return false;

        return _subscriptions.ContainsKey(subscriptionId);
    }

    public IEnumerable<string> GetActiveSubscriptions()
    {
        return _subscriptions.Keys.ToList();
    }
}