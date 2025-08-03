using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using NostrSure.Infrastructure.Client.Abstractions;

namespace NostrSure.Infrastructure.Client.Implementation;

/// <summary>
/// Manages WebSocket connection state tracking and notifications
/// </summary>
public sealed class ConnectionStateManager : IConnectionStateManager
{
    private readonly ILogger<ConnectionStateManager>? _logger;
    private WebSocketState _currentState = WebSocketState.None;
    private readonly object _stateLock = new();

    public ConnectionStateManager(ILogger<ConnectionStateManager>? logger = null)
    {
        _logger = logger;
    }

    public WebSocketState CurrentState
    {
        get
        {
            lock (_stateLock)
            {
                return _currentState;
            }
        }
    }

    public bool IsConnected => CurrentState == WebSocketState.Open;

    public event EventHandler<WebSocketState>? StateChanged;

    public void UpdateState(WebSocketState newState)
    {
        WebSocketState previousState;
        
        lock (_stateLock)
        {
            previousState = _currentState;
            if (previousState == newState)
                return; // No change, don't fire event
                
            _currentState = newState;
        }

        _logger?.LogDebug("WebSocket state changed from {PreviousState} to {NewState}", 
                         previousState, newState);

        // Fire event outside the lock to prevent deadlocks
        StateChanged?.Invoke(this, newState);
    }
}