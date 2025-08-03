using NostrSure.Infrastructure.Client.Abstractions;
using NostrSure.Infrastructure.Client.Messages;

namespace NostrSure.Infrastructure.Client.Implementation;

/// <summary>
/// Default event dispatcher implementation
/// </summary>
public class DefaultEventDispatcher : IEventDispatcher
{
    public event Func<RelayEventMessage, Task>? OnEvent;
    public event Func<EoseMessage, Task>? OnEndOfStoredEvents;
    public event Func<NoticeMessage, Task>? OnNotice;
    public event Func<ClosedMessage, Task>? OnClosed;
    public event Func<OkMessage, Task>? OnOk;

    public void Dispatch(NostrMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            switch (message)
            {
                case RelayEventMessage eventMsg:
                    OnEvent?.Invoke(eventMsg);
                    break;
                case EoseMessage eoseMsg:
                    OnEndOfStoredEvents?.Invoke(eoseMsg);
                    break;
                case NoticeMessage noticeMsg:
                    OnNotice?.Invoke(noticeMsg);
                    break;
                case ClosedMessage closedMsg:
                    OnClosed?.Invoke(closedMsg);
                    break;
                case OkMessage okMsg:
                    OnOk?.Invoke(okMsg);
                    break;
                default:
                    // Unknown message type - could log but don't throw
                    break;
            }
        }
        catch (Exception)
        {
            // Prevent handler exceptions from breaking the dispatcher
            // In production, this should be logged
            throw;
        }
    }
}