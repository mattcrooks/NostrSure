# NostrSure: Web-of-Trust Crawler & Event Ingestor for Nostr

![CI](https://github.com/mattcrooks/NostrSure/actions/workflows/ci.yml/badge.svg)
![Coverage](https://mattcrooks.github.io/NostrSure/coverage-badge.svg)

## Experiments with .NET 8, Neo4j, PostgreSQL, and Nostr Protocol


> **A modern, high-performance .NET 8 project for exploring the Nostr protocol, building a Web-of-Trust, and ingesting high-signal events.**

---

## 🚀 Project Overview

NostrSure is a modular, extensible .NET 8 solution for crawling the Nostr network, building a Web-of-Trust (WOT) graph, and ingesting only high-value events. It is designed for reliability, performance, and future extensibility, leveraging advanced .NET features, custom serialization, and a clean separation of concerns.

**Key Features:**
- **Web-of-Trust Crawler:** Recursively builds a trust graph from a seed pubkey using `kind:3` (contact list) events.
- **Signal-Only Event Ingestor:** Streams and stores only high-value events (`kind:1` notes, `kind:9735` zaps) from trusted pubkeys.
- **Graph & Event Persistence:** Persists the trust graph in Neo4j and events in PostgreSQL (with future vector search support).
- **CLI Tool:** Interact with relays, stream and process messages directly from the command line.
- **Comprehensive Test Suite:** Includes unit tests and performance benchmarks for core components.

---


## 🎯 Why .NET?

I haven’t touched .NET for over 18 months. I almost reached for Go… but figured:

- Perfect excuse to brush up on C# and the latest .NET  
- Leverage built‑in DI, background services, EF Core (if you like), and a rich ecosystem  
- See how the modern .NET developer experience stacks up for protocol‑level work  

So here we are—back in Visual Studio land and loving every minute of it.

---

## 🏗️ Solution Structure

```
NostrSure.Domain/           # Core domain models, validation, and services
NostrSure.Infrastructure/   # Nostr client, relay communication, serialization, DI
NosrtSure.CLI/              # Command-line interface for relay interaction
NostrSure.Tests/            # Unit tests and benchmarks
```

- **Domain:** Event, tag, pubkey, and validation logic (SOLID, testable, extensible)
- **Infrastructure:** NostrClient, relay protocol, DI extensions, optimized JSON converters
- **CLI:** Connect to relays, subscribe, stream, and process messages interactively
- **Tests:** MSTest-based suite for validation, serialization, and performance

---

## ⚙️ Design Principles

- **SOLID & Clean Architecture:** Each layer is decoupled and testable. DI is used throughout.
- **Performance:** Custom JSON converters, zero-allocation parsing, and caching for event IDs.
- **Extensibility:** Add new event types, validators, or relay strategies with minimal friction.
- **Reliability:** Robust error handling, retry/backoff policies, and validation pipelines.

---

## 🛠️ Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL 14+](https://www.postgresql.org/) (with `vector` extension for future features)
- [Neo4j 5+](https://neo4j.com/)
- Access to a Nostr relay (e.g. `wss://relay.damus.io`)

### Setup & Usage

1. **Clone the Repository**
   ```bash
   git clone https://github.com/mattcrooks/nostrsure.git
   cd nostrsure
   ```

2. **Configure**
   - Copy `appsettings.json.example` to `appsettings.json`.
   - Fill in your PostgreSQL and Neo4j connection strings, seed pubkey(s), and relay URLs.

3. **Run the CLI Tool**
   ```bash
   dotnet run --project NosrtSure.CLI -- [relay-url]
   ```
   - Connects to the specified relay and streams messages for 30 seconds.
   - Example: `dotnet run --project NosrtSure.CLI -- wss://relay.damus.io`

4. **Run the Crawler/Worker Services**
   ```bash
   dotnet run --project NostrSure.Infrastructure
   ```
   - Crawls the Nostr network, builds the WOT graph, and ingests events.

5. **Run the Test Suite**
   ```bash
   dotnet test NostrSure.Tests
   ```
   - Validates event logic, serialization, and performance.

---

## 🧩 Extensibility & Customization

- **Add New Event Types:** Extend domain models and update converters.
- **Integrate Vector Search:** Enable pgvector in Postgres or use Neo4j’s vector capabilities.
- **Front-End Visualization:** Build a React or MAUI client to visualize the WOT graph.
- **Custom Validation Pipelines:** Compose validators for new NIP standards or business rules.

---

## 🧪 Testing & Benchmarking

- **Unit Tests:** Located in `NostrSure.Tests` for all core logic.
- **Benchmarks:** Use BenchmarkDotNet for serialization and event processing performance.
- **Code Coverage:** [View full code coverage report](https://mattcrooks.github.io/NostrSure/)

---

## 📦 Core Components

- **NostrClient:** Main relay client, handles subscriptions, event streaming, and error handling.
- **Validation Pipeline:** Modular, async/sync validation for events, tags, IDs, and signatures.
- **RetryBackoffPolicy:** Robust retry logic for transient relay/network errors.
- **Custom Serialization:** High-performance JSON converters for Nostr events and messages.

---

## 🙌 Contributing & Feedback

This is a personal experiment—PRs, issues, and feedback are welcome! Let’s demystify Nostr and high-performance .NET together.

---

## 📄 License

MIT © Matt Crooks
