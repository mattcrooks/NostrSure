using NostrSure.Domain.Entities;

namespace NostrSure.Tests.Entities;

[TestClass]
public class RelayTests
{
    [TestMethod]
    public void Constructor_ValidEndpoint_CreatesRelay()
    {
        // Arrange
        const string endpoint = "wss://relay.damus.io";

        // Act
        var relay = new Relay(endpoint);

        // Assert
        Assert.AreEqual(new Uri(endpoint), relay.Endpoint);
    }

    [TestMethod]
    public void Constructor_ValidHttpsEndpoint_CreatesRelay()
    {
        // Arrange
        const string endpoint = "https://relay.example.com";

        // Act
        var relay = new Relay(endpoint);

        // Assert
        Assert.AreEqual(new Uri(endpoint), relay.Endpoint);
    }

    [TestMethod]
    public void Constructor_InvalidEndpoint_ThrowsArgumentException()
    {
        // Arrange
        const string invalidEndpoint = "not-a-valid-uri";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new Relay(invalidEndpoint));
    }

    [TestMethod]
    public void Constructor_RelativeEndpoint_ThrowsArgumentException()
    {
        // Arrange
        const string relativeEndpoint = "/relative/path";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new Relay(relativeEndpoint));
    }

    [TestMethod]
    public void Constructor_EmptyEndpoint_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new Relay(""));
    }
}