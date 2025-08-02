using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NostrSure.Domain.Entities;
using NostrSure.Infrastructure.Client.Abstractions;
using NostrSure.Infrastructure.Client.Messages;

namespace NostrSure.Infrastructure.Client.Implementation;

/// <summary>
/// Main Nostr client implementation
/// </summary>
public class NostrClient : INostrClient
{
    private readonly IWebSocketFactory _webSocketFactory;
    private readonly IMessageSerializer _messageSerializer;
    private readonly ISubscriptionManager _subscriptionManager;
    private readonly IEventDispatcher _eventDispatcher;
    private readonly IHealthPolicy _healthPolicy;
    private readonly ILogger<NostrClient>? _logger;

    private IWebSocketConnection? _connection;
    private readonly ConcurrentQueue<NostrMessage> _messageQueue = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _disposed;

    // Events
    public event Func<RelayEventMessage, Task>? OnEvent;
    public event Func<EoseMessage, Task>? OnEndOfStoredEvents;
    public event Func<NoticeMessage, Task>? OnNotice;
    public event Func<ClosedMessage, Task>? OnClosed;
    public event Func<OkMessage, Task>? OnOk;
    public event Func<Exception, Task>? OnError;

    public string? RelayUrl { get; private set; }
    public bool IsConnected => _connection?.State == WebSocketState.Open;

    public NostrClient(
        IWebSocketFactory webSocketFactory,
        IMessageSerializer messageSerializer,
        ISubscriptionManager subscriptionManager,
        IEventDispatcher eventDispatcher,
        IHealthPolicy healthPolicy,
        ILogger<NostrClient>? logger = null)
    {
        _webSocketFactory = webSocketFactory ?? throw new ArgumentNullException(nameof(webSocketFactory));
        _messageSerializer = messageSerializer ?? throw new ArgumentNullException(nameof(messageSerializer));
        _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
        _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
        _healthPolicy = healthPolicy ?? throw new ArgumentNullException(nameof(healthPolicy));
        _logger = logger;

        // Wire up event dispatcher to our events
        _eventDispatcher.OnEvent += (msg) => OnEvent?.Invoke(msg) ?? Task.CompletedTask;
        _eventDispatcher.OnEndOfStoredEvents += (msg) => OnEndOfStoredEvents?.Invoke(msg) ?? Task.CompletedTask;
        _eventDispatcher.OnNotice += (msg) => OnNotice?.Invoke(msg) ?? Task.CompletedTask;
        _eventDispatcher.OnClosed += (msg) => OnClosed?.Invoke(msg) ?? Task.CompletedTask;
        _eventDispatcher.OnOk += (msg) => OnOk?.Invoke(msg) ?? Task.CompletedTask;
    }

    public async Task ConnectAsync(string relayUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relayUrl))
            throw new ArgumentException("Relay URL cannot be null or empty", nameof(relayUrl));

        if (!Uri.TryCreate(relayUrl, UriKind.Absolute, out var uri))
            throw new ArgumentException("Invalid relay URL", nameof(relayUrl));

        if (uri.Scheme != "ws" && uri.Scheme != "wss")
            throw new ArgumentException("Relay URL must use ws:// or wss:// scheme", nameof(relayUrl));

        var combinedToken = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token)
            .Token;

        var attempt = 0;
        while (!combinedToken.IsCancellationRequested)
        {
            try
            {
                _connection = _webSocketFactory.Create();
                SetupConnectionEvents();

                // Set timeout for connection (requirement R1: within 5 seconds)
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var timeoutToken = CancellationTokenSource.CreateLinkedTokenSource(
                    combinedToken, timeoutCts.Token);

                await _connection.ConnectAsync(uri, timeoutToken.Token);
                
                RelayUrl = relayUrl;
                _logger?.LogInformation("Connected to relay: {RelayUrl}", relayUrl);
                
                return; // Success
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                attempt++;
                _logger?.LogWarning(ex, "Connection attempt {Attempt} failed to {RelayUrl}", attempt, relayUrl);

                if (!_healthPolicy.ShouldRetry(attempt))
                {
                    _logger?.LogError("Max connection attempts reached for {RelayUrl}", relayUrl);
                    await OnError?.Invoke(ex);
                    throw;
                }

                await _healthPolicy.DelayAsync(attempt, combinedToken);
            }
        }
    }

    public async Task SubscribeAsync(string subscriptionId, Dictionary<string, object> filter,
                                   CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
            throw new ArgumentException("Subscription ID cannot be null or empty", nameof(subscriptionId));

        ArgumentNullException.ThrowIfNull(filter);

        if (!IsConnected)
            throw new InvalidOperationException("Not connected to a relay");

        try
        {
            _subscriptionManager.AddSubscription(subscriptionId);
            
            var reqMessage = new object[] { "REQ", subscriptionId, filter };
            var json = _messageSerializer.Serialize(reqMessage);
            
            await _connection!.SendAsync(json, cancellationToken);
            _logger?.LogDebug("Sent subscription: {SubscriptionId}", subscriptionId);
        }
        catch (Exception ex)
        {
            _subscriptionManager.RemoveSubscription(subscriptionId);
            _logger?.LogError(ex, "Failed to send subscription: {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task CloseSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subscriptionId))
            throw new ArgumentException("Subscription ID cannot be null or empty", nameof(subscriptionId));

        if (!IsConnected)
            throw new InvalidOperationException("Not connected to a relay");

        try
        {
            var closeMessage = new object[] { "CLOSE", subscriptionId };
            var json = _messageSerializer.Serialize(closeMessage);
            
            await _connection!.SendAsync(json, cancellationToken);
            _subscriptionManager.RemoveSubscription(subscriptionId);
            
            _logger?.LogDebug("Closed subscription: {SubscriptionId}", subscriptionId);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to close subscription: {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task PublishAsync(NostrEvent nostrEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nostrEvent);

        if (!IsConnected)
            throw new InvalidOperationException("Not connected to a relay");

        try
        {
            var eventMessage = new object[] { "EVENT", nostrEvent };
            var json = _messageSerializer.Serialize(eventMessage);
            
            await _connection!.SendAsync(json, cancellationToken);
            _logger?.LogDebug("Published event: {EventId}", nostrEvent.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to publish event: {EventId}", nostrEvent.Id);
            throw;
        }
    }

    public async IAsyncEnumerable<NostrMessage> StreamAsync(string? subscriptionId = null,
                                                          [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var combinedToken = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token)
            .Token;

        while (!combinedToken.IsCancellationRequested)
        {
            if (_messageQueue.TryDequeue(out var message))
            {
                // Filter by subscription ID if specified
                if (subscriptionId == null || IsMessageForSubscription(message, subscriptionId))
                {
                    yield return message;
                }
            }
            else
            {
                // Wait a bit before checking again
                await Task.Delay(10, combinedToken);
            }
        }
    }

    private void SetupConnectionEvents()
    {
        if (_connection == null) return;

        _connection.MessageReceived += OnMessageReceived;
        _connection.ErrorOccurred += OnConnectionError;
        _connection.Disconnected += OnConnectionDisconnected;
    }

    private void OnMessageReceived(object? sender, string json)
    {
        try
        {
            var message = _messageSerializer.Deserialize(json);
            _messageQueue.Enqueue(message);
            _eventDispatcher.Dispatch(message);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to process received message: {Json}", json);
            OnError?.Invoke(ex);
        }
    }

    private void OnConnectionError(object? sender, Exception ex)
    {
        _logger?.LogError(ex, "WebSocket connection error");
        OnError?.Invoke(ex);
    }

    private void OnConnectionDisconnected(object? sender, EventArgs e)
    {
        _logger?.LogWarning("WebSocket disconnected from {RelayUrl}", RelayUrl);
        
        // Attempt reconnection in background
        _ = Task.Run(async () =>
        {
            if (RelayUrl != null && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await ConnectAsync(RelayUrl, _cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to reconnect to {RelayUrl}", RelayUrl);
                    await OnError?.Invoke(ex)!;
                }
            }
        });
    }

    private static bool IsMessageForSubscription(NostrMessage message, string subscriptionId)
    {
        return message switch
        {
            RelayEventMessage eventMsg => eventMsg.SubscriptionId == subscriptionId,
            EoseMessage eoseMsg => eoseMsg.SubscriptionId == subscriptionId,
            ClosedMessage closedMsg => closedMsg.SubscriptionId == subscriptionId,
            _ => true // NOTICE, OK messages are global
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cancellationTokenSource.Cancel();
            _connection?.Dispose();
            _cancellationTokenSource.Dispose();
            _disposed = true;
        }
    }
}