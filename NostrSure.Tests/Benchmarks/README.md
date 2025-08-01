# ?? NostrSure Benchmark Suite

High-performance benchmarking for NostrEventJsonConverter using BenchmarkDotNet.

## ?? Available Benchmark Options

### 1. **BenchmarkDotNet** (Implemented)
- ? **Industry Standard**: Used by Microsoft .NET team
- ? **Statistical Analysis**: Multiple runs with statistical validation
- ? **Memory Profiling**: Allocation tracking and GC pressure analysis
- ? **Cross-Platform**: Runs on Windows, Linux, macOS
- ? **Multiple Runtimes**: Test across different .NET versions

### 2. **Alternative Options** (Not implemented)
- **NBench**: Actor-based performance testing
- **Custom Stopwatch**: Basic timing measurements
- **dotnet-counters**: Real-time performance monitoring

## ????? Running Benchmarks

### Quick Start
```bash
# Run all benchmarks
./run-benchmarks.ps1

# Run specific suite
./run-benchmarks.ps1 -Suite converter
./run-benchmarks.ps1 -Suite comparison
./run-benchmarks.ps1 -Suite serialization

# Export results with profiling
./run-benchmarks.ps1 -Suite all -Export -Profile
```

### Manual Execution
```bash
# Build in Release mode
dotnet build -c Release

# Run specific benchmark class
dotnet run -c Release --project NostrSure.Tests --framework net8.0 -- --filter *NostrEventJsonConverterBenchmarks*

# Run by category
dotnet run -c Release --project NostrSure.Tests --framework net8.0 -- --categories Serialization,Deserialization

# Export results
dotnet run -c Release --project NostrSure.Tests --framework net8.0 -- --exporters json,html,csv
```

## ?? Benchmark Categories

### 1. **Serialization Benchmarks**
- `SerializeSimpleEvent`: Basic event with minimal tags
- `SerializeComplexEvent`: Event with multiple tags and metadata
- `SerializeLargeContentEvent`: Event with large content payload

### 2. **Deserialization Benchmarks**
- `DeserializeSimpleEvent`: Parse basic JSON to NostrEvent
- `DeserializeComplexEvent`: Parse complex JSON with many tags
- `DeserializeLargeContentEvent`: Parse large content with special characters

### 3. **Round-trip Benchmarks**
- `RoundTripSimpleEvent`: Serialize ? Deserialize cycle
- `RoundTripComplexEvent`: Full cycle with complex data

### 4. **Batch Processing Benchmarks**
- `SerializeBatch`: Bulk serialization (10, 100, 1000 events)
- `DeserializeBatch`: Bulk deserialization performance

### 5. **Memory Allocation Benchmarks**
- `SerializeSimpleEventToUtf8Bytes`: Memory-efficient byte serialization
- `DeserializeSimpleEventFromUtf8Bytes`: Direct byte deserialization

### 6. **Comparison Benchmarks**
- `CustomConverter_Serialize` vs `DefaultJson_Serialize`
- Performance comparison with System.Text.Json defaults

## ?? Performance Optimizations Tested

Our optimized `NostrEventJsonConverter` includes:

1. **Zero-Allocation Property Comparisons**
   - UTF-8 byte span comparisons (`"id"u8`)
   - No string allocations during parsing

2. **Fast EventKind Validation**
   - `HashSet<int>` lookup instead of `Enum.IsDefined()`
   - O(1) vs O(n) complexity

3. **Inline Tags Processing**
   - Custom tags serialization/deserialization
   - Eliminates recursive `JsonSerializer` calls

4. **Method Inlining**
   - Hot path methods marked `AggressiveInlining`
   - Exception methods marked `NoInlining`

5. **Efficient Field Validation**
   - Single counter for required field tracking
   - Reduced branching overhead

## ?? Expected Performance Improvements

Based on optimizations implemented:

- **50-70% reduction** in memory allocations
- **20-40% faster** serialization/deserialization
- **Reduced GC pressure** from fewer string allocations
- **Better cache locality** from span-based operations

## ?? Understanding Results

### Key Metrics
- **Mean**: Average execution time
- **Error**: Standard error of measurements
- **StdDev**: Standard deviation
- **Ratio**: Performance relative to baseline
- **Gen0/Gen1**: Garbage collections per 1000 operations
- **Allocated**: Memory allocated per operation

### Sample Output
```
|                    Method |     Mean |     Error |    StdDev | Ratio | Gen0 | Allocated |
|-------------------------- |---------:|----------:|----------:|------:|-----:|----------:|
|      SerializeSimpleEvent |  1.234 ?s |  0.012 ?s |  0.011 ?s |  1.00 | 0.15 |     312 B |
|     SerializeComplexEvent |  2.567 ?s |  0.023 ?s |  0.021 ?s |  2.08 | 0.23 |     486 B |
```

## ?? Customization

### Adding New Benchmarks
```csharp
[Benchmark]
[BenchmarkCategory("MyCategory")]
public string MyCustomBenchmark()
{
    // Your benchmark code here
    return JsonSerializer.Serialize(myEvent, _options);
}
```

### Custom Configurations
```csharp
[Config(typeof(MyCustomConfig))]
public class MyBenchmarkClass
{
    public class MyCustomConfig : ManualConfig
    {
        public MyCustomConfig()
        {
            AddJob(Job.Default.WithIterationCount(10));
            AddExporter(JsonExporter.Full);
        }
    }
}
```

## ?? Best Practices

1. **Always run in Release mode** (`-c Release`)
2. **Close unnecessary applications** during benchmarking
3. **Run multiple times** to ensure consistency
4. **Use steady workload** (no background tasks)
5. **Warm up the JIT** (BenchmarkDotNet handles this)

## ?? Output Files

Results are saved to `BenchmarkDotNet.Artifacts/` with formats:
- **JSON**: Machine-readable results
- **HTML**: Interactive charts and graphs
- **CSV**: Spreadsheet-compatible data
- **MD**: Markdown tables for documentation

---

*Benchmark suite optimized for high-throughput Nostr event processing scenarios.*