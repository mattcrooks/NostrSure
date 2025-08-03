using NostrSure.Domain.Entities;
using NostrSure.Domain.Services;
using NostrSure.Domain.ValueObjects;

namespace NostrSure.Tests.Services;

[TestClass]
public class SimpleEventIdCalculatorTests
{
    private SimpleEventIdCalculator _calculator = null!;

    [TestInitialize]
    public void Setup()
    {
        _calculator = new SimpleEventIdCalculator();
    }

    [TestMethod]
    public void CalculateEventId_ValidEvent_ReturnsHexString()
    {
        // Arrange
        var evt = new NostrEvent(
            "eventid123",
            new Pubkey("abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234"),
            DateTimeOffset.UnixEpoch.AddSeconds(1000),
            EventKind.Note,
            new List<NostrTag>
            {
                new NostrTag("p", new[] { "testvalue" }),
                new NostrTag("e", new[] { "eventref", "relay" })
            },
            "Hello, Nostr!",
            "signature"
        );

        // Act
        var result = _calculator.CalculateEventId(evt);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(64, result.Length); // SHA256 hash should be 64 characters when hex encoded
        Assert.IsTrue(result.All(c => "0123456789abcdef".Contains(c))); // Should be lowercase hex
    }

    [TestMethod]
    public void CalculateEventId_EventWithEmptyTags_ReturnsHexString()
    {
        // Arrange
        var evt = new NostrEvent(
            "eventid123",
            new Pubkey("abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234"),
            DateTimeOffset.UnixEpoch.AddSeconds(1000),
            EventKind.Note,
            new List<NostrTag>(),
            "Hello, Nostr!",
            "signature"
        );

        // Act
        var result = _calculator.CalculateEventId(evt);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(64, result.Length);
        Assert.IsTrue(result.All(c => "0123456789abcdef".Contains(c)));
    }

    [TestMethod]
    public void CalculateEventId_SameEvent_ReturnsSameHash()
    {
        // Arrange
        var evt1 = new NostrEvent(
            "eventid123",
            new Pubkey("abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234"),
            DateTimeOffset.UnixEpoch.AddSeconds(1000),
            EventKind.Note,
            new List<NostrTag> { new NostrTag("p", new[] { "testvalue" }) },
            "Hello, Nostr!",
            "signature"
        );

        var evt2 = new NostrEvent(
            "eventid123",
            new Pubkey("abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234"),
            DateTimeOffset.UnixEpoch.AddSeconds(1000),
            EventKind.Note,
            new List<NostrTag> { new NostrTag("p", new[] { "testvalue" }) },
            "Hello, Nostr!",
            "signature"
        );

        // Act
        var result1 = _calculator.CalculateEventId(evt1);
        var result2 = _calculator.CalculateEventId(evt2);

        // Assert
        Assert.AreEqual(result1, result2);
    }
}