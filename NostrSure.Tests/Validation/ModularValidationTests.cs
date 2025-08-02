using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NostrSure.Domain.Entities;
using NostrSure.Domain.Extensions;
using NostrSure.Domain.Interfaces;
using NostrSure.Domain.Services;
using NostrSure.Domain.Validation;
using NostrSure.Domain.ValueObjects;
using NostrSure.Infrastructure.Serialization;
using System;
using System.Collections.Generic;

namespace NostrSure.Tests.Validation
{
    [TestCategory("Validation")]
    [TestClass]
    public class ModularValidationTests
    {
        private ServiceProvider _serviceProvider;
        private INostrEventValidator _validator;

        [TestInitialize]
        public void Setup()
        {
            var services = new ServiceCollection();
            //services.AddLogging(builder => builder.AddConsole());
            services.AddNostrValidation();

            _serviceProvider = services.BuildServiceProvider();
            _validator = _serviceProvider.GetRequiredService<INostrEventValidator>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _serviceProvider?.Dispose();
        }

        [TestMethod]
        public async Task ModularValidator_ValidEvent_ReturnsSuccess()
        {
            // Arrange
            var validEvent = CreateValidEvent();

            // Act
            var result = await _validator.ValidateAsync(validEvent);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.IsNull(result.Error);
        }

        [TestMethod]
        public async Task ModularValidator_InvalidSignature_ReturnsFailure()
        {
            // Arrange
            var invalidEvent = CreateValidEvent() with { Sig = "invalid_signature" };

            // Act
            var result = await _validator.ValidateAsync(invalidEvent);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual(ValidationSeverity.Error, result.Severity);
            Assert.IsTrue(result.Error.Message.Contains("signature"));
        }

        [TestMethod]
        public async Task ModularValidator_InvalidEventKind_ReturnsFailure()
        {
            // Arrange
            var invalidEvent = CreateValidEvent() with { Kind = (EventKind)9999 };

            // Act
            var result = await _validator.ValidateAsync(invalidEvent);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual("UNKNOWN_EVENT_KIND", result.Error.Code);
        }

        [TestMethod]
        public async Task ModularValidator_InvalidTags_ReturnsFailure()
        {
            // Arrange
            var invalidPTag = new NostrTag("p", new[] { "invalid_hex" });
            var invalidEvent = CreateValidEvent() with { Tags = new List<NostrTag> { invalidPTag } };

            // Act
            var result = await _validator.ValidateAsync(invalidEvent);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsNotNull(result.Error);
            Assert.AreEqual("INVALID_TAG_FORMAT", result.Error.Code);
        }

        [TestMethod]
        public void ModularValidator_BackwardCompatibility_Works()
        {
            // Arrange
            var validEvent = CreateValidEvent();

            // Act - test legacy methods still work
            var signatureValid = _validator.ValidateSignature(validEvent, out var sigError);
            var kindValid = _validator.ValidateKind(validEvent, out var kindError);
            var tagsValid = _validator.ValidateTags(validEvent, out var tagError);
            var eventIdValid = _validator.ValidateEventId(validEvent, out var idError);

            // Assert
            Assert.IsTrue(signatureValid, $"Signature validation failed: {sigError}");
            Assert.IsTrue(kindValid, $"Kind validation failed: {kindError}");
            Assert.IsTrue(tagsValid, $"Tags validation failed: {tagError}");
            Assert.IsTrue(eventIdValid, $"Event ID validation failed: {idError}");
        }

        [TestMethod]
        public async Task ModularValidator_PerformanceTest_HandlesManyConcurrentValidations()
        {
            // Arrange
            var events = new List<NostrEvent>();
            for (int i = 0; i < 100; i++)
            {
                events.Add(CreateValidEvent());
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - validate all events concurrently
            var tasks = events.Select(e => _validator.ValidateAsync(e));
            var results = await Task.WhenAll(tasks);

            stopwatch.Stop();

            // Assert
            Assert.IsTrue(results.All(r => r.IsValid), "All events should be valid");
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, "Should complete in under 1 second");
        }

        [TestMethod]
        public void IndividualValidators_CanBeUsedSeparately()
        {
            // Arrange
            var signatureValidator = _serviceProvider.GetRequiredService<IEventSignatureValidator>();
            var eventIdValidator = _serviceProvider.GetRequiredService<IEventIdValidator>();
            var kindValidator = _serviceProvider.GetRequiredService<IEventKindValidator>();
            var tagValidator = _serviceProvider.GetRequiredService<IEventTagValidator>();
            
            var validEvent = CreateValidEvent();

            // Act
            var sigResult = signatureValidator.ValidateSignature(validEvent);
            var idResult = eventIdValidator.ValidateEventId(validEvent);
            var kindResult = kindValidator.ValidateKind(validEvent);
            var tagResult = tagValidator.ValidateTags(validEvent);

            // Assert
            Assert.IsTrue(sigResult.IsValid);
            Assert.IsTrue(idResult.IsValid);
            Assert.IsTrue(kindResult.IsValid);
            Assert.IsTrue(tagResult.IsValid);
        }

        [TestMethod]
        public void CachedEventIdCalculator_ImprovesPerfomance()
        {
            // Arrange
            var calculator = _serviceProvider.GetRequiredService<IEventIdCalculator>();
            var validEvent = CreateValidEvent();

            // Act - calculate twice to test caching
            var stopwatch1 = System.Diagnostics.Stopwatch.StartNew();
            var id1 = calculator.CalculateEventId(validEvent);
            stopwatch1.Stop();

            var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();
            var id2 = calculator.CalculateEventId(validEvent);
            stopwatch2.Stop();

            // Assert
            Assert.AreEqual(id1, id2);
            Assert.IsTrue(stopwatch2.ElapsedTicks < stopwatch1.ElapsedTicks, 
                "Second calculation should be faster due to caching");
        }

        [TestMethod]
        public void OptimizedHexConverter_HandlesDifferentFormats()
        {
            // Arrange
            var hexConverter = _serviceProvider.GetRequiredService<IHexConverter>();

            // Act & Assert
            var bytes1 = hexConverter.ParseHex("deadbeef");
            var bytes2 = hexConverter.ParseHex("0xdeadbeef");
            var bytes3 = hexConverter.ParseHex("DEADBEEF");

            Assert.IsTrue(bytes1.SequenceEqual(bytes2));
            Assert.IsTrue(bytes1.SequenceEqual(bytes3));
            Assert.AreEqual(4, bytes1.Length);
        }

        private static NostrEvent CreateValidEvent()
        {
            return new NostrEvent(
                "db7e784617a8caa09433cb0ec2250deb3ab20b59adaae1f8fe0f574243df015a",
                new Pubkey("aa4fc8665f5696e33db7e1a572e3b0f5b3d615837b0f362dcb1c8068b098c7b4"),
                DateTimeOffset.FromUnixTimeSeconds(1753958704),
                EventKind.Note,
                new List<NostrTag>
                {
               
                },
                "Bitcoin price: $118451, Sats per USD: 844",
                "177d555723178c1e6ec5dff8a9fd252b6b0768c085860df1e2a1ea881fc23734d2c846e061be31090db892cd1525ec2f976280a6ac90b1c02427f4e3db048db4"
            );
        }


    }
}