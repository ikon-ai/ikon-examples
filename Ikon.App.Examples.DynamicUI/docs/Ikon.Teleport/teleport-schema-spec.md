# Teleport message schema specification

## 1. Purpose

The Teleport message schema defines the compile-time structure and version evolution of a Teleport message type.
It is the schema used to generate code, not a runtime schema or reflection system.
Every Teleport file represents a message definition that compiles into strongly typed, deterministic code for multiple languages - C#, TypeScript, C++, etc.
Teleport schema aligns 1:1 with the Teleport binary format, ensuring that field ordering, identity, and layout produce identical bytes across languages and builds.

---

## 2. Core Goals

- Deterministic - same definition → same bytes.
- Cross-language identical - every compiler generates identical encoders/decoders.
- No runtime schema - the schema is purely compile-time.
- Compact - minimal syntax, no optional features.
- Composable - nested messages, arrays, and dicts supported.
- Versioned - explicit evolution between message versions.
- Schema-optional - field names map to 32-bit hash IDs at compile time.

---

## 3. File Structure

| Property               | Description                          |
|------------------------|--------------------------------------|
| Extension              | `.tp`                                |
| MIME Type              | `application/x-teleport-schema`      |
| Syntax                 | TOML 1.0                             |
| Runtime Representation | Binary `.tpx` (Teleport core format) |

Each file defines a single root message and may contain nested messages, enums, transforms, and constraints.

### Include Directive

A `.tp` file may include other `.tp` files using the `@include` directive. The directive must appear on its own line with a quoted path relative to the current file:

```toml
@include "shared/Common.tp"
```

The preprocessor inlines included content before TOML parsing. Circular includes are detected and rejected. Included files contribute their enum definitions to the current file's external enum resolution scope.

---

## 4. Top-Level Keys

| Key              | Required | Description                                                                              |
|------------------|----------|------------------------------------------------------------------------------------------|
| `type`           | optional | Message type. Required when defining fields/nested messages.                             |
| `namespace`      | optional | Root namespace applied to every code generator unless `[namespaces]` overrides it.       |
| `[namespaces]`   | optional | Code generator specific namespaces.                                                      |
| `version`        | optional | Integer version for message. Required when `type` is present.                            |
| `opcode`         | optional | Protocol opcode (int or string). Required when `type` is present, unless `data = true`.  |
| `data`           | optional | If `true`, the schema defines a pure data type instead of a wire message. See below.     |
| `unreliable`     | optional | If `true`, generated messages of this type get `MessageFlag.Unreliable` set by default.  |
| `sparse`         | optional | If `true`, writers omit fields holding the zero value instead of emitting them. See below.|
| `doc`            | optional | Comment or docstring                                                                     |
| `[fields]`       | optional | Field names and types. Only allowed when `type` is present.                              |
| `[nested.*]`     | optional | Nested subtypes                                                                          |
| `[enums.*]`      | optional | Enumerations. When `type` is omitted, these enums become namespace-level (global) types. |
| `[[transforms]]` | optional | Version upgrade logic                                                                    |
| `[constraints]`  | optional | Numeric/string constraints                                                               |
| `[obsolete]`     | optional | Ledger of removed fields: name → the type the field had when it was live. See section 5. |

### Data Schemas

A root message may declare `data = true` to define a pure data type — configuration files, manifests such as `ikon-bundle.json`, and other structures serialized by ordinary means (JSON, TOML) rather than the Teleport wire format:

```toml
type    = "AppBundleConfig"
version = 1
data    = true

[fields]
Name    = "string"
Version = "string"
```

Rules and generation behavior:

- `opcode` is forbidden — declaring both `data = true` and `opcode` fails the build. `version` remains required (it will drive config-version migration chains).
- Nested messages inherit data-ness from the root.
- C# emits `public sealed partial class` POCOs: properties with defaults as initializers, doc comments, nested classes, and enums — no `IProtocolMessagePayload`, no serializer registration, no opcode. The result serializes cleanly with System.Text.Json, and every class additionally carries Teleport payload codecs (see "Binary Codecs" below). A non-optional field of a nested type initializes to a fresh instance (`= new AuthConfig();`), so a default-constructed root is complete.
- TypeScript emits only `export interface` declarations and enums. Optional fields (`"T?"`) become true optional properties (`Field?: T`). No codecs, no opcode export, no opcode-registry participation.
- Dart, C++, and Rust generators emit nothing for data schemas — data-schema support is C#/TS-only for now.
- `string[]` fields may declare a default as a TOML array literal — `Methods = 'string[] = ["google", "email"]'` emits `new List<string> { "google", "email" }` in C#. List defaults are data-schema-only and string-element-only.
- Every `#` comment line above a field, a `[nested.X]` header, or an `[enums.X]` header is preserved verbatim as that member's doc lines. The XML doc summary is unchanged; the full line list feeds the TOML writer below.
- The generated type's public surface is deliberately minimal: its properties, `ToToml()` (toml mode), and `ToTeleportBytes`/`FromTeleportBytes` on the root class. Loader plumbing — the `ToToml` extras overload, `ReadRetired`, `RetiredKeys` — stays public for cross-assembly loaders but is hidden from IntelliSense and the API docs via `[EditorBrowsable(Never)]`, and section-class codecs are `internal`.

#### TOML Writer (`toml = true`)

A data schema may additionally declare `toml = true` (valid only together with `data = true`) when it describes a commented TOML config file such as `ikon-config.toml`. The generated C# root class then also carries:

```csharp
public string ToToml(IReadOnlyDictionary<string, IReadOnlyList<string>>? extraLinesBySection = null)
```

which serializes the instance to TOML with the schema's doc comments emitted as `#` comments: root fields first (one blank line between blocks, each preceded by its doc lines), then one `[FieldName]` section per nested-typed root field in schema order, written compactly with the nested type's doc lines above the header. `extraLinesBySection` appends raw lines per section — key `""` targets the root block, a section field name targets that section, and any other key becomes a trailing `[Key]` block.

The writer supports exactly the flat two-level shape of such configs, enforced at generation time: root fields are `string`, `bool`, `int32`, `int64`, `string[]`, or a non-optional nested type (a section); section fields are the same scalars/lists. Optional fields, enums, lists of nested types, and nesting below sections are rejected.

#### Binary Codecs

Every data schema gives every generated C# class — root and nested — Teleport payload codecs without any wire-message machinery (a schema that declares `binary = true` fails the build: binary codecs are always generated for data schemas, so the key does not exist):

```csharp
public void WriteToTeleport(TeleportWriter.TeleportObjectScope scope)
public static X ReadFromTeleport(ReadOnlySpan<byte> data)
public byte[] ToTeleportBytes()
public static X FromTeleportBytes(ReadOnlySpan<byte> data)
```

`ToTeleportBytes`/`FromTeleportBytes` wrap a standalone root object — no opcode, no `ProtocolMessage` attribute, no serializer registration, no version consts (nested object scopes inline the schema version), and no reset path: reads always populate a fresh instance. Field ids are the same xxHash32-of-name computation the wire generator uses, so the binary form of a config is an ordinary Teleport object.

Because the TOML writer and the binary codecs hang off the same object, a `toml = true` schema gives a lossless TOML ↔ binary conversion path for free: `FromToml(...)` → `ToTeleportBytes()` → `FromTeleportBytes(...)` → `ToToml()` reproduces the file — handy for debugging payloads and for shipping configs compactly.

Restrictions: external type references (`Foo:type`) are rejected in data schemas at generation time — their codecs and versions live in other schemas this generator cannot see. Binary codecs are C#-only; TypeScript data emission is unchanged.

### Unreliable Transport Default

A root message may declare `unreliable = true` at the top level to mark every wire message of that type as unreliable by default. The Ikon core server routes such messages through unreliable transports (UDP datagram channel or WebRTC SCTP data channel) when the recipient has one, falling back to the reliable channel otherwise. This removes the need for every call site to remember to set the flag manually when constructing the message.

```toml
type = "VoiceFrame"
version = 1
opcode = "VOICE_FRAME"
unreliable = true
```

Caller-supplied flags OR-merge with this default — adding `SendBackToSender` at a call site does not suppress the schema-declared `Unreliable` bit. The key is only valid when `type` is present.

---

### Sparse Payloads (`sparse = true`)

A root message may declare `sparse = true` to have every generated **writer** skip a field that holds the zero value rather than emit an empty one. Framing a field costs a 4-byte id, a type byte and (for variable types) a length before any content, so a message that is mostly empty for a given caller pays for a great deal of nothing. It is worth turning on where the payload is size-sensitive — something carried in a URL, or sent per connection.

```toml
type = "ConnectToken"
version = 4
opcode = "NONE"
sparse = true
```

Readers need no change and none was made: every generated reader starts from a defaulted instance and applies only the fields present, so an absent field simply keeps its starting value. This is what makes the key safe to turn on unilaterally — a sparse writer's output is decoded correctly by readers built before the flag existed, and a dense payload keeps decoding afterwards.

**Fields whose declared default is not the zero value are always written**, whatever they hold. This is not an optimization gap, it is the correctness boundary: the C++ reader value-initializes its struct and the Rust reader derives `Default`, so neither materializes a declared default. Omitting a `bool = true` would read back as `false` there — a silent wrong value rather than a decode failure. `Ikon.Teleport.CodeGen\TeleportSparse.cs` holds the rule, and it is deliberately conservative in the same direction: anything it cannot prove omittable is written, because being wrong that way costs bytes while being wrong the other way corrupts a value.

Optional fields are unaffected — they already write nothing when null. The key is only valid when `type` is present, and it is opt-in per type, so no existing message changes shape until its schema asks for it.

---

## 5. Field Definitions

### Example

```toml
type = "CacheConfig"
version = 1
opcode = "CACHE_CONFIG"
namespace = "Example.Namespace"

[namespaces]
csharp     = "Example.Namespace"
typescript = "Example.Namespace"
cpp        = "Example.Namespace"
dart       = "Example.Namespace"

[fields]
Description = "string"
Codec       = "AudioCodec"
SampleRate  = "int32"
Channels    = "int32"
BitDepth    = "int32 = 16"
```

`opcode` may be specified as an integer literal or as a string that names an opcode enum defined in the system.

### Namespaces

Set the root namespace with the top-level `namespace` field. This value is used by every code generator unless a language specific override is provided. Leaving `namespace` empty or omitting it entirely removes the namespace.

```toml
namespace = "Example.Namespace"

[namespaces]
csharp     = "Example.Namespace"  # C#
typescript = "Example.Namespace"  # TypeScript
cpp        = "Example.Namespace"  # C++
dart       = "Example.Namespace"  # Dart
rust       = "example_namespace"  # Rust
```

The `[namespaces]` table is optional and may contain only the `csharp`, `typescript`, `cpp`, `dart`, and `rust` keys. Set any of those entries to an empty string to suppress the namespace for that specific target while keeping it for the others.

### Allowed Field Type Forms

| Syntax                                                                                         | Meaning                                      |
|------------------------------------------------------------------------------------------------|----------------------------------------------|
| `int32`, `int64`, `uint32`, `uint64`, `float32`, `float64`, `bool`, `string`, `binary`, `guid` | Primitive Teleport types                     |
| `TypeName`                                                                                     | Reference to another defined message or enum |
| `TypeName[]`                                                                                   | Array of homogeneous elements                |
| `{K:V}`                                                                                        | Dictionary from key type K to value type V   |
| `string?`                                                                                      | Optional field                               |
| `int32 = 16`                                                                                   | Default value                                |
| `EnumType = Variant`                                                                           | Enum default                                 |
| `{string:User}`                                                                                | Dict of complex values                       |

### Field Identity

Each field's binary ID is:

```
fieldId = xxHash32(fieldName.UTF8, seed = 0)
```

This ensures reversible mapping between `.tp` and binary `.tpx` - identical to Teleport binary specification section 2.

Because identity is the name hash, the order fields appear in `[fields]` does not affect the wire
layout, and reordering is free to group a schema for readability. It is not free for the generated
**APIs**: the C# positional constructor takes its parameters in schema order, and the C++ struct is
a plain aggregate, so a reorder silently changes what a positional call means. Dart, TypeScript and
Rust construct by name and are unaffected. Before reordering, confirm nothing constructs the type
positionally.

---

### Removed Fields: the `[obsolete]` Ledger

`[obsolete]` is the ledger of fields that no longer exist. When a field is removed, its line moves
from `[fields]` to `[obsolete]`, declared as it was — a default suffix is accepted and ignored. One
rule, both wire and data schemas:

```toml
[fields]
RequireSignIn = "bool = false"

# The v1 auth surface, consumed by the v1 -> v2 upgrade (RequireSignIn = Enabled && !DeferLogin)
[obsolete]
Enabled = "bool"
DeferLogin = "bool"
```

A ledger entry does three things:

- **Keeps old data readable.** The C# reader recognizes the retired field id (the xxHash32 of the
  name — the same id the field always had) and decodes the value, typed, into a per-class
  `RetiredFields` bag instead of skipping it. Truly unknown ids still skip.
- **Feeds migrations.** The schema lists what existed; upgrade code decides what it means. A TOML
  config's `UpgradeFrom` step and a wire handler receiving an old peer's payload (detected via the
  message envelope version) both read the bag and map old values onto the current surface.
- **Reserves the name and its hash forever.** A new live field whose name (or hash) collides with a
  ledger entry fails generation, so a retired id can never be silently reused for different data.

Naming a field that is still declared in `[fields]` is an error — remove it from `[fields]`, or
delete the entry. There is no deprecation marker feature: while a field is still live, migration
guidance belongs in its `#` doc comment (which every generator carries into the generated code), and
removal is the point at which the line moves into the ledger. Nested scopes are addressed as
`[obsolete.NestedType]`, mirroring `[nested.X]`.

Entry values are the data-mode scalar set: `string`, `bool`, `int32`, `int64`, `float64` (alias
`double`), and `string[]`.

Generated C# per class with ledger entries:

```csharp
public static readonly IReadOnlyList<string> RetiredKeys;   // the ledger's names
public RetiredFields? GetRetiredFields();                    // what the last read captured, or null
public RetiredFields GetOrCreateRetiredFields();             // populate before writing
public void CopyRetiredFieldsFrom(T source);                 // carry the bag across a non-Teleport clone
public sealed partial class RetiredFields { public bool? Enabled { get; set; } ... }
```

Every bag member is nullable — absent means the source carried no value. `GetRetiredFields` is a
method rather than a property so TOML mapping and JSON serialization never treat the bag as data.
That invisibility cuts both ways: a clone made by any route other than Teleport — a JSON round trip,
a hand-written copy — silently arrives with an empty bag and stops emitting the retired fields, which
is what `CopyRetiredFieldsFrom` is for. Call it on the clone whenever an origin writer's outbound
value is a copy rather than the instance it populated.
Data schemas additionally get `public static RetiredFields ReadRetired(Func<string, object?>
valueLookup)`, a dependency-free typed extractor a TOML loader uses to fill the bag from the raw
parsed table (raw shapes are coerced; null or a wrong shape leaves the entry absent). Wire messages
are pooled, so `ResetTeleportState` clears the bag between reads.

The ledger is round-trippable. `WriteToTeleport` emits every populated bag member under its retired
id — the same hash the field had when it was live, so readers that still resolve the old name see
bytes identical to before the removal — and an absent bag (the normal case) emits nothing. This is
how a C# writer keeps sending a removed field during its sunset window
(`message.GetOrCreateRetiredFields().OldField = value;`), and it means captured retired values
survive a capture-modify-write cycle: what a read put in the bag goes back out on the next write.

Every SDK carries the same bag on wire messages, shaped to the language:

| Target     | Bag                                                   | Capture | Re-emit |
|------------|-------------------------------------------------------|---------|---------|
| C#         | private field, `GetRetiredFields()` / `GetOrCreate...` | yes     | yes     |
| TypeScript | `retiredFields?: {Name}RetiredFields`                  | yes     | yes     |
| C++        | `std::optional<RetiredFields> Retired`                 | yes     | yes     |
| Rust       | `retired_fields: Option<{Name}RetiredFields>`          | yes     | yes     |
| Dart       | `{Name}RetiredFields? retiredFields`                   | yes     | n/a     |

A schema with no `[obsolete]` section emits none of this in any target. Decode captures retired ids
into the bag instead of skipping; encode emits every set member under its original id, so a writer
in any of those languages can keep sending a removed field during its sunset window. An unset bag
emits nothing. Dart has no generated writers at all — its call sites hand-roll a
`TeleportObjectWriter` — so it captures on read and exposes `retiredFieldId{Name}` constants, but
has no write half to extend.

Naming is per-language: TypeScript rejects a field literally named `retiredFields`; C++ nests the
type as `RetiredFields` and names the member `Retired` (a member cannot share its nested type's
name); Dart and Rust prefix with the message name because neither nests. The JSON mirror of wire
messages does not carry retired keys in any target — the bag rides the binary side only, which is
why the Rust member is `#[serde(skip)]`.

## 6. Nested Messages

```toml
[nested.User]
Id     = "string"
Name   = "string"
Online = "bool = false"
```

- Defines a sub-message within the parent message.
- Nested messages share the parent's version unless explicitly versioned.

---

## 7. Enumerations

```toml
[enums.AudioCodec]
PCM16 = 0
FLAC  = 1
OPUS  = 2
```

- Enum values may be integer literals or quoted string literals.
- Every member within the same enum must use the same value kind (all integers or all strings).
- References appear as `AudioCodec` field types.
- Defaults: `AudioCodec = PCM16`.
- Numeric enums generate real enums in every target language.
- String enums generate TypeScript enums with string initializers, and `public static class` declarations with `const string` fields in C#.
- Fields that use string enums are serialized as strings on the wire while still exposing strongly typed constants in each target language.

```toml
[enums.UIElementLabels]
ChatMessage = "chat-message"
Disabled    = "disabled"
```

## 8. External Dependencies

When a Teleport document references enums or message types defined elsewhere, every occurrence MUST be annotated inline by suffixing the type name with `:enum` or `:type`. The suffix declares the dependency and removes the need for out-of-band declarations.

```toml
[fields]
ContextKind   = "Example.Namespace.ContextType:enum = Unknown"
Telemetry     = "SharedTelemetry:type"
TelemetryCopy = "SharedTelemetry:type"
Tags          = "{string:SharedTelemetry:type}"
Snapshots     = "SharedTelemetry:type[]"
```

- The suffix is required for **every** usage of an external symbol, including entries inside dictionaries or arrays. Apply the annotation to the base type before `[]` or after the key/value type inside `{}`.
- If the name includes dots before the suffix (e.g. `My.Namespace.TypeName:type`), everything before the final `.` is treated as the namespace, otherwise the current message namespace is assumed.
- A single symbol cannot be declared both as `:enum` and `:type` within the same file.

---

## 9. Version Transforms

Version transforms describe structural migrations between message versions.

### Short DSL

```toml
[[transforms]]
from = 2
to   = 3
steps = [
  "remove OldField",
  "rename sample_rate -> SampleRate",
  "map BitDepth = old.bit_depth ?? 16"
]
```

### Structured Form

```toml
[[transforms]]
from = 1
to   = 2

[[transforms.steps]]
rename = { from = "UserName", to = "Name" }

[[transforms.steps]]
remove = "ObsoleteFlag"
```

These are intended to compile into version-aware deserializers that automatically migrate older Teleport data streams.

> Status: `[[transforms]]` is reserved by the schema parser but the current code generators do not yet emit migration logic from it. Plan migrations manually in user code until this lands.

---

## 10. Constraints (Optional)

```toml
[constraints]
SampleRate.min = 8000
SampleRate.max = 192000
Channels.min   = 1
Channels.max   = 8
```

Used for static validation during compilation or generated code, never serialized.

> Status: `[constraints]` is reserved by the schema parser but the current code generators do not yet emit validation logic from it. Enforce bounds in user code until this lands.

---

## 11. Canonical Intermediate Representation (IR)

Compilers normalize each `.tp` file into this in-memory shape. A serialized example looks like:

```json
{
  "type": "AudioStreamBegin",
  "namespace": "Example.Namespace",
  "version": 3,
  "opcode": "0x00000001",
  "layoutHash": "0x50f602c5",
  "fields": [
    { "name": "Description", "type": "string", "id": "0x5193a16b" },
    { "name": "Codec", "type": "AudioCodec", "id": "0xc3c9400a" },
    { "name": "SampleRate", "type": "int32", "id": "0xf47d2c6e" },
    { "name": "Channels", "type": "int32", "id": "0x90edf947" },
    { "name": "BitDepth", "type": "int32", "id": "0xadb7a8a5", "default": 16 }
  ]
}
```

- `id` = xxHash32(fieldName.UTF8, seed = 0)
- `layoutHash` = hash of sorted field IDs + version.

---

## 12. Validation Rules

| Rule           | Description                                |
|----------------|--------------------------------------------|
| Field names    | `[A-Za-z_][A-Za-z0-9_]*`                   |
| Duplicates     | Forbidden per scope                        |
| Enum values    | Integers or strings (single kind per enum) |
| Version        | Must increase monotonically                |
| Transforms     | Must chain (vN → vN+1)                     |
| Layout hash    | Must be updated on edit                    |
| Non-zero flags | Invalid                                    |
| Depth >128     | Invalid                                    |
| `[obsolete]`   | Every key must name a field **not** declared in `[fields]`; the value is the removed field's scalar type (`string`, `bool`, `int32`, `int64`, `float64`/`double`, `string[]`); `[obsolete.X]` must name a declared nested type; retired names' hash ids must not collide with live field ids |

## 13. Compilation Workflow

```
.tp  →  Codegen  →  Generated source  →  Binary (Teleport)
```

### Example CLI

```bash
# Generate C# from one or more .tp files
ikon teleport generate --input ./messages/*.tp --type csharp --output ./generated

# Emit C++ headers for a specific schema
ikon teleport generate --input ./schemas/cache.tp --type cpp --output ./generated
```

The `ikon teleport generate` verb accepts `--type` values `csharp`, `typescript`, `cpp`, and `json-ir`. For Ikon AI apps, `ikon app teleport build` compiles every `schema/*.tp` file in the current app and emits C# for the host plus whichever frontend SDKs the app carries (TypeScript for `frontend-node/`, Dart for `frontend-flutter/`, Rust for `frontend-rust/`, C++ for `frontend-cpp/`).

### Language Targets

| Language    | Output Type            |
|-------------|------------------------|
| C#          | `sealed partial class` |
| TypeScript  | `interface`            |
| C++         | `struct`               |
| Dart        | `class`                |
| Rust        | `pub struct`           |

---

## 14. Example

```toml
# ChatRoom.tp
type      = "ChatRoom"
namespace = "Example.Namespace"
version   = 2
opcode    = 0x00020010
doc       = "Describes a chat room and its members."

[fields]
Id        = "string"
Title     = "string?"
Members   = "User[]"
CreatedAt = "uint64"
State     = "RoomState = Active"

[nested.User]
Id      = "string"
Name    = "string"
Online  = "bool = false"

[enums.RoomState]
Active   = 0
Archived = 1

[[transforms]]
from = 1
to   = 2
steps = [
  "rename UserName -> Name",
  "map State = old.IsArchived ? Archived : Active"
]

[constraints]
Members.max = 1024
```

### Enum-only Example

```toml
# AudioEnums.tp
namespace = "Example.Namespace"

[enums.AudioCodec]
Pcm16 = 0
Opus  = 1
Flac  = 2
```

The compiler emits these enums directly into the namespace without generating a wrapper class. No `type`, `version`, or `opcode` keys are required when a file only defines global enums.

---

## 15. Relationship to Teleport Core Format

| Aspect         | Teleport Binary        | Teleport schema               |
|----------------|------------------------|-------------------------------|
| Data model     | Objects, Arrays, Dicts | Fields, nested types, enums   |
| Field identity | 32-bit hash            | Defined implicitly            |
| Version        | varuint                | `version = n`                 |
| Encoding       | Canonical binary       | Deterministic schema          |
| JSON mirror    | Direct                 | Generated from schema         |
| Compatibility  | Skippable unknowns     | Transforms DSL                |
| Runtime        | None                   | None                          |
| Purpose        | Wire encoding          | Build-time layout definition  |

Together they form a closed, reversible system:
`.tp` (schema) → `.tpx` (binary) ↔ `.json` (mirror)

---
