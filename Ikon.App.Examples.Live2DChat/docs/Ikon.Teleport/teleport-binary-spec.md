# Teleport message binary specification

## Overview

Teleport is a schema-optional binary format for hierarchical data. It defines a single binary
encoding and a one-way JSON mirror for debugging. The runtime ships the binary→JSON direction only
(there is no JSON→binary decoder), and without a schema the JSON form loses field names (they become
8-hex-digit ids) and cannot be distinguished from strings for Binary/Guid values — see §5. Field
order is writer-defined, so two encodings of the same message are not guaranteed byte-identical (§6).

| Property    | Value                    |
|-------------|--------------------------|
| Extension   | `.tpx`                   |
| MIME Type   | `application/x-teleport` |
| Encoding    | Little-endian binary     |
| JSON Mirror | One-way (binary→JSON), for debugging |

### Design goals

- Minimal — one format, no optional features.
- Fixed little-endian layout. (Field order is writer-defined, not canonical — see §6.)
- Schema-optional — field names are optional; IDs are 32-bit hashes.
- Fast — flat, bounded layouts for zero-copy parsing.

---

## 1. Data Model

Teleport defines three container types and a fixed set of primitives.

| Category  | Form                                                                             | Meaning               |
|-----------|----------------------------------------------------------------------------------|-----------------------|
| Primitive | Null, Bool, Int32, Int64, UInt32, UInt64, Float32, Float64, String, Binary, Guid | atomic values         |
| Composite | Object, Array(T), Dict(K → T)                                                    | structured containers |

- Object — heterogeneous, versioned set of named fields.
- Array(T) — ordered sequence of homogeneous elements.
- Dict(K → T) — unordered map from primitive keys to arbitrary values.

---

## 2. Field Identity

Each object field is identified by a 32-bit unsigned integer:

```
fieldId = xxHash32(fieldName.UTF8, seed = 0)
```

- Field names are case-sensitive UTF-8.
- The xxHash32 seed is always 0 for deterministic cross-language behavior.
- When decoding binary → JSON:
  - If a name mapping exists, emit the name.
  - Otherwise, emit the 8-digit lowercase hex of fieldId (e.g. "5f1c9a6e").

Without a schema the emitted hex ids cannot be turned back into field names, so the JSON is a readable projection, not a lossless source the binary can be rebuilt from.

---

## 3. Binary Encoding

### 3.1 Type Codes

| Code | Type    | Payload        |
|------|---------|----------------|
| 0x01 | Null    | 0 bytes        |
| 0x02 | Bool    | 1 byte (0 / 1) |
| 0x03 | Int32   | 4 bytes        |
| 0x04 | Int64   | 8 bytes        |
| 0x05 | UInt32  | 4 bytes        |
| 0x06 | UInt64  | 8 bytes        |
| 0x07 | Float32 | 4 bytes        |
| 0x08 | Float64 | 8 bytes        |
| 0x09 | Array   | see § 3.4      |
| 0x0A | Dict    | see § 3.5      |
| 0x0B | Object  | nested object  |
| 0x0C | String  | UTF-8 bytes    |
| 0x0D | Binary  | raw bytes      |
| 0x0E | Guid    | 16 bytes       |

0x00 and 0x0F reserved for future use. (The type code is stored in the descriptor's high nibble, so only values 0x00–0x0F can appear on the wire.)

---

### 3.2 Object Layout

```
[objectLength:varuint]          // emitted whenever the object is nested; omit for the root envelope
0xA1                            // ObjectStart marker
[version:varuint]               // canonical unsigned LEB128
repeat fields until 0xA2:
  [fieldId:u32]
  [descriptor:u8]
  [length:varuint]              // only for variable-width payloads
  [payload]
0xA2                            // ObjectEnd marker
```

- `objectLength` counts every byte from the first `0xA1` through the closing `0xA2`, including the version varuint and every nested field blob.
- Unknown or future fields are skipped using the per-field length that precedes each variable-width payload, or the fixed size implied by the type code (see § 3.3).

### 3.3 Field Layout

```
fieldId:u32
descriptor:u8 = (type << 4) | flags
length:varuint                  // only for variable-width payloads
[payload]
```

- The descriptor's high nibble stores the type code. Its low nibble (`flags`) is reserved for future use and MUST be zero; writers always emit 0x0 and readers must reject non-zero values.
- Unknown fields are skipped using their recorded length (variable-width types) or the fixed size implied by their type code.
- All numeric values are little-endian.
- Fixed-size primitives omit `length` entirely.
- Variable-width field types are String, Binary, Array, Dict, and Object. Each emits the `length:varuint` before its payload so that readers can deterministically skip, copy, or buffer unknown data.
- The `length:varuint` counts every byte of the payload that follows. For object fields it spans the full `[0xA1 ... 0xA2]` blob (version + fields). For arrays and dictionaries it covers the nested descriptor, the element-count varuint, and all element bytes.
- Only the root object (top-level envelope) omits the leading `[objectLength]` shown in § 3.2.
- Strings and binaries are length-prefixed with canonical varuints (never null-terminated).

### 3.4 Array Payload

```
elementDescriptor:u8 = (elementType << 4) | elementFlags
count:varuint
repeat count times:
  [element payload]
```

Element payload encoding mirrors the field rules:

- Fixed-size primitives (ints, floats, bool, guid) — raw bytes, tightly packed.
- String/Binary — canonical varuint length followed by UTF-8/raw bytes.
- Object — canonical varuint byteLength followed by the exact object blob `[0xA1 | version | fields | 0xA2]`. `byteLength` counts every byte from the leading `0xA1` to the trailing `0xA2`, matching how writers reserve space for the size and patch it once the nested object completes.
- Array/Dict — nested descriptor + count + payload exactly as defined in their sections (no extra length, since their headers contain size information).
- `elementFlags` use the descriptor's low nibble and are reserved; they MUST be zero.

Arrays support up to 4 294 967 295 elements (bounded by payload size).

---

### 3.5 Dict Payload

```
keyDescriptor:u8   = (keyType << 4) | keyFlags      // primitive keys only
valueDescriptor:u8 = (valueType << 4) | valueFlags  // any Teleport type
count:varuint
repeat count times:
  [key payload for keyType]
  [value payload for valueType]
```

Rules:
- Keys must be unique and primitive.
- Values may be any type, including containers.
- `keyFlags` and `valueFlags` are reserved (low nibble) and MUST be zero.

Value payload encoding follows the same rules as arrays: fixed-size primitives are written inline, strings/binaries use canonical varuint lengths, and object values start with a canonical varuint byteLength followed by the `[0xA1 … 0xA2]` bytes of the nested object. The byteLength again spans the full object blob so that dictionary readers can slice out the correct number of bytes before handing it to the nested object parser.

---

## 4. String and Numeric Encoding

- String = UTF-8 bytes, length-prefixed by a canonical varuint length.
- Binary = raw bytes, same rule as string.
- Never null-terminated.

### Numeric Endianness and Sizes

| Type    | Bits | Encoding       | Example (LE)             |
|---------|------|----------------|--------------------------|
| Int32   | 32   | 2's complement | 78 56 34 12 → 0x12345678 |
| UInt32  | 32   | unsigned LE    | 78 56 34 12              |
| Int64   | 64   | 2's complement | EF CD AB 90 78 56 34 12  |
| UInt64  | 64   | unsigned LE    | EF CD AB 90 78 56 34 12  |
| Float32 | 32   | IEEE-754 LE    | standard                 |
| Float64 | 64   | IEEE-754 LE    | standard                 |

### Guid Encoding

- Fixed 16-byte payload: `A(4) | B(2) | C(2) | D(8)`.
- Segments A, B, and C are stored as little-endian UInt32, UInt16, and UInt16 respectively.
- Segment D is copied verbatim in the order listed in the textual GUID.

Example: `00112233-4455-6677-8899-aabbccddeeff` encodes as  
`33 22 11 00 55 44 77 66 88 99 aa bb cc dd ee ff`.

---

## 5. JSON Mirror

Binary → JSON mapping is direct. The runtime ships the binary→JSON direction only
(`TeleportJsonMirror.ToJson`); there is no JSON→binary entry point.

| Teleport type | JSON form                                              |
|---------------|--------------------------------------------------------|
| Object        | { "_v": 1, "Field": value, … }                         |
| Array         | [a, b, c]                                              |
| Dict          | { "key": value, … }                                    |
| Binary        | base64 string                                          |
| Guid          | "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" (lowercase hex) |
| Primitive     | native JSON types                                      |

Rules:
- Every object includes _v for version.
- Field order is semantically irrelevant.
- Dict keys are serialized as strings (canonical JSON of the key).
- Without a schema document, field names render as 8-hex-digit field ids, and Binary/Guid
  values render as plain strings indistinguishable from String fields — the JSON form is a
  debugging mirror, not a lossless round-trip format.
- null in JSON maps to type 0x01 (Null).

---

## 6. Determinism

1. Object field order is writer-defined: the runtime writer emits fields in call order and
   never sorts. Readers must accept any order, and byte-equality of two encodings of
   semantically equal messages is not guaranteed.
2. Dict key order non-semantic but preserved when writing.
3. All numeric values little-endian.
4. Maximum nesting depth = 128.
5. flags and reserved bytes must be 0.

---

## 7. Versioning & Compatibility

Each object begins with a canonical varuint version.
This enables forward/backward compatibility.

Forward: new readers handle old data (missing → defaults).
Backward: old readers skip unknown fields.

"Missing → defaults" is a requirement on the reader, not merely a consequence of version skew: a
writer may legitimately omit a field it has nothing to say about, and the schema key `sparse = true`
makes that the norm for a type. A decoder must therefore start from a fully defaulted instance and
apply only the fields present, never assume a field appears. Note that the default it lands on is the
one that implementation materializes — a language whose struct is zero-initialized does not
reconstruct a schema-declared non-zero default, which is why the schema spec restricts omission to
fields whose default is the zero value.

Example:

```
uint version = reader.BeginObject();   // read version
int timeout = version < 2 ? 1000 : 1500;
while (reader.ReadNextField(out id, out type, out len)) {
    // process known fields, skip unknown
}
reader.EndObject();
```

Evolution rules:
- Only add fields; never remove/rename.
- To rename: write both old + new for a deprecation window.
- Readers prefer new field IDs if both exist.

---

## 8. Example Read/Write

### 8.1 Write (Binary)

```
writer.BeginObject(1);
writer.WriteInt("Timeout", 1500);
writer.WriteBool("UseCache", true);

writer.BeginArray("Peers", ObjectType, 2);
  writer.BeginObjectElement(1);
    writer.WriteString("Host", "a");
    writer.WriteUInt("Port", 1234);
  writer.EndObjectElement();

  writer.BeginObjectElement(1);
    writer.WriteString("Host", "b");
    writer.WriteUInt("Port", 5678);
  writer.EndObjectElement();
writer.EndArray();

writer.EndObject();
```

### 8.2 Read (Binary)

```
uint v = reader.BeginObject(); // version 1
while (reader.ReadNextField(out id, out type, out len)) {
    if (id == Hash("Timeout")) timeout = reader.ReadInt32();
    else if (id == Hash("UseCache")) useCache = reader.ReadBool();
    else if (id == Hash("Peers")) {
        reader.BeginArray(out elemType, out count);
        for (int i = 0; i < count; i++) {
            reader.BeginObject();
            string host = reader.ReadStringField("Host");
            int port = reader.ReadUIntField("Port");
            reader.EndObject();
        }
        reader.EndArray();
    } else {
        reader.Skip(len);
    }
}
reader.EndObject();
```

### 8.3 Equivalent JSON

```json
{
  "_v": 1,
  "Timeout": 1500,
  "UseCache": true,
  "Peers": [
    { "_v": 1, "Host": "a", "Port": 1234 },
    { "_v": 1, "Host": "b", "Port": 5678 }
  ]
}
```

---

## 9. Error Handling

| Code                | Meaning                  |
|---------------------|--------------------------|
| ERR_UNDERFLOW       | not enough bytes         |
| ERR_BAD_MARKER      | missing 0xA1 / 0xA2      |
| ERR_BAD_TYPE        | unknown type code        |
| ERR_INVALID_LENGTH  | payload length mismatch  |
| ERR_DEPTH_OVERFLOW  | nesting too deep         |
| ERR_UTF8            | invalid UTF-8 sequence   |
| ERR_ARRAY_MALFORMED | bad array header/payload |
| ERR_DICT_MALFORMED  | bad dict structure       |

Readers must validate bounds, enforce max lengths and depth, and reject malformed UTF-8 or over-sized payloads.

---

## 10. Implementation Guidelines

- Precompute field IDs as static constants.
- Use contiguous growable buffers (double-capacity strategy).
- Readers: use span/slice cursors, no virtual calls on hot paths.
- Avoid per-field allocations; decode strings lazily.
- Keep migration logic local; avoid global schema registries.
- Object field counts are unbounded by the format itself; readers loop until the closing `0xA2` marker (subject only to the enclosing length field, which is a varuint capped at 4 294 967 295 bytes).
- Encode every varuint canonically (no redundant high-bit continuation).
- Provide bulk writers (e.g., WriteSpan<T>, WriteStruct<T>) for blittable data paths.
- Reject any non-zero low-nibble flags for forward-compat safety.

---

## 11. Example fieldId Fallback

When names are unknown:

```json
{ "_v": 1, "5f1c9a6e": 1500, "2f7a6b8c": true }
```

The runtime does not provide a JSON→binary decoder; this projection is for inspection, not round-tripping.

---

## 12. Byte Layout Summary

```
Object:
  [objectLength:varuint]    // emit only when nested; counts every byte of the [0xA1 ... 0xA2] blob
  0xA1
  [version:varuint]
  repeat fields until 0xA2:
    [fieldId:u32]
    [descriptor:u8]
    [length:varuint]        // only for variable-width payloads
    [payload]
  0xA2

Array:
  [elementDescriptor:u8]
  [count:varuint]
  repeat count times:
    [elementPayload]

Dict:
  [keyDescriptor:u8]
  [valueDescriptor:u8]
  [count:varuint]
  repeat count times:
    [keyPayload]
    [valuePayload]
```

---
