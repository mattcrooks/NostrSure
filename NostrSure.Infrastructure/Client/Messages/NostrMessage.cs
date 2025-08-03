using NostrSure.Domain.Entities;

namespace NostrSure.Infrastructure.Client.Messages;

/// <summary>
/// Base class for all Nostr protocol messages
/// </summary>
public abstract record NostrMessage(string Type);

/// <summary>
/// REQ message: ["REQ", subscription_id, filter...]
/// </summary>
public record ReqMessage(string SubscriptionId, Dictionary<string, object> Filter) 
    : NostrMessage("REQ");

/// <summary>
/// CLOSE message: ["CLOSE", subscription_id]
/// </summary>
public record CloseMessage(string SubscriptionId) 
    : NostrMessage("CLOSE");

/// <summary>
/// EVENT message for publishing: ["EVENT", event]
/// </summary>
public record EventMessage(NostrEvent Event) 
    : NostrMessage("EVENT");

/// <summary>
/// EVENT message from relay: ["EVENT", subscription_id, event]
/// </summary>
public record RelayEventMessage(string SubscriptionId, NostrEvent Event) 
    : NostrMessage("EVENT");

/// <summary>
/// EOSE message: ["EOSE", subscription_id]
/// </summary>
public record EoseMessage(string SubscriptionId) 
    : NostrMessage("EOSE");

/// <summary>
/// NOTICE message: ["NOTICE", message]
/// </summary>
public record NoticeMessage(string Message) 
    : NostrMessage("NOTICE");

/// <summary>
/// CLOSED message: ["CLOSED", subscription_id, message]
/// </summary>
public record ClosedMessage(string SubscriptionId, string Message) 
    : NostrMessage("CLOSED");

/// <summary>
/// OK message: ["OK", event_id, accepted, message]
/// </summary>
public record OkMessage(string EventId, bool Accepted, string Message) 
    : NostrMessage("OK");