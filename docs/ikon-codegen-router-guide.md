# Running coding agents on your own machine

How to connect a computer of yours to an Ikon app's Tasks board, so tasks can run external coding
agents — Claude Code, Codex, Gemini, Antigravity — on it. For app developers; everything here works
with the `ikon` CLI you already have.

## What the router is

The board runs in the cloud, but an external coding agent runs on a computer of yours, next to a
checkout of your code. `ikon codegen router` connects the two: it advertises your machine and the
agents installed on it to one app, receives that app's tasks, runs the agent locally, and streams
the session back — the transcript, permission requests, cost and lifecycle all render on the app's
Tasks board. Your code and your agent subscriptions stay on your machine; the board sees the
session, not your keys.

A machine belongs to the account that connected it. If the router stops, its tasks pause and resume
when it reconnects.

## 1. Prepare the computer

Install the ikon tool and sign in with the same account you use on the board:

```bash
ikon login
ikon codegen router install
```

`install` checks which coding agents the machine has and sets up what they need — including tmux,
which lets a terminal and the board share one session (macOS and Linux; Windows can run and watch
sessions but not share them).

## 2. Run the router from your repository checkout

```bash
ikon codegen router --app https://your-app.example.com
```

Run it in the checkout the agents should work in and leave it running in a terminal. The first
connection asks you to confirm the app's space id. Useful options:

| Option | What it does |
|---|---|
| `--app <name-or-url-or-id>` | The app to serve; defaults to the app project in the current directory |
| `--space-id <id>` | The precise form of `--app` |
| `--name <label>` | How the machine appears on the board; defaults to the host name |
| `--repo <path>` | Repository root the generators work in — the only tree the app may touch; defaults to the git root |
| `--local-url <url>` | Connect to an app running locally with `ikon app run` instead of the cloud |
| `--allow-terminal` | Let the app open an interactive terminal on this machine (off by default — it is keyboard access) |
| `--trust <space-id>` | Skip the interactive confirmation, for scripts and CI |

Running the app locally with `ikon app run`? Plain `ikon codegen router` from the app folder finds
the local instance on its own.

## Watching a device screen

If the machine can screenshot a running phone — a booted iOS simulator (macOS) or a connected
Android device/emulator (`adb`) — the task view offers **Watch device**: the router captures the
screen every few seconds and the board shows it live beside the task. The watch stops on request
and times out on its own after ten minutes.

## 3. Pick the agent when creating a task

The machine appears on the app's board within a few seconds, and its agents join the "Build with"
choice when you create a task. Each task runs on the machine chosen at creation, in a worktree
beside your repository, and the choice is remembered for your next task.
