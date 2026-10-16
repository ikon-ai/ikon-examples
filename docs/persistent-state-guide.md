<!-- checked-against: 1dfe904b73655e76aca02dc5 -->

# IKON Persistent State Guide

How to persist app state across restarts. Read this before reaching for files or hand-rolled storage.

## TL;DR — what to pick

**Records people create go in the database, written when they change.** Tasks, notes, documents,
orders, messages: anything a user would call "my data". Open the app's built-in Postgres database
with `app.DatabaseAsync()` and write each change before the UI says it is saved. A committed
database write is durable the moment it returns, and each row is written on its own, so two
instances that change different records do not overwrite each other.

A `Persistent…Reactive` is written within about two seconds of a change and again when the app
stops, so **a crash loses the changes of those last seconds.** Each value is stored whole and the
last writer wins. Use it for preferences, drafts, layout, view settings and small app state.

<!-- ikon-example: persistent-default -->
```csharp
// Preferences, drafts and small app state:
private readonly PersistentSessionReactive<MyState> _state = new(new MyState());
```

Then from any method:

<!-- ikon-example: persistent-read-write -->
```csharp
// Read and write like any reactive:
_state.Value = next;
var current = _state.Value;
```

**Renamed a field? Saved data does not follow a plain record.** A renamed property loses its stored value, and a removed or added one only drops or defaults that property; keep the stored name with `[JsonPropertyName("oldName")]`, or define the type as a Teleport data schema with a `version` and an `UpgradeFrom` step ("Schema-versioned state", below). Rows in a database table get no version from the platform: store a `version` beside the `jsonb` data. Moving or renaming the reactive's own field orphans its value unless it has a `key:`.

The framework loads from cloud storage before `Main()` runs, writes changes as the app runs, and saves on graceful shutdown. You need no file paths, no `Directory.CreateDirectory` and no hand-built AssetUri. Before telling a user a change is saved, `await app.SavePersistentStateAsync()` (see "Save semantics").

## Three scopes — pick by who shares the value

| Class | Same value seen by | When to pick |
|---|---|---|
| `PersistentReactive<T>` | All session identities, all users (app-wide) | App-wide config. Rare — most apps don't need this. |
| **`PersistentSessionReactive<T>`** | The same SessionIdentity (the app's routing key) | **Default.** One app instance = one bucket of state. |
| `PersistentUserReactive<T>` | The same UserId, across all of their sessions | State that follows a user (preferences, history, profile). |

Heuristic: if you can't articulate why it should be `Global` or `User`, it's `Session`. The session identity is what the app already declared as its routing key — using the same key for storage means your data partitions match how your app instances partition.

**Composer drafts and other "the user's words in progress" belong in USER scope** (`UserReactive<string>`, or `PersistentUserReactive<string>` to survive app restarts) — never `ClientReactive`. A page reload mints a NEW client session, so per-client state ends with it by design: a draft bound to `ClientReactive` is lost on every reload. User scope survives reloads and follows the user across tabs. Do NOT use browser storage (localStorage/sessionStorage in a custom component) to work around this; on this platform, drafts are kept in server state.

Each scope also has persistent collection variants — the durable counterparts of `ReactiveList<T>`, `ReactiveHashSet<T>` and `ReactiveDictionary<TKey, TValue>`. Use these instead of wrapping a collection in a `Persistent...Reactive<List<T>>`:

| Collection | App-wide | Per session identity | Per user |
|---|---|---|---|
| list | `PersistentReactiveList<T>` | `PersistentSessionReactiveList<T>` | `PersistentUserReactiveList<T>` |
| set | `PersistentReactiveHashSet<T>` | `PersistentSessionReactiveHashSet<T>` | `PersistentUserReactiveHashSet<T>` |
| dictionary | `PersistentReactiveDictionary<TKey, TValue>` | `PersistentSessionReactiveDictionary<TKey, TValue>` | `PersistentUserReactiveDictionary<TKey, TValue>` |

The user-scoped classes additionally expose per-user accessors usable outside an active user scope (background tasks): `ValueFor(userId)`, `SetFor(userId, value)`, and `UpdateFor(userId, ...)` on `PersistentUserReactive<T>`; the collection variants have equivalents like `AddFor` / `RemoveFor` / `ClearFor`.

## Starting values — pass them to the constructor

The initial value or items you pass to the constructor are what every user, session or app sees
until it has state of its own; a partition that was persisted keeps its own. That is the whole
seeding mechanism — there is no "seed if empty" step to write:

<!-- ikon-example: persistent-starting-values -->
```csharp
// Every new user starts with two example recipes; a returning user sees their own list.
private readonly PersistentUserReactiveList<Recipe> _recipes = new(
[
    new Recipe("Scrambled eggs", ["2 eggs", "butter"]),
    new Recipe("Toast", ["bread"]),
]);
```

Per-user state is partitioned by the `UserScope` that is active, and only UI callbacks and
rendering run inside one (a `Task.Run` started from a callback inherits its scope). `Main()`, the
constructor, `Task.Run` loops started from either, timers and endpoint handlers run with **no**
user active, so reading or adding to a user-scoped value there throws
(`Cannot read reactive variable '_recipes': it is partitioned per UserScope, and no UserScope is
active`). To touch one user's partition from such a place, capture the id where the scope exists
(`var userId = ReactiveScope.UserId;` inside the callback) and use the per-user accessors —
`ValueFor(userId)` / `SetFor(userId, …)` / `UpdateFor(userId, …)` on a `PersistentUserReactive<T>`,
`AddFor(userId, …)` (`SetFor(userId, key, value)` on the dictionary) / `RemoveFor` / `ClearFor` / `UpdateFor` on the collection variants. Session-scoped
and global values need no scope: the session is the app instance itself, so they read and write
like any other reactive from anywhere, `Main()` included.

## Backends — the default does the right thing

<!-- ikon-example: persistent-backends -->
```csharp
// Default — structured state lands in the app's built-in postgres database
private readonly PersistentSessionReactive<Prefs> _prefs = new(new Prefs());

// byte[] payloads stay on asset storage automatically — no backend parameter needed
private readonly PersistentSessionReactive<byte[]> _snapshot = new([]);

// Public asset URL needed (never sensitive data) — the URL serves the stored JSON wrapper,
// not the raw bytes, so it is no image src; serve media through app.Files.Public
private readonly PersistentSessionReactive<byte[]> _logo
    = new([], backend: PersistenceBackend.Public);

// Explicitly target a postgres DB of the app's own
private readonly PersistentSessionReactive<long> _counter
    = new(0, backend: PersistenceBackend.Postgres, postgresDatabase: "main");
```

Read the public URL from a method, once a save has happened:

<!-- ikon-example: persistent-public-url -->
```csharp
var url = _logo.PublicUrl;  // null until a value is stored; loaded before Main() once one is
```

Every app gets a built-in Postgres database named `app`. Nothing declares it and nothing has to
create it: the platform provisions it the first time the app asks for a database, and it is
quota-free. The default backend follows one rule: structured state goes to Postgres, and asset
storage is for binaries and public files:

- `Default` (what you get when you name no backend) — structured values (`T` is not `byte[]`) are
  stored as one row in the `ikon_reactive_storage` table of the app's state database: the
  built-in `app` database when the app has one, otherwise its only platform-created database.
  Persistence never provisions `app` itself — it exists once the app has asked for a database.
  `byte[]` payloads go to private asset storage. If the session has no state database (no database
  yet, several created ones and no `app`, plain local run), everything falls back to asset storage,
  with the same behavior. The app warns once only when `StateDatabase` in its `ikon-config`
  names a database it was not given.
- `Private` — S3-backed private cloud file, explicitly. Pick it only when a structured value must
  stay on asset storage despite the default.
- `Public` — asset storage with a public URL on `PublicUrl`: the URL of the stored JSON envelope
  (`application/json`) wrapping the value, not of the value's own bytes, so it is no image or file
  link — serve media through `app.Files.Public`. Don't use for anything sensitive.
- `Postgres` — a row in a postgres DB of the app's own, created with
  `ikon db create --name <name>`. If the app holds only one such database, omit
  `postgresDatabase`; with several, name the one you want. Omitted, the first postgres database is
  used, which can be the built-in `app`; a named database the app lacks logs one error and that
  value is never saved.

Existing data migrates by itself: when a structured value first loads from the app's state database and finds
no row, the old asset location is read and the value is copied into Postgres, so the next load hits
the row. The old asset blob is left in place. Apps already using a named database with
`backend: PersistenceBackend.Postgres` are untouched by all of this.

Every load and save logs its destination at debug level (member name, scope, and the postgres
database or asset path), so "where is my data?" is answered by the app's logs.

Backend and scope are chosen independently. Scope decides who sees the value; backend decides where it is stored.

### Database tiers

Every provisioned Postgres database lives on a tier — `shared-dense`, `shared`, or
`dedicated-small` — which decides how densely it is packed onto an instance and how many
connections it gets. `ikon db create` takes `--tier shared` or `--tier dedicated-small` and defaults to `shared`; the built-in `app` database starts on `shared-dense`.

A live database can move to another tier without redeploying:

```bash
ikon db tier dedicated-small
```

A database moves to `shared` or `dedicated-small`, the tiers `ikon db create` offers, so the
built-in database can leave `shared-dense` but none can move onto it. With several declared
databases, name the one to move with `--name <name>`. The platform
copies the data to an instance of the new tier, verifies it, and switches connections over —
the database keeps its name and credentials, but expect open connections to drop briefly while
the data moves (sessions reconnect automatically). Writes are refused while the data is copied;
reads keep working. `ikon db list` shows each database's tier and the state of an in-flight
migration.

A move that cannot finish is undone: the database stays on its current tier, unchanged and
writable again (unless it is over its storage quota), and `ikon db list` shows the move as
`failed` (it does not print why). Nothing needs cleaning up before you try again, to the same tier or
another. A database holding a single row too large to move — tens of megabytes as text on the
shared tiers — is refused before anything changes, as is one larger than the target tier's storage
(1 GB on `shared`, 10 GB on `dedicated-small`) or one with tables under row-level security or
extensions installed outside the `public` schema.

## The `key:` parameter — loops and refactor-proof state

<!-- ikon-example: persistent-dynamic-keys -->
```csharp
// WRONG — without a key, each iteration's stable id follows its position in the loop, not the
// camera: reorder the cameras or run the method again and every id shifts.
foreach (var camera in cameras)
{
    var baseline = new PersistentSessionReactive<byte[]>([]);  // id is the loop position
}

// RIGHT — explicit stable key derived from the dynamic identity.
foreach (var camera in cameras)
{
    var baseline = new PersistentSessionReactive<byte[]>(
        [],
        key: $"baseline:{camera.Id}");
}
```

Pass a stable identifier the app owns. Not `Guid.NewGuid()` — that changes on every restart and orphans the old data. Don't reuse the same key across different types or scopes.

Without `key:`, a field's stored value is found by its declaring source file's path under the app root, the constructing member's name, `T`, and its order among same-typed persistent reactives built in that member. Renaming or moving the file or field, or reordering such siblings, silently starts from the initial value and orphans the stored one. That is fine for state you can afford to lose; pass a fixed `key:`, unique within the app, for state that must survive refactoring, and always when constructing reactives in a loop or based on runtime identity. Either way, a stored value is loaded back only into a reactive that exists once the app instance is constructed (a field, or one built in the constructor); one constructed later — in `Main()` or a handler — is saved at shutdown but always starts from its initial value, so run a loop like the one above in the constructor.

## Save semantics

- **Load**: parallel for all persistent reactives that exist once the app instance is constructed, finishes before `Main()` runs. Your code sees persisted values from the start. If a startup load fails for any reason other than the stored payload's shape (store unreachable, stale database credentials), the app refuses to start; any other failed load logs an error, keeps the default, and skips saving that value for the run, so the stored value is kept until you fix the code. Such a load fails when the code cannot read the stored payload: the type changed, or the error says `stored state was captured as …` and names the type the value was saved as. Make the type readable again, or give the reactive a new `key:` to start it fresh. (User-scoped reactives load per user. The primary user's partition is preloaded before `Main()`. A connected user's partition loads before their first render and before `OnClientJoined` runs. If that takes over 10 s, the client renders and `OnClientJoined` runs with the initial values, and the stored values replace them when the load finishes. Any other user's partition loads the first time their scope is touched, and until it arrives `ValueFor` returns the initial value. Background code (a cron, a webhook) that reads a user's value to work out what to write should await `LoadForAsync(userId)` first. An `UpdateFor`, `AddFor` or other change made during that load is applied again to the stored value when it arrives; a plain `SetFor` made during it is replaced by the stored value, and a warning names the reactive.)
- **Autosave**: a persistent reactive assigned, or changed through its collection methods, is written within about two seconds, together with any other changes in that time. Only values whose stored payload changed are written. A failed write logs a warning and is retried, waiting longer after each failure, up to a minute. A value mutated in place (`_state.Value.Items.Add(x)` on a plain record) does not tell the reactive it changed; it is written by the next explicit save or at shutdown.
- **Explicit save**: `await app.SavePersistentStateAsync()` writes every value and completes when they are stored. That includes a value mutated in place, and a value this instance stored before and has not changed since, because another instance may have changed the stored copy. It throws the storage error when a write fails. Await it before a UI says "Saved". Set `app.AutosavePersistentState = false` in `Main` to write only on this call and at shutdown, for an app that wants to choose its save points or changes a value many times a second.
- **Save at shutdown**: parallel for all persistent reactives, on `StoppingAsync` (graceful shutdown). Locally, `ikon stop` asks the app to shut down and waits for it, so a run's changes are saved there too. It kills an app that has not exited within 30 seconds, or whose IKON libraries are too old to handle the request, and says so; that run's changes are then lost, as in a crash.
- **A local run shares the deployed app's state.** `ikon run` loads and saves persistent reactives in the space the app is linked to, through that space's databases or its asset storage, under the same keys as the deployed app in that space. Stopping a local run saves its values over the deployed app's, and the run logs this once at startup. The keys are the same wherever the app runs, because the platform's user-data erasure matches these keys. Once an app holds real data, test changes that write state with care.
- **A crash loses what was not yet written**: up to two seconds of changes with autosave on, everything since the last explicit save with it off. Records that must never be lost, or that several instances change at once, go in postgres directly through `app.DatabaseAsync()`.

## Erasing a user's state

`ClearFor(userId)` on any user-scoped persistent reactive erases that user's partition: the in-memory value is dropped (the next read sees the initial value) and the persisted copy is deleted from whichever store it routes to — including the legacy asset blob when the value lives in Postgres with read-through, so an erased value cannot resurrect on the next load. The deletion runs in the background; the user leaves the shutdown save immediately, so a cleared user is never re-persisted.

To erase everything the app has persisted about a user — a user-data-erasure (GDPR) request — use the app-level helper:

<!-- ikon-example: persistent-erase-user -->
```csharp
await app.EraseUserStateAsync(userId);
```

It applies `ClearFor` semantics to every registered user-scoped persistent reactive and additionally deletes every stored row under the user's storage scope key — including rows left behind by fields that no longer exist in the code — and completes only when the stored state is gone. Global- and session-scoped values are untouched: they are shared state, not per-user data. Each stored value is its own round trip to storage, so when a button starts the erasure, update the UI first and let the erasure finish in the background instead of making the person wait on the click.

## Schema-versioned state (`.tp` types)

**Keep a list in one root.** What follows covers a single-value `Persistent…Reactive<T>`, not the
list, dictionary and set forms, so a list whose shape will change goes in one `.tp` root as a
nested array (`Notes = "Note[]"` in a `NoteBook`), held in one `PersistentSessionReactive<NoteBook>`.
Rows in a database get no schema version from the platform: store each as `jsonb` with a version
field beside the data (`{"version": 2, "data": {…}}`) and translate older versions when you read a
row.

A plain record works fine — until you rename a property. The stored JSON still carries the old
name, the deserializer doesn't recognize it, and the value silently resets. If a persisted type is
worth evolving, define it as a Teleport data schema instead of a record and the `.tp` compat
contract comes with it:

```toml
# schema/PlayerProfile.tp
type = "PlayerProfile"
version = 2
data = true

[fields]
DisplayName = "string"
Score = "int32"

# Renamed to DisplayName in v2.
[obsolete]
Nickname = "string"
```

Bumping `version` obliges you to say what the old data means — the generated code calls a
migration you write in your partial class, so forgetting it is a compile error, not a data loss:

<!-- ikon-example: persistent-schema-migration -->
```csharp
// schema/PlayerProfile.tp generated the other half of this class in <Project>.Protocol (or the
// namespace its [namespaces] csharp line names, as here). A partial half must be in the same
// namespace, so this file is the one app file that declares a namespace.
namespace Ikon.App.Patterns.Protocol;

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
```

The migration's parameters follow the schema. With an `[obsolete]` ledger it is
`static void UpgradeFrom1(PlayerProfile value, PlayerProfile.RetiredFields? retiredFields)`, as
above; a schema without one has no `RetiredFields` type, and the method is
`static void UpgradeFrom1(PlayerProfile value)`. A missing step fails the build with `CS0103: The
name 'UpgradeFrom1' does not exist`, reported in the generated file.

Adding a field needs no version bump and no migration: a payload saved before the field existed
loads with the field's declared default. Bump `version` only when old data must be translated, as
in a rename. A default is part of the field's type string, so a string default carries its own
quotes inside it: `Title = 'string = "My family"'`.

The generated classes are in the namespace `<Project>.Protocol`, after the app's C# project name,
unless the schema sets `[namespaces] csharp`. The template's `GlobalUsings.cs` does not import it,
so add `global using <Project>.Protocol;` below its managed block. Then a schema type named like a
platform type, such as `MotionSample` from `Ikon.App`, is ambiguous (`CS0104`), so give schema
types names of their own (`DancerMotion`). Put the partial with the
`UpgradeFrom` steps in a file of its own that declares that namespace, as above: a partial in the
global namespace is a different class (`CS0426: The type name 'RetiredFields' does not exist`). It
is the one exception to AGENTS.md's rule against namespaces in app files. A `[nested.Person]` type is a
class nested in the root class: `FamilyTree.Person`, not `Person`.

Then use it like any other persisted value — `new PersistentSessionReactive<PlayerProfile>(new())`.
The generated class is mutable and has no `with` or copy method, so change a stored value inside
`Update`: `_profile.Update(p => { p.Score += 10; return p; });`. Setting a property on `Value` and
assigning the same instance back is skipped as unchanged, so nothing re-renders.
Nothing else changes; the contract is stored inside the payload. Three guarantees come with it:

- **Renames survive.** A removed property moves to the schema's `[obsolete]` ledger; on load its
  stored value arrives typed in `RetiredFields` and your `UpgradeFrom` step decides what it means.
- **Old branches can't destroy new fields.** Keys this build doesn't know — at the top level and
  inside nested section objects — are carried through a load→save cycle untouched, so a rollback
  build saving over a newer build's data keeps the newer fields. This covers only one process's
  load→save cycle, and saves are whole-blob last-writer-wins across
  instances — with `[Sharded]`/multi-instance saves, a stale instance's save can still resurrect
  values another instance changed (merge-on-save is future work). Arrays are not merged either;
  the last saved list wins whole.
- **Newer data is never downgraded — or destroyed.** If the stored payload was written by a newer
  schema version than this build understands (or the envelope is malformed, or the value fails to
  deserialize), the app sees the default value and the save re-emits the stored payload verbatim.
  One warning names the reactive and both versions at load, and the first write to such a reactive
  warns once more: **writes made while holding a newer payload update memory but do not persist**
  (`Reactive '<StableId>' holds stored state from a newer schema version; writes ... will not
  persist`).

One asymmetry to know: keys listed in the `[obsolete]` ledger are *consumed* — captured into the
bag, handed to your migration, and stripped from the next save — while unledgered unknown keys are
preserved forever. Retire a key only when this build owns deciding what it means.

Boundaries: the contract applies to single-value `Persistent…Reactive<T>` where `T` is a data-`.tp`
root — the collection variants keep plain behavior for now. Nested `[obsolete.Section]` entries are
not migrated by the state contract yet — a retired key inside a nested section is kept as an
unknown key and written back on every save: **don't retire nested fields on persisted
types**; keep ledger entries at the root. A retired field's type must be `string`, `bool`,
`int32`, `int64`, `float64` or `string[]`; any other type, such as an array of a nested type, fails
the build with `TELEPORT001`. So neither a field holding a list of nested records
(`Pages = "Page[]"`) nor a field inside a nested type can be renamed through the ledger. To rename
one, keep the old field, add the new one beside it, bump `version` and copy the values across in the
`UpgradeFrom` step.
Plain records keep today's behavior exactly; nothing opts in until the type is a Teleport data schema.

## Anti-patterns — don't do these

- ❌ `Path.Combine(app.DataDirectory, "data")` + `Directory.CreateDirectory(...)` for runtime state. `DataDirectory` is **read-only** in cloud. It's for bundled assets the app reads, not state it writes.
- ❌ Using the `Postgres` backend for binary blobs or image bytes — `byte[]` belongs on asset storage, which the default already does for you.
- ❌ Using `Public` backend for anything sensitive — assets get a real URL on the open web.
- ❌ Constructing AssetUris by hand for state that fits a `PersistentXxxReactive`.
- ❌ Using `Guid.NewGuid()` as `key:` — it changes on restart.
- ❌ Writing a reactive from inside a render lambda (`if (_boxes.Count == 0) _boxes.Add(new Box())` in the middle of `UI.Root(...)`). The platform drops the change notification of a write made while it renders — nothing re-renders from it — and logs `update was ignored because it was done within a reactive callback, at <call site>` once per site. Render only reads; create-if-missing belongs in the event handler that needs it (the switch that enables the feature, the button that adds the item) or in a lifecycle callback such as `app.OnClientJoined`. When the render needs something to show before it exists, render a detached placeholder value and let the first edit handler store it.
- ❌ Seeding a user-scoped value from `Main()` or the constructor (`if (_list.Count == 0) _list.Add(...)`) — no `UserScope` is active there, so it throws; pass the starting items to the constructor instead.
- ❌ Assuming the postgres-backed storage reads or writes through on every access — like the asset backends, the row is read at load and written by autosave, an explicit save and the shutdown save; in between, the value lives in memory. For read-your-writes durability, go through `app.DatabaseAsync()` directly.

## When to drop down to `Asset.Instance` directly

Reach for `Asset.Instance` (with a hand-built `AssetUri`) only when:
- You need to list files (`Asset.Instance.ListAsync`), not just read/write a known one — and only for
  `AssetClass.LocalFile` or `AssetClass.EmbeddedFile`. The cloud classes throw `NotSupportedException`
  on listing, so cloud files you must enumerate later need their keys kept in a persistent reactive.
- You need streaming reads/writes for very large files (multi-GB).
- The data isn't naturally a typed reactive value (e.g., a uploaded user file you only ever fetch on demand).

For everything else, use a `PersistentXxxReactive<T>`. It's strictly less code and keeps your state model uniform.
