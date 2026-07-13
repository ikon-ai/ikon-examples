# Ikon.Teleport Public API

namespace Ikon.Teleport
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
  // One element read from a Teleport array: the decoded TeleportArrayElement.Value with typed As* accessors.
  ref struct TeleportArrayElement
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
  // Forward-only reader over the elements of one Teleport array.
  ref struct TeleportArrayReader
    uint Count { get; }
    TeleportType ElementType { get; }
    static TeleportArrayReader Create(ReadOnlySpan<byte> data, int parentDepth = 0)
    bool TryGetUnmanagedSpan<T>(out ReadOnlySpan<T> span) where T : struct
    bool TryReadElement(out TeleportArrayElement element)
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
    void WriteSpan<T>(ReadOnlySpan<T> values) where T : struct
    void WriteString(ReadOnlySpan<byte> utf8)
    void WriteString(string value)
    void WriteUInt32(uint value)
    void WriteUInt64(ulong value)
  // Marks a class, record, or struct for Teleport serialization. A source generator then emits the type's binary writer and reader and registers them with TeleportSerializer, so TeleportSerializer.Serialize / Deserialize work on it and it can be nested as a field inside another [Teleport] type. Every instance property with both a getter and a setter (init counts) is serialized, unless it carries TeleportIgnoreAttribute. A get-only property is skipped — silently, so a property that was meant to be on the wire simply will not be. Wire compatibility. A field is found on read by its numeric field id, which is by default the hash of the property's name. Renaming a property therefore changes its id and breaks the wire in both directions, and it breaks quietly: the reader does not recognize the old id, skips the field, and leaves the property at its default value. Nothing throws. Use TeleportFieldAttribute to pin the id when a property must be renamed. Adding a new property or removing an unused one is safe — an unknown field is skipped, a missing field leaves the property at its default.
  sealed class TeleportAttribute : Attribute
    // Marks the type for Teleport serialization.
    ctor(uint version = 1)
    // The schema version stamped into every serialized instance of this type and readable from the object's header. Nothing in the runtime enforces or compares it — the generated readers accept any version and match fields purely by field id — so it is a marker for the application's own migration logic, not a compatibility gate. It does not need to be bumped when a field is added or removed; field-id matching already handles that.
    uint Version { get; }
  // One key/value pair read from a Teleport dictionary.
  ref struct TeleportDictEntry
    TeleportValue Key { get; }
    TeleportValue Value { get; }
  // Forward-only reader over the entries of one Teleport dictionary.
  ref struct TeleportDictReader
    uint Count { get; }
    TeleportType KeyType { get; }
    TeleportType ValueType { get; }
    static TeleportDictReader Create(ReadOnlySpan<byte> data, int parentDepth = 0)
    bool TryReadEntry(out TeleportDictEntry entry)
  // Writer scope for the entries of one Teleport dictionary. Write each entry through TeleportDictScope.BeginEntry; disposing completes the dictionary and records its entry count.
  ref struct TeleportWriter.TeleportDictScope
    TeleportWriter.TeleportDictScope.EntryScope BeginEntry()
    void Dispose()
  // Why a Teleport payload could not be decoded, carried on TeleportException. Every one of these means the bytes are malformed or truncated — none of them is raised by a mere schema difference between two peers, which Teleport absorbs silently (an unknown field is skipped, a missing one stays at its default).
  enum TeleportError
    Underflow
    BadMarker
    BadType
    InvalidLength
    InvalidUtf8
    ArrayMalformed
    DictMalformed
    DepthOverflow
  // Thrown when a Teleport payload cannot be decoded. TeleportException.Error says which way it was malformed. A schema difference between two peers does not raise this — see TeleportError.
  sealed class TeleportException : Exception
    // Creates an exception for a decoding failure.
    ctor(TeleportError error, string message)
    // Creates an exception for a decoding failure caused by an underlying exception.
    ctor(TeleportError error, string message, Exception innerException)
    // The decoding failure this exception reports.
    TeleportError Error { get; }
  // One field read from a Teleport object: the TeleportField.FieldId plus the decoded TeleportField.Value with typed As* accessors.
  ref struct TeleportField
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
  // Pins the wire identity of a property on a TeleportAttribute type. By default a property's field id is TeleportHasher.ComputeFieldId(propertyName) — a hash of the property's own name. That makes a rename a breaking wire change: the new name hashes to a new id, a peer on the old build no longer recognizes the field, and it silently leaves the value at its default. This attribute is how a property gets renamed without breaking the wire. Two forms, both of which keep the id stable across any number of future renames. [TeleportField("originalName")] hashes the given name instead of the property's name, so the C# property can be renamed freely while the wire keeps the id it always had. [TeleportField(0x1a2b3c4du)] uses the given id verbatim, skipping the hash entirely — for pinning an id captured from an existing payload, or reserving ids explicitly.
  sealed class TeleportFieldAttribute : Attribute
    // Derives the field id by hashing name instead of the property's own name, keeping the wire id stable across renames of the C# property.
    ctor(string name)
    // Uses explicitId as the field id verbatim, with no hashing, decoupling the wire id from every name.
    ctor(uint explicitId)
    // The field id used verbatim, or null when the id is derived by hashing TeleportFieldAttribute.Name (or, absent this attribute, the property's own name).
    uint? ExplicitId { get; }
    // The name hashed into the field id in place of the property's own name, or null when the attribute was constructed with an explicit id.
    string? Name { get; }
  // Derives Teleport field ids from field names. This is the function that makes a property rename a wire-breaking change (see TeleportFieldAttribute): the id a field is written and looked up by is the hash of its name, so a new name means a new id.
  static class TeleportHasher
    // Computes the wire field id for a field name: the xxHash32 of its UTF-8 bytes. Stable across processes, builds, and SDK languages — every Teleport implementation derives ids the same way.
    static uint ComputeFieldId(string fieldName)
  // Excludes a property of a TeleportAttribute type from serialization. The generator emits neither a write nor a read for it, so it never appears on the wire and, on the receiving side, is simply left at whatever value the constructor or initializer gave it. Use it for derived, cached, or local-only state that would otherwise be picked up automatically. Removing this attribute later gives the property a field id (see TeleportAttribute) and starts writing it, which older peers just skip; adding it to a property that was on the wire stops writing that field, and older peers fall back to its default. Both are silent — neither side errors.
  sealed class TeleportIgnoreAttribute : Attribute
    ctor()
  sealed class TeleportJsonIrDocument
    static TeleportJsonIrDocument Parse(string json)
  static class TeleportJsonMirror
    static string ToJson(ReadOnlySpan<byte> binary, TeleportJsonIrDocument? schema)
    static string ToJson(ReadOnlySpan<byte> binary, TeleportJsonMirrorOptions? options = null)
  sealed class TeleportJsonMirrorOptions
    ctor()
    bool Indented { get; set; }
    TeleportJsonIrDocument? Schema { get; set; }
  // Forward-only reader over the fields of one Teleport object.
  ref struct TeleportObjectReader
    uint Version { get; }
    static TeleportObjectReader Create(ReadOnlySpan<byte> data)
    bool TryReadField(out TeleportField field)
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
    void WriteStructField<T>(uint fieldId, ref T value) where T : struct
    void WriteUInt32Field(uint fieldId, uint value)
    void WriteUInt64Field(uint fieldId, ulong value)
  // Reads a value back from a Teleport-encoded object.
  delegate TeleportSerializer.TeleportReadDelegate<T>
    T TeleportReadDelegate<T>(ReadOnlySpan<byte> data)
  sealed class TeleportSerializedBuffer : IDisposable
    ReadOnlyMemory<byte> Memory { get; }
    ReadOnlySpan<byte> Span { get; }
    void Dispose()
    byte[] ToArray()
  // Entry point for reading and writing Teleport's binary format: Serialize turns a TeleportAttribute-marked object into bytes, Deserialize turns those bytes back into an object. Types register themselves — the source generator emits a TeleportSerializer.Register<T> call in each [Teleport] type's static constructor, which this class runs on first use. So marking a type with [Teleport] is all that is needed; TeleportSerializer.Register<T> is only for hand-written writers and readers. Serializing a type that never registered throws InvalidOperationException.
  static class TeleportSerializer
    static T Deserialize<T>(ReadOnlySpan<byte> data)
    static object Deserialize(Type type, ReadOnlySpan<byte> data)
    // Registers a writer and reader for T. Generated code calls this; hand write it only for a type whose encoding cannot be generated. The first registration for a type wins — a later one is ignored.
    static void Register<T>(uint version, TeleportSerializer.TeleportWriteDelegate<T> writer, TeleportSerializer.TeleportReadDelegate<T> reader)
    static byte[] Serialize<T>(T value)
    static byte[] Serialize(object value)
    static TeleportSerializedBuffer SerializeToBuffer<T>(T value)
    static TeleportSerializedBuffer SerializeToBuffer(object value)
  // The type tag that precedes every value in the Teleport binary format, identifying how the bytes that follow are encoded. Reading a value as the wrong type raises TeleportError.BadType. The numeric values are part of the wire format and are shared by every SDK — see docs/public/teleport-binary-spec.md.
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
  // A single decoded Teleport value viewed over the source buffer, exposing its wire TeleportValue.Type and typed As* accessors.
  ref struct TeleportValue
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
  // Writes a value's fields into an open Teleport object scope.
  delegate TeleportSerializer.TeleportWriteDelegate<T>
    void TeleportWriteDelegate<T>(TeleportWriter.TeleportObjectScope scope, T value)
  sealed class TeleportWriter : IDisposable
    ctor(int initialCapacity = 256)
    ReadOnlySpan<byte> WrittenSpan { get; }
    TeleportWriter.TeleportObjectScope BeginObject(uint version = 1)
    void Dispose()
    void EnsureCapacity(int minimumCapacity)
    void Reset(int? minimumCapacity = null)
    byte[] ToArray()
  sealed class TeleportWriterPool : IDisposable
    ctor(int initialCapacity = 256)
    void Dispose()
    TeleportWriter Rent(int? minimumCapacity = null)
    void Return(TeleportWriter writer)
