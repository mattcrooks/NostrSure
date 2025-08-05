#!/usr/bin/env pwsh
# Benchmark runner script for NostrSure performance tests

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("all", "converter", "comparison", "serialization", "deserialization", "batch", "memory")]
    [string]$Suite = "all",
    
    [Parameter(Mandatory=$false)]
    [switch]$Profile,
    
    [Parameter(Mandatory=$false)]
    [switch]$Export
)

Write-Host ">> Running NostrSure Benchmark Suite" -ForegroundColor Green
Write-Host "Suite: $Suite" -ForegroundColor Cyan

# Build in Release mode first
Write-Host "Building in Release mode..." -ForegroundColor Yellow
dotnet build -c Release --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}

# Prepare benchmark arguments
$benchmarkArgs = @()

switch ($Suite) {
    "converter" { 
        $benchmarkArgs += "--filter"
        $benchmarkArgs += "*NostrEventJsonConverterBenchmarks*"
    }
    "comparison" { 
        $benchmarkArgs += "--filter"
        $benchmarkArgs += "*SerializationComparisonBenchmarks*"
    }
    "serialization" {
        $benchmarkArgs += "--filter"
        $benchmarkArgs += "*"
        $benchmarkArgs += "--categories"
        $benchmarkArgs += "Serialization"
    }
    "deserialization" {
        $benchmarkArgs += "--filter"
        $benchmarkArgs += "*"
        $benchmarkArgs += "--categories"
        $benchmarkArgs += "Deserialization"
    }
    "batch" {
        $benchmarkArgs += "--filter"
        $benchmarkArgs += "*"
        $benchmarkArgs += "--categories"
        $benchmarkArgs += "Batch"
    }
    "memory" {
        $benchmarkArgs += "--filter"
        $benchmarkArgs += "*"
        $benchmarkArgs += "--categories"
        $benchmarkArgs += "Memory"
    }
}

if ($Profile) {
    $benchmarkArgs += "--profiler"
    $benchmarkArgs += "ETW"
}

if ($Export) {
    $benchmarkArgs += "--exporters"
    $benchmarkArgs += "json,html,csv"
}

# Run benchmarks
Write-Host "Running benchmarks..." -ForegroundColor Yellow
Write-Host "Command: dotnet run -c Release --project NostrSure.Tests --framework net8.0 -- $($benchmarkArgs -join ' ')" -ForegroundColor Gray

dotnet run -c Release --project NostrSure.Tests --framework net8.0 -- @benchmarkArgs

if ($LASTEXITCODE -eq 0) {
    Write-Host "✔ Benchmarks completed successfully!" -ForegroundColor Green
    
    if ($Export) {
        Write-Host "✔✔ Results exported to BenchmarkDotNet.Artifacts folder" -ForegroundColor Cyan
    }
} else {
    Write-Error "✖ Benchmarks failed!"
    exit 1
}