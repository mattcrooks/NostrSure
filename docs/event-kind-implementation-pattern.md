# Event Kind Implementation Pattern

This document outlines the steps taken to implement NIP-02 (Contact List Events) in NostrSure, serving as a template for adding other event kinds.

## 1. Domain Model

- Defined a new record [`NostrSure.Domain.Entities.ContactListEvent`](NostrSure.Domain/Entities/ContactListEvent.cs) inheriting from [`NostrSure.Domain.Entities.NostrEvent`](NostrSure.Domain/Entities/NostrEvent.cs).
- Introduced value object [`ContactEntry`](NostrSure.Domain/Entities/ContactListEvent.cs) to represent each `p` tag:
  - `Pubkey`: 32-byte hex string
  - Optional `Petname` and `RelayUrl`

## 2. Serialization / Deserialization

- Extended [`NostrEventJsonConverter`](NostrSure.Infrastructure/Client/Serialization/NostrEventJsonConverter.cs) to:
  - Recognize `kind == 3`
  - Map each `["p", pubkey, petname?, relay?]` tag array to a `ContactEntry`
  - Serialize `ContactEntry` list back to `p` tags

## 3. Validation

- Enhanced [`NostrSure.Domain.Services.EventTagValidator`](NostrSure.Domain/Services/EventTagValidator.cs) (or a dedicated `ContactListValidator`) to enforce:
  - All `p` tags have valid 64-char hex pubkeys
  - Optional `petname` and `relay` are non-empty strings if present
- Reused existing signature and ID validation in [`NostrSure.Domain.Services.NostrEventValidator`](NostrSure.Domain/Services/NostrEventValidator.cs)

## 4. CLI & Client Support

- Added commands in [NosrtSure.CLI/Program.cs](NosrtSure.CLI/Program.cs):
  - Publish a contact list with `client.PublishAsync(new ContactListEvent(...))`
  - Subscribe/stream `kind:3` events via `client.SubscribeAsync(subscriptionId, new { kinds = new[] { 3 } })`
- Updated dependency injection with `services.AddNostrClient()` to include new converters/validators

## 5. Testing

- Created unit tests under `NostrSure.Tests`:
  - Serialization/deserialization round-trip for `ContactListEvent`
  - Validation failures for malformed `p` tags
  - Integration tests in CLI to publish and fetch contact lists
- Followed naming and category conventions demonstrated in [`NostrSure.Tests.Client.Nip01RequirementsTests`](NostrSure.Tests/Client/Nip01RequirementsTests.cs)

## 6. Implementation Pattern

To add a new event kind (e.g., kind 5):

1. **Domain**  
   - Create `<KindName>Event : NostrEvent` in `NostrSure.Domain.Entities`
   - Define any specific properties and factory methods (`FromNostrEvent`, `Create`)

2. **Serialization**  
   - Extend `NostrEventJsonConverter` to handle `kind == <newKind>`
   - Map JSON tag arrays to your new value objects

3. **Validation**  
   - Add or extend validators in `NostrSure.Domain.Services` to enforce per-kind rules

4. **Client / CLI**  
   - Expose publish/subscribe patterns in `NostrClient` and CLI (`Program.cs`)

5. **Tests**  
   - Add unit tests for serialization, validation, and ID/signature rules
   - Add client integration tests for protocol compliance

By following this pattern, you ensure consistency, test coverage, and adherence to NIP specifications.// filepath: docs/EventKindImplementationPattern.md

# Event Kind Implementation Pattern

This document outlines the steps taken to implement NIP-02 (Contact List Events) in NostrSure, serving as a template for adding other event kinds.

## 1. Domain Model

- Defined a new record [`NostrSure.Domain.Entities.ContactListEvent`](NostrSure.Domain/Entities/ContactListEvent.cs) inheriting from [`NostrSure.Domain.Entities.NostrEvent`](NostrSure.Domain/Entities/NostrEvent.cs).
- Introduced value object [`ContactEntry`](NostrSure.Domain/Entities/ContactListEvent.cs) to represent each `p` tag:
  - `Pubkey`: 32-byte hex string
  - Optional `Petname` and `RelayUrl`

## 2. Serialization / Deserialization

- Extended [`NostrEventJsonConverter`](NostrSure.Infrastructure/Client/Serialization/NostrEventJsonConverter.cs) to:
  - Recognize `kind == 3`
  - Map each `["p", pubkey, petname?, relay?]` tag array to a `ContactEntry`
  - Serialize `ContactEntry` list back to `p` tags

## 3. Validation

- Enhanced [`NostrSure.Domain.Services.EventTagValidator`](NostrSure.Domain/Services/EventTagValidator.cs) (or a dedicated `ContactListValidator`) to enforce:
  - All `p` tags have valid 64-char hex pubkeys
  - Optional `petname` and `relay` are non-empty strings if present
- Reused existing signature and ID validation in [`NostrSure.Domain.Services.NostrEventValidator`](NostrSure.Domain/Services/NostrEventValidator.cs)

## 4. CLI & Client Support

- Added commands in [NosrtSure.CLI/Program.cs](NosrtSure.CLI/Program.cs):
  - Publish a contact list with `client.PublishAsync(new ContactListEvent(...))`
  - Subscribe/stream `kind:3` events via `client.SubscribeAsync(subscriptionId, new { kinds = new[] { 3 } })`
- Updated dependency injection with `services.AddNostrClient()` to include new converters/validators

## 5. Testing

- Created unit tests under `NostrSure.Tests`:
  - Serialization/deserialization round-trip for `ContactListEvent`
  - Validation failures for malformed `p` tags
  - Integration tests in CLI to publish and fetch contact lists
- Followed naming and category conventions demonstrated in [`NostrSure.Tests.Client.Nip01RequirementsTests`](NostrSure.Tests/Client/Nip01RequirementsTests.cs)

## 6. Implementation Pattern

To add a new event kind (e.g., kind 5):

1. **Domain**  
   - Create `<KindName>Event : NostrEvent` in `NostrSure.Domain.Entities`
   - Define any specific properties and factory methods (`FromNostrEvent`, `Create`)

2. **Serialization**  
   - Extend `NostrEventJsonConverter` to handle `kind == <newKind>`
   - Map JSON tag arrays to your new value objects

3. **Validation**  
   - Add or extend validators in `NostrSure.Domain.Services` to enforce per-kind rules

4. **Client / CLI**  
   - Expose publish/subscribe patterns in `NostrClient` and CLI (`Program.cs`)

5. **Tests**  
   - Add unit tests for serialization, validation, and ID/signature rules
   - Add client integration tests for protocol compliance

By following this pattern, you ensure consistency, test coverage, and adherence to NIP specifications.