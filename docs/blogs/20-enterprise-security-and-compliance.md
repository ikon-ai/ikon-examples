# Built for the Security Review

Most AI applications are built first and secured later. A team wires up a frontend to an API, gets it working, ships it — and then someone from security asks where the API keys are stored. The answer is usually complicated. Keys in environment variables that get bundled into the client. Tokens in localStorage. Credentials proxied through a thin backend that was added after the prototype. Each question in the security review reveals another layer of improvisation.

Ikon applications do not have this problem. Not because someone went through a hardening checklist, but because the architecture eliminates the conditions that create the vulnerabilities in the first place.

## What this unlocks

**Ship AI applications into regulated environments without a security rewrite.** Financial services, healthcare, government procurement — these are environments where AI deployments typically stall for months in security review. Ikon applications pass these reviews quickly because the answers are architecturally simple, not because someone prepared good documentation.

**Build powerful AI workflows where the security model is invisible.** A healthcare app where a doctor dictates notes, an AI generates a clinical summary, and the summary is shared with the patient — all in a multiuser session where both participants see the same interface in real time. The doctor sees the full clinical record, AI confidence scores, and differential diagnoses. The patient sees only the approved summary and their care plan. Same application, same session, different views — because the server decides what each connected client receives. The doctor's dictation, the AI's reasoning, the patient's view — all handled by server-side code that never exposes credentials, raw AI output, or internal state to either client. The developer writes the medical workflow. The platform handles the security.

**Use multiple AI providers without multiplying your risk.** An app that combines language, speech, image generation, and web scraping would traditionally need separate credential sets managed on the client or proxied through separate backend services. On Ikon, adding a new AI provider is a code change, not a security architecture change. The developer never handles credentials at all.

**Stop thinking about client-side security entirely.** There is no client-side business logic to audit. No API keys to rotate on the frontend. No token refresh logic to get wrong. No npm dependencies that could exfiltrate credentials. The entire category of client-side security work does not exist.

## The thin client

In a traditional web application, the client is powerful — and that power is the source of most security problems. It holds credentials, makes API calls, stores tokens, runs business logic, and manages state. Every one of those capabilities is an attack surface.

In an Ikon application, the client is a rendering layer. It maintains a persistent encrypted connection to the server, displays the interface updates it receives, and sends user inputs back. That is the complete list.

It cannot call third-party APIs. It cannot access credentials. It cannot inject AI prompts. It cannot reach other applications. An attacker who fully compromises the client gets access to UI rendering code and nothing else. There are no credentials to steal, no APIs to call, no tokens to reuse.

The client also has almost no dependencies. There is no API library, no fetch wrapper, no state management framework, no authentication library — all common targets for supply chain attacks in traditional frontends. The attack surface is not small. It is nearly zero.

## Credentials never reach your code

This is the part that surprises people.

Not only does the client never touch API keys — the application code running on the server does not touch them either. When a developer writes an AI call, they specify which model to use, not which credentials to authenticate with:

```csharp
var (result, _) = await Emerge.Run<ClinicalSummary>(LLMModel.Claude46Sonnet, context, pass =>
{
    pass.Command = "Generate a clinical summary from these consultation notes";
    pass.Regions = [ModelRegion.Eu];
}).FinalAsync();
```

The developer chose a model and a region. The platform resolved the credentials. The API key for Anthropic was loaded from a secure credential store, used for this request, and never exposed to the application process. The developer's code cannot log it, leak it, or accidentally include it in a response.

This is true across all AI providers and capabilities — language, image, speech, video, web scraping, OCR, embeddings — all accessed through the same library, all with credentials managed by the platform. If a vulnerability exists in the application code, the blast radius does not include AI provider credentials. They were never there.

## Container isolation

Each Ikon application runs in its own isolated container — a separate process, filesystem, and network namespace. A compromised application cannot reach other applications, their data, or the platform infrastructure. All containers run as a non-root user with dedicated service accounts.

This is not a feature the developer enables. It is how every application runs.

## The binary protocol

Ikon applications communicate over Teleport, a binary protocol that carries everything — UI updates, user input, audio, video — over a single encrypted connection. There are no REST endpoints to discover, no GraphQL schema to introspect, no separate WebSocket channels to probe. The protocol uses compact binary encoding with a 27-byte header, which is efficient but also means there is simply less surface for an attacker to work with compared to text-based API layers.

## Function policies

Ikon has a built-in policy system that governs what users can do inside an application. Policies are attached to functions and evaluated before every call. There are three kinds.

**Rate limits** restrict how often a function can be called — globally or per session. A patient-facing AI assistant can be limited to a fixed number of consultations per hour. An internal tool can allow unlimited use. The limits are enforced by the platform, not by application code that someone might forget to add.

**Usage limits** check quotas and credits before execution. An enterprise deployment can cap AI usage per department, per user, or per billing period — with the check happening before the AI call is made, not after.

**Approval gates** require human authorization before a function executes. This is where the policy system becomes genuinely different from what other platforms offer. Consider a healthcare scenario: an AI generates a diagnosis summary, but before it is shared with the patient, the system routes an approval request to the attending physician. The physician reviews the summary on their device and approves or rejects it. The patient sees nothing until the doctor signs off.

```csharp
[RequireApproval("Clinical summary requires physician approval before sharing")]
public async Task ShareSummaryWithPatient(string summaryId) { ... }
```

The approval can be routed to the caller, to a specific user, or to a specific session. It has a configurable expiry. It generates an audit trail. And it works across the multiuser session — the doctor and the patient are both connected to the same application, seeing the same real-time interface, but the policy system controls who can trigger which actions and who must approve them.

This is not a workflow engine bolted on top. It is a first-class part of the function execution pipeline, evaluated with priority ordering before any code runs.

## The AI-specific security story

AI applications introduce security concerns that traditional software does not have. Ikon addresses them architecturally.

**Credential theft scales differently with AI.** A stolen API key is not like a stolen password — it is a direct line to expensive compute. On Ikon, AI provider credentials are managed by the platform and never exposed to application code or clients. There is nothing to steal at either layer.

**Prompt injection is harder when the client has no access to prompts.** In traditional setups, the client constructs prompts from user input and sends them directly to a model. On Ikon, user input travels to the server as data. The server constructs the prompt in code the user cannot see or modify. Prompt injection remains a server-side concern, but the easiest and most common attack vector — client-side manipulation — is eliminated.

**Model abuse requires breaching the server, not opening developer tools.** If API keys are in the client, anyone with a browser can make unlimited calls against your account. On Ikon, all model calls are mediated by the platform. Rate limits and usage policies are enforced before the AI call is made, not after the bill arrives.

## The compliance picture

Ikon applications inherit compliance-relevant properties from the platform without the developer doing anything.

**Data residency.** The platform supports regional deployment across multiple EU and US regions. AI model calls can be pinned to specific regions — request `ModelRegion.Eu` and the call stays in Europe. Data residency is an architectural property, not a configuration you hope someone remembered to set.

**Encryption in transit.** TLS on all connections. HTTP automatically redirects to HTTPS. Certificate renewal is automatic. The binary protocol runs over encrypted WebSocket or WebTransport connections.

**Audit trails.** Every significant action is recorded — structured logging, session-level logs per user connection, and analytics streaming for long-term retention. Policy decisions, approval outcomes, and rate limit events are all captured. The app developer does not add logging code.

**Credential management.** All secrets in a secure credential store. Automatic rotation every 300 seconds. Separate service accounts per service. No credentials in environment variables, configuration files, or application code.

These properties align with GDPR, SOC 2, and ISO 27001 requirements — not because someone mapped controls to a compliance checklist, but because the architecture was designed this way from the start.

## The security review

When an enterprise security team evaluates an Ikon application, the conversation is different from what they are used to.

*Where are API keys stored?* In the platform's credential store. Rotated every 300 seconds. Never exposed to application code or clients.

*What does the client have access to?* Rendering instructions delivered over an encrypted binary protocol. No business logic, no credentials, no API access.

*What happens if the client is compromised?* Nothing useful. There are no credentials to steal, no APIs to call, no tokens to reuse.

*What happens if the application code is compromised?* The attacker gets access to one isolated container. AI provider credentials are not in the application process. Other applications and the platform are unreachable.

*How do you control what users can do?* Function-level policies — rate limits, usage quotas, and human approval gates — evaluated before every call, enforced by the platform.

*Where does data reside?* Configurable by region. AI calls can be pinned to EU or US regions.

The answers are short because the architecture is simple. Most AI platforms start with a complex client and add security later. Ikon starts with no client-side complexity, keeps credentials out of application code, enforces policies at the platform level, and isolates every app in its own container. There is nothing to secure because there is nothing to attack.
