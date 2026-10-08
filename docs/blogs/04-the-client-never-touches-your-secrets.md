# The Client Never Touches Your Secrets

*Published 2026-03-19*

Open your browser's developer tools on most AI-powered web applications and look at the network tab. You'll find API keys in headers, tokens stored where anyone can read them, and service credentials embedded in the code that was sent to your browser.

Ikon doesn't solve this by adding security layers. Credentials stay on the server, so there is nothing in the client to find.

## What you can build

Because the client holds nothing sensitive, creators can ship AI applications with capabilities that would be reckless to expose in a traditional setup.

**Give AI agents access to sensitive tools without exposing them to the client.** All AI calls run on the server, so you can give your AI agent access to databases, code execution, file systems, and external APIs, and the client can reach none of them. One developer built an app where people describe an interface in plain English, and the AI writes code, runs it, and renders the result in real time. The code is generated from user input, and none of it is exposed to the client. In a traditional architecture, running code generated from user input would be a serious security risk. On Ikon, the client never sees the generated code, the execution environment, or the raw results.

**Ship AI apps without lengthy security reviews for the client.** The client has no business logic, no API keys, no credentials, no state management, and minimal dependencies. There is nothing sensitive in the client to audit.

**Pass enterprise security reviews on day one.** When auditors ask where API keys are stored, where credentials are sent, and what the client can access, the answers are short: only on the server, never to the client, and nothing. Developers building on Ikon have shipped AI applications into regulated environments, such as financial services, healthcare, and enterprise, without the months-long security reviews that usually hold up AI deployments.

**Use multiple AI providers without multiplying your risk.** An app that combines one service for language, another for speech, and a third for images would traditionally need separate sets of credentials managed on the client or proxied through separate backend services. On Ikon, all provider credentials are stored on the server. Adding a new AI provider is a code change and does not change your security architecture.

## A thin client

In a traditional web stack, the client is "fat". It contains business logic, state management, integration code, and often direct access to backend services. The server mostly provides data.

In Ikon, it is the other way around, and the server does everything:

- **All interface rendering** happens on the server. The client receives pre-computed updates and applies them.
- **All AI calls** originate from the server. The client never communicates directly with any AI provider.
- **All business logic** runs in an isolated server process.
- **All state** is stored on the server.

The client only keeps a persistent connection to the server, renders the interface updates it receives, and sends user input back.

## Credentials stay on the server

When you build an AI application with Ikon, your API keys for OpenAI, Anthropic, Google, ElevenLabs, or any other provider are stored only in server-side configuration and used by code running in a cloud container. They never appear in browser code, browser storage, network requests visible in developer tools, or URL parameters.

Here is server-side AI code in which the AI queries a database directly. None of it is visible to the client:

```csharp
// This runs on the server — the client never sees the API key,
// the request, or the raw response
var (analysis, _) = await Emerge.Run<Report>(LLMModel.Claude46Sonnet, context, pass =>
{
    pass.Command = "Analyze this financial data";
    pass.AddTool(Tool.Of("query_database", "Run a SQL query", async (string sql) =>
    {
        return await _database.QueryAsync(sql);
    }));
}).FinalAsync();
```

The client sees only the final rendered result, such as a chart, a summary, or a table. It never sees the API calls, database queries, or credentials that produced it.

## Container isolation

Each Ikon application instance runs in its own isolated container with separate memory, its own filesystem, and an isolated network stack. If an attacker compromises the application code running inside a container, the damage is limited to that one instance. The attacker can't reach other apps, other users' data, or the platform infrastructure.

## Vulnerabilities that no longer apply

Because the client has no access to APIs or credentials, several kinds of vulnerability do not apply:

**No client-side injection of AI prompts.** The server controls what prompts are sent, what tools are available, and what models are used. An attacker can't craft malicious requests by modifying client-side code because the client doesn't make those requests.

**No credential theft.** Even if a cross-site scripting vulnerability existed in the client, there are no credentials to steal. The client doesn't have API keys, database passwords, or service tokens.

**No interception of AI traffic.** The client doesn't communicate with third-party AI services. An attacker monitoring network traffic from the client sees only the encrypted connection to the Ikon server.

**No supply chain exposure.** The thin client has minimal dependencies. It has no API library, fetch wrapper, or state management framework, which are common targets for supply chain attacks.

## How authentication works

People still need to log in, of course. Ikon supports OAuth (Google, Apple, Microsoft), magic links, passkeys, anonymous sessions, and API keys for programmatic access. After the user logs in, the persistent connection carries a secure session token. The client doesn't manage tokens, refresh them, or attach them to later requests, because there are no later requests. All traffic goes over the connection that was authenticated at the start.

## The trade-off

There's no offline mode. The client depends on the server connection. For applications that need offline capability, such as note-taking apps, document editors, and read-later services, a traditional architecture with client-side logic and local storage may be more appropriate.

Interactive AI applications need to be online anyway, because they use server-side compute, external APIs, and often real-time collaboration. For these applications, depending on the server is not a real limitation, and the security model is what they need.
