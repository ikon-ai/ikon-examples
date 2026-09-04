# Ikon.Teleport Public API

namespace Ikon.Teleport
  // The As* accessors follow TeleportValue's contract: the wire Type must match exactly (no numeric widening; a mismatch throws TeleportError.BadType), and AsString validates UTF-8 while AsUtf8 never throws.
  readonly ref struct TeleportArrayElement
    bool IsNull { get; }
    TeleportType Type { get; }
    TeleportValue Value { get; }
    TeleportArrayReader AsArray()
    ReadOnlySpan<byte> AsBinary()
    bool AsBool()
    TeleportDictReader AsDictionary()
    float AsFloat32()
    double AsFloat64()
    Guid AsGuid()
    int AsInt32()
    long AsInt64()
    TeleportObjectReader AsObject()
    string AsString()
    uint AsUInt32()
    ulong AsUInt64()
    ReadOnlySpan<byte> AsUtf8()
  ref struct TeleportArrayReader
    uint Count { get; }
    TeleportType ElementType { get; }
    // parentDepth: Top-level callers pass 0. It exists only to propagate the anti-recursion depth counter when hand-walking nested containers; the reader-returning As*() accessors thread it automatically, so most callers never set it.
    static TeleportArrayReader Create(ReadOnlySpan<byte> data, int parentDepth = 0)
    // Attempts a zero-copy view of the whole array as a span of T. On success the reader is advanced past the end and cannot be re-read. Returns false — without consuming anything — for three look-alike reasons: an element has already been read (this only works from the start); T does not map to ElementType; or the element type is not fixed-size. The span reinterprets the little-endian wire bytes, so a big-endian host silently reads byte-swapped values; prefer the per-element accessors when in doubt.
    bool TryGetUnmanagedSpan<T>(out ReadOnlySpan<T> span) where T : unmanaged
    bool TryReadElement(out TeleportArrayElement element)
  // Serializes instances of the type as their ToString() string; deserializes via the (string) constructor. The wire sees a plain string field, so adding or removing the attribute on a type whose string form is unchanged is wire-compatible.
  sealed class TeleportAsStringAttribute : Attribute
    ctor()
  // Serializes every instance property with both a getter and a setter (init counts); a get-only property is silently dropped, and TeleportIgnoreAttribute excludes one explicitly. A field is matched on read by the hash of its property name, so renaming a property silently breaks the wire in both directions (the field is skipped and left at its default) unless the id is pinned with TeleportFieldAttribute; adding or removing a property is safe.
  sealed class TeleportAttribute : Attribute
    // version: The schema version to stamp into serialized instances. Defaults to 1.
    ctor(uint version = 1)
    // Not a compatibility gate — readers accept any version and match fields by id, so this is only a marker for the application's own migration logic and need not be bumped when fields change.
    uint Version { get; }
  readonly ref struct TeleportDictEntry
    TeleportValue Key { get; }
    TeleportValue Value { get; }
  ref struct TeleportDictReader
    uint Count { get; }
    TeleportType KeyType { get; }
    TeleportType ValueType { get; }
    // parentDepth: Top-level callers pass 0. It exists only to propagate the anti-recursion depth counter when hand-walking nested containers; the reader-returning As*() accessors thread it automatically, so most callers never set it.
    static TeleportDictReader Create(ReadOnlySpan<byte> data, int parentDepth = 0)
    bool TryReadEntry(out TeleportDictEntry entry)
  // Why a Teleport payload could not be decoded, carried on TeleportException. Every value means the bytes are malformed or truncated — none is raised by a schema difference between peers, which Teleport absorbs silently (an unknown field is skipped, a missing one stays at its default).
  enum TeleportError
    Underflow
    BadMarker
    BadType
    InvalidLength
    InvalidUtf8
    ArrayMalformed
    DictMalformed
    DepthOverflow
  // Thrown when a Teleport payload cannot be decoded — Error says which way it was malformed. A schema difference between two peers never raises this (see TeleportError).
  sealed class TeleportException : Exception
    // error: Which decoding failure occurred.
    // message: A description of the failure.
    ctor(TeleportError error, string message)
    // error: Which decoding failure occurred.
    // message: A description of the failure.
    ctor(TeleportError error, string message, Exception innerException)
    TeleportError Error { get; }
  // The As* accessors follow TeleportValue's contract: the wire Type must match exactly (no numeric widening; a mismatch throws TeleportError.BadType), and AsString validates UTF-8 while AsUtf8 never throws.
  readonly ref struct TeleportField
    uint FieldId { get; }
    bool IsNull { get; }
    TeleportType Type { get; }
    TeleportValue Value { get; }
    TeleportArrayReader AsArray()
    ReadOnlySpan<byte> AsBinary()
    bool AsBool()
    TeleportDictReader AsDictionary()
    float AsFloat32()
    double AsFloat64()
    Guid AsGuid()
    int AsInt32()
    long AsInt64()
    TeleportObjectReader AsObject()
    string AsString()
    uint AsUInt32()
    ulong AsUInt64()
    ReadOnlySpan<byte> AsUtf8()
  // Overrides the default field id (the hash of the property's own name) so the C# property can be renamed without changing the wire id. [TeleportField("originalName")] hashes the given name instead; [TeleportField(id)] uses the id verbatim. The id must be unique within the type.
  sealed class TeleportFieldAttribute : Attribute
    // name: The name to hash — the property's original name, i.e. the name the id already on the wire was derived from.
    ctor(string name)
    // explicitId: The field id. Must be unique within the type.
    ctor(uint explicitId)
    uint? ExplicitId { get; }
    string? Name { get; }
  // Excludes the property from the wire entirely; on read it keeps whatever the constructor or initializer set. Adding or removing this attribute is a silent, non-erroring change — older peers skip an added field or fall back to the default for a removed one.
  sealed class TeleportIgnoreAttribute : Attribute
    ctor()
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
  // Entry point for reading and writing Teleport's binary format. Types register themselves: the source generator emits a Register<T> call in each [Teleport] type's static constructor, which this class runs on first use — marking a type with [Teleport] is all that is needed, and Register<T> is only for hand-written writers and readers. Serializing a type that never registered throws InvalidOperationException.
  static class TeleportSerializer
    static T Deserialize<T>(ReadOnlySpan<byte> data)
    static object Deserialize(Type type, ReadOnlySpan<byte> data)
    // Generated code calls this; hand write it only for a type whose encoding cannot be generated. The first registration for a type wins — a later one is ignored.
    // version: The schema version stamped into every serialized instance (see TeleportAttribute.Version).
    // writer: Writes the value's fields into an open object scope.
    // reader: Reads a value back from an encoded object.
    static void Register<T>(uint version, TeleportSerializer.TeleportWriteDelegate<T> writer, TeleportSerializer.TeleportReadDelegate<T> reader)
    // Resolves the codec from the STATIC type T: passing a base-typed reference to a derived value (BaseMsg m = derived; Serialize(m)) writes only the base type's fields — call Serialize, which resolves from the runtime type, when the static type may be a base of the actual value.
    static byte[] Serialize<T>(T value)
    static byte[] Serialize(object value)
    static TeleportSerializedBuffer SerializeToBuffer<T>(T value)
    static TeleportSerializedBuffer SerializeToBuffer(object value)
  delegate TeleportSerializer.TeleportReadDelegate<T>
    T TeleportReadDelegate<T>(ReadOnlySpan<byte> data)
  delegate TeleportSerializer.TeleportWriteDelegate<T>
    void TeleportWriteDelegate<T>(TeleportWriter.TeleportObjectScope scope, T value)
  // The type tag that precedes every value in the Teleport binary format. The numeric values are part of the wire format and are shared by every SDK — see docs/public/teleport-binary-spec.md.
  enum TeleportType
    Null
    Bool
    Int32
    Int64
    UInt32
    UInt64
    Float32
    Float64
    Array
    Dict
    Object
    String
    Binary
    Guid
  // Every As* accessor requires the wire Type to match exactly — no integer or float widening — and throws TeleportException (TeleportError.BadType) on a mismatch, so check Type first. AsString additionally validates UTF-8 and throws on malformed bytes (TeleportError.InvalidUtf8); AsUtf8 is the raw, never-throwing alternative.
  readonly ref struct TeleportValue
    bool IsNull { get; }
    TeleportType Type { get; }
    TeleportArrayReader AsArray()
    ReadOnlySpan<byte> AsBinary()
    bool AsBool()
    TeleportDictReader AsDictionary()
    float AsFloat32()
    double AsFloat64()
    Guid AsGuid()
    int AsInt32()
    long AsInt64()
    TeleportObjectReader AsObject()
    string AsString()
    uint AsUInt32()
    ulong AsUInt64()
    ReadOnlySpan<byte> AsUtf8()
  sealed class TeleportWriter : IDisposable
    // initialCapacity: Starting capacity of the pooled backing buffer. A value of zero or less is silently clamped to 256 rather than rejected — unlike TeleportWriterPool.#ctor, which throws for a non-positive capacity.
    ctor(int initialCapacity = 256)
    // The completed payload as a span over the pooled backing array, valid ONLY until the next Reset or IDisposable.Dispose — either returns or reshapes that array, so a captured span then reads different bytes with no exception. For bytes that must outlive the writer, copy via ToArray.
    ReadOnlySpan<byte> WrittenSpan { get; }
    TeleportWriter.TeleportObjectScope BeginObject(uint version = 1)
    void Dispose()
    // Reserves backing-buffer capacity BEFORE writing begins: throws InvalidOperationException if the root scope is already in progress or has completed (call Reset first in the latter case). A minimumCapacity of 0 is a no-op. It cannot grow the buffer mid-write; growth during writing happens automatically.
    void EnsureCapacity(int minimumCapacity)
    void Reset(int? minimumCapacity = null)
    byte[] ToArray()
  // Write elements in order; disposing completes the array and records its element count.
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
  // Disposing completes the dictionary and records its entry count.
  ref struct TeleportWriter.TeleportDictScope
    TeleportWriter.TeleportDictScope.EntryScope BeginEntry()
    void Dispose()
  // Write the key with exactly one WriteKey* method and the value with one WriteValue*/BeginValue* method, then dispose to complete the entry.
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
  // Disposing completes the object.
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
    // Writes the raw Unsafe.SizeOf<T> bytes of value as an opaque TeleportType.Binary field. The payload is the host's in-memory struct layout, so it is NOT cross-SDK portable — the C++, Rust, Dart and TypeScript readers cannot decode it, and even .NET readers on a differently-laid-out host may misread it. Use the explicit typed field writers (e.g. WriteInt32Field) for anything that crosses a process or SDK boundary.
    void WriteStructField<T>(uint fieldId, in T value) where T : unmanaged
    void WriteUInt32Field(uint fieldId, uint value)
    void WriteUInt64Field(uint fieldId, ulong value)
