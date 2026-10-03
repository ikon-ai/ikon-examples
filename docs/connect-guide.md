# Connecting your computer to an Ikon app
<!-- checked-against: 81c6264d12d42310 -->
How to connect a computer of yours to an Ikon app with Ikon Connect, so the app's tasks can run
external coding agents — Claude Code, Codex, Gemini, Antigravity — on it, and its web tasks can use
the computer's own browser. For app developers; everything here works
with the `ikon` CLI you already have.

## What Ikon Connect is

The board runs in the cloud, but an external coding agent runs on a computer of yours, next to a
checkout of your code. `ikon connect` connects the two: it advertises your machine and the
agents installed on it to each app you connect, receives their tasks, runs the agent locally, and streams
the session back — the transcript, permission requests, cost and lifecycle all render on the app's
Tasks board. Your code and your agent subscriptions stay on your machine; the board sees the
session, not your keys.

A computer belongs to the account that connected it. If Ikon Connect stops, its tasks pause and
resume when it reconnects.

Connecting lets an app start the coding agents installed on the computer, and they run as you.
What an agent may do there is that agent's own configuration — its permissions and settings —
not something Ikon limits; the tool says so and asks before it connects.

## 1. Prepare the computer

Install the ikon tool and sign in with the same account you use on the board:

```bash
ikon login
ikon service install
```

`install` checks which coding agents the machine has and sets up what they need — including tmux,
which lets a terminal and the board share one session (macOS and Linux; Windows can run and watch
sessions but not share them). On macOS it also lets an app's **Connect this computer** link open
here, and starts Ikon Connect at login, so a computer that restarted is back without a command;
`--no-links` and `--no-start-at-login` leave either out, or undo it. Elsewhere, run `ikon connect`
yourself and paste a link after it.

## 2. Connect an app

```bash
ikon connect https://your-app.example.com
```

The argument is the app — its name, its URL, or its id — and defaults to the app project in the
current directory. The first time, you confirm the app's id, organisation and what it may do
on your computer. Leave it running: **one** `ikon connect` serves every app you connect, so run the
same command for a second app in another terminal and the running process picks it up within
seconds, then that command returns.

An app has to be in one of your organisations, or be one of Ikon's own — Studio and O, which the
tool names as platform apps on the confirmation.

### The code the computer shows

An app that pairs — Studio and O do — takes a computer only once a person has typed the code the
computer shows into the app. Once the service has reached the app, `ikon connect` prints the
six-digit code, the service shows it in a notification, and `ikon connect list` shows it as the
connection's state until it is typed:

```text
Studio asks you to pair this computer: type 482 913 into the "Connect a computer" dialog in Studio
```

The computer appears in that dialog with a box for the code; the right code pairs it, and the
app remembers the computer, so it is asked once. This is what keeps a service left running from
connecting a computer on its own: nothing attaches until someone at the app says so.

What an app may do is a set of **grants**, checked on your computer:

| Grant | What it lets the app do |
|---|---|
| `repo` | Work in the repository: worktrees of it, landing and pushing, its local agent sessions, and reading and committing its files on branches nobody has checked out |
| `agents` | Run the coding agents installed on the computer |
| `files` | Put files into, and read files from, the app's folder |
| `devices` | See and control phones and simulators |
| `browser` | Use the computer's signed-in browser, which every connected app shares |
| `terminal` | Open a terminal — keyboard access to the computer |

Run inside a repository, an app gets `repo`, `agents`, `files` and `devices`; elsewhere, `agents`,
`files` and `devices`. `browser` and `terminal` are never given by default. A command that needs a
grant the app lacks is refused on the computer, and the app is told which grant is missing.

| Option | What it does |
|---|---|
| `--grant <grant>` | Give the app one more grant; repeatable. Adds to what a connected app already has |
| `--no-grant <grant>` | Take a grant away, or leave a default out; repeatable |
| `--repo <path>` | The repository the app may work in; defaults to the git root of the current directory |
| `--app-id <id>` | The precise form of the app argument |
| `--name <label>` | How the computer appears in the apps; defaults to the host name |
| `--local-url <url>` | Connect to an app running locally with `ikon run` instead of the cloud |
| `--trust <app-id>` | Skip the interactive confirmation, for scripts and CI |

Running the app locally with `ikon run`? Plain `ikon connect` from the app folder finds the
local instance on its own. Each app gets a folder of its own on your computer, `~/Ikon/Apps/<app>/`,
where it keeps its sessions' files and, without a repository, its checkouts.

### Pairing links

An app can show a **Connect this computer** link, `ikon://connect/add?space=<id>&grants=browser,files`,
naming one or more apps and the grants each asks for. Opening it runs `ikon connect` with the link in
a terminal, which shows you the app and what it asks for and connects only what you confirm — a link
can ask, never grant. A link cannot name a repository, so for work in one, run `ikon connect <app>`
from the repository instead. `ikon connect "<link>"` does the same thing where links do not open.

When a connection cannot start or stops — most often because your sign-in expired — Ikon Connect
says so in its output and, on macOS, in a notification.

```bash
ikon connect list                  # the connected apps, their grants and folders, and whether each is live
ikon connect delete <app>          # disconnect one; its folder stays
```

## Phones, simulators and emulators

If the machine has phone tooling — Xcode's simulators (macOS) or `adb` for Android — the app's
Preview tab gains a **Phones and simulators** button, opening a Devices view that lists every
phone-shaped run target on every connected machine: iOS simulators, Android emulators and
plugged-in devices, and emulator images that are not running yet. Two verbs per device:

- **Open the app** boots the device if needed and opens the running preview's app on it, signed
  in — the same app instance the Preview tab shows, on real phone glass. Android emulators get
  the host-loopback rewrite (`10.0.2.2`) automatically, and a remote machine's devices get your
  machine's LAN address instead of `localhost`.
- **Live Preview** mirrors the device's screen into Studio. When the machine can encode — an
  Android device encodes on-device, an iOS simulator needs ffmpeg installed beside it — the
  mirror is a live H.264 video stream, fanned out to viewers over the platform's normal video
  path; otherwise it falls back to a ~1.5 s screenshot cadence. Either way a device on another
  machine — or one whose window is buried — is visible where you work.

The task view offers the same mirror as **Watch device**: Ikon Connect captures the screen every
few seconds and the board shows it live beside the task. A watch stops on request and times out
on its own after ten minutes.

## 3. Pick the agent when creating a task

The machine appears on the app's board within a few seconds, and its agents join the "Build with"
choice when you create a task. Each task runs on the machine chosen at creation, in a worktree
beside your repository, and the choice is remembered for your next task.

### Tapping the mirrored screen

With Live Preview on, clicks on the mirror are forwarded to the device itself, and Home/Lock
(plus Back on Android) sit beside the toggle. Android needs adb, which ships with the emulator
tooling. The iOS simulator needs idb:

```bash
brew install facebook/fb/idb-companion
pip install fb-idb
```

Without it, a tap answers with that install hint instead of failing silently.
