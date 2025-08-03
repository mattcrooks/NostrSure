using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NostrSure.Domain.Interfaces;
using NostrSure.Domain.Services;
using NostrSure.Domain.Validation;

namespace NostrSure.Domain.Extensions;

/// <summary>
/// Dependency injection extensions for the modular validation pipeline
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the modular Nostr validation pipeline to the service collection
    /// </summary>
    public static IServiceCollection AddNostrValidation(this IServiceCollection services)
    {
        // Core validators
        services.TryAddSingleton<INostrEventValidator, ModularNostrEventValidator>();
        services.TryAddSingleton<IEventSignatureValidator, EventSignatureValidator>();
        services.TryAddSingleton<IEventIdValidator, EventIdValidator>();
        services.TryAddSingleton<IEventKindValidator, EventKindValidator>();
        services.TryAddSingleton<IEventTagValidator, EventTagValidator>();

        // Supporting services
        services.TryAddSingleton<IEventIdCalculator, CachedEventIdCalculator>();
        services.TryAddSingleton<ICryptographicService, OptimizedCryptographicService>();
        services.TryAddSingleton<IHexConverter, OptimizedHexConverter>();

        // Infrastructure dependencies
        services.AddMemoryCache();

        return services;
    }

    /// <summary>
    /// Adds the modular Nostr validation pipeline with custom memory cache configuration
    /// </summary>
    public static IServiceCollection AddNostrValidation(this IServiceCollection services,
        System.Action<MemoryCacheOptions> configureCacheOptions)
    {
        services.AddMemoryCache(configureCacheOptions);

        // Add validation services without adding default memory cache
        services.TryAddSingleton<INostrEventValidator, ModularNostrEventValidator>();
        services.TryAddSingleton<IEventSignatureValidator, EventSignatureValidator>();
        services.TryAddSingleton<IEventIdValidator, EventIdValidator>();
        services.TryAddSingleton<IEventKindValidator, EventKindValidator>();
        services.TryAddSingleton<IEventTagValidator, EventTagValidator>();
        services.TryAddSingleton<IEventIdCalculator, CachedEventIdCalculator>();
        services.TryAddSingleton<ICryptographicService, OptimizedCryptographicService>();
        services.TryAddSingleton<IHexConverter, OptimizedHexConverter>();

        return services;
    }

    /// <summary>
    /// Adds validation services without caching (for scenarios where caching is not desired)
    /// </summary>
    public static IServiceCollection AddNostrValidationWithoutCaching(this IServiceCollection services)
    {
        services.TryAddSingleton<INostrEventValidator, ModularNostrEventValidator>();
        services.TryAddSingleton<IEventSignatureValidator, EventSignatureValidator>();
        services.TryAddSingleton<IEventIdValidator, EventIdValidator>();
        services.TryAddSingleton<IEventKindValidator, EventKindValidator>();
        services.TryAddSingleton<IEventTagValidator, EventTagValidator>();
        services.TryAddSingleton<ICryptographicService, OptimizedCryptographicService>();
        services.TryAddSingleton<IHexConverter, OptimizedHexConverter>();

        // Use a simple non-cached event ID calculator
        services.TryAddSingleton<IEventIdCalculator, SimpleEventIdCalculator>();

        return services;
    }
}