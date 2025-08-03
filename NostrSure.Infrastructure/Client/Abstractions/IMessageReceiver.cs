namespace NostrSure.Infrastructure.Client.Abstractions;

/// <summary>
/// Handles WebSocket message reception with background polling capabilities
/// </summary>
public interface IMessageReceiver : IDisposable
{
    /// <summary>
    /// Receives a single message from the WebSocket
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The received message as a string</returns>
    Task<string> ReceiveAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Starts the background message receiving loop
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the start operation</returns>
    Task StartReceivingAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops the background message receiving loop
    /// </summary>
    /// <returns>Task representing the stop operation</returns>
    Task StopReceivingAsync();
    
    /// <summary>
    /// Event raised when a message is received via background polling
    /// </summary>
    event EventHandler<string>? MessageReceived;
}