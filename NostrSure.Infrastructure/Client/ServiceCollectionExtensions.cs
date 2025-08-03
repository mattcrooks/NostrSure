using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using NostrSure.Infrastructure.Client.Abstractions;
using NostrSure.Infrastructure.Client.Implementation;

namespace NostrSure.Infrastructure.Client;

/// <summary>
/// Extension methods for configuring Nostr client services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Nostr client services to the DI container using refactored WebSocket implementation
    /// </summary>
    public static IServiceCollection AddNostrClient(this IServiceCollection services)
    {
        // Object pooling for performance
        services.AddSingleton<ObjectPool<StringBuilder>>(provider =>
        {
            var objectPoolProvider = new DefaultObjectPoolProvider();
            var policy = new StringBuilderPooledObjectPolicy();
            return objectPoolProvider.Create(policy);
        });

        // Refactored WebSocket components
        services.AddSingleton<IWebSocketFactory, RefactoredWebSocketFactory>();
        
        // Core services
        services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
        services.AddSingleton<ISubscriptionManager, InMemorySubscriptionManager>();
        services.AddSingleton<IEventDispatcher, DefaultEventDispatcher>();
        services.AddSingleton<IHealthPolicy, RetryBackoffPolicy>();
        
        // Main client
        services.AddTransient<INostrClient, NostrClient>();
        
        return services;
    }

    /// <summary>
    /// Adds Nostr client services with custom health policy configuration
    /// </summary>
    public static IServiceCollection AddNostrClient(this IServiceCollection services,
                                                   TimeSpan? baseDelay = null,
                                                   TimeSpan? maxDelay = null,
                                                   int maxRetries = 5)
    {
        // Object pooling for performance
        services.AddSingleton<ObjectPool<StringBuilder>>(provider =>
        {
            var objectPoolProvider = new DefaultObjectPoolProvider();
            var policy = new StringBuilderPooledObjectPolicy();
            return objectPoolProvider.Create(policy);
        });

        // Refactored WebSocket components
        services.AddSingleton<IWebSocketFactory, RefactoredWebSocketFactory>();
        
        // Core services
        services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
        services.AddSingleton<ISubscriptionManager, InMemorySubscriptionManager>();
        services.AddSingleton<IEventDispatcher, DefaultEventDispatcher>();
        
        // Custom health policy
        services.AddSingleton<IHealthPolicy>(provider => 
            new RetryBackoffPolicy(baseDelay, maxDelay, maxRetries));
        
        // Main client
        services.AddTransient<INostrClient, NostrClient>();
        
        return services;
    }
}