using Ikon.Teleport;

namespace Ikon.App.Patterns.Protocol;

// The last fences that were descriptions rather than code.
//
// A listing of member signatures reads correctly forever while being wrong — rename
// `ToTeleportBytes` and the Teleport spec still looks right. The same is true of "add this
// parameter, then wire it here": instructions compile nowhere, so nothing catches the day
// `CreateAction` changes shape. Each is now a call against a type this repo really generates.

file static class TeleportSpecExamples
{
    public static void TomlWriter()
    {
        #region example:teleport-toml-writer
        // AppProjectConfig is this repo's own toml-mode schema — the one behind ikon-config.toml.
        var config = new AppProjectConfig();
        string toml = config.ToToml();

        // extraLinesBySection appends raw lines: "" targets the root block, a section field name that
        // section, and any other key becomes a trailing [Key] block.
        string annotated = config.ToToml(new Dictionary<string, IReadOnlyList<string>>
        {
            [""] = ["# written by the deploy step"],
        });
        #endregion

        Log.Instance.Debug($"{toml.Length} {annotated.Length}");
    }

    public static void BinaryCodecs()
    {
        #region example:teleport-binary-codecs
        // PlayerProfile is a data schema; every data schema's root class gets these.
        var profile = new PlayerProfile { DisplayName = "Ada", Score = 12 };

        byte[] bytes = profile.ToTeleportBytes();
        PlayerProfile roundTripped = PlayerProfile.FromTeleportBytes(bytes);
        #endregion

        Log.Instance.Debug($"{roundTripped.DisplayName}");
    }

    public static void RetiredLedger(byte[] stored)
    {
        #region example:teleport-retired-ledger
        IReadOnlyList<string> names = PlayerProfile.RetiredKeys;   // the ledger's names

        var loaded = PlayerProfile.FromTeleportBytes(stored);
        PlayerProfile.RetiredFields? captured = loaded.GetRetiredFields();   // null when the payload carried none

        // Carry the bag across a clone the codec did not make, then populate before writing:
        // the copy replaces the whole bag, so anything set before it is lost.
        var next = new PlayerProfile { DisplayName = captured?.Nickname ?? loaded.DisplayName };
        next.CopyRetiredFieldsFrom(loaded);
        next.GetOrCreateRetiredFields().Nickname = captured?.Nickname;
        #endregion

        Log.Instance.Debug($"{names.Count} {next.DisplayName}");
    }
}

// The Teleport schema spec's section on hand-written C# types. The `.tp` compiler is not involved
// here — the attributes drive a source generator instead — so the example only compiles if that
// generator ran over this file, which is what pinning it is worth.

#region example:teleport-attribute-type
[Teleport]
public sealed partial class SavedLayout
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

public static class TeleportRuntimeExamples
{
    #region example:teleport-serializer-roundtrip
    public static SavedLayout RoundTrip(SavedLayout layout)
    {
        byte[] bytes = TeleportSerializer.Serialize(layout);
        return TeleportSerializer.Deserialize<SavedLayout>(bytes);
    }
    #endregion

    #region example:teleport-serialized-buffer
    public static void SendPooled(SavedLayout layout, Action<ReadOnlySpan<byte>> send)
    {
        // The array is returned to the pool on dispose, so the payload must be consumed in scope.
        using TeleportSerializedBuffer buffer = TeleportSerializer.SerializeToBuffer(layout);
        send(buffer.Span);
    }
    #endregion
}
