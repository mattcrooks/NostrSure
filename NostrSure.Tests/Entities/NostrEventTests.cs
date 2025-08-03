using NostrSure.Domain.Entities;
using NostrSure.Domain.Interfaces;
using NostrSure.Domain.ValueObjects;
using NostrSure.Domain.Validation;

namespace NostrSure.Tests.Entities;

[TestClass]
public class NostrEventTests
{
    [TestMethod]
    public void Constructor_ValidArguments_CreatesEvent()
    {
        // Arrange
        const string id = "eventid123";
        var pubkey = new Pubkey("abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234");
        var createdAt = DateTimeOffset.UtcNow;
        var kind = EventKind.Note;
        var tags = new List<NostrTag> { new NostrTag("p", new[] { "testvalue" }) };
        const string content = "test content";
        const string sig = "signature";

        // Act
        var nostrEvent = new NostrEvent(id, pubkey, createdAt, kind, tags, content, sig);

        // Assert
        Assert.AreEqual(id, nostrEvent.Id);
        Assert.AreEqual(pubkey, nostrEvent.Pubkey);
        Assert.AreEqual(createdAt, nostrEvent.CreatedAt);
        Assert.AreEqual(kind, nostrEvent.Kind);
        Assert.AreEqual(tags, nostrEvent.Tags);
        Assert.AreEqual(content, nostrEvent.Content);
        Assert.AreEqual(sig, nostrEvent.Sig);
    }

    [TestMethod]
    public void Validate_AllValidationsPassing_ReturnsTrue()
    {
        // Arrange
        var mockValidator = new MockValidator(true, true, true, true);
        var nostrEvent = CreateTestEvent();

        // Act
        var result = nostrEvent.Validate(mockValidator, out var errors);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void Validate_EventIdValidationFailing_ReturnsFalse()
    {
        // Arrange
        var mockValidator = new MockValidator(false, true, true, true) 
        { 
            IdError = "Invalid event ID" 
        };
        var nostrEvent = CreateTestEvent();

        // Act
        var result = nostrEvent.Validate(mockValidator, out var errors);

        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual("Event ID invalid: Invalid event ID", errors[0]);
    }

    [TestMethod]
    public void Validate_SignatureValidationFailing_ReturnsFalse()
    {
        // Arrange
        var mockValidator = new MockValidator(true, false, true, true) 
        { 
            SignatureError = "Invalid signature" 
        };
        var nostrEvent = CreateTestEvent();

        // Act
        var result = nostrEvent.Validate(mockValidator, out var errors);

        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual("Signature invalid: Invalid signature", errors[0]);
    }

    [TestMethod]
    public void Validate_KindValidationFailing_ReturnsFalse()
    {
        // Arrange
        var mockValidator = new MockValidator(true, true, false, true) 
        { 
            KindError = "Invalid kind" 
        };
        var nostrEvent = CreateTestEvent();

        // Act
        var result = nostrEvent.Validate(mockValidator, out var errors);

        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual("Kind invalid: Invalid kind", errors[0]);
    }

    [TestMethod]
    public void Validate_TagsValidationFailing_ReturnsFalse()
    {
        // Arrange
        var mockValidator = new MockValidator(true, true, true, false) 
        { 
            TagsError = "Invalid tags" 
        };
        var nostrEvent = CreateTestEvent();

        // Act
        var result = nostrEvent.Validate(mockValidator, out var errors);

        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual("Tags invalid: Invalid tags", errors[0]);
    }

    [TestMethod]
    public void Validate_MultipleValidationsFailing_ReturnsAllErrors()
    {
        // Arrange
        var mockValidator = new MockValidator(false, false, false, false) 
        { 
            IdError = "Bad ID",
            SignatureError = "Bad Signature",
            KindError = "Bad Kind",
            TagsError = "Bad Tags"
        };
        var nostrEvent = CreateTestEvent();

        // Act
        var result = nostrEvent.Validate(mockValidator, out var errors);

        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual(4, errors.Count);
        Assert.AreEqual("Event ID invalid: Bad ID", errors[0]);
        Assert.AreEqual("Signature invalid: Bad Signature", errors[1]);
        Assert.AreEqual("Kind invalid: Bad Kind", errors[2]);
        Assert.AreEqual("Tags invalid: Bad Tags", errors[3]);
    }

    private static NostrEvent CreateTestEvent()
    {
        return new NostrEvent(
            "eventid123",
            new Pubkey("abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234abcd1234"),
            DateTimeOffset.UtcNow,
            EventKind.Note,
            new List<NostrTag>(),
            "test content",
            "signature"
        );
    }

    private class MockValidator : INostrEventValidator
    {
        private readonly bool _eventIdValid;
        private readonly bool _signatureValid;
        private readonly bool _kindValid;
        private readonly bool _tagsValid;

        public string? IdError { get; set; }
        public string? SignatureError { get; set; }
        public string? KindError { get; set; }
        public string? TagsError { get; set; }

        public MockValidator(bool eventIdValid, bool signatureValid, bool kindValid, bool tagsValid)
        {
            _eventIdValid = eventIdValid;
            _signatureValid = signatureValid;
            _kindValid = kindValid;
            _tagsValid = tagsValid;
        }

        public Task<ValidationResult> ValidateAsync(NostrEvent evt, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Validate(evt));
        }

        public ValidationResult Validate(NostrEvent evt)
        {
            if (!_eventIdValid) return ValidationResult.Failure(IdError ?? "Event ID validation failed");
            if (!_signatureValid) return ValidationResult.Failure(SignatureError ?? "Signature validation failed");
            if (!_kindValid) return ValidationResult.Failure(KindError ?? "Kind validation failed");
            if (!_tagsValid) return ValidationResult.Failure(TagsError ?? "Tags validation failed");
            return ValidationResult.Success();
        }

        public bool ValidateEventId(NostrEvent evt, out string error)
        {
            error = _eventIdValid ? "" : IdError ?? "";
            return _eventIdValid;
        }

        public bool ValidateSignature(NostrEvent evt, out string error)
        {
            error = _signatureValid ? "" : SignatureError ?? "";
            return _signatureValid;
        }

        public bool ValidateKind(NostrEvent evt, out string error)
        {
            error = _kindValid ? "" : KindError ?? "";
            return _kindValid;
        }

        public bool ValidateTags(NostrEvent evt, out string error)
        {
            error = _tagsValid ? "" : TagsError ?? "";
            return _tagsValid;
        }
    }
}