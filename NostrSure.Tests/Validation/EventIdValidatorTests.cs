using Microsoft.VisualStudio.TestTools.UnitTesting;
using NostrSure.Domain.Entities;
using NostrSure.Domain.Services;
using NostrSure.Domain.Validation;
using NostrSure.Domain.ValueObjects;
using System.Threading;
using System.Threading.Tasks;

namespace NostrSure.Tests.Validation
{
    [TestClass]
    [TestCategory("Validation - EventId")]
    public class EventIdValidatorTests
    {
        private class FakeCalculator : IEventIdCalculator
        {
            private readonly string _idToReturn;
            private readonly bool _throw;

            public FakeCalculator(string idToReturn, bool throwException = false)
            {
                _idToReturn = idToReturn;
                _throw = throwException;
            }

            public string CalculateEventId(NostrEvent evt)
            {
                if (_throw)
                    throw new System.InvalidOperationException("Calculation failed!");
                return _idToReturn;
            }
        }

        private static NostrEvent CreateEvent(string id = "abc123")
        {
            return new NostrEvent(
                id,
                new Pubkey("pubkey"),
                System.DateTimeOffset.UtcNow,
                EventKind.Note,
                new List<NostrTag>(),
                "content",
                "sig"
            );
        }

        [TestMethod]
        public void ValidateEventId_ReturnsSuccess_WhenIdMatches()
        {
            var evt = CreateEvent("goodid");
            var validator = new EventIdValidator(new FakeCalculator("goodid"));
            var result = validator.ValidateEventId(evt);

            Assert.IsTrue(result.IsValid);
            Assert.IsNull(result.Error);
        }

        [TestMethod]
        public void ValidateEventId_ReturnsFailure_WhenIdDoesNotMatch()
        {
            var evt = CreateEvent("badid");
            var validator = new EventIdValidator(new FakeCalculator("expectedid"));
            var result = validator.ValidateEventId(evt);

            Assert.IsFalse(result.IsValid);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual("EVENT_ID_MISMATCH", result.Error.Code);
            Assert.IsTrue(result.Error.Message.Contains("Expected: expectedid"));
        }

        [TestMethod]
        public void ValidateEventId_ReturnsCriticalFailure_OnException()
        {
            var evt = CreateEvent("anyid");
            var validator = new EventIdValidator(new FakeCalculator("ignored", true));
            var result = validator.ValidateEventId(evt);

            Assert.IsFalse(result.IsValid);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(ValidationSeverity.Critical, result.Severity);
            Assert.IsTrue(result.Error.Message.Contains("Exception during event ID validation"));
        }

        [TestMethod]
        public async Task ValidateEventIdAsync_WrapsSyncMethod()
        {
            var evt = CreateEvent("asyncid");
            var validator = new EventIdValidator(new FakeCalculator("asyncid"));
            var result = await validator.ValidateEventIdAsync(evt, CancellationToken.None);

            Assert.IsTrue(result.IsValid);
            Assert.IsNull(result.Error);
        }
    }
}