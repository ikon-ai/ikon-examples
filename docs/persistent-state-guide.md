<!-- checked-against: a3556832d93d56c4d34ae461 -->

# IKON Persistent State Guide

How to persist app state across restarts. Read this before reaching for files or hand-rolled storage.

## TL;DR — what to pick

**Records people create go in the database, written when they change.** Tasks, notes, documents,
orders, messages: anything a user would call "my data". Open the app's built-in Postgres database
with `app.DatabaseAsync()` and write each change before the UI says it is saved. A committed
database write is durable the moment it returns, and each row is written on its own, so two
instances that change different records do not overwrite each other. A request that says "we keep
no database" still gets this one, since the platform runs it: thousands of records are rows, never
one reactive (256 KB at most) or one JSON document (a `CloudJson` write takes at most 2 MB).
When the request says explicitly that the app must use no database at all, keep its state in a
`PersistentReactive` with `backend: PersistenceBackend.Private` (the app's private file storage), and
store records that keep growing (orders, finished batches, reports) one file each under
`app.Files.Data`, which can be listed.

A `Persistent…Reactive` is written within about two seconds of a change and again when the app
stops, so **a crash loses the changes of those last seconds.** Each value is stored whole. It needs no database of yours: the platform keeps it in the app's state database or, when there is none, in the app's file storage ("Backends", below). Use it for
preferences, drafts, layout, view settings and small app state. A `PersistentReactive` belongs to
one app instance. For a setting or a short list that every instance reads, use a
`PersistentGlobalReactive` ("Global values", below).

**Persistent values are best effort.** The platform starts the app and draws a joining user's first
screen without waiting long for storage, so a stored value can arrive a moment after the first frame
and replace the initial value on screen. A save can be lost when storage fails: a value whose writes
keep failing is given up until it changes, and an app that stops waits only a few seconds for its last
saves. Data that must not be lost belongs in the app's own database.

<!-- ikon-example: persistent-default -->
```csharp
// Small state of this instance, kept across its restarts and deploys:
private readonly PersistentReactive<MyState> _state = new(new MyState());
```

Then from any method:

<!-- ikon-example: persistent-read-write -->
```csharp
// Read and write like any reactive:
_state.Value = next;
var current = _state.Value;
```

**Renamed a field? Saved data does not follow a plain record.** A renamed property loses its stored value, and a removed or added one only drops or defaults that property; keep the stored name with `[JsonPropertyName("oldName")]`, or define the type as a Teleport data schema with a `version` and an `UpgradeFrom` step ("Schema-versioned state", below). Rows in a database table get no version from the platform: store a `version` beside the `jsonb` data. Moving or renaming the reactive's own field orphans its value unless it has a `key:`.

The framework loads from cloud storage as the app starts, writes changes as the app runs, and saves on graceful shutdown. You need no file paths, no `Directory.CreateDirectory` and no hand-built AssetUri. Before telling a user a change is saved, `await app.SavePersistentStateAsync()` (see "Save semantics").

## Four types — pick by who shares the value

A reactive type's name says who shares one value and what it survives:
`[Persistent][Global][User]Reactive[List|Dictionary|HashSet]`. `ClientReactive` and
`MountReactive` have no `Persistent` or `Global` form. With no scope word,
everyone connected to the app instance shares the value. `User` gives each user a value of their
own. `Global` shares the value with every instance of the app. `Persistent` keeps it across restarts
of the instance and deploys of the app.

An instance is one running session of the app. The platform starts one per value of the app's
`SessionIdentity` record: the scaffold's `SessionIdentity(string? UserId)` gives each user one,
`SessionIdentity()` gives everyone one, and `SessionIdentity(string? Room)` gives each room one. Each
shard of a sharded identity (`[Sharding]` in `ikon-config.toml`), and each session of a deploy that is not live yet, is a separate
instance too.

| Class | Same value seen by | Kept across | When to pick |
|---|---|---|---|
| **`PersistentReactive<T>`** | Everyone in this instance | restarts and deploys | **Default.** The state of one room, game or workspace, or of one user under the per-user identity. |
| `PersistentUserReactive<T>` | One user in this instance, across all of their sessions | restarts and deploys | State that follows a user within the instance (preferences, history, a draft). |
| `PersistentGlobalReactive<T>` | Every instance of the app | restarts and deploys | A setting, a flag, a counter or a short list every room or user's instance reads. |
| `PersistentGlobalUserReactive<T>` | One user, in every instance of the app | restarts and deploys | A preference that follows the user into every room, or a value a `[Cron]` job writes for a user. |

`PersistentReactive` and `PersistentUserReactive` are stored under the instance's session identity,
so another identity's instance holds a separate value: another room, another user's instance under
the per-user identity, and each shard. `[Cron]` and `[Trigger]` methods, and endpoints and MCP tools
called without a user's bearer token, run in the instance with an empty identity unless the URL was
minted with one, and under an identity with members that instance is no user's or room's. A call
proved with a bearer token (`EndpointAuth.User`, the `[Mcp]` default) runs in that user's instance
when `SessionIdentity` declares `UserId`. So what such a method writes for users' pages goes in
a `PersistentGlobal…` value or in the database. A `[Cron]` or `[Trigger]` method uses only `Global…`
values under an identity with members, and the build warns IKON051 on any other reactive in one. It warns IKON046
on a `PersistentReactive` or `PersistentUserReactive` write in an endpoint or MCP tool that runs in the
empty-identity instance. Nothing warns about one of those values in a sharded app, where each shard
keeps its own value, because sharding is set in `ikon-config.toml` rather than in the code.

`GlobalReactive<T>` and `GlobalUserReactive<T>` are the same as the persistent global types, except
that a deploy of a new version, or 7 days without a write, starts them again from their initial values
("Global values", below).

**Composer drafts, choices a person has not made yet and other "the user's words in progress" belong in USER scope** (`UserReactive<string>`, or `PersistentUserReactive<string>` to survive app restarts) — never `ClientReactive`. A page reload mints a NEW client session, so per-client state ends with it by design: a draft bound to `ClientReactive` is lost on every reload. User scope survives reloads and follows the user across tabs. Do NOT use browser storage (localStorage/sessionStorage in a custom component) to work around this; on this platform, drafts are kept in server state.

Each type also has collection variants, the durable counterparts of `ReactiveList<T>`, `ReactiveHashSet<T>` and `ReactiveDictionary<TKey, TValue>`. Use these instead of wrapping a collection in a `Persistent...Reactive<List<T>>`:

| Collection | This instance | One user in this instance | Every instance | One user in every instance |
|---|---|---|---|---|
| list | `PersistentReactiveList<T>` | `PersistentUserReactiveList<T>` | `PersistentGlobalReactiveList<T>` | `PersistentGlobalUserReactiveList<T>` |
| set | `PersistentReactiveHashSet<T>` | `PersistentUserReactiveHashSet<T>` | `PersistentGlobalReactiveHashSet<T>` | `PersistentGlobalUserReactiveHashSet<T>` |
| dictionary | `PersistentReactiveDictionary<TKey, TValue>` | `PersistentUserReactiveDictionary<TKey, TValue>` | `PersistentGlobalReactiveDictionary<TKey, TValue>` | `PersistentGlobalUserReactiveDictionary<TKey, TValue>` |

The forms without `Persistent`, which a deploy or 7 days without a write resets, are `GlobalReactiveList<T>`, `GlobalReactiveHashSet<T>`, `GlobalReactiveDictionary<TKey, TValue>`, `GlobalUserReactiveList<T>`, `GlobalUserReactiveHashSet<T>` and `GlobalUserReactiveDictionary<TKey, TValue>`.

The user-scoped classes additionally expose per-user accessors usable outside an active user scope (background tasks): `ValueFor(userId)`, `SetFor(userId, value)`, and `UpdateFor(userId, ...)` on `PersistentUserReactive<T>` and `PersistentGlobalUserReactive<T>`; the collection variants have equivalents like `AddFor` / `RemoveFor` / `ClearFor`, and `ValueFor(userId)` returns the user's whole list, set or dictionary.

## Global values

<!-- ikon-example: persistent-global-values -->
```csharp
// One value for every instance of the app, kept across deploys: a banner each room shows.
private readonly PersistentGlobalReactive<string> _announcement = new("", key: "announcement");

// One value per user, the same in every instance (every room) the user opens.
private readonly PersistentGlobalUserReactive<string> _nickname = new("", key: "nickname");
```

The platform's backend keeps a global value, and each instance keeps a copy that it loads before
`Main()` runs, so reading `.Value` never waits for the network. A user's values load as a
`PersistentUserReactive`'s do (see "Save semantics").

- **A write shows at once in the instance that made it.** The instance sends it to the backend in
  the background, and the other instances usually see it within a few seconds, always within a
  minute while the backend can be reached.
- **Writes in quick succession are delayed.** The first two writes to a value after a quiet spell are
  sent at once. When a third comes soon after, it waits a second, and the wait doubles with each
  further such write, up to 60 seconds; the writes made while it waits are sent as one. A value
  written all the time therefore reaches the other instances once a minute, and the app's log shows
  a warning naming it the first time its wait reaches 8 seconds.
- **Conflicts.** Assigning `Value` is last-writer-wins. `Update` and every collection operation
  (`Add`, `Remove`, setting a key) are kept: when another instance stored a newer value first, they
  are applied again over it, so the function given to `Update` can run more than once and must have
  no side effects. On the sixth conflict in a row, the instance instead writes its own value over the
  other instances' value, losing their changes, and a warning names the reactive. A value changed in
  place is sent only after `NotifyUpdate()`, and then as an assignment.
- **Size.** A value is at most 256 KB, the app's values that are not per user at most 1 MB together,
  and one user's values at most 256 KB together. A value over a limit is not stored: the app logs an
  error naming the reactive and the limit, and sends the value again only after its next change.
- **Lifetime.** `GlobalReactive` and `GlobalUserReactive` values belong to the deployed version that
  wrote them, so a new deploy does not see them and starts from the initial values. They are deleted
  7 days after their last write; a read does not extend that, and a rollback to the earlier version
  sees them again until then.
  `PersistentGlobalReactive` values are never deleted, and `PersistentGlobalUserReactive` values are
  deleted by `ClearFor(userId)` and by user-data erasure.
  A session of a deploy that is not live yet has `Global` values of its own and shares the live
  app's `PersistentGlobal` values. A local `ikon run` shares the `PersistentGlobal` values of the app
  it is linked to and keeps its own `Global` values.
- **Saving.** A global value is sent on its own schedule, not by autosave, and
  `app.SavePersistentStateAsync()` does not wait for it. When the instance stops, a write still
  waiting is sent if its wait ends within 5 seconds; otherwise it is lost and a warning names the
  reactive. When the backend cannot be reached, reads keep the local value and writes are retried,
  waiting longer each time, up to a minute.
- **Only a value that exists when the app instance is constructed is synced** (a field, or one built
  in the constructor). The global types take `key:` and no `backend:`.

Use the global types for values that change now and then: settings, flags, counters, a lobby, a
short list, a user's preference in every room. State several instances share that changes many
times a second belongs in a `[Cell]`, where one process orders every write. Records, and anything
that grows, belong in the database, because a global collection is sent whole after every change.

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
`AddFor(userId, …)` (`SetFor(userId, key, value)` on the dictionary) / `RemoveFor` / `ClearFor` / `UpdateFor` on the collection variants. The
types without `User` (`PersistentReactive`, `PersistentGlobalReactive` and their collections) need
no scope, so they read and write like any other reactive from anywhere, `Main()` included.

## Backends — the default does the right thing

<!-- ikon-example: persistent-backends -->
```csharp
// Default — structured state lands in the app's built-in postgres database
private readonly PersistentReactive<Prefs> _prefs = new(new Prefs());

// byte[] payloads stay on asset storage automatically — no backend parameter needed
private readonly PersistentReactive<byte[]> _snapshot = new([]);

// Public asset URL needed (never sensitive data) — the URL serves the stored JSON wrapper,
// not the raw bytes, so it is no image src; serve media through app.Files.Public
private readonly PersistentReactive<byte[]> _logo
    = new([], backend: PersistenceBackend.Public);

// Explicitly target a postgres DB of the app's own
private readonly PersistentReactive<long> _counter
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
  database `StateDatabase` in its `ikon-config` names, else the built-in `app` database when the app
  has one. A deployed app without `app` uses its only platform-created database, and keeps the
  database chosen first; a local `ikon run` does not, so set `StateDatabase` or the two keep their
  values in different stores.
  Persistence never provisions `app` itself — it exists once the app has asked for a database.
  `byte[]` payloads go to private asset storage. If the session has no state database (no database
  yet, several created ones and no `app`, a local run without `app` or `StateDatabase`), everything falls back to asset storage,
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

Asset storage keeps each value as a file of its own, which costs more than a database row. Saving a
changed value takes two calls to the platform and an upload. Loading a user's values takes one call
to the platform per value when nothing is stored for them, and two calls and a download when
something is. A value a user never changed is not written. With autosave on, a user's values leave
memory a minute after their last client leaves. An instance writes at most six values at a time and
waits when the platform says it is busy. An app with many users online at once, hundreds on one instance or
thousands across its instances, should keep its state in a database: call `app.DatabaseAsync()`
once, which provisions the built-in `app` database, and the platform copies the state that asset
storage held into it (next paragraph).

An app that kept its state in asset storage while it had no database gets that state copied into
its built-in database by the platform when the database is created, before the app can use it. A
value the app already saved to the database is kept, and the old asset blobs are left in place. The
app never reads asset storage for a value that has no row. Apps already using a named database with
`backend: PersistenceBackend.Postgres` are untouched by all of this.

Every load and save logs its destination at debug level (member name, scope, and the postgres
database or asset path), so "where is my data?" is answered by the app's logs.

To start over, `ikon db state reset` deletes every value saved in the `ikon_reactive_storage` table
of the app's state database (`--name` picks another database, `--target staging` a staging copy).
It stops the app's cloud sessions first, since a running session keeps the values it loaded and
saves the ones it changes; a local run keeps them until it restarts. Your own tables stay, unlike
with `ikon db reset`. An app with no state database (or `StateDatabase = "ikon-asset-storage"`)
keeps the values as files under `reactive-storage/` in asset storage, and `ikon db state reset`
deletes those instead, every user's included. Other values in asset storage are files, which
`ikon file delete` removes.

Backend and scope are chosen independently. Scope decides who sees the value; backend decides where it is stored. Everything in this section is about `PersistentReactive` and `PersistentUserReactive`: the global types take no backend, because the platform's backend keeps their values.

### Database tiers

Every provisioned Postgres database lives on a tier — `shared-dense`, `shared`, or a dedicated
tier from `dedicated-nano` to `dedicated-large` — which decides how densely it is packed onto an
instance and how many connections it gets. `ikon db tiers` lists the tiers `ikon db create --tier`
takes, with each tier's storage, connections and backups. `--tier` defaults to `shared`, and the
built-in `app` database starts on `shared-dense`.

The shared tiers are for development and promise no backups, so a database on one cannot be
restored. If it is lost, its data is gone. Put a database you cannot lose on a dedicated tier, which
keeps backups and can be restored to any moment of the last seven days. The smallest is
`dedicated-nano`.

The app reaches a database through a connection pooler, which runs its queries on 2 server
connections on `shared-dense`, 3 on `shared` and 48 on `dedicated-small`, shared by every instance
of the app. Queries beyond that wait at the pooler for a free one, except on `shared-dense`: it runs two queries at a time, and a third concurrent query is refused with "too many connections" (SQLSTATE 53300) instead of waiting. Persistent state loads a joining
user's values with one query per database, however many values the user has.

A live database can move to another tier without redeploying:

```bash
ikon db tier dedicated-small
```

A database moves to any tier `ikon db create` offers, so the built-in database can leave
`shared-dense` but none can move onto it. With several declared
databases, name the one to move with `--name <name>`. The platform
copies the data to an instance of the new tier, verifies it, and switches connections over —
the database keeps its name and credentials, but expect open connections to drop briefly while
the data moves (sessions reconnect automatically). Writes are refused while the data is copied;
reads keep working. `ikon db list` shows each database's tier and the state of an in-flight
migration, and `ikon db tier dedicated-small --wait` waits for the move to finish.

A move that cannot finish is undone: the database stays on its current tier, unchanged and
writable again (unless it is over its storage quota), and `ikon db list` shows the move as
`failed`, and `ikon db show` says why. Nothing needs cleaning up before you try again, to the same tier or
another. A database holding a single row too large to move — tens of megabytes as text on the
shared tiers — is refused before anything changes, as is one larger than the target tier's storage
(1 GB on `shared`, 225 GB on `dedicated-small`) or one with tables under row-level security or
extensions installed outside the `public` schema.

A database that grows past its tier's storage becomes read-only: writes fail and reads keep
working. Writes come back once it is below the limit again, or once it moves to a larger tier.
The size is checked every two minutes, so a database can go a little past its limit before
writes stop. A database that is still written to after it turns read-only (by overriding the
read-only setting), or that grows to 1.5 times its limit, is locked: every connection is refused
until it moves to a larger tier. The organisation's admins are emailed at 80% of the limit, at
the limit and at a lockout, and `ikon status`, `ikon run` and `ikon deploy` say so too.
`ikon db set --auto-tier <tier>` moves a database on its own at 80% of its limit.

A database on a dedicated tier can be put back to any moment in the last seven days:

```bash
ikon db restore --at 2026-11-01T06:30:00Z
```

The platform clones the database's instance as it was at that moment and switches the app to the
clone, so everything written after the moment is gone and open connections drop briefly, as in a
tier move. The instance it left is kept for a day. `ikon db list` shows each dedicated database's
last backup and the earliest moment it can be restored to, both in UTC. `--at` also reads a time
written without a zone as UTC. A moment within the last minute, older than seven days, before that
earliest moment or before the database reached its current instance is refused before anything
changes, and so is a database on a shared tier, which has no backups to restore from: move it to a
dedicated tier, such as `dedicated-nano`, before you need one. When a database has just reached a new
dedicated instance, because it was created, moved or restored, it can be restored only once the
instance has its first backup and a restore point, a few minutes later. Until then `ikon db list` shows no "Restorable from" moment. The same
restore is in Studio under Settings › Data.

`ikon db import <dump>` loads a `pg_dump --format=custom` archive, such as a database file of
`ikon export create`, into a database with no tables; `--clear` drops the tables first, after
asking. `ikon db copy --from <target> --to <target>` copies a database from one app to another, and
each side can be a target name or an app id. Use it to move data into a new app before it goes
live. Both run on the platform, so you need no Postgres tools and the copied
data never passes through your machine. `--wait` waits until they finish. If a connection string
leaks, `ikon db rotate` gives the database a new password, ends the connections that used the old
one, and stops the app's sessions so the new ones connect with the new password.

## The `key:` parameter — loops and refactor-proof state

<!-- ikon-example: persistent-dynamic-keys -->
```csharp
// WRONG — without a key, each iteration's stable id follows its position in the loop, not the
// camera: reorder the cameras or run the method again and every id shifts.
foreach (var camera in cameras)
{
    var baseline = new PersistentReactive<byte[]>([]);  // id is the loop position
}

// RIGHT — explicit stable key derived from the dynamic identity.
foreach (var camera in cameras)
{
    var baseline = new PersistentReactive<byte[]>(
        [],
        key: $"baseline:{camera.Id}");
}
```

Pass a stable identifier the app owns. Not `Guid.NewGuid()` — that changes on every restart and orphans the old data. Don't reuse the same key across different types or scopes.

Without `key:`, a field's stored value is found by its declaring source file's path under the app root, the constructing member's name, `T`, and its order among the reactives of type `T` built in that member, persistent or not. Renaming or moving the file or field, or adding, removing or reordering same-typed reactives built before it in that member (a plain `Reactive<T>` too), silently starts from the initial value and orphans the stored one. That is fine for state you can afford to lose; pass a fixed `key:`, unique within the app, for state that must survive refactoring, and always when constructing reactives in a loop or based on runtime identity. Either way, a stored value is loaded back only into a reactive that exists once the app instance is constructed (a field, or one built in the constructor); one constructed later — in `Main()` or a handler — is saved at shutdown but always starts from its initial value, so run a loop like the one above in the constructor. A global one constructed later is never synced with the other instances.

## Save semantics

These are the rules for `PersistentReactive`, `PersistentUserReactive` and their collections. The
global types are sent on their own schedule ("Global values", above).

- **Load**: parallel for all persistent reactives that exist once the app instance is constructed, as the app starts. The app waits at most 1.5 s for them before `Main()` runs and clients connect. When storage answers later, `Main()`, the `OnStarting` handlers and the first frames see the initial values, and each stored value replaces its initial value when it arrives, so whatever read it renders again. Code that needs the stored value, such as seeding a list only when nothing is stored, first awaits `LoadAsync()` on that reactive. That holds in `OnClientJoined` as much as in `Main` and `OnStarting`: the first join can run before an app-level load finishes, so a join that checks the list and adds its member works on the initial value, and the stored list then arrives with that member already in it (a duplicate in a roster). An `Update` or a collection change made during the load is applied again to the stored value; a plain set made during it is replaced by the stored value, and a warning names the reactive. Nothing saves a value while its load runs. A failed load logs an error, keeps the initial value, and skips saving that value for the run, so the stored value is kept until you fix the cause; the app starts anyway, also when the store cannot be reached or the database credentials are stale. A load also fails when the code cannot read the stored payload: the type changed, or the error says `stored state was captured as …` and names the type the value was saved as. Make the type readable again, or give the reactive a new `key:` to start it fresh. (User-scoped reactives load per user. The primary user's partition loads as the app starts, within the same wait. A connected user's partition loads before their first render and before `OnClientJoined` runs. If that takes over 1.5 s, the client renders and `OnClientJoined` runs with the initial values, and the stored values replace them when the load finishes. Any other user's partition loads the first time their scope is touched, and until it arrives `ValueFor` returns the initial value. With autosave on, a user's partition also leaves memory a minute after their last client left, once it is stored, and loads again the same way. Background code (a cron, a webhook) that reads a user's value to work out what to write should await `LoadForAsync(userId)` first. An `UpdateFor`, `AddFor` or other change made during that load is applied again to the stored value when it arrives; a plain `SetFor` made during it is replaced by the stored value, and a warning names the reactive.)
- **Autosave**: a persistent reactive assigned, or changed through its collection methods, is written within about two seconds, together with any other changes in that time. A value is written only when its payload differs from what this instance last loaded or wrote, and a value still at its initial value is not written while nothing is stored for it. A failed write logs a warning and is retried for that user alone, 4, 8 and 16 seconds later. After 4 failed writes in a row the value is dropped: a warning names it, and it is written again only once it changes. When the platform answers that it is busy, every write of the instance waits as long as it asks. A value mutated in place (`_state.Value.Items.Add(x)` on a plain record) does not tell the reactive it changed; it is written by the next explicit save or at shutdown.
- **Explicit save**: `await app.SavePersistentStateAsync()` writes every value and completes when they are stored, except a value still at its initial value with nothing stored for it. That includes a value mutated in place, and a value this instance stored before and has not changed since, because another process running the same identity (a local run beside the deployed app) may have changed the stored copy. It throws the storage error when a write fails, and an error naming the value when its stored value is still loading. Await it before a UI says "Saved". Set `app.AutosavePersistentState = false` in `Main` to write only on this call and at shutdown, for an app that wants to choose its save points or changes a value many times a second.
- **Save at shutdown**: for all persistent reactives, at most six writes at a time, on `StoppingAsync` (graceful shutdown), writing only the values whose payload differs from what this instance last loaded or wrote; a value it only read is not written back, nor one still at its initial value with nothing stored for it, nor a dropped value that has not changed since. The stop waits at most 5 seconds for these writes (a local `ikon run` waits for all of them). A value not stored by then is named in an error, and its changes are lost if the process stops before the write ends. Locally, `ikon stop` asks the app to shut down and waits for it, so a run's changes are saved there too. It kills an app that has not exited within 30 seconds, or whose IKON libraries are too old to handle the request, and says so; that run's changes are then lost, as in a crash.
- **A local run shares the deployed app's state.** `ikon run` loads and saves persistent reactives in the space the app is linked to, through that space's databases or its asset storage, under the same keys as the deployed app in that space. So a local and a cloud instance of the same identity (`?room=A` in both) read and write the same values while both use the same state database ("Backends", above), and the local run shares every `PersistentGlobal…` value with the deployed app. Stopping a local run saves the values it changed over the deployed app's, and the run logs this once at startup. The keys are the same wherever the app runs, because the platform's user-data erasure matches these keys. Once an app holds real data, test changes that write state with care.
- **A crash loses what was not yet written**: up to two seconds of changes with autosave on, everything since the last explicit save with it off. Records that must never be lost, or that several instances change at once, go in postgres directly through `app.DatabaseAsync()`.
- **A full database stops the saves**: the postgres-backed storage is stored in the `app` database unless `StateDatabase` names another, so when that database reaches its storage limit and turns read-only, every save fails until it moves to a larger tier with `ikon db tier` ("Database tiers", above).

## Erasing a user's state

`ClearFor(userId)` on any user-scoped persistent reactive erases that user's partition (on a `PersistentGlobalUserReactive`, for every instance): the in-memory value is dropped (the next read sees the initial value) and the copy stored under the calling instance's session identity is deleted from whichever store it routes to. The deletion runs in the background; the user leaves the shutdown save immediately, so a cleared user is never re-persisted.

To erase everything the app has persisted about a user — a user-data-erasure (GDPR) request — use the app-level helper:

<!-- ikon-example: persistent-erase-user -->
```csharp
await app.EraseUserStateAsync(userId);
```

It applies `ClearFor` semantics to every registered user-scoped persistent reactive and additionally deletes every stored row under the user's storage scope key — including rows left behind by fields that no longer exist in the code — and completes only when those deletions have finished. Database rows go under every session identity, but a value stored as a file (an app with no database) goes only under the calling instance's session identity, so with a per-user `SessionIdentity` the files the user's own instance stored stay unless the erasure runs in that instance. The user's `PersistentGlobalUserReactive` values are erased too. `PersistentReactive` and `PersistentGlobalReactive` values are untouched: they are shared state, not per-user data. Each stored value is its own round trip to storage, so when a button starts the erasure, update the UI first and let the erasure finish in the background instead of making the person wait on the click.

## Schema-versioned state (`.tp` types)

**Keep a list in one root.** What follows covers a single-value `Persistent…Reactive<T>`, not the
list, dictionary and set forms, so a list whose shape will change goes in one `.tp` root as a
nested array (`Notes = "Note[]"` in a `NoteBook`, which the generated C# declares as `List<Note>`), held in one `PersistentReactive<NoteBook>`.
Rows in a database get no schema version from the platform: store each as `jsonb` with a version
field beside the data (`{"version": 2, "data": {…}}`) and translate older versions when you read a
row. Read a row whose version is newer than the build knows for the fields that parse, and never
write it back, so an older build does not drop the fields it cannot read.

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

An array field (`string[]`, `Note[]`) is declared in C# as a `List<string>` (`List<Note>`), so assign a list or a collection expression, never an array.

Bumping `version` obliges you to say what the old data means — the generated code calls a
migration you write in your partial class, so forgetting it is a compile error, not a data loss:

<!-- ikon-example: persistent-schema-migration -->
```csharp
// schema/PlayerProfile.tp generated the other half of this class in <Project>.Protocol (or the
// namespace its [namespaces] csharp line names, as here). A partial half must be in the same
// namespace, so this file is the one app file that declares a namespace.
namespace Ikon.App.Platform.Patterns.Protocol;

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
unless the schema sets `[namespaces] csharp`. Hyphens become PascalCase and a leading digit gets an underscore, so `pixel-art-collab` gives `PixelArtCollab.Protocol` and `3d-viewer` gives `_3dViewer.Protocol`. The build imports it for the app
in `Generated/Protocol/GlobalUsings.g.cs`, so a schema type named like a
platform type, such as `MotionSample` from `Ikon.App`, is ambiguous (`CS0104`), so give schema
types names of their own (`DancerMotion`). Put the partial with the
`UpgradeFrom` steps in a file of its own that declares that namespace, as above: a partial in the
global namespace is a different class (`CS0426: The type name 'RetiredFields' does not exist`). It
is the one exception to AGENTS.md's rule against namespaces in app files. A `[nested.Person]` type is a
class nested in the root class: `FamilyTree.Person`, not `Person`.

```text
// schema/NoteBook.tp:  Notes = "Note[]"  and  [nested.Note] with Title = "string"
// A name outside the NoteBook class is CS0246 (the type 'Note' could not be found)
var note = new NoteBook.Note { Title = "First" };
_book.Update(book => { book.Notes.Add(note); return book; });
```

Then use it like any other persisted value — `new PersistentReactive<PlayerProfile>(new())`.
The generated class is mutable and has no `with` or copy method, so change a stored value inside
`Update`: `_profile.Update(p => { p.Score += 10; return p; });`. Setting a property on `Value` and
assigning the same instance back is skipped as unchanged, so nothing re-renders.
Nothing else changes; the contract is stored inside the payload. Three guarantees come with it:

- **Renames survive.** A removed property moves to the schema's `[obsolete]` ledger; on load its
  stored value arrives typed in `RetiredFields` and your `UpgradeFrom` step decides what it means.
- **Old branches can't destroy new fields.** Keys this build doesn't know — at the top level and
  inside nested section objects — are carried through a load→save cycle untouched, so a rollback
  build saving over a newer build's data keeps the newer fields. This covers only one process's
  load→save cycle, and each save writes the whole value: when two processes save the same stored
  value (a local run beside the deployed app), the last save wins and can bring back values the
  other changed. Arrays are not merged either; the last saved list wins whole.
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
not migrated by the state contract, so an app schema that has one fails the build with
`TELEPORT001` (a retired key would stay in the stored data and the new field would start empty):
keep ledger entries at the root. A retired field's type must be `string`, `bool`,
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
- You need to list files (`Asset.Instance.ListAsync`), not just read/write a known one. A cloud file
  listing needs a folder URI with the space in it; `CloudJson` cannot be listed at all, so keys of JSON
  values you must enumerate later go in a persistent reactive.
- You need streaming reads/writes for very large files (multi-GB).
- The data isn't naturally a typed reactive value (e.g., a uploaded user file you only ever fetch on demand).

For everything else, use a `PersistentXxxReactive<T>`. It's strictly less code and keeps your state model uniform.
