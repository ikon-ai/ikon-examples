# Firewall requirements for Ikon AI apps

For the network administrator opening access to an Ikon AI app. It lists everything that must be
allowed, and nothing else is required. Nothing here needs a developer.

## How an Ikon app connects

An Ikon app runs on our servers, not in the browser. A user's session therefore has three parts:

1. The browser loads the app frontend from the app's own domain.
2. It opens a streaming connection to the server running the app, which stays open for the whole
   session. This is what makes the app respond.
3. If the app uses audio or video, a real-time media connection is added alongside it.

Steps 2 and 3 each try a direct connection first and fall back to port 443 if the network blocks it.
The table below covers both paths.

## What to allow

Outbound only, from user devices. No inbound rules and no VPN are required.

| Destination | Port | Protocol | Purpose |
|---|---|---|---|
| the app's primary domain, e.g. `yourapp.ikonai.app` | 443 | TCP | App frontend |
| `*.ikon-app.ikonai.app` | 9000–19999 | TCP and UDP | App streaming (preferred) |
| `*.ikon-app.ikonai.app` | 443 | TCP | App streaming (proxy fallback) |
| `*.ikon-turn.ikonai.app` | 443 | TCP and UDP | Audio and video streaming (proxy fallback) |

If the app is served on your own domain instead of `ikonai.app`, replace the first row with that
address. The other three rows do not change.

## Before you write the rules

**Wildcards are required.** Streaming and media hostnames are assigned dynamically and change over
time. There is no fixed list to allow.

**Do not allowlist by IP address.** These services run across different data centres and providers,
and the addresses change without notice. Allow by hostname.

**443 alone is enough to work.** If policy does not permit the 9000–19999 range, allow only the 443
rows. The app detects this and routes everything over 443.

**TLS inspection must pass WebSocket connections through.** The streaming connection in step 2 is a
WebSocket over HTTPS.
