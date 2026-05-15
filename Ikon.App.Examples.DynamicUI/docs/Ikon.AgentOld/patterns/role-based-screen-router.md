<!-- mined-from: MuistiSeniori -->
# Role-Based Screen Router — One Screen Enum, Per-Role Home + Nav

A single `AppScreen` enum lists every screen across all user roles (Admin, Nurse, Patient). A `HomeScreenForRole` switch maps the current user's `Role` to its starting screen, and the navbar conditionally renders different button rows based on `_currentUser.Value?.Role`. Top-level `RenderUI` is one big `switch (_screen.Value)` that delegates to per-screen `Render*` methods — no route table, no per-role app subclass.

## When to use

Multi-role apps where the role distinction is real but most plumbing (auth, profile, language toggle, footer) is shared. Keeps the entire app navigable by reading one enum and one switch. Stop using this when role-specific surfaces grow large enough that you need separate sub-apps.

## Snippet

```csharp
private static AppScreen HomeScreenForRole(UserRole role) => role switch
{
    UserRole.Admin => AppScreen.AdminHome,
    UserRole.Nurse => AppScreen.NurseHome,
    UserRole.Patient => AppScreen.PatientHome,
    _ => AppScreen.NoRole
};

private void RenderNavbar(UIView view)
{
    var role = _currentUser.Value?.Role;
    var homeScreen = role.HasValue ? HomeScreenForRole(role.Value) : AppScreen.Loading;

    view.Row(["h-14 items-center px-6 border-b border-gray-200 bg-white justify-between"], content: view =>
    {
        view.Row(["items-center gap-6"], content: view =>
        {
            view.Button(["text-2xl font-bold tracking-tight"],
                "MuistiSeniori",
                onClick: async () => Navigate(homeScreen));

            if (role == UserRole.Patient)
            {
                view.Button([], T("nav.physical"), onClick: async () => Navigate(AppScreen.PatientPhysicalList));
                view.Button([], T("nav.brain"), onClick: async () => Navigate(AppScreen.PatientGameCategories));
                view.Button([], T("nav.memory_book"), onClick: async () => Navigate(AppScreen.PatientMemoryBookSelect));
            }
            else if (role == UserRole.Nurse)
            {
                view.Button([], T("nav.patients"), onClick: async () => Navigate(AppScreen.NurseHome));
                view.Button([], T("nav.statistics"), onClick: async () => Navigate(AppScreen.NurseUnitStatistics));
            }
            else if (role == UserRole.Admin)
            {
                view.Button([], T("nav.activities"), onClick: async () => Navigate(AppScreen.AdminActivityList));
                view.Button([], T("nav.units"), onClick: async () => Navigate(AppScreen.AdminServiceUnitList));
            }
        });
    });
}

private void RenderUI(UIView view)
{
    switch (_screen.Value)
    {
        case AppScreen.AdminHome: RenderAdminHome(view); break;
        case AppScreen.NurseHome: RenderNurseHome(view); break;
        case AppScreen.PatientHome: RenderHome(view); break;
        case AppScreen.NoRole: RenderNoRole(view); break;
        // ... one case per screen
    }
}
```

## Notes

- Prefix screen names with the role (`AdminHome`, `NurseHome`, `PatientHome`) so the switch reads top-down per role and code review catches "leaked" cross-role navigation.
- Treat `AppScreen.NoRole` and `AppScreen.Loading` as first-class screens, not error states — every authenticated user lands on one of them on first paint while the role lookup runs.
- `Navigate(AppScreen.X)` is a one-liner that sets `_screen.Value`. Prefer that over scattered `_screen.Value = ...` so a future logger/breadcrumb hook has one funnel.
- The shared header/footer wraps the switch — putting it inside the switch duplicates layout per case.

## See also

- `state-machine-cards-and-transitions` — when transitions between screens follow a strict graph, not flat enum picking
- `bottom-tab-bar-nav` — mobile-style nav alternative
