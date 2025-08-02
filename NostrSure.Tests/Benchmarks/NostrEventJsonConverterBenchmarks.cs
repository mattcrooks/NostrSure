using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NostrSure.Domain.Entities;
using NostrSure.Domain.ValueObjects;
using NostrSure.Infrastructure.Serialization;

namespace NostrSure.Tests.Benchmarks;

/// <summary>
/// Comprehensive benchmark suite for NostrEventJsonConverter performance testing.
/// 
/// Run with: dotnet run -c Release --project NostrSure.Tests --framework net8.0 -- --filter *NostrEventJsonConverterBenchmarks*
/// </summary>
[Config(typeof(Config))]
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[RankColumn]
public class NostrEventJsonConverterBenchmarks
{
    private readonly JsonSerializerOptions _options;
    private readonly NostrEvent _simpleEvent;
    private readonly NostrEvent _complexEvent;
    private readonly NostrEvent _largeContentEvent;
    private readonly string _simpleEventJson;
    private readonly string _complexEventJson;
    private readonly string _largeContentEventJson;

    public class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance));
        }
    }

    public NostrEventJsonConverterBenchmarks()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new NostrEventJsonConverter());

        // Simple event with minimal tags
        _simpleEvent = new NostrEvent(
            "9007b89f5626b945174a2a8c8d9d0aefc44389fcdd45da2d14ec21bd2f943efe",
            new Pubkey("82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2"),
            DateTimeOffset.FromUnixTimeSeconds(1673311423),
            EventKind.Note,
            new List<NostrTag>
            {
                new("p", new[] { "3bf0c63fcb93463407af97a5e5ee64fa883d107ef9e558472c4eb9aaaefa459d" })
            },
            "Simple test message",
            "f188ace3426d97dbe1641b35984dc839a5c88a728e7701c848144920616967eb64a30a7d657ca16d556bea718311b15260c886568531399ed14239868aedbcee"
        );

        // Complex event with many tags
        _complexEvent = new NostrEvent(
            "0be97f227cf7758e72a62eb6392d1a67b65aef48d684517ea496d17d799b292b",
            new Pubkey("82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2"),
            DateTimeOffset.FromUnixTimeSeconds(1750012616),
            EventKind.ContactList,
            new List<NostrTag>
            {
                new("p", new[] { "b66be78da89991544a05c3a2b63da1d15eefe8e9a1bb6a4369f8616865bd6b7c" }),
                new("p", new[] { "a39199ccb5ec92b1cd047bf3dc7e8923ede769d1a5ccc47d579912f0f5cbdab4" }),
                new("e", new[] { "9007b89f5626b945174a2a8c8d9d0aefc44389fcdd45da2d14ec21bd2f943efe" }),
                new("t", new[] { "nostr" }),
                new("t", new[] { "bitcoin" }),
                new("relay", new[] { "wss://relay.primal.net" }),
                new("relay", new[] { "wss://relay.damus.io" }),
                new("nonce", new[] { "12345", "16" })
            },
            "{\"wss://relay.primal.net\":{\"read\":true,\"write\":true}}",
            "88925482183cabd79c94e179309d0b5314efd1ce55848b1f264480ae610e55e45dfb3c873eb6b3850060124b7c70742295a1ecd32e7447848b061778f26e3375"
        );

        // Event with large content containing special characters
        var largeContent = string.Join("\n", Enumerable.Repeat(
            "This is a long message with special characters: \"quotes\", \\backslashes\\, \ttabs\t, and \rcarriage returns\r. " +
            "It contains Unicode characters: 漢字, العربية, and emojis like 😊 and 🚀 to test performance with larger payloads.",
            50));

        _largeContentEvent = new NostrEvent(
            "1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef",
            new Pubkey("1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef"),
            DateTimeOffset.FromUnixTimeSeconds(1700000000),
            EventKind.Note,
            new List<NostrTag>
            {
                new("p", new[] { "abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890" }),
                new("e", new[] { "fedcba0987654321fedcba0987654321fedcba0987654321fedcba0987654321" }),
                new("t", new[] { "performance" }),
                new("t", new[] { "testing" })
            },
            largeContent,
            "abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890"
        );

        // Pre-serialize for deserialization benchmarks
        _simpleEventJson = JsonSerializer.Serialize(_simpleEvent, _options);
        _complexEventJson = JsonSerializer.Serialize(_complexEvent, _options);
        _largeContentEventJson = JsonSerializer.Serialize(_largeContentEvent, _options);
    }

    #region Serialization Benchmarks

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Serialization")]
    public string SerializeSimpleEvent()
    {
        return JsonSerializer.Serialize(_simpleEvent, _options);
    }

    [Benchmark]
    [BenchmarkCategory("Serialization")]
    public string SerializeComplexEvent()
    {
        return JsonSerializer.Serialize(_complexEvent, _options);
    }

    [Benchmark]
    [BenchmarkCategory("Serialization")]
    public string SerializeLargeContentEvent()
    {
        return JsonSerializer.Serialize(_largeContentEvent, _options);
    }

    #endregion

    #region Deserialization Benchmarks

    [Benchmark]
    [BenchmarkCategory("Deserialization")]
    public NostrEvent? DeserializeSimpleEvent()
    {
        return JsonSerializer.Deserialize<NostrEvent>(_simpleEventJson, _options);
    }

    [Benchmark]
    [BenchmarkCategory("Deserialization")]
    public NostrEvent? DeserializeComplexEvent()
    {
        return JsonSerializer.Deserialize<NostrEvent>(_complexEventJson, _options);
    }

    [Benchmark]
    [BenchmarkCategory("Deserialization")]
    public NostrEvent? DeserializeLargeContentEvent()
    {
        return JsonSerializer.Deserialize<NostrEvent>(_largeContentEventJson, _options);
    }

    #endregion

    #region Round-trip Benchmarks

    [Benchmark]
    [BenchmarkCategory("RoundTrip")]
    public NostrEvent? RoundTripSimpleEvent()
    {
        var json = JsonSerializer.Serialize(_simpleEvent, _options);
        return JsonSerializer.Deserialize<NostrEvent>(json, _options);
    }

    [Benchmark]
    [BenchmarkCategory("RoundTrip")]
    public NostrEvent? RoundTripComplexEvent()
    {
        var json = JsonSerializer.Serialize(_complexEvent, _options);
        return JsonSerializer.Deserialize<NostrEvent>(json, _options);
    }

    #endregion

    #region Batch Processing Benchmarks

    [Params(10, 100, 1000)]
    public int BatchSize { get; set; }

    [Benchmark]
    [BenchmarkCategory("Batch")]
    public List<string> SerializeBatch()
    {
        var results = new List<string>(BatchSize);
        for (int i = 0; i < BatchSize; i++)
        {
            results.Add(JsonSerializer.Serialize(_simpleEvent, _options));
        }
        return results;
    }

    [Benchmark]
    [BenchmarkCategory("Batch")]
    public List<NostrEvent?> DeserializeBatch()
    {
        var results = new List<NostrEvent?>(BatchSize);
        for (int i = 0; i < BatchSize; i++)
        {
            results.Add(JsonSerializer.Deserialize<NostrEvent>(_simpleEventJson, _options));
        }
        return results;
    }

    #endregion

    #region Memory Allocation Benchmarks

    [Benchmark]
    [BenchmarkCategory("Memory")]
    public void SerializeSimpleEventToUtf8Bytes()
    {
        JsonSerializer.SerializeToUtf8Bytes(_simpleEvent, _options);
    }

    [Benchmark]
    [BenchmarkCategory("Memory")]
    public NostrEvent? DeserializeSimpleEventFromUtf8Bytes()
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(_simpleEvent, _options);
        return JsonSerializer.Deserialize<NostrEvent>(bytes, _options);
    }

    #endregion

    #region NostrTag Performance Benchmarks

    [Benchmark]
    [BenchmarkCategory("TagPerformance")]
    public NostrEvent CreateEventWithManyTags()
    {
        var tags = new List<NostrTag>();
        for (int i = 0; i < 50; i++)
        {
            tags.Add(new NostrTag("p", new[] { $"pubkey_{i:x64}" }));
            tags.Add(new NostrTag("e", new[] { $"event_{i:x64}" }));
            tags.Add(new NostrTag("t", new[] { $"tag_{i}" }));
        }

        return new NostrEvent(
            "test_id",
            new Pubkey("test_pubkey"),
            DateTimeOffset.UtcNow,
            EventKind.Note,
            tags,
            "Test content",
            "test_sig"
        );
    }

    [Benchmark]
    [BenchmarkCategory("TagPerformance")]
    public List<NostrTag> ParseTagArraysFromJson()
    {
        var json = """
        [
            ["p", "82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2"],
            ["e", "9007b89f5626b945174a2a8c8d9d0aefc44389fcdd45da2d14ec21bd2f943efe", "wss://relay.example.com"],
            ["t", "nostr"],
            ["relay", "wss://relay.primal.net", "read", "write"],
            ["nonce", "12345", "16"]
        ]
        """;

        var tagArrays = JsonSerializer.Deserialize<List<List<string>>>(json);
        var tags = new List<NostrTag>();
        
        foreach (var tagArray in tagArrays!)
        {
            tags.Add(NostrTag.FromArray(tagArray));
        }

        return tags;
    }

    #endregion
}

/// <summary>
/// Comparison benchmark between optimized and standard System.Text.Json serialization
/// </summary>
[Config(typeof(NostrEventJsonConverterBenchmarks.Config))]
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class SerializationComparisonBenchmarks
{
    private readonly JsonSerializerOptions _customOptions;
    private readonly JsonSerializerOptions _defaultOptions;
    private readonly NostrEvent _testEvent;
    private readonly string _testEventJson;

    public SerializationComparisonBenchmarks()
    {
        _customOptions = new JsonSerializerOptions();
        _customOptions.Converters.Add(new NostrEventJsonConverter());

        _defaultOptions = new JsonSerializerOptions();

        _testEvent = new NostrEvent(
            "9007b89f5626b945174a2a8c8d9d0aefc44389fcdd45da2d14ec21bd2f943efe",
            new Pubkey("82341f882b6eabcd2ba7f1ef90aad961cf074af15b9ef44a09f9d2a8fbfbe6a2"),
            DateTimeOffset.FromUnixTimeSeconds(1673311423),
            EventKind.Note,
            new List<NostrTag>
            {
                new("p", new[] { "3bf0c63fcb93463407af97a5e5ee64fa883d107ef9e558472c4eb9aaaefa459d" })
            },
            "Test message",
            "f188ace3426d97dbe1641b35984dc839a5c88a728e7701c848144920616967eb"
        );

        _testEventJson = JsonSerializer.Serialize(_testEvent, _customOptions);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Serialize")]
    public string CustomConverter_Serialize()
    {
        return JsonSerializer.Serialize(_testEvent, _customOptions);
    }

    [Benchmark]
    [BenchmarkCategory("Serialize")]
    public string DefaultJson_Serialize()
    {
        return JsonSerializer.Serialize(_testEvent, _defaultOptions);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Deserialize")]
    public NostrEvent? CustomConverter_Deserialize()
    {
        return JsonSerializer.Deserialize<NostrEvent>(_testEventJson, _customOptions);
    }

    // Note: Default deserializer cannot deserialize back to NostrEvent due to constructor requirements
    // This benchmark would require additional setup with a parameterless constructor
}

/// <summary>
/// Program entry point for running benchmarks
/// </summary>
public class BenchmarkProgram
{
    public static void Main(string[] args)
    {
        // Run specific benchmark suites based on command line arguments
        if (args.Length > 0 && args[0].Contains("comparison"))
        {
            BenchmarkRunner.Run<SerializationComparisonBenchmarks>();
        }
        else if (args.Length > 0 && args[0].Contains("converter"))
        {
            BenchmarkRunner.Run<NostrEventJsonConverterBenchmarks>();
        }
        else
        {
            // Run all benchmarks
            BenchmarkSwitcher.FromAssembly(typeof(NostrEventJsonConverterBenchmarks).Assembly).Run(args);
        }
    }
}