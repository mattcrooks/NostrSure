using NostrSure.Domain.Entities;
using NostrSure.Domain.Services;
using NostrSure.Domain.ValueObjects;

namespace NostrSure.Tests.Validation;

[TestClass]
public class EventTagValidatorTests
{
    private EventTagValidator _validator = null!;

    [TestInitialize]
    public void Setup()
    {
        _validator = new EventTagValidator();
    }

    [TestMethod]
    public void ValidateTags_ValidTags_ReturnsSuccess()
    {
        // Arrange
        var validTags = new List<NostrTag>
        {
            new NostrTag("p", new[] { "abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234" }),
            new NostrTag("e", new[] { "efgh5678efgh5678efgh5678efgh5678efgh5678efgh5678efgh5678efgh5678" })
        };
        var evt = CreateEvent(validTags);

        // Act
        var result = _validator.ValidateTags(evt);

        // Assert
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateTags_NullTags_ReturnsFailure()
    {
        // Arrange
        var evt = CreateEventWithNullTags();

        // Act
        var result = _validator.ValidateTags(evt);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("Tags are null", result.Error?.Message);
        Assert.AreEqual("NULL_TAGS", result.Error?.Code);
    }

    [TestMethod]
    public void ValidateTags_EmptyTagsList_ReturnsSuccess()
    {
        // Arrange
        var emptyTags = new List<NostrTag>();
        var evt = CreateEvent(emptyTags);

        // Act
        var result = _validator.ValidateTags(evt);

        // Assert
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateTags_InvalidTag_ReturnsFailure()
    {
        // Arrange - Create a tag that will fail IsValid() check (p tag with invalid hex length)
        var invalidTags = new List<NostrTag>
        {
            new NostrTag("p", new[] { "invalidhex" }) // Too short for p tag validation
        };
        var evt = CreateEvent(invalidTags);

        // Act
        var result = _validator.ValidateTags(evt);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("Invalid tag: p", result.Error?.Message);
        Assert.AreEqual("INVALID_TAG_FORMAT", result.Error?.Code);
    }

    private static NostrEvent CreateEvent(IReadOnlyList<NostrTag> tags)
    {
        return new NostrEvent(
            "eventid123",
            new Pubkey("abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234"),
            DateTimeOffset.UtcNow,
            EventKind.Note,
            tags,
            "test content",
            "signature"
        );
    }

    private static NostrEvent CreateEventWithNullTags()
    {
        return new NostrEvent(
            "eventid123",
            new Pubkey("abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234"),
            DateTimeOffset.UtcNow,
            EventKind.Note,
            null!,
            "test content",
            "signature"
        );
    }
}