using Ikon.App.Platform.Validation.Protocol;

namespace Ikon.App.Platform.Validation.Protocol
{
    public sealed partial class ValidationProfile
    {
        // v1 named the display name Nickname. The generated ApplyTeleportLoad dispatches here for
        // any payload stored before version 2; the retired bag carries the old value, typed. The
        // generator emits one such call per version gate, so bumping `version` in
        // ValidationState.tp without writing the matching UpgradeFrom is a compile error.
        private static void UpgradeFrom1(ValidationProfile value, ValidationProfile.RetiredFields? retiredFields)
        {
            if (string.IsNullOrEmpty(value.DisplayName))
            {
                value.DisplayName = retiredFields?.Nickname ?? "";
            }
        }
    }
}

// Validation tab exercising schema-versioned persisted state: a PersistentSessionReactive whose
// value type is a data .tp (ValidationState.tp) rather than a plain record, so the stored payload
// carries the schema version and the .tp compat contract applies on load. See the
// "Schema-versioned state" section of the persistent-state guide.
public partial class Validation
{
    private readonly PersistentSessionReactive<ValidationProfile> _versionedProfile = new(new ValidationProfile());

    // Per-client so concurrent sessions don't trample each other's in-progress edit.
    private readonly ClientReactive<string> _versionedProfileNameDraft = new("");

    private void RenderVersionedStateSection(UIView view)
    {
        ValidationProfile profile = _versionedProfile.Value;

        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-1"], "Schema-versioned persisted state");
                view.Text([Text.BodySm, "text-tertiary mb-2"],
                    "This profile persists through a PersistentSessionReactive whose value type is a data .tp schema (ValidationState.tp, version 2) instead of a plain record. The payload is stored inside a version envelope, so renamed fields survive: v1 payloads carrying the retired Nickname key migrate onto DisplayName on load via UpgradeFrom1.");
                view.Text([Text.Caption, "text-muted-foreground"],
                    "Restart the app to see the values reload from storage. Old builds saving over this data cannot destroy fields a newer schema added, and a payload stored by a newer schema version is passed through untouched.");
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Current profile");

                view.Row([Layout.Row.InlineCenter, "mb-2 flex-wrap"], content: view =>
                {
                    view.Text([Text.BodyStrong, "w-40"], "DisplayName");
                    view.Text([Text.Body], profile.DisplayName.Length > 0 ? profile.DisplayName : "(empty)");
                });

                view.Row([Layout.Row.InlineCenter, "mb-2 flex-wrap"], content: view =>
                {
                    view.Text([Text.BodyStrong, "w-40"], "VisitCount");
                    view.Text([Text.Body], profile.VisitCount.ToString());
                });

                view.Row([Layout.Row.InlineCenter, "flex-wrap"], content: view =>
                {
                    view.Text([Text.BodyStrong, "w-40"], "FavoriteColors");
                    view.Text([Text.Body], string.Join(", ", profile.FavoriteColors));
                });
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Update");

                view.Row([Layout.Row.InlineCenter, "mb-3 flex-wrap"], content: view =>
                {
                    view.Text([Text.BodyStrong, "w-40"], "New display name");
                    view.TextField(
                        [Input.Default, "w-64"],
                        bind: _versionedProfileNameDraft,
                        placeholder: "Type a name…");
                });

                view.Row([Layout.Row.Md, "flex-wrap mb-3"], content: view =>
                {
                    view.Button([Button.PrimaryMd],
                        text: "Set display name",
                        disabled: _versionedProfileNameDraft.Value.Trim().Length == 0,
                        onClick: async () =>
                        {
                            // Reactives notify on assignment, so state changes replace the value
                            // instead of mutating the instance in place.
                            ValidationProfile current = _versionedProfile.Value;
                            _versionedProfile.Value = new ValidationProfile
                            {
                                DisplayName = _versionedProfileNameDraft.Value.Trim(),
                                VisitCount = current.VisitCount,
                                FavoriteColors = current.FavoriteColors,
                            };
                            _versionedProfileNameDraft.Value = "";
                        });

                    view.Button([Button.OutlineMd],
                        text: "Increment VisitCount",
                        onClick: async () =>
                        {
                            ValidationProfile current = _versionedProfile.Value;
                            _versionedProfile.Value = new ValidationProfile
                            {
                                DisplayName = current.DisplayName,
                                VisitCount = current.VisitCount + 1,
                                FavoriteColors = current.FavoriteColors,
                            };
                        });
                });

                view.Text([Text.Caption, "text-muted-foreground"],
                    "State persists via a v2 .tp schema; v1 payloads carrying Nickname migrate on load, and the retired value stays readable through GetRetiredFields() during the sunset window.");
            });
        });
    }
}
