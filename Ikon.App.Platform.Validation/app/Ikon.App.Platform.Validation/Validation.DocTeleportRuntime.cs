using Ikon.Teleport;

namespace Ikon.App.Platform.Validation.Protocol;

// The Teleport schema spec's section on hand-written C# types. The `.tp` compiler is not involved
// here — the attributes drive a source generator instead — so the example only compiles if that
// generator ran over this file, which is what pinning it is worth.

#region docsnippet:teleport-attribute-type
[Teleport]
public sealed class SavedLayout
{
    // Pinned to the original name, so the C# property can be renamed without moving the field id.
    [TeleportField("PanelName")]
    public string Panel { get; set; } = "";

    public int Width { get; set; }

    // Recomputed on the receiving side, so it never goes on the wire.
    [TeleportIgnore]
    public bool IsWide { get; set; }
}
#endregion

public static class TeleportRuntimeDocs
{
    #region docsnippet:teleport-serializer-roundtrip
    public static SavedLayout RoundTrip(SavedLayout layout)
    {
        byte[] bytes = TeleportSerializer.Serialize(layout);
        return TeleportSerializer.Deserialize<SavedLayout>(bytes);
    }
    #endregion

    #region docsnippet:teleport-serialized-buffer
    public static void SendPooled(SavedLayout layout, Action<ReadOnlySpan<byte>> send)
    {
        // The array is returned to the pool on dispose, so the payload must be consumed in scope.
        using TeleportSerializedBuffer buffer = TeleportSerializer.SerializeToBuffer(layout);
        send(buffer.Span);
    }
    #endregion
}
