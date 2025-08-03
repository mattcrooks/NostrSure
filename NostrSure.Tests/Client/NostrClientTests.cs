using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NostrSure.Domain.Entities;
using NostrSure.Domain.ValueObjects;
using NostrSure.Infrastructure.Client.Abstractions;
using NostrSure.Infrastructure.Client.Implementation;
using NostrSure.Infrastructure.Client.Messages;

namespace NostrSure.Tests.Client;

[TestCategory("Client")]
[TestClass]
public class NostrClientTests
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
        _healthPolicy = new RetryBackoffPolicy(TimeSpan.FromMilliseconds(10), TimeSpan.FromSeconds(1), 3);
        
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
    public async Task ConnectAsync_ValidRelayUrl_ConnectsSuccessfully()
    {
        // Arrange
        var relayUrl = "wss://relay.example.com";

        // Act
        await _client.ConnectAsync(relayUrl);

        // Assert
        Assert.IsTrue(_client.IsConnected);
        Assert.AreEqual(relayUrl, _client.RelayUrl);
        Assert.IsTrue(_webSocketConnection.ConnectAsyncCalled);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task ConnectAsync_InvalidUrl_ThrowsArgumentException()
    {
        // Arrange
        var invalidUrl = "not-a-url";

        // Act & Assert
        await _client.ConnectAsync(invalidUrl);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task ConnectAsync_NonWebSocketScheme_ThrowsArgumentException()
    {
        // Arrange
        var httpUrl = "https://example.com";

        // Act & Assert
        await _client.ConnectAsync(httpUrl);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task ConnectAsync_EmptyUrl_ThrowsArgumentException()
    {
        // Act & Assert
        await _client.ConnectAsync("");
    }

    [TestMethod]
    public async Task SubscribeAsync_ValidSubscription_SendsReqMessage()
    {
        // Arrange
        await _client.ConnectAsync("wss://relay.example.com");
        var subscriptionId = "sub1";
        var filter = new Dictionary<string, object> { { "kinds", new[] { 1 } } };

        // Act
        await _client.SubscribeAsync(subscriptionId, filter);

        // Assert
        Assert.IsTrue(_subscriptionManager.HasSubscription(subscriptionId));
        Assert.IsTrue(_webSocketConnection.SentMessages.Count > 0);
        
        var sentMessage = _webSocketConnection.SentMessages.Last();
        Assert.IsTrue(sentMessage.Contains("REQ"));
        Assert.IsTrue(sentMessage.Contains(subscriptionId));
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task SubscribeAsync_NotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        var subscriptionId = "sub1";
        var filter = new Dictionary<string, object> { { "kinds", new[] { 1 } } };

        // Act & Assert
        await _client.SubscribeAsync(subscriptionId, filter);
    }

    [TestMethod]
    public async Task CloseSubscriptionAsync_ValidSubscription_SendsCloseMessage()
    {
        // Arrange
        await _client.ConnectAsync("wss://relay.example.com");
        var subscriptionId = "sub1";
        var filter = new Dictionary<string, object> { { "kinds", new[] { 1 } } };
        await _client.SubscribeAsync(subscriptionId, filter);

        // Act
        await _client.CloseSubscriptionAsync(subscriptionId);

        // Assert
        Assert.IsFalse(_subscriptionManager.HasSubscription(subscriptionId));
        
        var sentMessage = _webSocketConnection.SentMessages.Last();
        Assert.IsTrue(sentMessage.Contains("CLOSE"));
        Assert.IsTrue(sentMessage.Contains(subscriptionId));
    }

    [TestMethod]
    public async Task PublishAsync_ValidEvent_SendsEventMessage()
    {
        // Arrange
        await _client.ConnectAsync("wss://relay.example.com");
        var nostrEvent = CreateTestEvent();

        // Act
        await _client.PublishAsync(nostrEvent);

        // Assert
        Assert.IsTrue(_webSocketConnection.SentMessages.Count > 0);
        
        var sentMessage = _webSocketConnection.SentMessages.Last();
        Assert.IsTrue(sentMessage.Contains("EVENT"));
        Assert.IsTrue(sentMessage.Contains(nostrEvent.Id));
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task PublishAsync_NotConnected_ThrowsInvalidOperationException()
    {
        // Arrange
        var nostrEvent = CreateTestEvent();

        // Act & Assert
        await _client.PublishAsync(nostrEvent);
    }

    [TestMethod]
    public async Task StreamAsync_ReceivesEoseMessage_YieldsMessage()
    {
        // Arrange
        await _client.ConnectAsync("wss://relay.example.com");
        var subscriptionId = "sub1";
        
        // Simulate receiving an EOSE message
        var eoseJson = """["EOSE", "sub1"]""";
        _webSocketConnection.SimulateMessageReceived(eoseJson);

        // Act
        var messages = new List<NostrMessage>();
        var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)).Token;
        
        try
        {
            await foreach (var message in _client.StreamAsync(subscriptionId, cancellationToken))
            {
                messages.Add(message);
                if (messages.Count >= 1) break; // Get at least one message
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when timeout occurs
        }

        // Assert
        Assert.AreEqual(1, messages.Count);
        Assert.IsInstanceOfType<EoseMessage>(messages[0]);
        var eoseMsg = (EoseMessage)messages[0];
        Assert.AreEqual("sub1", eoseMsg.SubscriptionId);
    }

    [TestMethod]
    public async Task OnNotice_ReceivesNoticeMessage_TriggersEvent()
    {
        // Arrange
        await _client.ConnectAsync("wss://relay.example.com");
        var noticeReceived = false;
        var receivedNotice = (NoticeMessage?)null;

        _client.OnNotice += notice =>
        {
            noticeReceived = true;
            receivedNotice = notice;
            return Task.CompletedTask;
        };

        // Act
        var noticeJson = """["NOTICE", "test warning"]""";
        _webSocketConnection.SimulateMessageReceived(noticeJson);

        // Wait a bit for the event to be processed
        await Task.Delay(50);

        // Assert
        Assert.IsTrue(noticeReceived);
        Assert.IsNotNull(receivedNotice);
        Assert.AreEqual("test warning", receivedNotice.Message);
    }

    private static NostrEvent CreateTestEvent()
    {
        return new NostrEvent(
            Id: "test_event_id_123",
            Pubkey: new Pubkey("test_pubkey_123"),
            CreatedAt: DateTimeOffset.UtcNow,
            Kind: EventKind.Note,
            Tags: new List<NostrTag>(),
            Content: "test content",
            Sig: "test_signature_123"
        );
    }

    // Mock implementations for testing
    private class MockWebSocketFactory : IWebSocketFactory
    {
        private readonly MockWebSocketConnection _connection;

        public MockWebSocketFactory(MockWebSocketConnection connection)
        {
            _connection = connection;
        }

        public IWebSocketConnection Create()
        {
            return _connection;
        }
    }

    private class MockWebSocketConnection : IWebSocketConnection
    {
        public WebSocketState State { get; private set; } = WebSocketState.None;
        public bool ConnectAsyncCalled { get; private set; }
        public List<string> SentMessages { get; } = new();

        public event EventHandler<string>? MessageReceived;
        public event EventHandler<Exception>? ErrorOccurred;
        public event EventHandler? Disconnected;

        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
        {
            ConnectAsyncCalled = true;
            State = WebSocketState.Open;
            return Task.CompletedTask;
        }

        public Task SendAsync(string message, CancellationToken cancellationToken = default)
        {
            if (State != WebSocketState.Open)
                throw new InvalidOperationException("WebSocket is not connected");
                
            SentMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task<string> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            // Not implemented for these tests
            throw new NotImplementedException();
        }

        public Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure,
                              string? statusDescription = null,
                              CancellationToken cancellationToken = default)
        {
            State = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public void SimulateMessageReceived(string message)
        {
            MessageReceived?.Invoke(this, message);
        }

        public void SimulateError(Exception error)
        {
            ErrorOccurred?.Invoke(this, error);
        }

        public void SimulateDisconnected()
        {
            State = WebSocketState.Closed;
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            State = WebSocketState.Closed;
        }
    }
}