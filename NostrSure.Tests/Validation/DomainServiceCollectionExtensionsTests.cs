using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NostrSure.Domain.Extensions;
using NostrSure.Domain.Interfaces;
using NostrSure.Domain.Validation;

namespace NostrSure.Tests.Validation;

[TestClass]
public class DomainServiceCollectionExtensionsTests
{
    [TestMethod]
    public void AddNostrValidation_RegistersAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNostrValidation();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.IsNotNull(serviceProvider.GetService<INostrEventValidator>());
        Assert.IsNotNull(serviceProvider.GetService<IEventSignatureValidator>());
        Assert.IsNotNull(serviceProvider.GetService<IEventIdValidator>());
        Assert.IsNotNull(serviceProvider.GetService<IEventKindValidator>());
        Assert.IsNotNull(serviceProvider.GetService<IEventTagValidator>());
        Assert.IsNotNull(serviceProvider.GetService<IEventIdCalculator>());
        Assert.IsNotNull(serviceProvider.GetService<ICryptographicService>());
        Assert.IsNotNull(serviceProvider.GetService<IHexConverter>());
        Assert.IsNotNull(serviceProvider.GetService<IMemoryCache>());
    }

    [TestMethod]
    public void AddNostrValidation_WithCustomCacheConfiguration_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNostrValidation(options =>
        {
            options.SizeLimit = 100;
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.IsNotNull(serviceProvider.GetService<IMemoryCache>());
        Assert.IsNotNull(serviceProvider.GetService<INostrEventValidator>());
    }

    [TestMethod]
    public void AddNostrValidationWithoutCaching_RegistersServicesWithoutCache()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNostrValidationWithoutCaching();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.IsNotNull(serviceProvider.GetService<INostrEventValidator>());
        Assert.IsNotNull(serviceProvider.GetService<IEventSignatureValidator>());
        Assert.IsNotNull(serviceProvider.GetService<IEventIdValidator>());
        Assert.IsNotNull(serviceProvider.GetService<IEventKindValidator>());
        Assert.IsNotNull(serviceProvider.GetService<IEventTagValidator>());
        Assert.IsNotNull(serviceProvider.GetService<IEventIdCalculator>());
        Assert.IsNotNull(serviceProvider.GetService<ICryptographicService>());
        Assert.IsNotNull(serviceProvider.GetService<IHexConverter>());
        
        // Memory cache should not be registered in this case
        Assert.IsNull(serviceProvider.GetService<IMemoryCache>());
    }

    [TestMethod]
    public void AddNostrValidation_ServicesAreSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNostrValidation();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var validator1 = serviceProvider.GetService<INostrEventValidator>();
        var validator2 = serviceProvider.GetService<INostrEventValidator>();
        var calculator1 = serviceProvider.GetService<IEventIdCalculator>();
        var calculator2 = serviceProvider.GetService<IEventIdCalculator>();

        // Assert
        Assert.IsNotNull(validator1);
        Assert.IsNotNull(validator2);
        Assert.AreSame(validator1, validator2); // Singleton services should be the same instance
        
        Assert.IsNotNull(calculator1);
        Assert.IsNotNull(calculator2);
        Assert.AreSame(calculator1, calculator2); // Singleton services should be the same instance
    }

    [TestMethod]
    public void AddNostrValidation_MultipleCallsDoNotDuplicate()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNostrValidation();
        services.AddNostrValidation(); // Call again to test TryAdd behavior
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Should still work and not throw exceptions
        Assert.IsNotNull(serviceProvider.GetService<INostrEventValidator>());
        
        // Check that we don't have duplicate registrations
        var validators = serviceProvider.GetServices<INostrEventValidator>();
        Assert.AreEqual(1, validators.Count());
    }
}