using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using NostrSure.Infrastructure.Client.Abstractions;

namespace NostrSure.Infrastructure.Client.Implementation.Connection;

/// <summary>
/// Manages WebSocket connection lifecycle operations
/// </summary>
public sealed class ConnectionManager : IConnectionManager
{
    private readonly ClientWebSocket _webSocket;
    private readonly IConnectionStateManager _stateManager;
    private readonly IConnectionErrorHandler _errorHandler;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ILogger<ConnectionManager>? _logger;
    private bool _disposed;

    public ConnectionManager(
        IConnectionStateManager stateManager,
        IConnectionErrorHandler errorHandler,
        ILogger<ConnectionManager>? logger = null)
    {
        _webSocket = new ClientWebSocket();
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _cancellationTokenSource = new CancellationTokenSource();
        _logger = logger;
    }

    public WebSocketState State => _webSocket.State;

    public event EventHandler? Disconnected;

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogDebug("Attempting to connect to {Uri}", uri);
            
            var combinedToken = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token)
                .Token;
                
            await _webSocket.ConnectAsync(uri, combinedToken);
            _stateManager.UpdateState(WebSocketState.Open);
            
            _logger?.LogInformation("Successfully connected to {Uri}", uri);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect to {Uri}", uri);
            await _errorHandler.HandleErrorAsync(ex, nameof(ConnectAsync));
            throw;
        }
    }

    public async Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure,
                                string? statusDescription = null,
                                CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogDebug("Closing WebSocket connection with status {CloseStatus}", closeStatus);
            
            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
                _stateManager.UpdateState(_webSocket.State);
                _logger?.LogInformation("WebSocket connection closed successfully");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error occurred while closing WebSocket connection");
            await _errorHandler.HandleErrorAsync(ex, nameof(CloseAsync));
        }
        finally
        {
            _cancellationTokenSource.Cancel();
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _logger?.LogDebug("Disposing ConnectionManager");
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _webSocket.Dispose();
            _disposed = true;
        }
    }
}