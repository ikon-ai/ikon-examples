// The app's `/router/` edge policies — the canonical reference for the policy matrix.
//
// The gateway evaluates these BEFORE (and without) provisioning the app server, so authorization /
// identity / abuse are decided cheaply at the edge. Each policy is referenced from C# by name, e.g.
// `[HttpPost("/orders", AuthPolicy = "api-key")]`. Built-in `Auth` values not listed here:
// `EndpointAuth.Public` (anonymous) and `EndpointAuth.Grant` (a signed grant URL, the default).
//
// The helpers (`hmac` / `apiKey` / `ipAllow` / `grant`) and `secret(name)` are AMBIENT — provided by the
// runtime, no import. `secret(name)` references a value in the app's secret store; the value is resolved
// host-side and never enters this file.

export const policies = {
  // API key in a header (default `X-Api-Key`), matched against a comma-separated secret.
  'api-key': apiKey({ keys: secret('api-keys') }),

  // A custom header carrying a fixed key — same helper, explicit header.
  'demo-header': apiKey({ header: 'X-Demo-Auth', keys: secret('demo-key') }),

  // HMAC webhook: verify a signature header over the raw request body.
  'stripe-webhook': hmac({ header: 'Stripe-Signature', secret: secret('stripe-webhook') }),

  // Allow only requests from an office/CIDR range.
  'office-only': ipAllow({ cidrs: ['10.0.0.0/8', '203.0.113.7'] }),

  // The signed grant, named explicitly (same as the unset default).
  'grant': grant(),
};
