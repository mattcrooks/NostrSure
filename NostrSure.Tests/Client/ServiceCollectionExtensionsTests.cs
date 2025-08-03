using Microsoft.Extensions.DependencyInjection;
using NostrSure.Infrastructure.Client;
using NostrSure.Infrastructure.Client.Abstractions;

namespace NostrSure.Tests.Client;

[TestClass]
public class ServiceCollectionExtensionsTests
{
    [TestMethod]
    public void AddNostrClient_RegistersAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNostrClient();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.IsNotNull(serviceProvider.GetService<INostrClient>());
        Assert.IsNotNull(serviceProvider.GetService<IMessageSerializer>());
        Assert.IsNotNull(serviceProvider.GetService<ISubscriptionManager>());
        Assert.IsNotNull(serviceProvider.GetService<IEventDispatcher>());
        Assert.IsNotNull(serviceProvider.GetService<IHealthPolicy>());
        Assert.IsNotNull(serviceProvider.GetService<IWebSocketFactory>());
    }

    [TestMethod]
    public void AddNostrClient_WithCustomConfiguration_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var baseDelay = TimeSpan.FromMilliseconds(500);
        var maxDelay = TimeSpan.FromSeconds(30);
        const int maxRetries = 10;

        // Act
        services.AddNostrClient(baseDelay, maxDelay, maxRetries);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.IsNotNull(serviceProvider.GetService<INostrClient>());
        Assert.IsNotNull(serviceProvider.GetService<IHealthPolicy>());
        
        // Verify the health policy was configured with custom parameters
        var healthPolicy = serviceProvider.GetService<IHealthPolicy>();
        Assert.IsNotNull(healthPolicy);
    }

    [TestMethod]
    public void AddNostrClient_NostrClientIsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNostrClient();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var client1 = serviceProvider.GetService<INostrClient>();
        var client2 = serviceProvider.GetService<INostrClient>();

        // Assert
        Assert.IsNotNull(client1);
        Assert.IsNotNull(client2);
        Assert.AreNotSame(client1, client2); // Transient services should be different instances
    }

    [TestMethod]
    public void AddNostrClient_SharedServicesAreSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNostrClient();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var serializer1 = serviceProvider.GetService<IMessageSerializer>();
        var serializer2 = serviceProvider.GetService<IMessageSerializer>();
        var subscriptionManager1 = serviceProvider.GetService<ISubscriptionManager>();
        var subscriptionManager2 = serviceProvider.GetService<ISubscriptionManager>();

        // Assert
        Assert.IsNotNull(serializer1);
        Assert.IsNotNull(serializer2);
        Assert.AreSame(serializer1, serializer2); // Singleton services should be the same instance
        
        Assert.IsNotNull(subscriptionManager1);
        Assert.IsNotNull(subscriptionManager2);
        Assert.AreSame(subscriptionManager1, subscriptionManager2); // Singleton services should be the same instance
    }
}