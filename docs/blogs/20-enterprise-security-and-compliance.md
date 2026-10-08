# Built for the Security Review

*Published 2026-03-30*

Most AI applications are built first and secured later. A team connects a frontend to an API, gets it working and ships it. Then someone from security asks where the API keys are stored, and the answer is usually complicated: keys in environment variables that get bundled into the client, tokens in localStorage, or credentials proxied through a thin backend that was added after the prototype. Each question in the security review uncovers another part that was improvised.

Ikon applications do not have this problem. Their architecture removes the conditions that create these vulnerabilities, so their security does not depend on someone working through a hardening checklist.

## What this makes possible

**Ship AI applications into regulated environments without a security rewrite.** In financial services, healthcare and government procurement, AI deployments typically stall in security review for months. Ikon applications pass these reviews quickly because the architecture makes the answers simple.

**Build AI workflows where the platform handles security.** Take a healthcare app where a doctor dictates notes, an AI generates a clinical summary, and the summary is shared with the patient. The doctor and the patient are in the same multiuser session and use the same interface in real time. The doctor sees the full clinical record, AI confidence scores and differential diagnoses. The patient sees only the approved summary and their care plan. Both use the same application and the same session, but the server decides what each connected client receives, so each of them sees a different view. Server-side code handles the doctor's dictation, the AI's reasoning and the patient's view, and it never exposes credentials, raw AI output or internal state to either client. The developer writes the medical workflow, and the platform handles the security.

**Use multiple AI providers without multiplying your risk.** An app that combines language, speech, image generation and web scraping would traditionally need separate credential sets, managed on the client or proxied through separate backend services. On Ikon, adding a new AI provider is a code change and does not change the security architecture. The developer never handles credentials at all.

**Stop thinking about client-side security entirely.** The client has no business logic to audit, no API keys to rotate, no token refresh logic to get wrong and no npm dependencies that could exfiltrate credentials. There is no client-side security work to do.

## The thin client

In a traditional web application, the client does a lot, and that is where most security problems come from. It holds credentials, makes API calls, stores tokens, runs business logic and manages state. Each of these is an attack surface.

In an Ikon application, the client only renders. It keeps a persistent encrypted connection to the server, displays the interface updates it receives, and sends user input back. It does nothing else.

The client cannot call third-party APIs, access credentials, inject AI prompts or reach other applications. An attacker who fully compromises the client gets access to UI rendering code and nothing else. There are no credentials to steal, no APIs to call and no tokens to reuse.

The client also has almost no dependencies. It has no API library, fetch wrapper, state management framework or authentication library, which are common targets for supply chain attacks in traditional frontends. Its attack surface is close to zero.

## Credentials never reach your code

Neither the client nor the application code running on the server ever touches API keys. When a developer writes an AI call, they specify which model to use, not which credentials to authenticate with:

```csharp
var (result, _) = await Emerge.Run<ClinicalSummary>(LLMModel.Claude46Sonnet, context, pass =>
{
    pass.Command = "Generate a clinical summary from these consultation notes";
    pass.Regions = [ModelRegion.Eu];
}).FinalAsync();
```

The developer chose a model and a region, and the platform resolved the credentials. It loaded the Anthropic API key from a secure credential store and used it for this request without exposing it to the application process. The developer's code cannot log the key, leak it or accidentally include it in a response.

The same is true for every AI provider and capability, including language, image, speech, video, web scraping, OCR and embeddings. They are all used through the same library, and the platform manages the credentials for all of them. A vulnerability in the application code cannot expose AI provider credentials, because the credentials are never in the application.

## Container isolation

Each Ikon application runs in its own isolated container, with a separate process, filesystem and network namespace. A compromised application cannot reach other applications, their data or the platform infrastructure. All containers run as a non-root user with dedicated service accounts.

The developer does not have to turn this on. Every application runs this way.

## The binary protocol

Ikon applications communicate over Teleport, a binary protocol that carries all traffic, including UI updates, user input, audio and video, over a single encrypted connection. There are no REST endpoints to discover, no GraphQL schema to introspect and no separate WebSocket channels to probe. The protocol uses compact binary encoding with a 27-byte header. This is efficient, and it also gives an attacker less to work with than a text-based API layer.

## Function policies

Ikon has a built-in policy system that controls what users can do inside an application. Policies are attached to functions and evaluated before every call. There are three kinds.

**Rate limits** restrict how often a function can be called, either globally or per session. A patient-facing AI assistant can be limited to a fixed number of consultations per hour, while an internal tool can allow unlimited use. The platform enforces the limits, so they do not depend on application code that someone might forget to add.

**Usage limits** check quotas and credits before execution. An enterprise deployment can cap AI usage per department, per user or per billing period, and the check happens before the AI call is made.

**Approval gates** require a person to authorize a function before it runs. This is the part of the policy system that differs most from what other platforms offer. For example, in a healthcare app an AI generates a diagnosis summary. Before the summary is shared with the patient, an approval request goes to the attending physician. The physician reviews the summary on their device and approves or rejects it. The patient sees nothing until the doctor approves it.

```csharp
[RequireApproval("Clinical summary requires physician approval before sharing")]
public async Task ShareSummaryWithPatient(string summaryId) { ... }
```

The approval request can go to the caller, to a specific user or to a specific session. It expires after a time you configure, and it is recorded in an audit trail. It also works within a multiuser session. The doctor and the patient are connected to the same application and see the same real-time interface, but the policy system controls who can trigger which actions and who must approve them.

Approval gates are part of the function execution pipeline itself, not a separate workflow engine added on top. Policies are evaluated in priority order before any of the function's code runs.

## Security risks specific to AI

AI applications have security concerns that traditional software does not. Ikon addresses them in its architecture.

**Credential theft costs more with AI.** A stolen AI provider API key gives the thief direct access to expensive compute. On Ikon, AI provider credentials are managed by the platform and never exposed to application code or clients, so there is nothing to steal at either layer.

**Prompt injection is harder when the client has no access to prompts.** In traditional setups, the client builds prompts from user input and sends them directly to a model. On Ikon, user input is sent to the server as data, and the server builds the prompt in code the user cannot see or change. Prompt injection still has to be handled on the server, but client-side manipulation, the easiest and most common way to attempt it, is no longer possible.

**Model abuse requires breaching the server, not opening developer tools.** If API keys are in the client, anyone with a browser can make unlimited calls against your account. On Ikon, every model call goes through the platform, which enforces rate limits and usage policies before the call is made.

## The compliance picture

Ikon applications get these compliance-related properties from the platform, without any work from the developer.

**Data residency.** The platform supports regional deployment across multiple EU and US regions. AI model calls can be pinned to specific regions. If you request `ModelRegion.Eu`, the call stays in Europe. Data residency is part of the architecture, not a configuration you hope someone remembered to set.

**Encryption in transit.** All connections use TLS. HTTP automatically redirects to HTTPS, and certificates are renewed automatically. The binary protocol runs over encrypted WebSocket or WebTransport connections.

**Audit trails.** Every significant action is recorded through structured logging, session-level logs for each user connection, and analytics streaming for long-term retention. Policy decisions, approval outcomes and rate limit events are all recorded. The app developer does not add logging code.

**Credential management.** All secrets are kept in a secure credential store and rotated automatically every 300 seconds. Each service has its own service account. No credentials are stored in environment variables, configuration files or application code.

These properties align with GDPR, SOC 2 and ISO 27001 requirements because the architecture was designed this way from the start, not because someone mapped controls to a compliance checklist.

## The security review

When an enterprise security team evaluates an Ikon application, the conversation is different from what they are used to.

*Where are API keys stored?* In the platform's credential store. They are rotated every 300 seconds and never exposed to application code or clients.

*What does the client have access to?* Rendering instructions delivered over an encrypted binary protocol. It has no business logic, no credentials and no API access.

*What happens if the client is compromised?* The attacker gets nothing useful. There are no credentials to steal, no APIs to call and no tokens to reuse.

*What happens if the application code is compromised?* The attacker gets access to one isolated container. AI provider credentials are not in the application process, and other applications and the platform cannot be reached from it.

*How do you control what users can do?* With function-level policies: rate limits, usage quotas and human approval gates. The platform evaluates and enforces them before every call.

*Where does data reside?* That is configurable by region. AI calls can be pinned to EU or US regions.

The answers are short because the architecture is simple. Most AI platforms start with a complex client and add security later. Ikon has no client-side complexity, keeps credentials out of application code, enforces policies at the platform level and isolates every app in its own container. There is nothing to secure because there is nothing to attack.
