using Ikon.Teleport;

namespace Ikon.App.Patterns.Protocol;

// The Teleport binary spec's hand-driven reader and writer. These are the low-level scopes a codec is
// built from, so the example pins the call shapes the spec walks through byte by byte.

file static class TeleportBinarySpecExamples
{
    #region example:teleport-binary-write
    // A field id is the xxHash32 (seed 0) of the field name's UTF-8 bytes — the id generated code uses.
    private static readonly uint TimeoutId = TeleportHasher.ComputeFieldId("Timeout");
    private static readonly uint UseCacheId = TeleportHasher.ComputeFieldId("UseCache");
    private static readonly uint PeersId = TeleportHasher.ComputeFieldId("Peers");
    private static readonly uint HostId = TeleportHasher.ComputeFieldId("Host");
    private static readonly uint PortId = TeleportHasher.ComputeFieldId("Port");

    public static byte[] WriteConfig()
    {
        using var writer = new TeleportWriter();

        using (TeleportWriter.TeleportObjectScope config = writer.BeginObject(version: 1))
        {
            config.WriteInt32Field(TimeoutId, 1500);
            config.WriteBoolField(UseCacheId, true);

            // No element count is passed: disposing the array scope patches it into the header.
            using (TeleportWriter.TeleportArrayScope peers = config.BeginArrayField(PeersId, TeleportType.Object))
            {
                using (TeleportWriter.TeleportObjectScope peer = peers.BeginObjectElement(version: 1))
                {
                    peer.WriteStringField(HostId, "a");
                    peer.WriteUInt32Field(PortId, 1234);
                }

                using (TeleportWriter.TeleportObjectScope peer = peers.BeginObjectElement(version: 1))
                {
                    peer.WriteStringField(HostId, "b");
                    peer.WriteUInt32Field(PortId, 5678);
                }
            }
        }

        return writer.ToArray();
    }
    #endregion

    #region example:teleport-binary-versioning
    public static int ReadTimeout(ReadOnlySpan<byte> bytes)
    {
        TeleportObjectReader reader = TeleportObjectReader.Create(bytes);

        // Start from the default of the version that wrote the object; a field present overrides it.
        int timeout = reader.Version < 2 ? 1000 : 1500;

        while (reader.TryReadField(out TeleportField field))
        {
            if (field.FieldId == TimeoutId && field.Type == TeleportType.Int32)
            {
                timeout = field.AsInt32();
            }

            // Any other id, including one only a newer writer knows, needs no code: TryReadField has
            // already stepped past its payload.
        }

        return timeout;
    }
    #endregion

    #region example:teleport-binary-read
    public static (int Timeout, bool UseCache, List<(string Host, uint Port)> Peers) ReadConfig(ReadOnlySpan<byte> bytes)
    {
        TeleportObjectReader config = TeleportObjectReader.Create(bytes);   // config.Version == 1
        int timeout = 0;
        bool useCache = false;
        var peers = new List<(string Host, uint Port)>();

        while (config.TryReadField(out TeleportField field))
        {
            if (field.FieldId == TimeoutId)
            {
                timeout = field.AsInt32();
            }
            else if (field.FieldId == UseCacheId)
            {
                useCache = field.AsBool();
            }
            else if (field.FieldId == PeersId)
            {
                // The element type and count come from the array header (array.ElementType, array.Count).
                TeleportArrayReader array = field.AsArray();

                while (array.TryReadElement(out TeleportArrayElement element))
                {
                    TeleportObjectReader peer = element.AsObject();
                    string host = "";
                    uint port = 0;

                    while (peer.TryReadField(out TeleportField peerField))
                    {
                        if (peerField.FieldId == HostId)
                        {
                            host = peerField.AsString();
                        }
                        else if (peerField.FieldId == PortId)
                        {
                            port = peerField.AsUInt32();
                        }
                    }

                    peers.Add((host, port));
                }
            }

            // An unknown field id falls through here: its payload is already skipped.
        }

        return (timeout, useCache, peers);
    }
    #endregion
}
