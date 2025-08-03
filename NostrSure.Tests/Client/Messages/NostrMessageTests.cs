using NostrSure.Domain.Entities;
using NostrSure.Infrastructure.Client.Messages;
using NostrSure.Domain.ValueObjects;

namespace NostrSure.Tests.Client.Messages;

[TestClass]
public class NostrMessageTests
{
    [TestMethod]
    public void ReqMessage_Constructor_SetsProperties()
    {
        // Arrange
        const string subscriptionId = "test-sub";
        var filter = new Dictionary<string, object> { ["since"] = 1234567890 };

        // Act
        var message = new ReqMessage(subscriptionId, filter);

        // Assert
        Assert.AreEqual("REQ", message.Type);
        Assert.AreEqual(subscriptionId, message.SubscriptionId);
        Assert.AreEqual(filter, message.Filter);
    }

    [TestMethod]
    public void CloseMessage_Constructor_SetsProperties()
    {
        // Arrange
        const string subscriptionId = "test-sub";

        // Act
        var message = new CloseMessage(subscriptionId);

        // Assert
        Assert.AreEqual("CLOSE", message.Type);
        Assert.AreEqual(subscriptionId, message.SubscriptionId);
    }

    [TestMethod]
    public void EventMessage_Constructor_SetsProperties()
    {
        // Arrange
        var nostrEvent = new NostrEvent(
            "id123",
            new Pubkey("abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234"),
            DateTimeOffset.UtcNow,
            EventKind.Note,
            new List<NostrTag>(),
            "test content",
            "signature"
        );

        // Act
        var message = new EventMessage(nostrEvent);

        // Assert
        Assert.AreEqual("EVENT", message.Type);
        Assert.AreEqual(nostrEvent, message.Event);
    }

    [TestMethod]
    public void RelayEventMessage_Constructor_SetsProperties()
    {
        // Arrange
        const string subscriptionId = "test-sub";
        var nostrEvent = new NostrEvent(
            "id123",
            new Pubkey("abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234"),
            DateTimeOffset.UtcNow,
            EventKind.Note,
            new List<NostrTag>(),
            "test content",
            "signature"
        );

        // Act
        var message = new RelayEventMessage(subscriptionId, nostrEvent);

        // Assert
        Assert.AreEqual("EVENT", message.Type);
        Assert.AreEqual(subscriptionId, message.SubscriptionId);
        Assert.AreEqual(nostrEvent, message.Event);
    }

    [TestMethod]
    public void EoseMessage_Constructor_SetsProperties()
    {
        // Arrange
        const string subscriptionId = "test-sub";

        // Act
        var message = new EoseMessage(subscriptionId);

        // Assert
        Assert.AreEqual("EOSE", message.Type);
        Assert.AreEqual(subscriptionId, message.SubscriptionId);
    }

    [TestMethod]
    public void NoticeMessage_Constructor_SetsProperties()
    {
        // Arrange
        const string messageText = "Test notice";

        // Act
        var message = new NoticeMessage(messageText);

        // Assert
        Assert.AreEqual("NOTICE", message.Type);
        Assert.AreEqual(messageText, message.Message);
    }

    [TestMethod]
    public void ClosedMessage_Constructor_SetsProperties()
    {
        // Arrange
        const string subscriptionId = "test-sub";
        const string messageText = "Test closed message";

        // Act
        var message = new ClosedMessage(subscriptionId, messageText);

        // Assert
        Assert.AreEqual("CLOSED", message.Type);
        Assert.AreEqual(subscriptionId, message.SubscriptionId);
        Assert.AreEqual(messageText, message.Message);
    }

    [TestMethod]
    public void OkMessage_Constructor_SetsProperties()
    {
        // Arrange
        const string eventId = "event123";
        const bool accepted = true;
        const string messageText = "Event accepted";

        // Act
        var message = new OkMessage(eventId, accepted, messageText);

        // Assert
        Assert.AreEqual("OK", message.Type);
        Assert.AreEqual(eventId, message.EventId);
        Assert.AreEqual(accepted, message.Accepted);
        Assert.AreEqual(messageText, message.Message);
    }
}