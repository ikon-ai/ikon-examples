# Ikon.Teleport Public API

namespace Ikon.Teleport
  // One element read from a Teleport array: the decoded Value with typed As* accessors.
  readonly ref struct TeleportArrayElement
    bool IsNull { get; }
    TeleportType Type { get; }
    TeleportValue Value { get; }
    // Requires the wire Type to be exactly TeleportType.Array; check Type before calling.
    TeleportArrayReader AsArray()
    // Requires the wire Type to be exactly TeleportType.Binary; check Type before calling.
    ReadOnlySpan<byte> AsBinary()
    // Requires the wire Type to be exactly TeleportType.Bool; check Type before calling.
    bool AsBool()
    // Requires the wire Type to be exactly TeleportType.Dict; check Type before calling.
    TeleportDictReader AsDictionary()
    // Requires the wire Type to be exactly TeleportType.Float32 — no integer or float widening is performed, so check Type before calling.
    float AsFloat32()
    // Requires the wire Type to be exactly TeleportType.Float64 — no integer or float widening is performed, so check Type before calling.
    double AsFloat64()
    // Requires the wire Type to be exactly TeleportType.Guid; check Type before calling.
    Guid AsGuid()
    // Requires the wire Type to be exactly TeleportType.Int32 — no integer or float widening is performed, so check Type before calling.
    int AsInt32()
    // Requires the wire Type to be exactly TeleportType.Int64 — no integer or float widening is performed, so check Type before calling.
    long AsInt64()
    // Requires the wire Type to be exactly TeleportType.Object; check Type before calling.
    TeleportObjectReader AsObject()
    // Requires the wire Type to be exactly TeleportType.String; check Type before calling. Validates the payload as UTF-8 and throws TeleportException (TeleportError.InvalidUtf8) on malformed bytes, so it can throw even when Type is TeleportType.String; use AsUtf8 for raw, never-throwing access.
    string AsString()
    // Requires the wire Type to be exactly TeleportType.UInt32 — no integer or float widening is performed, so check Type before calling.
    uint AsUInt32()
    // Requires the wire Type to be exactly TeleportType.UInt64 — no integer or float widening is performed, so check Type before calling.
    ulong AsUInt64()
    // Requires the wire Type to be exactly TeleportType.String; check Type before calling.
    ReadOnlySpan<byte> AsUtf8()
  // Forward-only reader over the elements of one Teleport array.
  ref struct TeleportArrayReader
    uint Count { get; }
    TeleportType ElementType { get; }
    // parentDepth: Top-level callers pass 0. It exists only to propagate the anti-recursion depth counter when hand-walking nested containers; the reader-returning As*() accessors thread it automatically, so most callers never set it.
    static TeleportArrayReader Create(ReadOnlySpan<byte> data, int parentDepth = 0)
    // Attempts a zero-copy view of the whole array as a span of T. On success the reader is advanced past the end and cannot be re-read. Returns false — without consuming anything — for three distinct reasons that all look alike to the caller: any element has already been read from this reader (this accessor only works from the start); T does not map to the array's ElementType; or the element type is not fixed-size. The returned span is MemoryMarshal.Cast<TFrom, TTo> over the little-endian wire bytes, so it is only valid on a little-endian host — on a big-endian host the reinterpreted values are silently byte-swapped. Prefer the per-element accessors when in doubt.
    bool TryGetUnmanagedSpan<T>(out ReadOnlySpan<T> span) where T : unmanaged
    bool TryReadElement(out TeleportArrayElement element)
  // Serializes instances of the type as their ToString() string; deserializes via the (string) constructor. The wire sees a plain string field, so adding or removing the attribute on a type whose string form is unchanged is wire-compatible.
  sealed class TeleportAsStringAttribute : Attribute
    ctor()
  // Serializes every instance property with both a getter and a setter (init counts); a get-only property is silently dropped, and TeleportIgnoreAttribute excludes one explicitly. A field is matched on read by the hash of its property name, so renaming a property silently breaks the wire in both directions (the field is skipped and left at its default) unless the id is pinned with TeleportFieldAttribute; adding or removing a property is safe.
  sealed class TeleportAttribute : Attribute
    // Marks the type for Teleport serialization.
    // version: The schema version to stamp into serialized instances. Defaults to 1.
    ctor(uint version = 1)
    // Not a compatibility gate — readers accept any version and match fields by id, so this is only a marker for the application's own migration logic and need not be bumped when fields change.
    uint Version { get; }
  // One key/value pair read from a Teleport dictionary.
  readonly ref struct TeleportDictEntry
    TeleportValue Key { get; }
    TeleportValue Value { get; }
  // Forward-only reader over the entries of one Teleport dictionary.
  ref struct TeleportDictReader
    uint Count { get; }
    TeleportType KeyType { get; }
    TeleportType ValueType { get; }
    // parentDepth: Top-level callers pass 0. It exists only to propagate the anti-recursion depth counter when hand-walking nested containers; the reader-returning As*() accessors thread it automatically, so most callers never set it.
    static TeleportDictReader Create(ReadOnlySpan<byte> data, int parentDepth = 0)
    bool TryReadEntry(out TeleportDictEntry entry)
  // Why a Teleport payload could not be decoded, carried on TeleportException. Every one of these means the bytes are malformed or truncated — none of them is raised by a mere schema difference between two peers, which Teleport absorbs silently (an unknown field is skipped, a missing one stays at its default).
  enum TeleportError
    // The payload ended in the middle of a value — truncated or under-sized data.
    Underflow
    // A structural marker byte was not one the format defines.
    BadMarker
    // A value was read as one TeleportType but is tagged as another.
    BadType
    // A length or count prefix is unusable — negative, overlong, or past the end of the payload.
    InvalidLength
    // A string's bytes are not valid UTF-8.
    InvalidUtf8
    // An array's element count, element type, or extent does not hold together.
    ArrayMalformed
    // A dictionary's entry count or key-value pairing does not hold together.
    DictMalformed
    // Nesting exceeded the format's depth limit — a guard against hostile payloads that would otherwise recurse without bound.
    DepthOverflow
  // Thrown when a Teleport payload cannot be decoded. Error says which way it was malformed. A schema difference between two peers does not raise this — see TeleportError.
  sealed class TeleportException : Exception
    // Creates an exception for a decoding failure.
    // error: Which decoding failure occurred.
    // message: A description of the failure.
    ctor(TeleportError error, string message)
    // Creates an exception for a decoding failure caused by an underlying exception.
    // error: Which decoding failure occurred.
    // message: A description of the failure.
    // innerException: The underlying exception.
    ctor(TeleportError error, string message, Exception innerException)
    // The decoding failure this exception reports.
    TeleportError Error { get; }
  // One field read from a Teleport object: the FieldId plus the decoded Value with typed As* accessors.
  readonly ref struct TeleportField
    uint FieldId { get; }
    bool IsNull { get; }
    TeleportType Type { get; }
    TeleportValue Value { get; }
    // Requires the wire Type to be exactly TeleportType.Array; check Type before calling.
    TeleportArrayReader AsArray()
    // Requires the wire Type to be exactly TeleportType.Binary; check Type before calling.
    ReadOnlySpan<byte> AsBinary()
    // Requires the wire Type to be exactly TeleportType.Bool; check Type before calling.
    bool AsBool()
    // Requires the wire Type to be exactly TeleportType.Dict; check Type before calling.
    TeleportDictReader AsDictionary()
    // Requires the wire Type to be exactly TeleportType.Float32 — no integer or float widening is performed, so check Type before calling.
    float AsFloat32()
    // Requires the wire Type to be exactly TeleportType.Float64 — no integer or float widening is performed, so check Type before calling.
    double AsFloat64()
    // Requires the wire Type to be exactly TeleportType.Guid; check Type before calling.
    Guid AsGuid()
    // Requires the wire Type to be exactly TeleportType.Int32 — no integer or float widening is performed, so check Type before calling.
    int AsInt32()
    // Requires the wire Type to be exactly TeleportType.Int64 — no integer or float widening is performed, so check Type before calling.
    long AsInt64()
    // Requires the wire Type to be exactly TeleportType.Object; check Type before calling.
    TeleportObjectReader AsObject()
    // Requires the wire Type to be exactly TeleportType.String; check Type before calling. Validates the payload as UTF-8 and throws TeleportException (TeleportError.InvalidUtf8) on malformed bytes, so it can throw even when Type is TeleportType.String; use AsUtf8 for raw, never-throwing access.
    string AsString()
    // Requires the wire Type to be exactly TeleportType.UInt32 — no integer or float widening is performed, so check Type before calling.
    uint AsUInt32()
    // Requires the wire Type to be exactly TeleportType.UInt64 — no integer or float widening is performed, so check Type before calling.
    ulong AsUInt64()
    // Requires the wire Type to be exactly TeleportType.String; check Type before calling.
    ReadOnlySpan<byte> AsUtf8()
  // Overrides the default field id (the hash of the property's own name) so the C# property can be renamed without changing the wire id. [TeleportField("originalName")] hashes the given name instead; [TeleportField(id)] uses the id verbatim. The id must be unique within the type.
  sealed class TeleportFieldAttribute : Attribute
    // Derives the field id by hashing name instead of the property's own name, keeping the wire id stable across renames of the C# property.
    // name: The name to hash — the property's original name, i.e. the name the id already on the wire was derived from.
    ctor(string name)
    // Uses explicitId as the field id verbatim, with no hashing, decoupling the wire id from every name.
    // explicitId: The field id. Must be unique within the type.
    ctor(uint explicitId)
    // The field id used verbatim, or null when the id is derived by hashing Name (or, absent this attribute, the property's own name).
    uint? ExplicitId { get; }
    // The name hashed into the field id in place of the property's own name, or null when the attribute was constructed with an explicit id.
    string? Name { get; }
  // Derives Teleport field ids from field names. This is the function that makes a property rename a wire-breaking change (see TeleportFieldAttribute): the id a field is written and looked up by is the hash of its name, so a new name means a new id.
  static class TeleportHasher
    // The id is the xxHash32 of the name's UTF-8 bytes — identical across processes, builds, and every SDK language — so ids may be precomputed and pinned.
    // fieldName: The field name to hash.
    // throws ArgumentNullException: Thrown when the field name is null.
    static uint ComputeFieldId(string fieldName)
  // Excludes the property from the wire entirely; on read it keeps whatever the constructor or initializer set. Adding or removing this attribute is a silent, non-erroring change — older peers skip an added field or fall back to the default for a removed one.
  sealed class TeleportIgnoreAttribute : Attribute
    ctor()
  sealed class TeleportJsonIrDocument
    // Parses a Teleport IR schema document from JSON. The expected shape is a single root message object:
    // {
    //   "type": "MessageName",
    //   "fields": [ { "name": "fieldName", "id": "0x1A", "type": "int32" }, ... ],
    //   "nested": [ { "type": "NestedMessage", "fields": [...] }, ... ],
    //   "enums":  [ { "name": "EnumName" }, ... ]
    // }
    // type (the message name) is required; fields, nested and enums are optional arrays. Each field needs name, an id as a hex string (with or without a 0x prefix), and a type expression. The type grammar is: a primitive (int32, uint32, int64, uint64, float32, float64, bool, string, binary, guid); T[] for an array of T; {K:V} for a dictionary; or a bare name referencing another message or enum.
    // throws ArgumentException: The input is null, empty, or whitespace.
    // throws JsonException: The input is not well-formed JSON.
    // throws InvalidOperationException: The JSON is well-formed but violates the IR shape or type grammar (missing type/field name/id/type, or an unparseable type expression).
    static TeleportJsonIrDocument Parse(string json)
  static class TeleportJsonMirror
    static string ToJson(ReadOnlySpan<byte> binary, TeleportJsonMirrorOptions? options = null)
    // Renders the Teleport payload to JSON using a schema IR document to resolve field names. The no-schema rendering is ToJson.
    static string ToJsonWithSchema(ReadOnlySpan<byte> binary, TeleportJsonIrDocument? schema)
  sealed class TeleportJsonMirrorOptions
    ctor()
    bool Indented { get; set; }
    TeleportJsonIrDocument? Schema { get; set; }
  // Forward-only reader over the fields of one Teleport object.
  ref struct TeleportObjectReader
    uint Version { get; }
    static TeleportObjectReader Create(ReadOnlySpan<byte> data)
    bool TryReadField(out TeleportField field)
  // A serialized Teleport payload backed by a pooled array. Memory/Span are views over that array and are valid ONLY until IDisposable.Dispose — disposing returns the array to the pool, where the next serialization overwrites it, so a captured ReadOnlyMemory read after disposal yields another message's bytes with no exception. Dispose with using and consume the payload within that scope; for bytes that must outlive it, copy via ToArray().
  sealed class TeleportSerializedBuffer : IDisposable
    ReadOnlyMemory<byte> Memory { get; }
    ReadOnlySpan<byte> Span { get; }
    void Dispose()
    byte[] ToArray()
  // Entry point for reading and writing Teleport's binary format: Serialize turns a TeleportAttribute-marked object into bytes, Deserialize turns those bytes back into an object. Types register themselves — the source generator emits a Register<T> call in each [Teleport] type's static constructor, which this class runs on first use. So marking a type with [Teleport] is all that is needed; Register<T> is only for hand-written writers and readers. Serializing a type that never registered throws InvalidOperationException.
  static class TeleportSerializer
    static T Deserialize<T>(ReadOnlySpan<byte> data)
    static object Deserialize(Type type, ReadOnlySpan<byte> data)
    // Registers a writer and reader for T. Generated code calls this; hand write it only for a type whose encoding cannot be generated. The first registration for a type wins — a later one is ignored.
    // version: The schema version stamped into every serialized instance (see TeleportAttribute.Version).
    // writer: Writes the value's fields into an open object scope.
    // reader: Reads a value back from an encoded object.
    static void Register<T>(uint version, TeleportSerializer.TeleportWriteDelegate<T> writer, TeleportSerializer.TeleportReadDelegate<T> reader)
    // Serializes value to Teleport bytes, resolving the codec from the STATIC type T. Passing a base-typed reference to a derived value (BaseMsg m = derived; Serialize(m)) writes only the base type's fields — call Serialize, which resolves from the runtime type, when the static type may be a base of the actual value.
    static byte[] Serialize<T>(T value)
    static byte[] Serialize(object value)
    // The returned TeleportSerializedBuffer aliases a pooled array: its Memory/Span are valid only until it is disposed, and disposing returns the array to the pool where the next serialization overwrites it. Dispose it with using and consume the bytes before disposal, or call its ToArray() for a copy that outlives it.
    static TeleportSerializedBuffer SerializeToBuffer<T>(T value)
    static TeleportSerializedBuffer SerializeToBuffer(object value)
  // Reads a value back from a Teleport-encoded object.
  delegate TeleportSerializer.TeleportReadDelegate<T>
    T TeleportReadDelegate<T>(ReadOnlySpan<byte> data)
  // Writes a value's fields into an open Teleport object scope.
  delegate TeleportSerializer.TeleportWriteDelegate<T>
    void TeleportWriteDelegate<T>(TeleportWriter.TeleportObjectScope scope, T value)
  // The type tag that precedes every value in the Teleport binary format, identifying how the bytes that follow are encoded. Reading a value as the wrong type raises TeleportError.BadType. The numeric values are part of the wire format and are shared by every SDK — see docs/public/teleport-binary-spec.md.
  enum TeleportType
    // No value.
    Null
    // A boolean.
    Bool
    // A signed 32-bit integer.
    Int32
    // A signed 64-bit integer.
    Int64
    // An unsigned 32-bit integer.
    UInt32
    // An unsigned 64-bit integer.
    UInt64
    // A 32-bit IEEE-754 float.
    Float32
    // A 64-bit IEEE-754 float.
    Float64
    // An ordered sequence of values sharing one element type.
    Array
    // A map of key-value pairs.
    Dict
    // A versioned object: a header followed by its fields, each keyed by a field id.
    Object
    // A UTF-8 string.
    String
    // An opaque byte blob.
    Binary
    // A 16-byte GUID.
    Guid
  // A single decoded Teleport value viewed over the source buffer, exposing its wire Type and typed As* accessors.
  readonly ref struct TeleportValue
    bool IsNull { get; }
    TeleportType Type { get; }
    // Requires the wire Type to be exactly TeleportType.Array; check Type before calling.
    TeleportArrayReader AsArray()
    // Requires the wire Type to be exactly TeleportType.Binary; check Type before calling.
    ReadOnlySpan<byte> AsBinary()
    // Requires the wire Type to be exactly TeleportType.Bool; check Type before calling.
    bool AsBool()
    // Requires the wire Type to be exactly TeleportType.Dict; check Type before calling.
    TeleportDictReader AsDictionary()
    // Requires the wire Type to be exactly TeleportType.Float32 — no integer or float widening is performed, so check Type before calling.
    float AsFloat32()
    // Requires the wire Type to be exactly TeleportType.Float64 — no integer or float widening is performed, so check Type before calling.
    double AsFloat64()
    // Requires the wire Type to be exactly TeleportType.Guid; check Type before calling.
    Guid AsGuid()
    // Requires the wire Type to be exactly TeleportType.Int32 — no integer or float widening is performed, so check Type before calling.
    int AsInt32()
    // Requires the wire Type to be exactly TeleportType.Int64 — no integer or float widening is performed, so check Type before calling.
    long AsInt64()
    // Requires the wire Type to be exactly TeleportType.Object; check Type before calling.
    TeleportObjectReader AsObject()
    // Requires the wire Type to be exactly TeleportType.String; check Type before calling. Unlike AsUtf8, this validates the payload as UTF-8 and throws TeleportException (TeleportError.InvalidUtf8) on malformed bytes — so it can throw even when Type is correctly TeleportType.String. Use AsUtf8 for raw, lossless, never-throwing access.
    string AsString()
    // Requires the wire Type to be exactly TeleportType.UInt32 — no integer or float widening is performed, so check Type before calling.
    uint AsUInt32()
    // Requires the wire Type to be exactly TeleportType.UInt64 — no integer or float widening is performed, so check Type before calling.
    ulong AsUInt64()
    // Requires the wire Type to be exactly TeleportType.String; check Type before calling.
    ReadOnlySpan<byte> AsUtf8()
  sealed class TeleportWriter : IDisposable
    // initialCapacity: Starting capacity of the pooled backing buffer. A value of zero or less is silently clamped to 256 rather than rejected — unlike TeleportWriterPool.#ctor, which throws for a non-positive capacity.
    ctor(int initialCapacity = 256)
    // The completed payload as a span over the pooled backing array. The span is valid ONLY until the next Reset or IDisposable.Dispose — either returns or reshapes that array, so a captured span then reads different bytes with no exception. Consume it before resetting the writer; for bytes that must outlive the writer, copy via ToArray.
    ReadOnlySpan<byte> WrittenSpan { get; }
    TeleportWriter.TeleportObjectScope BeginObject(uint version = 1)
    void Dispose()
    // Reserves backing-buffer capacity BEFORE writing begins. This is a pre-write-only reservation: it throws InvalidOperationException if the root scope is already in progress (BeginObject has been called and not yet completed) or has completed — call Reset first in the latter case. A minimumCapacity of 0 is a no-op. It cannot grow the buffer mid-write; growth during writing happens automatically.
    void EnsureCapacity(int minimumCapacity)
    void Reset(int? minimumCapacity = null)
    byte[] ToArray()
  // Writer scope for the elements of one Teleport array. Write elements in order through the Write*/Begin* methods; disposing completes the array and records its element count.
  ref struct TeleportWriter.TeleportArrayScope
    TeleportWriter.TeleportArrayScope BeginArrayElement(TeleportType childElementType)
    TeleportWriter.TeleportDictScope BeginDictionaryElement(TeleportType keyType, TeleportType valueType)
    TeleportWriter.TeleportObjectScope BeginObjectElement(uint version = 1)
    void Dispose()
    void WriteBinary(ReadOnlySpan<byte> value)
    void WriteBool(bool value)
    void WriteFloat32(float value)
    void WriteFloat64(double value)
    void WriteGuid(Guid value)
    void WriteInt32(int value)
    void WriteInt64(long value)
    void WriteNull()
    void WriteSpan<T>(ReadOnlySpan<T> values) where T : unmanaged
    void WriteString(ReadOnlySpan<byte> utf8)
    void WriteString(string value)
    void WriteUInt32(uint value)
    void WriteUInt64(ulong value)
  // Writer scope for the entries of one Teleport dictionary. Write each entry through BeginEntry; disposing completes the dictionary and records its entry count.
  ref struct TeleportWriter.TeleportDictScope
    TeleportWriter.TeleportDictScope.EntryScope BeginEntry()
    void Dispose()
  // Writer scope for a single dictionary entry: write the key with one WriteKey* method and the value with one WriteValue*/BeginValue* method, then dispose to complete the entry.
  ref struct TeleportWriter.TeleportDictScope.EntryScope
    TeleportWriter.TeleportArrayScope BeginValueArray(TeleportType elementType)
    TeleportWriter.TeleportDictScope BeginValueDictionary(TeleportType entryKeyType, TeleportType entryValueType)
    TeleportWriter.TeleportObjectScope BeginValueObject(uint version = 1)
    void Dispose()
    void WriteKeyBinary(ReadOnlySpan<byte> data)
    void WriteKeyBool(bool data)
    void WriteKeyFloat32(float data)
    void WriteKeyFloat64(double data)
    void WriteKeyGuid(Guid data)
    void WriteKeyInt32(int data)
    void WriteKeyInt64(long data)
    void WriteKeyString(ReadOnlySpan<byte> utf8)
    void WriteKeyString(string text)
    void WriteKeyUInt32(uint data)
    void WriteKeyUInt64(ulong data)
    void WriteNullValue()
    void WriteValueBinary(ReadOnlySpan<byte> data)
    void WriteValueBool(bool data)
    void WriteValueFloat32(float data)
    void WriteValueFloat64(double data)
    void WriteValueGuid(Guid data)
    void WriteValueInt32(int data)
    void WriteValueInt64(long data)
    void WriteValueString(ReadOnlySpan<byte> utf8)
    void WriteValueString(string valueText)
    void WriteValueUInt32(uint data)
    void WriteValueUInt64(ulong data)
  // Writer scope for the fields of one Teleport object, created by TeleportWriter.BeginObject or a nested Begin*Field/Begin*Element call. Write fields through the Write*Field methods; disposing completes the object.
  ref struct TeleportWriter.TeleportObjectScope
    TeleportWriter.TeleportArrayScope BeginArrayField(uint fieldId, TeleportType elementType)
    TeleportWriter.TeleportDictScope BeginDictionaryField(uint fieldId, TeleportType keyType, TeleportType valueType)
    TeleportWriter.TeleportObjectScope BeginObjectField(uint fieldId, uint version = 1)
    void Dispose()
    void WriteBinaryField(uint fieldId, ReadOnlySpan<byte> value)
    void WriteBoolField(uint fieldId, bool value)
    void WriteFloat32Field(uint fieldId, float value)
    void WriteFloat64Field(uint fieldId, double value)
    void WriteGuidField(uint fieldId, Guid value)
    void WriteInt32Field(uint fieldId, int value)
    void WriteInt64Field(uint fieldId, long value)
    void WriteStringField(uint fieldId, string value)
    void WriteStringField(uint fieldId, ReadOnlySpan<byte> utf8)
    // Writes the raw Unsafe.SizeOf<T> bytes of value as an opaque TeleportType.Binary field. The payload is the host's in-memory struct layout — native byte order plus whatever padding and field ordering the runtime chose — so it is NOT cross-SDK portable: the C++, Rust, Dart and TypeScript readers cannot decode it as a struct, and even .NET readers on a differently-laid-out host may misread it. Use the explicit typed field writers (e.g. WriteInt32Field) for anything that crosses a process or SDK boundary; reserve this for a same-process, same-runtime round trip.
    void WriteStructField<T>(uint fieldId, in T value) where T : unmanaged
    void WriteUInt32Field(uint fieldId, uint value)
    void WriteUInt64Field(uint fieldId, ulong value)
  sealed class TeleportWriterPool : IDisposable
    ctor(int initialCapacity = 256)
    void Dispose()
    TeleportWriter Rent(int? minimumCapacity = null)
    void Return(TeleportWriter writer)
