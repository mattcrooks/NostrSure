namespace NostrSure.Infrastructure.Client.Abstractions;

/// <summary>
/// Centralized error handling for WebSocket connections
/// </summary>
public interface IConnectionErrorHandler
{
    /// <summary>
    /// Event raised when an error occurs
    /// </summary>
    event EventHandler<Exception>? ErrorOccurred;

    /// <summary>
    /// Handles an error that occurred during WebSocket operations
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="context">Context where the error occurred</param>
    /// <returns>Task representing the error handling operation</returns>
    Task HandleErrorAsync(Exception exception, string context);

    /// <summary>
    /// Determines if a connection should be retried based on the exception
    /// </summary>
    /// <param name="exception">The exception to evaluate</param>
    /// <returns>True if the connection should be retried, false otherwise</returns>
    bool ShouldReconnect(Exception exception);
}