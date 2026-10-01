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
// carries the schema version and the .tp compat contract applies on load. v1 payloads carrying
// Nickname migrate on load, and the retired value stays readable through GetRetiredFields() during
// the sunset window. See the "Schema-versioned state" section of the persistent-state guide.
public partial class Validation
{
    private readonly PersistentSessionReactive<ValidationProfile> _versionedProfile = new(new ValidationProfile());

    // Per-client so concurrent sessions don't trample each other's in-progress edit.
    private readonly ClientReactive<string> _versionedProfileNameDraft = new("");

    private void RenderPersistentStateSection(UIView view)
    {
        ValidationProfile profile = _versionedProfile.Value;

        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "Persistent State");

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H3, "mb-4"], "Versioned profile");

                RenderFieldGrid(view,
                    ("Display name", v => v.Text([Text.Body], profile.DisplayName.Length > 0 ? profile.DisplayName : "(empty)")),
                    ("Visit count", v => v.Text([Text.Body], profile.VisitCount.ToString())),
                    ("Favorite colors", v => v.Text([Text.Body], profile.FavoriteColors.Count > 0 ? string.Join(", ", profile.FavoriteColors) : "(none)")));

                view.Row([Layout.Row.Md, "items-end flex-wrap mt-6"], content: view =>
                {
                    view.TextField(
                        [Input.Default, "w-64"],
                        bind: _versionedProfileNameDraft,
                        label: "Display name",
                        placeholder: "Type a name…");
                });

                view.Row([Layout.Row.Md, "flex-wrap mt-3"], content: view =>
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

                    view.Button([Button.PrimaryMd],
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
            });
        });
    }
}
