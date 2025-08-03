using NostrSure.Infrastructure.Client.Implementation;
using NostrSure.Infrastructure.Client.Messages;

namespace NostrSure.Tests.Client;

[TestCategory("Client")]
[TestClass]
public class JsonMessageSerializerTests
{
    private JsonMessageSerializer _serializer = null!;

    [TestInitialize]
    public void Setup()
    {
        _serializer = new JsonMessageSerializer();
    }

    [TestMethod]
    public void Serialize_ReqMessage_ProducesValidJsonArray()
    {
        // Arrange
        var message = new object[] { "REQ", "sub1", new { kinds = new[] { 1 } } };

        // Act
        var json = _serializer.Serialize(message);

        // Assert
        Assert.IsNotNull(json);
        Assert.IsTrue(json.StartsWith("["));
        Assert.IsTrue(json.EndsWith("]"));
        Assert.IsTrue(json.Contains("REQ"));
        Assert.IsTrue(json.Contains("sub1"));
    }

    [TestMethod]
    public void Serialize_CloseMessage_ProducesValidJsonArray()
    {
        // Arrange
        var message = new object[] { "CLOSE", "sub1" };

        // Act
        var json = _serializer.Serialize(message);

        // Assert
        Assert.IsNotNull(json);
        Assert.IsTrue(json.StartsWith("["));
        Assert.IsTrue(json.EndsWith("]"));
        Assert.IsTrue(json.Contains("CLOSE"));
        Assert.IsTrue(json.Contains("sub1"));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Serialize_EmptyArray_ThrowsArgumentException()
    {
        // Arrange
        var message = new object[0];

        // Act & Assert
        _serializer.Serialize(message);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Serialize_NullArray_ThrowsArgumentException()
    {
        // Arrange
        object[] message = null!;

        // Act & Assert
        _serializer.Serialize(message);
    }

    [TestMethod]
    public void Deserialize_EoseMessage_ParsesCorrectly()
    {
        // Arrange
        var json = """["EOSE", "sub1"]""";

        // Act
        var result = _serializer.Deserialize(json);

        // Assert
        Assert.IsInstanceOfType<EoseMessage>(result);
        var eoseMsg = (EoseMessage)result;
        Assert.AreEqual("sub1", eoseMsg.SubscriptionId);
    }

    [TestMethod]
    public void Deserialize_NoticeMessage_ParsesCorrectly()
    {
        // Arrange
        var json = """["NOTICE", "test warning message"]""";

        // Act
        var result = _serializer.Deserialize(json);

        // Assert
        Assert.IsInstanceOfType<NoticeMessage>(result);
        var noticeMsg = (NoticeMessage)result;
        Assert.AreEqual("test warning message", noticeMsg.Message);
    }

    [TestMethod]
    public void Deserialize_ClosedMessage_ParsesCorrectly()
    {
        // Arrange
        var json = """["CLOSED", "sub1", "subscription ended"]""";

        // Act
        var result = _serializer.Deserialize(json);

        // Assert
        Assert.IsInstanceOfType<ClosedMessage>(result);
        var closedMsg = (ClosedMessage)result;
        Assert.AreEqual("sub1", closedMsg.SubscriptionId);
        Assert.AreEqual("subscription ended", closedMsg.Message);
    }

    [TestMethod]
    public void Deserialize_OkMessage_ParsesCorrectly()
    {
        // Arrange
        var json = """["OK", "event123", true, "accepted"]""";

        // Act
        var result = _serializer.Deserialize(json);

        // Assert
        Assert.IsInstanceOfType<OkMessage>(result);
        var okMsg = (OkMessage)result;
        Assert.AreEqual("event123", okMsg.EventId);
        Assert.IsTrue(okMsg.Accepted);
        Assert.AreEqual("accepted", okMsg.Message);
    }

    [TestMethod]
    public void Deserialize_OkMessageDenied_ParsesCorrectly()
    {
        // Arrange
        var json = """["OK", "event123", false, "spam"]""";

        // Act
        var result = _serializer.Deserialize(json);

        // Assert
        Assert.IsInstanceOfType<OkMessage>(result);
        var okMsg = (OkMessage)result;
        Assert.AreEqual("event123", okMsg.EventId);
        Assert.IsFalse(okMsg.Accepted);
        Assert.AreEqual("spam", okMsg.Message);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Deserialize_EmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        _serializer.Deserialize("");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Deserialize_NullString_ThrowsArgumentException()
    {
        // Act & Assert
        _serializer.Deserialize(null!);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Deserialize_NonArrayJson_ThrowsArgumentException()
    {
        // Arrange
        var json = """{"not": "an array"}""";

        // Act & Assert
        _serializer.Deserialize(json);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Deserialize_EmptyArray_ThrowsArgumentException()
    {
        // Arrange
        var json = """[]""";

        // Act & Assert
        _serializer.Deserialize(json);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Deserialize_UnknownMessageType_ThrowsArgumentException()
    {
        // Arrange
        var json = """["UNKNOWN", "param1"]""";

        // Act & Assert
        _serializer.Deserialize(json);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Deserialize_InvalidJson_ThrowsArgumentException()
    {
        // Arrange
        var json = """[invalid json""";

        // Act & Assert
        _serializer.Deserialize(json);
    }
}