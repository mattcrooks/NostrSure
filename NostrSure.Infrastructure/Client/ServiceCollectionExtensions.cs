using Microsoft.Extensions.DependencyInjection;
using NostrSure.Infrastructure.Client.Abstractions;
using NostrSure.Infrastructure.Client.Implementation;

namespace NostrSure.Infrastructure.Client;

/// <summary>
/// Extension methods for configuring Nostr client services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Nostr client services to the DI container
    /// </summary>
    public static IServiceCollection AddNostrClient(this IServiceCollection services)
    {
        // Core services
        services.AddSingleton<IWebSocketFactory, WebSocketFactory>();
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
        // Core services
        services.AddSingleton<IWebSocketFactory, WebSocketFactory>();
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