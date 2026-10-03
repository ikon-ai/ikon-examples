# Browser Connector Guide
<!-- checked-against: a5bd92f494b212d7 -->
This guide covers `Ikon.Connectors.Browser` — a real, Playwright-driven browser operated by an agent or by your code — for app developers automating websites from an Ikon app.

## Browser

`Ikon.Connectors.Browser` operates a real (Playwright-driven) browser. There are two entry points; pick by who is driving:

| Entry point | Who drives | Use when |
|---|---|---|
| `WebAgent.OperateAsync` | An LLM agent subthread | You have an objective in natural language and want the agent to figure out the clicks. Needs an `AgentThread` (from `Ikon.Agent`) and a registered browser-operator persona. |
| `BrowserSession` | Your code | You know the exact actions — scripted navigation, screenshots, page evaluation. No LLM involved. |

A failure the browser cannot turn into a result is a `ConnectorException` (from `Ikon.Connectors`) with `Provider` `"browser"`; a replay given inputs that do not match its flow is one. Most of what goes wrong on a page is not an exception: an action that did not work is a result with `Ok` false, and a run that did not reach its objective has an `Outcome` that says so.

### Agentic operation

Register the persona `BrowserOperatorPersona.Create()` returns on your app's orchestrator (its default name, `"browser-operator"`, matches `OperateAsync`'s default `personaName`). Then hand the agent an objective:

<!-- ikon-example: connectors-web-agent -->
```csharp
var run = await WebAgent.OperateAsync(
    thread,                                    // an AgentThread from Ikon.Agent
    "https://portal.example.com",
    "Log in with the provided credentials and extract the current account balance",
    new WebAgentOptions(PublicInternetOnly: true, MaxPasses: 25, Headless: true));

if (run.Outcome == WebOutcome.Succeeded)
{
    var balance = run.Outputs["balance"];
}
```

`WebRun` carries the `Outcome` (`Succeeded`, `Failed`, or `BudgetExhausted` when `MaxPasses` ran out), a `Summary`, the full action trace in `Steps`, any `Extract`ed `Outputs`, and `Looks` — the count of on-demand vision inspections, which consume agent budget without appearing in the trace.

### Sites you do not control

A site that is not your own app decides what the browser loads next, and the agent can press anything on it. Three options make that safe to hand to a person:

- `PublicInternetOnly: true` confines the browser to public addresses. Every request is made by the platform's guarded HTTP client, so no page can reach the network your app runs in, and certificates are validated. Every `WebAgentOptions` states it; `false` is only for your own app on localhost or a private address.
- `ReviewWrite` is asked before every action that could change something on the site — a click on a submit, send, pay or delete control, Enter outside a search field, and anything the classifier does not recognise. The action runs only on `WebApproval.Allow`; `WebApproval.Deny(reason)` is reported to the agent, which does not try it again. The `WebActionReview` carries a one-line `Description` and a JPEG `Screenshot` of the page. Nobody answering must be a refusal, so bound the wait.
- `OnProgress` hands you a `WebProgress` — step number, URL, what just happened, and a JPEG `Screenshot` — after every observation, for a live view.

<!-- ikon-example: connectors-web-agent-review -->
```csharp
var run = await WebAgent.OperateAsync(
    thread,
    "https://supplier.example.com/orders",
    "Reorder last month's printer paper",
    new WebAgentOptions(
        PublicInternetOnly: true,
        MaxPasses: 40,
        ReviewWrite: async (review, ct) => await askPerson(review.Description, review.Screenshot, ct)
            ? WebApproval.Allow
            : WebApproval.Deny("the person declined it"),
        OnProgress: progress =>
        {
            liveView.Value = progress.Screenshot;
            return Task.CompletedTask;
        }));
```

Typing into a field is not a write, because on most sites nothing is committed until a submit; a field that saves as you type is the case the classifier cannot see.

### Manual driving

`BrowserSession` owns the browser lifecycle: start once, dispose to release the process. `WebTarget` resolution tries the perception mark first, then accessibility role + name, then a CSS/XPath selector — populate whichever you know.

<!-- ikon-example: connectors-browser-session -->
```csharp
await using var session = new BrowserSession();
await session.StartAsync(headless: true);
await session.NavigateAsync("https://example.com/login");

var marks = await session.MarkElementsAsync();
var result = await session.ExecuteAsync(
    new WebAction.Fill(new WebTarget(Role: "textbox", Name: "Email"), "user@example.com"));

if (!result.Ok)
{
    Log.Instance.Warning($"Action failed: {result.Failure}");            // caller-actionable diagnosis
    Log.Instance.Warning(string.Join("\n", session.ConsoleTail));       // the page's own account
}
```

The action vocabulary is a tagged union: `Navigate`, `Click`, `Fill`, `FillLogin`, `FillDetail`, `UsePasskey`, `Press`, `Scroll`, `Extract` (which records the target's inner text under an output name), `Upload`, `Select`, `Hover`, `Back`, `ClickAt`, `AnswerDialog` and `ReadVisible`. `ScreenshotAsync` returns a PNG; prefer `ScreenshotJpegAsync` when the image goes into an LLM context. `ConsoleTail` holds the last ~40 console messages, page errors, and failed requests — the first place to look when a page that "should" render stays blank.

### A browser somewhere else

The agent needs only an `IWebPage` — navigate, screenshot (PNG and JPEG), mark the elements, execute an action, the current URL, stage files and take downloads, report saved logins, and dispose — and `BrowserSession` is the one in your process. `WebAgentOptions.OpenPage` hands a run a page that lives elsewhere instead, such as a browser on a person's own computer driven over a connection of your own; the run disposes the page it opened, and `Headless` and `PublicInternetOnly` are then for the opener to honour. A page reached over a network should also implement `IWebPage.ObserveAsync`, which returns the marks and a screenshot as one `WebObservation`: the agent observes after every step, and the default asks for each in turn.

`BrowserSession.StartPersistentAsync(profileDirectory, headless)` starts on a profile kept in a directory, so its cookies, saved passwords and sign-ins survive from one session to the next: a person signs in to a site once in that profile and every later run is signed in. Only one session can hold a profile at a time; `NewTabAsync` opens another tab on the same profile, so several agents can work at once on one set of sign-ins, and disposing a tab closes only that tab. `Closed` is raised when the person closes the window, or the tab. `AddCookiesAsync` puts `BrowserCookie`s into the session's jar — into the profile, for a persistent session — which is how a host brings in the sign-ins of the browser the person already uses.

### Files in and out

A run can hand a site files and bring files back. `WebAgentOptions.Files` lists `WebFile`s — a name, a MIME type and the bytes — that the agent may put into a page's file input with `WebAction.Upload`; the agent is told their names, and an upload is a write, so `ReviewWrite` is asked first. Whatever the pages download during the run, including a url that is itself a file and a PDF the browser would only have shown, comes back in `WebRun.Downloads`. Driving a page yourself, `IWebPage.StageFileAsync` hands it a file to upload and `TakeDownloadsAsync` returns each download once.

### Saved logins

An agent signs in without ever seeing a password. Give the page an `ILoginVault` — `BrowserSession.Logins` — and each observation lists the `SavedLogin`s that cover the current page, by id and label only. The agent calls `use_login`, which is `WebAction.FillLogin(target, loginId, field)` with a `LoginField` of `Username`, `Password` or `OneTimeCode`, and the browser asks the vault for that one value at the moment it fills the field. The value never reaches the model, the step trace or a distilled flow, so a replayed flow signs in again through the vault.

A login fills only where `SavedLogin.Covers` holds for the document the field is in — https on the login's site or a subdomain of it, plain http only on loopback — so a frame from another site, or a look-alike host, gets nothing; a password fills only into a password field. `ILoginVault.RevealAsync` returns null to refuse, and a vault that holds the secret checks the page itself rather than trusting its caller. `IWebPage.SavedLoginsAsync` is what a page reports; a page on a person's computer answers from the vault there. A `SavedLogin` with `AskFirst` is filled only after `ReviewWrite` approves it, as a write would be; with no reviewer the agent is told to leave the sign-in to the person. `WebAgent.ReplayAsync` stops at a step that fills such a login, since a replay asks nobody.

To keep a sign-in rather than a password, `BrowserSession.ExportStorageStateAsync` returns the session's cookies and storage, and `BrowserSession.StartAsync` takes them back as `storageState`; treat that text as a credential. For a tab a person signs in on themselves, `BrowserSession.OfferToSaveSignInsAsync` notices the password being submitted and, on the next page, offers to save it, calling you with the site, the username and the password when they accept. `BrowserSession.OfferToSaveAuthenticatorsAsync` does the same for an authenticator being set up: it reads the `otpauth://totp/` address from the page's QR code or link and offers to keep the secret with a login already saved for that site, which you name through its first callback.

### Saved passkeys

A passkey signs in the same way, without the agent holding the key. Give the page an `IPasskeyVault` — `BrowserSession.Passkeys` — and each observation lists the `SavedPasskey`s that cover the current page. The agent calls `use_passkey`, which is `WebAction.UsePasskey(passkeyId)`: the browser takes the `PasskeyKey` from the vault into an authenticator it runs for that page in place of the computer's own, and the site's own passkey sign-in, pressed next, succeeds with nobody touching anything. After each sign-in `IPasskeyVault.UsedAsync` gets the count the site has now seen.

A passkey is used only where `SavedPasskey.Covers` holds for the page, and the browser itself gives a key to no site but the one it was made for. A `SavedPasskey` has `AskFirst` unless the vault says otherwise, so `ReviewWrite` is asked before it is made ready, and with no reviewer the agent is told to leave the sign-in to the person; `WebAgent.ReplayAsync` stops at such a step. While a passkey is ready on a page, the person's own security key or device cannot answer there.

For a tab a person signs in on themselves, `BrowserSession.OfferToSavePasskeysAsync` asks at the moment a site makes a passkey whether to keep it: on yes the browser's authenticator makes it and you get the `PasskeyKey`, on no the person's own device makes it as it would have. Treat a `PasskeyKey` as the credential it is. `IWebPage.SavedPasskeysAsync` is what a page reports.

### Saved cards and details

The same holds for a payment card and for the person's name and address. Give the page an `IDetailVault` — `BrowserSession.Details` — and an observation of a page with a form lists each `SavedDetail` by id, kind (`SavedDetail.Card` or `SavedDetail.Identity`), label and the names of its fields, never a value. The agent calls `use_detail`, which is `WebAction.FillDetail(target, detailId, field)`, and the browser asks the vault for that one field as it fills it.

<!-- ikon-example: connectors-saved-detail -->
```csharp
await using var session = new BrowserSession { Details = detailVault };
await session.StartAsync(headless: true);
await session.NavigateAsync("https://shop.example/checkout");

var filled = await session.ExecuteAsync(
    new WebAction.FillDetail(new WebTarget(Role: "textbox", Name: "Card number"), "personal-visa", "number"));
```

A detail fills only where `SavedDetail.MayFill` holds both for the page and for the document the field is in — https, or plain http on loopback — and that document may be a frame from another host, as a payment provider's card form is. A field where `SavedDetail.IsSecret` holds, a card's number and security code, is masked on the page once filled, and no filled field's value is read back into an observation. In an agent run the person is asked through `ReviewWrite` before the first fill of a detail on a site, and the rest of that detail's fields then fill there without asking again; with no reviewer the fill is refused and the agent is told to leave the form to the person. Submitting the form is a write of its own, reviewed as any other. `WebAgent.ReplayAsync` refuses a flow that holds a `WebAction.FillDetail`, since a replay asks nobody. `IWebPage.SavedDetailsAsync` is what a page reports, and a page that keeps none reports none.

### Distill and replay

A successful `WebRun` can be **distilled** into a `WebFlow` — a deterministic, replayable integration — and replayed **without an LLM**:

<!-- ikon-example: connectors-replay -->
```csharp
var flow = WebAgent.Distill(run, name: "portal-balance");
// ... persist flow (it serializes losslessly), later:
var replay = await WebAgent.ReplayAsync(flow, new Dictionary<string, string>
{
    ["email"] = accountEmail,
    ["password"] = accountPassword,
}, headless: true, publicInternetOnly: true);   // as the run it was distilled from

if (replay.Ok)
{
    var balance = replay.Outputs["balance"];
}
```

Distillation keeps only the steps that succeeded and parameterizes each filled field into a named input slot (`WebFlow.Inputs`); slot names are slugs of the field's accessible name (`"Password"` becomes `password`). A `Fill` marked `Secret` is stored **redacted** everywhere the trace is persisted — the step trace, the distilled flow JSON, logs — so the flow never carries the credential. That means every slot **must** be supplied in `inputs` at replay — a missing one, secret or not, fails upfront with `ConnectorException` rather than typing a recorded or placeholder value into the field, and a key that names no slot is rejected the same way, so a misspelt input can never be silently ignored. Replay failures are ordinary results, not exceptions — check `WebReplay.Ok`. Pass `publicInternetOnly` as the run the flow was distilled from had it: the overload without it replays with `PublicInternetOnly` off, private addresses reachable and certificates unchecked, which is for your own app only.

`WebAgent.ReplayAsync(page, flow, inputs)` replays on an `IWebPage` you opened and still own — a `BrowserSession`, or a page from the same opener you give `WebAgentOptions.OpenPage` — and leaves it open. A replay asks nobody before a step, so check `WebAgent.WritesIn(flow)` before replaying unattended: it names, in an approval's words, each step an agent run would have asked a person about, and is empty for a flow that only reads.

A fact on a page that is not a control — a heading, a price, a count — has no mark, so the agent reads it with the `read` tool by the words it shows. The recorded `Extract` keeps the element's structural path in `WebTarget.Selector`, and a replay reads that element first, so it returns next week's price rather than looking for this week's. To read a whole result list — every name and price on screen — the agent uses `read_visible`, recorded as `WebAction.ReadVisible`: the text in view in reading order, including text a shop gives only to screen readers, far cheaper than looking at a screenshot. Whatever the agent reads is also shown back to it, not only kept in `WebRun.Outputs`.

Some steps only the person can take: a code sent to their phone, a passkey, a CAPTCHA, signing in where no saved login exists. With `WebAgentOptions.HandToPerson`, the agent hands the page over — your callback gets a sentence saying what is needed, shows it to the person, and returns true once they have done it in that browser — and the run carries on from the page as they left it. Leave it null for a browser the person cannot reach; the agent then finishes and says what is left. A cloud browser can be made reachable: `BrowserSession.StartScreencastAsync` streams the page as JPEG frames while it changes, and `TapAsync`, `PressKeyAsync`, `TypeTextAsync` and `ScrollAsync` act on the page as the person watching it would, so an app can show the page live and let them sign in there themselves. `WebAgentOptions.Steering` is a `WebRunSteering` you keep: whatever you `Tell` it while the run works reaches the agent with its next observation, so a person can correct a task in flight.

To work in the browser a person already uses, signed in where they are, drive a tab of their own Chrome through the Ikon Connect extension. `ChromeExtensionRelay` listens on localhost for the extension — `ChromeExtensionRelay.WriteExtension` writes the person's copy, which carries the relay's key — and `BrowserSession.StartInChromeAsync` opens a tab there, in a tab group of its own, and drives it as any other session; disposing the session closes that tab and nothing else of theirs. Nothing of the session's is put on their browser: no user agent, no script, no network guard. A file the tab downloads goes to the person's downloads folder, as their browser does it, and comes through `TakeDownloadsAsync` and `WebRun.Downloads` as well once it is whole: the relay, on the same computer, reads it from there. `ChromeExtensionRelay.PageShared` hands you a `SharedPage` when the person sends the page they are on from the extension's button.

An address someone guessed can be wrong. With `WebAgentOptions.StartFromSiteRootOnMissingPage`, a starting address that answers 404 or 410 starts the agent on the site's home page, told why, instead of failing the run; leave it off when the address is the thing under test.

### What the browser hands back

A run's trace is a list of `WebStep` — the `WebAction` attempted, the `ResolvedSelector` it actually
landed on, and whether it was `Ok`. Resolution tries the perception mark id first, then accessibility
role and name, then a CSS or XPath selector, which is what lets a distilled flow still find an
element after the marks have gone stale. A `WebTarget` with a `Name` and no `Role` names an element
by the text it shows; it resolves by its `Selector` first, then by that text. Driving the page manually returns a `WebActionResult`
instead: `Ok`, the `Selector` used, whatever was `Extracted`, and a `Failure` string when it did not
work — a failed action is a result, not an exception. Perception returns `MarkedElement` records, one
per interactive element, each with the numeric `Mark` the model refers to it by plus the `Role`,
`Name` and `Selector` behind it.
