# Profiles and Roles

## Profiles and Roles

The signed-in person behind a client session has a platform profile — name, email, phone, birth
date, language, address, and the roles they hold. `ClientProfiles` reads and writes it; an app does
not store its own copy. Hold it as a field, like `Audio` — it subscribes to client join and leave to
keep its cache warm, so constructing one per call would miss those:
`private ClientProfiles Profiles { get; } = new(app);`

```csharp
public async Task<string> GreetAsync(Context clientContext)
{
    var profile = await Profiles.GetProfileAsync(clientContext);

    if (profile is null)
    {
        return "Welcome, guest";
    }

    return profile.HasRole(UserRole.Admin)
        ? $"Welcome back, {profile.VisibleName} (admin)"
        : $"Welcome back, {profile.VisibleName}";
}
```

`GetProfileAsync` takes either the `Context` of a connected client or a bare `userId`, and returns
null when the context carries no user (a guest) or the backend has no such profile. A connected
client's profile is cached at join, so the common lookup does not hit the backend; `RefreshProfileAsync`
drops that entry and `ClearCache` drops all of them. `ClientProfile` also computes `VisibleName` —
`PreferredName ?? FirstName ?? ""` — which is what to render rather than assembling a name yourself.
Structured location lives in a `ProfileAddress` (`Street`, `City`, `Zip`, `State`, `Municipality`,
`Country`).

Writing goes through `UpdateAsync` with a `ProfileData` mutator. **Only the properties you assign are
sent**, and assigning null is an assignment: it clears the field rather than leaving it alone.

```csharp
public async Task SetPreferredNameAsync(Context clientContext, string preferredName)
{
    // Only PreferredName is sent; every other field is left as it was.
    await Profiles.UpdateAsync(clientContext, data => data.PreferredName = preferredName);
}
```

`FindProfilesAsync(filters)` and `GetAllProfilesAsync()` are the space-wide queries, both capped by
`maxResults` (1000 by default).

### Roles

`UserRole` is `Guest`, `User`, `Moderator`, `Admin` — note that `Guest` maps to the `"anonymous"`
role string on the wire, not `"guest"`. `profile.HasRole(role)` asks, and `profile.RequireRole(role)`
throws `RoleRequiredException` (carrying `RequiredRole` and `UserId`) when the caller lacks it, which
is the shape to use inside a handler. `AddRoleAsync`, `RemoveRoleAsync` and `SetRolesAsync` take
either the enum or a raw role string, so an app can define roles beyond the four.

For a function exposed over the protocol, prefer the declarative `[RequireRole("admin")]` — it is
checked before your code runs. `RequireRole` is for the paths an attribute cannot cover.

### Custom Attributes

Anything the platform profile does not model goes in a typed attribute bag: declare a class
implementing `IProfileAttributes`, then `GetAttributesAsync<T>` / `SetAttributesAsync`. The same bag
is reachable from a loaded profile with `GetAttributes<T>()`, and a single value with
`GetAttribute(key)`.

```csharp
public sealed class GameAttributes : IProfileAttributes
{
    public int HighScore { get; set; }
    public string FavouriteTrack { get; set; } = "";
}
```

```csharp
public async Task RecordScoreAsync(Context clientContext, int score)
{
    var attributes = await Profiles.GetAttributesAsync<GameAttributes>(clientContext)
        ?? new GameAttributes();

    if (score > attributes.HighScore)
    {
        attributes.HighScore = score;
        await Profiles.SetAttributesAsync(clientContext, attributes);
    }
}
```

### Delegated Access

`MintedUserToken` is a short-lived token minted for one named `Resource`, with the `ExpiresAt` it
stops working at — for handing a client or a third party scoped access without sharing a
long-lived credential.
