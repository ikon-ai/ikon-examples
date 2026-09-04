namespace Ikon.App.Platform.Validation.Protocol;

// The persisted-state guide's migration example, as the partial half a reader writes.
//
// Its twin is generated from schema/PlayerProfile.tp, which is why this can be pinned at all: the
// fence is one half of a partial class, and a half compiles nowhere without the other. The schema
// exists for this example, so the guide's `[obsolete] Nickname` ledger and the UpgradeFrom1 chain
// are the ones the Teleport compiler actually emits rather than a description of them.

#region docsnippet:persistent-schema-migration
public sealed partial class PlayerProfile
{
    static void UpgradeFrom1(PlayerProfile value, PlayerProfile.RetiredFields? retiredFields)
    {
        if (string.IsNullOrEmpty(value.DisplayName) && retiredFields?.Nickname is { } nickname)
        {
            value.DisplayName = nickname;
        }
    }
}
#endregion
