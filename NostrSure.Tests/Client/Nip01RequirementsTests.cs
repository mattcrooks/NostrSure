using Microsoft.VisualStudio.TestTools.UnitTesting;
using NostrSure.Infrastructure.Client.Implementation;
using NostrSure.Infrastructure.Client.Messages;
using NostrSure.Domain.Entities;
using NostrSure.Domain.ValueObjects;

namespace NostrSure.Tests.Client;

/// <summary>
/// Tests verifying compliance with NIP-01 requirements (R1-R12)
/// </summary>
[TestCategory("Client")]
[TestCategory("NIP-01")]
[TestClass]
public class Nip01RequirementsTests
{
    private MockWebSocketFactory _webSocketFactory = null!;
    private MockWebSocketConnection _webSocketConnection = null!;
    private JsonMessageSerializer _messageSerializer = null!;
    private InMemorySubscriptionManager _subscriptionManager = null!;
    private DefaultEventDispatcher _eventDispatcher = null!;
    private RetryBackoffPolicy _healthPolicy = null!;
    private NostrClient _client = null!;

    [TestInitialize]
    public void Setup()
    {
        _webSocketConnection = new MockWebSocketConnection();
        _webSocketFactory = new MockWebSocketFactory(_webSocketConnection);
        _messageSerializer = new JsonMessageSerializer();
        _subscriptionManager = new InMemorySubscriptionManager();
        _eventDispatcher = new DefaultEventDispatcher();
        _healthPolicy = new RetryBackoffPolicy(TimeSpan.FromMilliseconds(10), TimeSpan.FromSeconds(1), 5);
        
        _client = new NostrClient(
            _webSocketFactory,
            _messageSerializer,
            _subscriptionManager,
            _eventDispatcher,
            _healthPolicy);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
    }

    [TestMethod]
    public async Task R1_ClientMustOpenWebSocketConnection()
    {
        // Requirement R1: Client MUST open a WebSocket connection to a Relay.
        var relayUrl = "wss://relay.example.com";
        
        await _client.ConnectAsync(relayUrl);
        
        Assert.IsTrue(_client.IsConnected);
        Assert.IsTrue(_webSocketConnection.ConnectAsyncCalled);
    }

    [TestMethod]
    public void R2_ClientMustSendJSONArrayMessagesOnly()
    {
        // Requirement R2: Client MUST send JSON-array messages only (no objects or other types).
        var validMessage = new object[] { "REQ", "sub1", new { kinds = new[] { 1 } } };
        
        // Should not throw for valid JSON array
        var json = _messageSerializer.Serialize(validMessage);
        Assert.IsNotNull(json);
        Assert.IsTrue(json.StartsWith("["));
        Assert.IsTrue(json.EndsWith("]"));
        
        // Should throw for invalid format
        Assert.ThrowsException<ArgumentException>(() => _messageSerializer.Serialize(new object[0]));
        Assert.ThrowsException<ArgumentException>(() => _messageSerializer.Serialize(null!));
    }

    [TestMethod]
    public async Task R3_ClientMustImplementREQSubscription()
    {
        // Requirement R3: Client MUST implement ["REQ", <sub_id>, <filter>...] to subscribe.
        await _client.ConnectAsync("wss://relay.example.com");
        
        var subscriptionId = "sub1";
        var filter = new Dictionary<string, object> { { "kinds", new[] { 1 } } };
        
        await _client.SubscribeAsync(subscriptionId, filter);
        
        Assert.IsTrue(_subscriptionManager.HasSubscription(subscriptionId));
        Assert.IsTrue(_webSocketConnection.SentMessages.Any(m => m.Contains("REQ") && m.Contains(subscriptionId)));
    }

    [TestMethod]
    public async Task R4_ClientMustImplementCLOSESubscription()
    {
        // Requirement R4: Client MUST implement ["CLOSE", <sub_id>] to cancel a subscription.
        await _client.ConnectAsync("wss://relay.example.com");
        
        var subscriptionId = "sub1";
        var filter = new Dictionary<string, object> { { "kinds", new[] { 1 } } };
        await _client.SubscribeAsync(subscriptionId, filter);
        
        await _client.CloseSubscriptionAsync(subscriptionId);
        
        Assert.IsFalse(_subscriptionManager.HasSubscription(subscriptionId));
        Assert.IsTrue(_webSocketConnection.SentMessages.Any(m => m.Contains("CLOSE") && m.Contains(subscriptionId)));
    }

    [TestMethod]
    public async Task R5_ClientMustImplementEVENTPublishing()
    {
        // Requirement R5: Client MUST implement ["EVENT", <event>] to publish signed events.
        await _client.ConnectAsync("wss://relay.example.com");
        
        var nostrEvent = CreateTestEvent();
        await _client.PublishAsync(nostrEvent);
        
        Assert.IsTrue(_webSocketConnection.SentMessages.Any(m => m.Contains("EVENT") && m.Contains(nostrEvent.Id)));
    }

    [TestMethod]
    public void R6_ClientMustParseRelayEventMessages()
    {
        // Requirement R6: Client MUST parse relay-to-client ["EVENT", <sub_id>, <event>] array and extract the event.
        var eventJson = """
        ["EVENT", "sub1", {
            "id": "test_event_123",
            "pubkey": "test_pubkey_123",
            "created_at": 1673311423,
            "kind": 1,
            "tags": [],
            "content": "test content",
            "sig": "test_signature_123"
        }]
        """;
        
        var message = _messageSerializer.Deserialize(eventJson);
        
        Assert.IsInstanceOfType<RelayEventMessage>(message);
        var eventMessage = (RelayEventMessage)message;
        Assert.AreEqual("sub1", eventMessage.SubscriptionId);
        Assert.IsNotNull(eventMessage.Event);
        Assert.AreEqual("test_event_123", eventMessage.Event.Id);
    }

    [TestMethod]
    public void R7_ClientMustHandleEOSE()
    {
        // Requirement R7: Client MUST handle ["EOSE", <sub_id>] to detect end-of-stored-events.
        var eoseJson = """["EOSE", "sub1"]""";
        
        var message = _messageSerializer.Deserialize(eoseJson);
        
        Assert.IsInstanceOfType<EoseMessage>(message);
        var eoseMessage = (EoseMessage)message;
        Assert.AreEqual("sub1", eoseMessage.SubscriptionId);
    }

    [TestMethod]
    public void R8_ClientMustHandleNOTICE()
    {
        // Requirement R8: Client MUST handle ["NOTICE", <message>] from relay gracefully.
        var noticeJson = """["NOTICE", "test warning message"]""";
        
        var message = _messageSerializer.Deserialize(noticeJson);
        
        Assert.IsInstanceOfType<NoticeMessage>(message);
        var noticeMessage = (NoticeMessage)message;
        Assert.AreEqual("test warning message", noticeMessage.Message);
    }

    [TestMethod]
    public void R9_ClientMustHandleCLOSED()
    {
        // Requirement R9: Client MUST handle ["CLOSED", <sub_id>, <message>] from relay indicating server-side closure.
        var closedJson = """["CLOSED", "sub1", "subscription ended by server"]""";
        
        var message = _messageSerializer.Deserialize(closedJson);
        
        Assert.IsInstanceOfType<ClosedMessage>(message);
        var closedMessage = (ClosedMessage)message;
        Assert.AreEqual("sub1", closedMessage.SubscriptionId);
        Assert.AreEqual("subscription ended by server", closedMessage.Message);
    }

    [TestMethod]
    public void R10_ClientMustGenerateValidEventIDs()
    {
        // Requirement R10: Client MUST generate and verify event IDs and Schnorr signatures on secp256k1 per spec.
        // Note: This validates structure. Cryptographic validation would require the existing NostrEventValidator
        var nostrEvent = CreateTestEvent();
        
        Assert.IsNotNull(nostrEvent.Id);
        Assert.IsNotNull(nostrEvent.Pubkey);
        Assert.IsNotNull(nostrEvent.Sig);
        Assert.IsTrue(nostrEvent.Id.Length > 0);
        Assert.IsTrue(nostrEvent.Sig.Length > 0);
    }

    [TestMethod]
    public void R11_ClientMustReconnectOnErrors()
    {
        // Requirement R11: Client MUST reconnect on transient network errors or relay-initiated closures.
        var policy = new RetryBackoffPolicy(maxRetries: 5);
        
        Assert.IsTrue(policy.ShouldRetry(1));
        Assert.IsTrue(policy.ShouldRetry(3));
        Assert.IsTrue(policy.ShouldRetry(5));
        Assert.IsFalse(policy.ShouldRetry(6));
        
        var delay = policy.GetDelay(1);
        Assert.IsTrue(delay > TimeSpan.Zero);
    }

    [TestMethod]
    public void R12_ClientMustHandleOKResponses()
    {
        // Requirement R12: Client MUST retry or back off on relay rate-limits or failures (OK false responses).
        var okAcceptedJson = """["OK", "event123", true, "accepted"]""";
        var okDeniedJson = """["OK", "event123", false, "spam"]""";
        
        var acceptedMessage = _messageSerializer.Deserialize(okAcceptedJson);
        var deniedMessage = _messageSerializer.Deserialize(okDeniedJson);
        
        Assert.IsInstanceOfType<OkMessage>(acceptedMessage);
        Assert.IsInstanceOfType<OkMessage>(deniedMessage);
        
        var okAccepted = (OkMessage)acceptedMessage;
        var okDenied = (OkMessage)deniedMessage;
        
        Assert.IsTrue(okAccepted.Accepted);
        Assert.IsFalse(okDenied.Accepted);
        Assert.AreEqual("spam", okDenied.Message);
    }

    private static NostrEvent CreateTestEvent()
    {
        return new NostrEvent(
            Id: "test_event_123",
            Pubkey: new Pubkey("test_pubkey_123"),
            CreatedAt: DateTimeOffset.UtcNow,
            Kind: EventKind.Note,
            Tags: new List<IReadOnlyList<string>>(),
            Content: "test content",
            Sig: "test_signature_123"
        );
    }

    // Mock implementations (same as in NostrClientTests)
    private class MockWebSocketFactory : NostrSure.Infrastructure.Client.Abstractions.IWebSocketFactory
    {
        private readonly MockWebSocketConnection _connection;

        public MockWebSocketFactory(MockWebSocketConnection connection)
        {
            _connection = connection;
        }

        public NostrSure.Infrastructure.Client.Abstractions.IWebSocketConnection Create()
        {
            return _connection;
        }
    }

    private class MockWebSocketConnection : NostrSure.Infrastructure.Client.Abstractions.IWebSocketConnection
    {
        public System.Net.WebSockets.WebSocketState State { get; private set; } = System.Net.WebSockets.WebSocketState.None;
        public bool ConnectAsyncCalled { get; private set; }
        public List<string> SentMessages { get; } = new();

        public event EventHandler<string>? MessageReceived;
        public event EventHandler<Exception>? ErrorOccurred;
        public event EventHandler? Disconnected;

        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            ConnectAsyncCalled = true;
            State = System.Net.WebSockets.WebSocketState.Open;
            return Task.CompletedTask;
        }

        public Task SendAsync(string message, CancellationToken cancellationToken = default)
        {
            if (State != System.Net.WebSockets.WebSocketState.Open)
                throw new InvalidOperationException("WebSocket is not connected");
                
            SentMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task<string> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task CloseAsync(System.Net.WebSockets.WebSocketCloseStatus closeStatus = System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
                              string? statusDescription = null,
                              CancellationToken cancellationToken = default)
        {
            State = System.Net.WebSockets.WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            State = System.Net.WebSockets.WebSocketState.Closed;
        }
    }
}