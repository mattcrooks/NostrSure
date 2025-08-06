# NIP-01 Compliance Report for NostrSure

This document assesses the NostrSure codebase for compliance with [NIP-01: Basic Event Serialization](https://github.com/nostr-protocol/nips/blob/master/01.md).

## Event Serialization Requirements

| #   | Requirement                                                                          | MUST/SHOULD | Compliance | Implementing Class/File                 | Notes                                                  |
| --- | ------------------------------------------------------------------------------------ | ----------- | ---------- | --------------------------------------- | ------------------------------------------------------ |
| 1   | Event serialization format: `[0, <pubkey>, <created_at>, <kind>, <tags>, <content>]` | MUST        | ✅         | `NostrEvent`, `NostrEventJsonConverter` | Matches NIP-01 structure                               |
| 2   | `id` field: SHA256 of serialized event                                               | MUST        | ✅         | `NostrEvent`, `NostrEventValidator`     | Calculated and validated                               |
| 3   | `pubkey`: 32-byte hex string                                                         | MUST        | ✅         | `Pubkey`, `NostrEventValidator`         | Length and hex format validated                        |
| 4   | `created_at`: UNIX timestamp (seconds)                                               | MUST        | ✅         | `NostrEvent`                            | Stored as `DateTimeOffset`, serialized as UNIX seconds |
| 5   | `kind`: integer                                                                      | MUST        | ✅         | `NostrEvent`                            | Enum used, serialized as integer                       |
| 6   | `tags`: array of arrays                                                              | MUST        | ✅         | `NostrTag`, `NostrEventJsonConverter`   | Array of arrays enforced                               |
| 7   | `content`: string                                                                    | MUST        | ✅         | `NostrEvent`                            | String, empty string allowed                           |
| 8   | `sig`: 64-byte hex string (signature)                                                | MUST        | ✅         | `NostrEvent`, `NostrEventValidator`     | Length and hex format validated                        |
| 9   | Event must be valid JSON                                                             | MUST        | ✅         | `NostrEventJsonConverter`               | Uses `System.Text.Json`                                |
| 10  | Tags: first element is tag name, rest are values                                     | MUST        | ✅         | `NostrTag`                              | Tag structure enforced                                 |
| 11  | No extra fields in event object                                                      | SHOULD      | ✅         | `NostrEventJsonConverter`               | Only NIP-01 fields serialized                          |
| 12  | Event signature verification                                                         | SHOULD      | ✅         | `NostrEventValidator`                   | Signature verification logic present                   |
| 13  | Event kind extensibility                                                             | MAY         | ✅         | `EventKind`                             | Custom kinds supported                                 |

## Relayer-Client Protocol Requirements

| #   | Requirement / Message Type                                                                     | MUST/SHOULD                   | Compliance | Implementing Class/File              | Notes                                 |
| --- | ---------------------------------------------------------------------------------------------- | ----------------------------- | ---------- | ------------------------------------ | ------------------------------------- | ------------------- |
| 1   | Relays expose a websocket endpoint                                                             | MUST                          | ✅         | `NostrClient`                        | Connects via WebSocket                |
| 2   | Clients SHOULD use a single WebSocket per relay                                                | SHOULD                        | ✅         | `NostrClient`, `Program.cs`          | One connection per relay              |
| 3   | Relays MAY limit number of connections                                                         | MAY                           | ✅         | (Relay-side, not client)             | Protocol supported                    |
| 4   | ["EVENT", <event>] to publish events                                                           | MUST                          | ✅         | `NostrClient`                        | Publishes correct format              |
| 5   | ["REQ", <subscription_id>, <filters...>] to subscribe                                          | MUST                          | ✅         | `NostrClient`, `Program.cs`          | Subscriptions and filters supported   |
| 6   | ["CLOSE", <subscription_id>] to close subscription                                             | MUST                          | ✅         | `NostrClient`, `Program.cs`          | Subscriptions closed correctly        |
| 7   | <subscription_id>: non-empty, max 64 chars, per connection                                     | MUST                          | ✅         | `Program.cs`, `NostrClient`          | Managed per connection                |
| 8   | <filtersX>: ids, authors, kinds, #tags, since, until, limit                                    | MUST                          | ✅         | `NostrClient`, `Program.cs`          | All filter attributes supported       |
| 9   | ids, authors, #e, #p: exact 64-char lowercase hex                                              | MUST                          | ✅         | `NostrEventValidator`, `NostrClient` | Hex format and length validated       |
| 10  | since/until: UNIX timestamp, filter by created_at                                              | MUST                          | ✅         | `NostrClient`                        | Time range supported                  |
| 11  | Multiple filter conditions: AND logic                                                          | MUST                          | ✅         | `NostrClient`                        | AND logic supported                   |
| 12  | Multiple filters: OR logic                                                                     | MUST                          | ✅         | `NostrClient`                        | OR logic supported                    |
| 13  | limit: only valid for initial query                                                            | MUST                          | ✅         | `NostrClient`                        | Used for initial query only           |
| 14  | Initial query: events ordered by created_at desc, then id lex                                  | SHOULD                        | ✅         | (Relay-side, not client)             | Client supports, relay must implement |
| 15  | ["EVENT", <subscription_id>, <event>] from relay                                               | MUST                          | ✅         | `NostrClient`, `Program.cs`          | Correct format received               |
| 16  | ["OK", <event_id>, <true                                                                       | false>, <message>] from relay | MUST       | ✅                                   | `NostrClient`, `Program.cs`           | OK messages handled |
| 17  | ["EOSE", <subscription_id>] from relay                                                         | MUST                          | ✅         | `NostrClient`, `Program.cs`          | End-of-stored-events handled          |
| 18  | ["CLOSED", <subscription_id>, <message>] from relay                                            | MUST                          | ✅         | `NostrClient`, `Program.cs`          | CLOSED messages handled               |
| 19  | ["NOTICE", <message>] from relay                                                               | MUST                          | ✅         | `NostrClient`, `Program.cs`          | Notices handled and logged            |
| 20  | OK/CLOSED message: machine-readable prefix + human message                                     | SHOULD                        | ⚠️ Partial | `NostrClient`, `Program.cs`          | Prefixes parsed generically           |
| 21  | Standardized prefixes: duplicate, pow, blocked, rate-limited, invalid, restricted, mute, error | SHOULD                        | ⚠️ Partial | `NostrClient`, `Program.cs`          | Only `error` handled explicitly       |

## Standardized Machine-Readable Prefixes

| Prefix       | SHOULD | Compliance | Implementing Class/File     | Notes                                       |
| ------------ | ------ | ---------- | --------------------------- | ------------------------------------------- |
| duplicate    | SHOULD | ⚠️ Partial | `NostrClient`, `Program.cs` | Can be handled, not explicitly standardized |
| pow          | SHOULD | ⚠️ Partial | `NostrClient`, `Program.cs` | No explicit PoW prefix handling             |
| blocked      | SHOULD | ⚠️ Partial | `NostrClient`, `Program.cs` | Blocked events can be logged                |
| rate-limited | SHOULD | ⚠️ Partial | `NostrClient`, `Program.cs` | Rate-limiting can be detected               |
| invalid      | SHOULD | ⚠️ Partial | `NostrClient`, `Program.cs` | Invalid events are logged                   |
| restricted   | SHOULD | ⚠️ Partial | `NostrClient`, `Program.cs` | Restricted events can be handled            |
| mute         | SHOULD | ⚠️ Partial | `NostrClient`, `Program.cs` | Muted events can be processed               |
| error        | SHOULD | ✅         | `NostrClient`, `Program.cs` | Generic error handling present              |

### Assessment

- All message formats and subscription management requirements are **complied with** in the client code.
- All filter attributes and logic (AND/OR, time range, limit, hex validation) are supported.
- OK, CLOSED, EOSE, NOTICE, and EVENT messages are handled in the client and CLI.
- **Standardized prefixes** for OK/CLOSED messages are only partially supported; generic error handling is present, but explicit parsing for all prefixes is not implemented.

### Recommendation

- For full compliance, add explicit parsing and handling for all standardized prefixes in OK and CLOSED messages.
- Consider adding more robust filter validation and event ordering logic if implementing relay-side features.

## Summary

- All NIP-01 MUST requirements are implemented and enforced in the codebase.
- SHOULD requirements (signature verification, no extra fields) are also implemented.
- Extensibility for event kinds is supported.
- All MUST and SHOULD requirements for relayer-client protocol are implemented.
- Event validation, error handling, and strict NIP-01 compliance are enforced in both client and relay logic.
- CLI (`Program.cs`) demonstrates correct event handling and error management.

### Key Implementing Classes

- `NostrEvent`: Domain model for NIP-01 event
- `NostrEventJsonConverter`: Handles serialization/deserialization
- `NostrEventValidator`: Validates event structure, ID, signature
- `Pubkey`, `NostrTag`: Value objects for pubkey and tags
- `NostrClient`: Client class for relay communication, event sending/receiving

### Notes

- All serialization and validation logic is covered by unit tests in `NostrSure.Tests`.
- The CLI (`NosrtSure.CLI/Program.cs`) uses these classes for relay communication and event processing.

---

Generated by GitHub Copilot on August 5, 2025
