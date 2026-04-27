# Secrets and Credentials

## Secrets and Credentials

Tokens, API keys, and passwords for an Ikon app live in the Ikon backend, scoped to the app. Never hardcode them in source, commit them to `.env` / `appsettings*.json` / TOML config, or read them from environment variables.

### Setting a secret (CLI, from the app project directory)

```bash
# Interactive — value is prompted for and masked.
ikon app secret set GITHUB_TOKEN

# From stdin — preferred for scripts; keeps the value out of shell history
# and process arguments.
printf %s "<value>" | CI=true ikon app secret set GITHUB_TOKEN --stdin

ikon app secret list                  # lists keys; values are never shown
ikon app secret delete GITHUB_TOKEN   # prompts; add --yes to skip
```

### Reading a secret at runtime (C#)

Secrets are fetched from the backend once at app startup and exposed synchronously via `app.Secrets`. Indexer access throws if the key is not set; use `TryGet` for optional secrets.

```csharp
[App]
public class MyApp(IApp<SessionIdentity, ClientParams> app)
{
    public async Task Main()
    {
        string token = app.Secrets["GITHUB_TOKEN"];

        if (app.Secrets.TryGet("SENTRY_DSN", out var dsn))
        {
            // wire up optional integration
        }
    }
}
```

Rotating a secret with `ikon app secret set` while the app is running only takes effect after the app restarts.
