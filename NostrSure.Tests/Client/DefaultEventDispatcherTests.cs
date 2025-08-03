using NostrSure.Infrastructure.Client.Implementation;
using NostrSure.Infrastructure.Client.Messages;

namespace NostrSure.Tests.Client;

[TestCategory("Client")]
[TestClass]
public class DefaultEventDispatcherTests
{
    private DefaultEventDispatcher _dispatcher = null!;

    [TestInitialize]
    public void Setup()
    {
        _dispatcher = new DefaultEventDispatcher();
    }

    [TestMethod]
    public void Dispatch_EoseMessage_TriggersOnEndOfStoredEvents()
    {
        // Arrange
        var called = false;
        var receivedMessage = (EoseMessage?)null;

        _dispatcher.OnEndOfStoredEvents += msg =>
        {
            called = true;
            receivedMessage = msg;
            return Task.CompletedTask;
        };

        var eoseMessage = new EoseMessage("sub1");

        // Act
        _dispatcher.Dispatch(eoseMessage);

        // Assert
        Assert.IsTrue(called);
        Assert.IsNotNull(receivedMessage);
        Assert.AreEqual("sub1", receivedMessage.SubscriptionId);
    }

    [TestMethod]
    public void Dispatch_NoticeMessage_TriggersOnNotice()
    {
        // Arrange
        var called = false;
        var receivedMessage = (NoticeMessage?)null;

        _dispatcher.OnNotice += msg =>
        {
            called = true;
            receivedMessage = msg;
            return Task.CompletedTask;
        };

        var noticeMessage = new NoticeMessage("test warning");

        // Act
        _dispatcher.Dispatch(noticeMessage);

        // Assert
        Assert.IsTrue(called);
        Assert.IsNotNull(receivedMessage);
        Assert.AreEqual("test warning", receivedMessage.Message);
    }

    [TestMethod]
    public void Dispatch_ClosedMessage_TriggersOnClosed()
    {
        // Arrange
        var called = false;
        var receivedMessage = (ClosedMessage?)null;

        _dispatcher.OnClosed += msg =>
        {
            called = true;
            receivedMessage = msg;
            return Task.CompletedTask;
        };

        var closedMessage = new ClosedMessage("sub1", "connection closed");

        // Act
        _dispatcher.Dispatch(closedMessage);

        // Assert
        Assert.IsTrue(called);
        Assert.IsNotNull(receivedMessage);
        Assert.AreEqual("sub1", receivedMessage.SubscriptionId);
        Assert.AreEqual("connection closed", receivedMessage.Message);
    }

    [TestMethod]
    public void Dispatch_OkMessage_TriggersOnOk()
    {
        // Arrange
        var called = false;
        var receivedMessage = (OkMessage?)null;

        _dispatcher.OnOk += msg =>
        {
            called = true;
            receivedMessage = msg;
            return Task.CompletedTask;
        };

        var okMessage = new OkMessage("event123", true, "accepted");

        // Act
        _dispatcher.Dispatch(okMessage);

        // Assert
        Assert.IsTrue(called);
        Assert.IsNotNull(receivedMessage);
        Assert.AreEqual("event123", receivedMessage.EventId);
        Assert.IsTrue(receivedMessage.Accepted);
        Assert.AreEqual("accepted", receivedMessage.Message);
    }

    [TestMethod]
    public void Dispatch_MultipleHandlers_TriggersAllHandlers()
    {
        // Arrange
        var handler1Called = false;
        var handler2Called = false;

        _dispatcher.OnNotice += _ =>
        {
            handler1Called = true;
            return Task.CompletedTask;
        };

        _dispatcher.OnNotice += _ =>
        {
            handler2Called = true;
            return Task.CompletedTask;
        };

        var noticeMessage = new NoticeMessage("test");

        // Act
        _dispatcher.Dispatch(noticeMessage);

        // Assert
        Assert.IsTrue(handler1Called);
        Assert.IsTrue(handler2Called);
    }

    [TestMethod]
    public void Dispatch_NoHandlers_DoesNotThrow()
    {
        // Arrange
        var noticeMessage = new NoticeMessage("test");

        // Act & Assert (should not throw)
        _dispatcher.Dispatch(noticeMessage);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Dispatch_NullMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        _dispatcher.Dispatch(null!);
    }

    [TestMethod]
    public void Dispatch_UnknownMessageType_DoesNotThrow()
    {
        // Arrange
        var unknownMessage = new TestMessage("test");

        // Act & Assert (should not throw)
        _dispatcher.Dispatch(unknownMessage);
    }

    [TestMethod]
    public void Dispatch_HandlerThrowsException_PropagatesException()
    {
        // Arrange
        _dispatcher.OnNotice += _ => throw new InvalidOperationException("handler error");
        var noticeMessage = new NoticeMessage("test");

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => _dispatcher.Dispatch(noticeMessage));
    }

    private record TestMessage(string Type) : NostrMessage(Type);
}