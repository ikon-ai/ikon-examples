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
  // One element read from a Teleport array: the decoded Value with typed As* accessors.
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
  sealed class TeleportAttribute : Attribute
    ctor(uint version = 1)
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
  // Writer scope for the entries of one Teleport dictionary. Write each entry through BeginEntry ; disposing completes the dictionary and records its entry count.
  ref struct TeleportWriter.TeleportDictScope
    TeleportWriter.TeleportDictScope.EntryScope BeginEntry()
    void Dispose()
  enum TeleportError
    Underflow
    BadMarker
    BadType
    InvalidLength
    InvalidUtf8
    ArrayMalformed
    DictMalformed
    DepthOverflow
  sealed class TeleportException : Exception
    ctor(TeleportError error, string message)
    ctor(TeleportError error, string message, Exception innerException)
    TeleportError Error { get; }
  // One field read from a Teleport object: the FieldId plus the decoded Value with typed As* accessors.
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
  sealed class TeleportFieldAttribute : Attribute
    ctor(string name)
    ctor(uint explicitId)
    uint? ExplicitId { get; }
    string? Name { get; }
  static class TeleportHasher
    static uint ComputeFieldId(string fieldName)
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
  // Writer scope for the fields of one Teleport object, created by BeginObject or a nested Begin*Field/Begin*Element call. Write fields through the Write*Field methods; disposing completes the object.
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
  delegate TeleportSerializer.TeleportReadDelegate<T>
    T TeleportReadDelegate<T>(ReadOnlySpan<byte> data)
  sealed class TeleportSerializedBuffer : IDisposable
    ReadOnlyMemory<byte> Memory { get; }
    ReadOnlySpan<byte> Span { get; }
    void Dispose()
    byte[] ToArray()
  static class TeleportSerializer
    static T Deserialize<T>(ReadOnlySpan<byte> data)
    static object Deserialize(Type type, ReadOnlySpan<byte> data)
    static void Register<T>(uint version, TeleportSerializer.TeleportWriteDelegate<T> writer, TeleportSerializer.TeleportReadDelegate<T> reader)
    static byte[] Serialize<T>(T value)
    static byte[] Serialize(object value)
    static TeleportSerializedBuffer SerializeToBuffer<T>(T value)
    static TeleportSerializedBuffer SerializeToBuffer(object value)
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
  // A single decoded Teleport value viewed over the source buffer, exposing its wire Type and typed As* accessors.
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
