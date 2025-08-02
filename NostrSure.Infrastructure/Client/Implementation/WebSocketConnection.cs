using System.Net.WebSockets;
using System.Text;
using NostrSure.Infrastructure.Client.Abstractions;

namespace NostrSure.Infrastructure.Client.Implementation;

/// <summary>
/// WebSocket connection implementation for Nostr relay communication
/// </summary>
public class WebSocketConnection : IWebSocketConnection
{
    private readonly ClientWebSocket _webSocket;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _disposed;

    public WebSocketConnection()
    {
        _webSocket = new ClientWebSocket();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public WebSocketState State => _webSocket.State;

    public event EventHandler<string>? MessageReceived;
    public event EventHandler<Exception>? ErrorOccurred;
    public event EventHandler? Disconnected;

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        try
        {
            var combinedToken = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token)
                .Token;
                
            await _webSocket.ConnectAsync(uri, combinedToken);
            
            // Start receiving messages in background
            _ = Task.Run(() => ReceiveLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
    }

    public async Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        if (_webSocket.State != WebSocketState.Open)
            throw new InvalidOperationException("WebSocket is not connected");

        try
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            var combinedToken = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token)
                .Token;
                
            await _webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                combinedToken);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
    }

    public async Task<string> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        if (_webSocket.State != WebSocketState.Open)
            throw new InvalidOperationException("WebSocket is not connected");

        var buffer = new byte[8192];
        var result = new StringBuilder();
        
        try
        {
            var combinedToken = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token)
                .Token;

            WebSocketReceiveResult receiveResult;
            do
            {
                var segment = new ArraySegment<byte>(buffer);
                receiveResult = await _webSocket.ReceiveAsync(segment, combinedToken);
                
                if (receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    result.Append(Encoding.UTF8.GetString(buffer, 0, receiveResult.Count));
                }
            } while (!receiveResult.EndOfMessage);

            return result.ToString();
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
    }

    public async Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure,
                                string? statusDescription = null,
                                CancellationToken cancellationToken = default)
    {
        try
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
        }
        finally
        {
            _cancellationTokenSource.Cancel();
        }
    }

    private async Task ReceiveLoop(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _webSocket.State == WebSocketState.Open)
            {
                var message = await ReceiveAsync(cancellationToken);
                MessageReceived?.Invoke(this, message);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
        }
        finally
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _webSocket.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Factory for creating WebSocket connections
/// </summary>
public class WebSocketFactory : IWebSocketFactory
{
    public IWebSocketConnection Create()
    {
        return new WebSocketConnection();
    }
}