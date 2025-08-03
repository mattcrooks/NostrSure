namespace NostrSure.Infrastructure.Client.Abstractions;

/// <summary>
/// Handles WebSocket message transmission
/// </summary>
public interface IMessageSender
{
    /// <summary>
    /// Sends a message through the WebSocket
    /// </summary>
    /// <param name="message">Message to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the send operation</returns>
    Task SendAsync(string message, CancellationToken cancellationToken = default);
}