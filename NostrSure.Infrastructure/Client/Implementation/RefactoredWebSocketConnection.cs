using Microsoft.Extensions.Logging;
using NostrSure.Infrastructure.Client.Abstractions;
using System.Net.WebSockets;

namespace NostrSure.Infrastructure.Client.Implementation;

/// <summary>
/// Refactored WebSocket connection implementation using the façade pattern with SOLID principles
/// </summary>
public sealed class RefactoredWebSocketConnection : IWebSocketConnection
{
    private readonly IConnectionManager _connectionManager;
    private readonly IMessageReceiver _messageReceiver;
    private readonly IMessageSender _messageSender;
    private readonly IConnectionErrorHandler _errorHandler;
    private readonly IConnectionStateManager _stateManager;
    private readonly ILogger<RefactoredWebSocketConnection>? _logger;
    private bool _disposed;

    public RefactoredWebSocketConnection(
        IConnectionManager connectionManager,
        IMessageReceiver messageReceiver,
        IMessageSender messageSender,
        IConnectionErrorHandler errorHandler,
        IConnectionStateManager stateManager,
        ILogger<RefactoredWebSocketConnection>? logger = null)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _messageReceiver = messageReceiver ?? throw new ArgumentNullException(nameof(messageReceiver));
        _messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _logger = logger;
    }

    public WebSocketState State => _stateManager.CurrentState;

    // Delegate events to component implementations
    public event EventHandler<string>? MessageReceived
    {
        add => _messageReceiver.MessageReceived += value;
        remove => _messageReceiver.MessageReceived -= value;
    }

    public event EventHandler<Exception>? ErrorOccurred
    {
        add => _errorHandler.ErrorOccurred += value;
        remove => _errorHandler.ErrorOccurred -= value;
    }

    public event EventHandler? Disconnected
    {
        add => _connectionManager.Disconnected += value;
        remove => _connectionManager.Disconnected -= value;
    }

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Connecting to {Uri}", uri);

        await _connectionManager.ConnectAsync(uri, cancellationToken);
        await _messageReceiver.StartReceivingAsync(cancellationToken);

        _logger?.LogInformation("Successfully connected and started receiving messages");
    }

    public Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        return _messageSender.SendAsync(message, cancellationToken);
    }

    public Task<string> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        return _messageReceiver.ReceiveAsync(cancellationToken);
    }

    public async Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure,
                                string? statusDescription = null,
                                CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Closing WebSocket connection");

        await _messageReceiver.StopReceivingAsync();
        await _connectionManager.CloseAsync(closeStatus, statusDescription, cancellationToken);

        _logger?.LogInformation("WebSocket connection closed");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _logger?.LogDebug("Disposing RefactoredWebSocketConnection");

            _messageReceiver?.Dispose();
            _connectionManager?.Dispose();

            _disposed = true;
        }
    }
}