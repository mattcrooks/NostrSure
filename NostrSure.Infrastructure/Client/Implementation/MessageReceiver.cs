using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using NostrSure.Infrastructure.Client.Abstractions;
using System.Buffers;
using System.Net.WebSockets;
using System.Text;

namespace NostrSure.Infrastructure.Client.Implementation;

/// <summary>
/// Handles WebSocket message reception with background polling capabilities and object pooling for performance
/// </summary>
public sealed class MessageReceiver : IMessageReceiver
{
    private static readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Shared;
    private readonly ObjectPool<StringBuilder> _stringBuilderPool;

    private readonly ClientWebSocket _webSocket;
    private readonly IConnectionErrorHandler _errorHandler;
    private readonly IConnectionStateManager _stateManager;
    private readonly CancellationTokenSource _receiveCancellation;
    private readonly ILogger<MessageReceiver>? _logger;
    private Task? _receiveTask;
    private bool _disposed;

    public MessageReceiver(
        ClientWebSocket webSocket,
        IConnectionErrorHandler errorHandler,
        IConnectionStateManager stateManager,
        ObjectPool<StringBuilder> stringBuilderPool,
        ILogger<MessageReceiver>? logger = null)
    {
        _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _stringBuilderPool = stringBuilderPool ?? throw new ArgumentNullException(nameof(stringBuilderPool));
        _receiveCancellation = new CancellationTokenSource();
        _logger = logger;
    }

    public event EventHandler<string>? MessageReceived;

    public async Task<string> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        if (_webSocket.State != WebSocketState.Open)
            throw new InvalidOperationException("WebSocket is not connected");

        var buffer = BufferPool.Rent(8192);
        var stringBuilder = _stringBuilderPool.Get();

        try
        {
            var combinedToken = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken, _receiveCancellation.Token)
                .Token;

            WebSocketReceiveResult receiveResult;
            do
            {
                var segment = new ArraySegment<byte>(buffer);
                receiveResult = await _webSocket.ReceiveAsync(segment, combinedToken);

                if (receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    stringBuilder.Append(Encoding.UTF8.GetString(buffer, 0, receiveResult.Count));
                }
                else if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    _logger?.LogInformation("WebSocket connection closed by remote endpoint");
                    _stateManager.UpdateState(WebSocketState.Closed);
                    throw new WebSocketException("Connection closed by remote endpoint");
                }
            } while (!receiveResult.EndOfMessage && !combinedToken.IsCancellationRequested);

            var message = stringBuilder.ToString();
            _logger?.LogDebug("Received message: {MessageLength} characters", message.Length);
            return message;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error receiving WebSocket message");
            await _errorHandler.HandleErrorAsync(ex, nameof(ReceiveAsync));
            throw;
        }
        finally
        {
            BufferPool.Return(buffer);
            _stringBuilderPool.Return(stringBuilder);
        }
    }

    public async Task StartReceivingAsync(CancellationToken cancellationToken = default)
    {
        if (_receiveTask != null && !_receiveTask.IsCompleted)
        {
            _logger?.LogWarning("Message receiving is already active");
            return;
        }

        _logger?.LogDebug("Starting background message receiving");
        _receiveTask = ReceiveLoopAsync(_receiveCancellation.Token);
        await Task.CompletedTask;
    }

    public async Task StopReceivingAsync()
    {
        _logger?.LogDebug("Stopping background message receiving");
        _receiveCancellation.Cancel();

        if (_receiveTask != null)
        {
            try
            {
                await _receiveTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error occurred while stopping message receiving");
            }
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogDebug("Message receive loop started");

        try
        {
            while (!cancellationToken.IsCancellationRequested &&
                   _webSocket.State == WebSocketState.Open)
            {
                var message = await ReceiveAsync(cancellationToken);
                MessageReceived?.Invoke(this, message);
            }
        }
        catch (OperationCanceledException)
        {
            _logger?.LogDebug("Message receive loop cancelled");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in message receive loop");
            await _errorHandler.HandleErrorAsync(ex, nameof(ReceiveLoopAsync));
        }
        finally
        {
            _logger?.LogDebug("Message receive loop ended");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _logger?.LogDebug("Disposing MessageReceiver");
            _receiveCancellation.Cancel();
            _receiveCancellation.Dispose();
            _disposed = true;
        }
    }
}