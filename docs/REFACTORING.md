# WebSocketConnection Refactoring Action Plan

## ?? Objective
Refactor the high-CRAP `WebSocketConnection` class (CRAP=110) to improve maintainability, testability, and performance by applying SOLID principles and extracting focused responsibilities.

---

## ?? Current State Analysis

### Problems Identified
- **CRAP Score**: 110 (Critical - Target: <30)
- **Cyclomatic Complexity**: 10 (High)
- **SOLID Violations**:
  - **Single Responsibility**: Mixing connection management, message handling, error handling, and state management
  - **Open/Closed**: Hard to extend with new message types or error handling strategies
  - **Dependency Inversion**: Tight coupling to `ClientWebSocket` implementation

### Current Responsibilities (Violation of SRP)
1. WebSocket connection lifecycle management
2. Message sending/receiving operations
3. Background message polling (`ReceiveLoop`)
4. Error handling and event notification
5. Resource disposal and cancellation

---

## ??? Proposed Architecture

### New Component Structure

```
IWebSocketConnection (Façade)
??? IConnectionManager       # Connection lifecycle
??? IMessageReceiver        # Message reception & polling
??? IMessageSender          # Message transmission  
??? IConnectionErrorHandler # Error handling & notifications
??? IConnectionStateManager # State tracking & events
```

### Core Interfaces

```csharp
// Connection lifecycle management
public interface IConnectionManager
{
    WebSocketState State { get; }
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default);
    Task CloseAsync(WebSocketCloseStatus closeStatus, string? description, CancellationToken cancellationToken = default);
    event EventHandler? Disconnected;
}

// Message reception with background polling
public interface IMessageReceiver
{
    Task<string> ReceiveAsync(CancellationToken cancellationToken = default);
    Task StartReceivingAsync(CancellationToken cancellationToken = default);
    Task StopReceivingAsync();
    event EventHandler<string>? MessageReceived;
}

// Message transmission
public interface IMessageSender  
{
    Task SendAsync(string message, CancellationToken cancellationToken = default);
}

// Centralized error handling
public interface IConnectionErrorHandler
{
    event EventHandler<Exception>? ErrorOccurred;
    Task HandleErrorAsync(Exception exception, string context);
    bool ShouldReconnect(Exception exception);
}

// Connection state tracking
public interface IConnectionStateManager
{
    WebSocketState CurrentState { get; }
    bool IsConnected { get; }
    event EventHandler<WebSocketState>? StateChanged;
    void UpdateState(WebSocketState newState);
}
```

---

## ?? File Structure

### New Files to Create

```
NostrSure.Infrastructure/Client/Abstractions/Connection/
??? IConnectionManager.cs
??? IMessageReceiver.cs
??? IMessageSender.cs
??? IConnectionErrorHandler.cs
??? IConnectionStateManager.cs

NostrSure.Infrastructure/Client/Implementation/Connection/
??? ConnectionManager.cs           # IConnectionManager implementation
??? MessageReceiver.cs            # IMessageReceiver implementation  
??? MessageSender.cs              # IMessageSender implementation
??? ConnectionErrorHandler.cs     # IConnectionErrorHandler implementation
??? ConnectionStateManager.cs     # IConnectionStateManager implementation
??? RefactoredWebSocketConnection.cs  # New IWebSocketConnection façade

NostrSure.Infrastructure/Client/Configuration/
??? WebSocketConnectionOptions.cs  # Configuration options

NostrSure.Tests/Client/Connection/
??? ConnectionManagerTests.cs
??? MessageReceiverTests.cs
??? MessageSenderTests.cs
??? ConnectionErrorHandlerTests.cs
??? ConnectionStateManagerTests.cs
??? RefactoredWebSocketConnectionTests.cs
```

### Files to Modify/Remove
```
NostrSure.Infrastructure/Client/Implementation/
??? WebSocketConnection.cs            # REMOVE - No backward compatibility needed

NostrSure.Infrastructure/Client/ServiceCollectionExtensions.cs  # Update DI registration

NostrSure.Tests/Client/
??? NostrClientTests.cs               # Update mock implementations
??? Nip01RequirementsTests.cs        # Update mock implementations
??? DefaultEventDispatcherTests.cs   # No changes needed
```

---

## ?? Implementation Details

### 1. ConnectionManager Implementation

```csharp
public sealed class ConnectionManager : IConnectionManager, IDisposable
{
    private readonly ClientWebSocket _webSocket;
    private readonly IConnectionStateManager _stateManager;
    private readonly IConnectionErrorHandler _errorHandler;
    private readonly CancellationTokenSource _cancellationTokenSource;
    
    public WebSocketState State => _webSocket.State;
    public event EventHandler? Disconnected;

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        try
        {
            var combinedToken = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token)
                .Token;
                
            await _webSocket.ConnectAsync(uri, combinedToken);
            _stateManager.UpdateState(WebSocketState.Open);
        }
        catch (Exception ex)
        {
            await _errorHandler.HandleErrorAsync(ex, nameof(ConnectAsync));
            throw;
        }
    }
    
    // ... other methods
}
```

### 2. MessageReceiver with Object Pooling

```csharp
public sealed class MessageReceiver : IMessageReceiver, IDisposable
{
    private static readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Shared;
    private static readonly ObjectPool<StringBuilder> StringBuilderPool;
    
    private readonly ClientWebSocket _webSocket;
    private readonly IConnectionErrorHandler _errorHandler;
    private readonly CancellationTokenSource _receiveCancellation;
    private Task? _receiveTask;
    
    public event EventHandler<string>? MessageReceived;

    public async Task<string> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        if (_webSocket.State != WebSocketState.Open)
            throw new InvalidOperationException("WebSocket is not connected");

        var buffer = BufferPool.Rent(8192);
        var stringBuilder = StringBuilderPool.Get();
        
        try
        {
            WebSocketReceiveResult receiveResult;
            do
            {
                var segment = new ArraySegment<byte>(buffer);
                receiveResult = await _webSocket.ReceiveAsync(segment, cancellationToken);
                
                if (receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    stringBuilder.Append(Encoding.UTF8.GetString(buffer, 0, receiveResult.Count));
                }
            } while (!receiveResult.EndOfMessage);

            return stringBuilder.ToString();
        }
        finally
        {
            BufferPool.Return(buffer);
            StringBuilderPool.Return(stringBuilder);
        }
    }
    
    public async Task StartReceivingAsync(CancellationToken cancellationToken = default)
    {
        _receiveTask = ReceiveLoopAsync(_receiveCancellation.Token);
        await Task.CompletedTask;
    }
    
    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && 
                   _webSocket.State == WebSocketState.Open)
            {
                var message = await ReceiveAsync(cancellationToken);
                MessageReceived?.Invoke(this, message);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during cancellation
        }
        catch (Exception ex)
        {
            await _errorHandler.HandleErrorAsync(ex, nameof(ReceiveLoopAsync));
        }
    }
}
```

### 3. Refactored WebSocket Connection (Façade)

```csharp
public sealed class RefactoredWebSocketConnection : IWebSocketConnection
{
    private readonly IConnectionManager _connectionManager;
    private readonly IMessageReceiver _messageReceiver;
    private readonly IMessageSender _messageSender;
    private readonly IConnectionErrorHandler _errorHandler;
    private readonly IConnectionStateManager _stateManager;

    public WebSocketState State => _stateManager.CurrentState;
    
    // Delegate events to component implementations
    public event EventHandler<string>? MessageReceived
    {
        add => _messageReceiver.MessageReceived += value;
        remove => _messageReceiver.MessageReceived -= value;
    }
    
    public event EventHandler<Exception>? ErrorOccurred
    {
        add => _errorHandler.ErrorOccurred += value;
        remove => _errorHandler.ErrorOccurred -= value;
    }
    
    public event EventHandler? Disconnected
    {
        add => _connectionManager.Disconnected += value;
        remove => _connectionManager.Disconnected -= value;
    }

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        await _connectionManager.ConnectAsync(uri, cancellationToken);
        await _messageReceiver.StartReceivingAsync(cancellationToken);
    }
    
    public Task SendAsync(string message, CancellationToken cancellationToken = default)
        => _messageSender.SendAsync(message, cancellationToken);
        
    public Task<string> ReceiveAsync(CancellationToken cancellationToken = default)
        => _messageReceiver.ReceiveAsync(cancellationToken);
        
    public async Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure,
                                string? statusDescription = null,
                                CancellationToken cancellationToken = default)
    {
        await _messageReceiver.StopReceivingAsync();
        await _connectionManager.CloseAsync(closeStatus, statusDescription, cancellationToken);
    }
}
```

---

## ?? Testing Strategy

### Unit Test Coverage Goals
- **Target Coverage**: 90%+ for each new component
- **Focus Areas**: Error paths, edge cases, concurrent operations

### Test Structure

```csharp
// Example: MessageReceiver tests
[TestClass]
public class MessageReceiverTests
{
    [TestMethod]
    public async Task ReceiveAsync_WithValidMessage_ReturnsCorrectString() { }
    
    [TestMethod]
    public async Task ReceiveAsync_WithLargeMessage_UsesObjectPooling() { }
    
    [TestMethod]
    public async Task ReceiveAsync_WhenNotConnected_ThrowsInvalidOperationException() { }
    
    [TestMethod]
    public async Task ReceiveLoopAsync_WithNetworkError_HandlesGracefully() { }
    
    [TestMethod]
    public async Task StartReceivingAsync_CalledTwice_DoesNotStartMultipleLoops() { }
}
```

### Integration Tests

```csharp
[TestClass]
public class RefactoredWebSocketConnectionIntegrationTests
{
    [TestMethod]
    public async Task ConnectSendReceiveClose_FullWorkflow_CompletesSuccessfully() { }
    
    [TestMethod]
    public async Task ConcurrentSendOperations_MultipleThreads_HandlesConcurrency() { }
    
    [TestMethod]
    public async Task NetworkInterruption_DuringReceive_TriggersErrorHandling() { }
}
```

---

## ?? Performance Improvements

### Memory Optimization
- **Object Pooling**: Use `ArrayPool<byte>` and `ObjectPool<StringBuilder>`
- **Reduced Allocations**: Eliminate per-message `StringBuilder` creation
- **Target**: 60-80% reduction in GC pressure

### Concurrency Improvements
- **Dedicated Background Tasks**: Separate receive loop from main operations
- **CancellationToken Handling**: Proper token chaining and cleanup
- **Thread Safety**: Lock-free concurrent operations where possible

### Benchmarks to Track
```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class WebSocketConnectionBenchmarks
{
    [Benchmark]
    public async Task SendReceive_OriginalImplementation() { }
    
    [Benchmark]
    public async Task SendReceive_RefactoredImplementation() { }
    
    [Benchmark]
    public void MessageParsing_WithObjectPooling() { }
}
```

---

## ?? Migration Strategy

### Phase 1: Create New Components (1-2 days)
1. Create all new interfaces and implementations
2. Add comprehensive unit tests for each component
3. Ensure all tests pass

### Phase 2: Integration & Testing (1 day)
1. Create `RefactoredWebSocketConnection` façade
2. Add integration tests
3. Run performance benchmarks

### Phase 3: Dependency Injection Update (0.5 days)
1. Replace original `WebSocketConnection` with `RefactoredWebSocketConnection` in DI
2. Update all test mocks to use new implementation
3. Ensure all existing functionality works

### Phase 4: Cleanup (0.5 days)
1. Remove original `WebSocketConnection` class
2. Update all existing tests to use new implementation
3. Update CLI and other consumers

---

## ? Definition of Done

### Code Quality Metrics
- [ ] CRAP score reduced from 110 to <30
- [ ] Cyclomatic complexity <5 for all new methods
- [ ] All SOLID principles followed

### Test Coverage
- [ ] 90%+ line coverage for new components
- [ ] 85%+ branch coverage for error paths
- [ ] All integration tests passing
- [ ] All existing tests updated and passing

### Performance
- [ ] 60%+ reduction in memory allocations during message processing
- [ ] No performance regression in happy-path scenarios
- [ ] Benchmarks demonstrating improvement

### Migration Complete
- [ ] Original `WebSocketConnection` removed
- [ ] All test mocks updated
- [ ] CLI application working with new implementation
- [ ] All NIP-01 compliance tests passing

### Documentation
- [ ] XML documentation for all public interfaces
- [ ] Architecture decision record (ADR) explaining design choices

---
    
## ??? Dependencies & Prerequisites

### NuGet Packages
```xml
<!-- Already available in project -->
<PackageReference Include="Microsoft.Extensions.ObjectPool" Version="8.0.10" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.2" />

<!-- May need to add -->
<PackageReference Include="System.Threading.Channels" Version="8.0.0" />
```

### Development Tools
- .NET 8 SDK
- BenchmarkDotNet (already included)
- Code coverage tools (already configured)

---

## ?? Success Criteria

### Primary Goals
1. **CRAP Reduction**: From 110 to <30 ?
2. **Maintainability**: Each class has single responsibility ?
3. **Testability**: All components fully unit testable ?
4. **Performance**: Reduced memory allocations ?

### Secondary Goals
1. **Extensibility**: Easy to add new connection types
2. **Monitoring**: Better error reporting and diagnostics
3. **Configuration**: Configurable timeouts and buffer sizes

This refactoring will significantly improve the codebase maintainability while replacing the existing implementation completely with a more robust, testable, and performant solution.