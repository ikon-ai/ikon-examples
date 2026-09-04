namespace Ikon.Common.Core.Functions.Policy
  // Use this on framework-shipped or genuinely public endpoints where capability is provided by something other than session auth (e.g. a stableId, a webhook signature, or the endpoint being read-only public). Pair with explicit [RateLimit] when abuse is a concern.
  sealed class AllowAnonymousAttribute : Attribute
    ctor()
  sealed class ApprovalAuditEntry
    ctor(Guid approvalId, Guid callId, string functionName, int approverSessionId, string? approverUserId, bool approved, string? reason, string policyName, DateTimeOffset timestamp)
    Guid ApprovalId { get; }
    bool Approved { get; }
    int ApproverSessionId { get; }
    string? ApproverUserId { get; }
    Guid CallId { get; }
    string FunctionName { get; }
    string PolicyName { get; }
    string? Reason { get; }
    DateTimeOffset Timestamp { get; }
    static ApprovalAuditEntry CreateApproved(Guid approvalId, Guid callId, string functionName, int approverSessionId, string? approverUserId, string policyName)
    static ApprovalAuditEntry CreateRejected(Guid approvalId, Guid callId, string functionName, int approverSessionId, string? approverUserId, string? reason, string policyName)
  sealed class ApprovalContext
    Guid ApprovalId { get; }
    // The raw token is only provided to the designated approver via protocol.
    string ApprovalTokenHash { get; }
    object?[] Args { get; }
    string ArgsHash { get; }
    PolicyCallContext CallContext { get; }
    int CallerSessionId { get; }
    DateTimeOffset ExpiresAt { get; }
    string FunctionName { get; }
    string Reason { get; }
    // Always at least PolicyDecision.MinExpirySeconds (30 seconds).
    int TimeoutSeconds { get; }
    // The raw token must only be sent to the designated approver.
    // functionName: The name of the function requiring approval.
    // reason: The reason why approval is required.
    // args: The arguments being passed to the function.
    // callContext: The original policy call context.
    // timeoutSeconds: The timeout in seconds (minimum 30).
    static (ApprovalContext Context, Guid RawToken) Create(string functionName, string reason, object?[] args, PolicyCallContext callContext, int timeoutSeconds = 300)
    bool IsExpired()
    // Uses constant-time comparison of hashes to prevent timing attacks.
    // providedToken: The token GUID provided by the approver.
    bool ValidateToken(Guid providedToken)
    // providedToken: The token string provided by the approver.
    bool ValidateToken(string providedToken)
  delegate ApprovalHandlerDelegate
    Task<ApprovalResult> ApprovalHandlerDelegate(ApprovalContext context)
  readonly struct ApprovalResult
    bool IsApproved { get; }
    string? RejectionReason { get; }
    static ApprovalResult Approved()
    static ApprovalResult Rejected(string? reason = null)
    override string ToString()
  enum ApproverType
    Caller
    SpecificClient
    SpecificUser
  interface IFunctionPolicy
    string Name { get; }
    // Lower values are evaluated first; the default is 100.
    virtual int Priority { get; }
    // args: The arguments being passed to the function.
    // context: The policy call context with metadata about the call.
    ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
  static class PolicyArgs
    // args: The arguments array.
    // requiredIndices: The indices that must have non-null values.
    static bool HasAll(object?[] args, params int[] requiredIndices)
    // args: The arguments array.
    // index: The zero-based index of the argument.
    // defaultValue: The default value to return if the argument is missing or null.
    static T? Optional<T>(object?[] args, int index, T? defaultValue = default)
    // args: The arguments array.
    // index: The zero-based index of the argument.
    // throws PolicyDeniedException: Thrown if the argument is missing, null, or wrong type.
    static T Required<T>(object?[] args, int index)
    // args: The arguments array.
    // index: The zero-based index of the argument.
    // value: The output value if successful.
    static bool TryGet<T>(object?[] args, int index, out T? value)
  abstract class PolicyAttribute : Attribute
    // Lower values are evaluated first.
    int Priority { get; set; }
  sealed class PolicyAttribute<TPolicy> : PolicyAttribute where TPolicy : IFunctionPolicy, new()
    ctor()
  sealed class PolicyCallContext
    ctor(Guid callId, string functionName, int callerSessionId, string? userId, string? tenantId, Guid? instanceId, bool isInternal, CancellationToken cancellationToken, string? authSessionId = null, bool? isAnonymous = null, DateTime? callTimestamp = null, IReadOnlyDictionary<string, object?>? additionalContext = null)
    IReadOnlyDictionary<string, object?>? AdditionalContext { get; }
    // A per-login correlation identifier, not an authentication flag — use IsAnonymous for guest detection.
    string? AuthSessionId { get; }
    Guid CallId { get; }
    DateTime CallTimestamp { get; }
    int CallerSessionId { get; }
    CancellationToken CancellationToken { get; }
    string FunctionName { get; }
    Guid? InstanceId { get; }
    // true for a guest, false for an authenticated user or machine, null when unknown (no resolvable client context for the caller session).
    bool? IsAnonymous { get; }
    bool IsInternal { get; }
    string? TenantId { get; }
    string? UserId { get; }
  // A discriminated union with three states: Allow, Deny, or NeedsApproval — pattern match on the subtypes.
  abstract class PolicyDecision
    static PolicyDecision Allowed()
    // reason: The reason for denying the function call.
    // code: Optional error code for programmatic handling.
    static PolicyDecision Denied(string reason, string? code = null)
    // message: The message explaining why approval is required.
    static PolicyDecision RequireApproval(string message)
    // message: The message explaining why approval is required.
    // expirySeconds: How long the approval request is valid (minimum 30 seconds).
    static PolicyDecision RequireApproval(string message, int expirySeconds)
    // message: The message explaining why approval is required.
    // handler: The custom handler to process the approval request.
    static PolicyDecision RequireApproval(string message, ApprovalHandlerDelegate handler)
    // message: The message explaining why approval is required.
    // expirySeconds: How long the approval request is valid (minimum 30 seconds).
    // handler: The custom handler to process the approval request.
    static PolicyDecision RequireApproval(string message, int expirySeconds, ApprovalHandlerDelegate handler)
    const int DefaultExpirySeconds = 300
    const int MinExpirySeconds = 30
  sealed class PolicyDecision.Allow : PolicyDecision
  sealed class PolicyDecision.Deny : PolicyDecision
    string? Code { get; }
    string Reason { get; }
  sealed class PolicyDecision.NeedsApproval : PolicyDecision
    int ExpirySeconds { get; }
    ApprovalHandlerDelegate? Handler { get; }
    string Message { get; }
  delegate PolicyDelegate
    ValueTask<PolicyDecision> PolicyDelegate(object?[] args, PolicyCallContext context)
  sealed class PolicyDeniedException : Exception
    // reason: The reason for denying the call.
    // code: Error code for programmatic handling (e.g., "rate_limit_exceeded", "bad_args").
    ctor(string? reason, string? code)
    // reason: The reason for denying the call.
    // code: Optional error code for programmatic handling.
    // policyName: The name of the policy that denied the call.
    // functionName: The name of the function that was denied.
    ctor(string? reason, string? code, string? policyName, string? functionName)
    ctor(string? reason, Exception innerException, string? policyName = null, string? functionName = null)
    ctor(string? reason, string? code, Exception innerException, string? policyName = null, string? functionName = null)
    string? Code { get; }
    string? FunctionName { get; }
    string? PolicyName { get; }
  sealed class PolicyEvaluationResult
    ctor(PolicyDecision decision, string functionName, Guid callId, string? decidingPolicyName, TimeSpan evaluationDuration)
    Guid CallId { get; }
    // Null if the decision is Allow.
    string? DecidingPolicyName { get; }
    PolicyDecision Decision { get; }
    TimeSpan EvaluationDuration { get; }
    string FunctionName { get; }
    bool IsAllowed { get; }
    bool IsDenied { get; }
    bool RequiresApproval { get; }
    static PolicyEvaluationResult Allowed(string functionName, Guid callId)
    static PolicyEvaluationResult Denied(string functionName, Guid callId, string? reason, string policyName, TimeSpan evaluationDuration)
    static PolicyEvaluationResult Denied(string functionName, Guid callId, string reason, string? code, string policyName, TimeSpan evaluationDuration)
    static PolicyEvaluationResult NeedsApproval(PolicyDecision decision, string functionName, Guid callId, string policyName, TimeSpan evaluationDuration)
    override string ToString()
  sealed class PolicyTypeAttribute : PolicyAttribute
    // policyType: The type of policy to create. Must implement IFunctionPolicy and have a parameterless constructor.
    ctor(Type policyType)
    Type PolicyType { get; }
  sealed class RateLimitAttribute : PolicyAttribute
    // limit: Maximum number of calls allowed in the window.
    ctor(int limit, int windowSeconds)
    int Limit { get; }
    // If true, the rate limit is per-session; if false (the default), it is global.
    bool PerSession { get; set; }
    int WindowSeconds { get; }
  sealed class RequireApprovalAttribute : PolicyAttribute
    ctor()
    ApproverType ApproverType { get; set; }
    // Only used when ApproverType is SpecificClient.
    int ClientSessionId { get; set; }
    string Reason { get; set; }
    // Only used when ApproverType is SpecificUser.
    string? UserId { get; set; }
  // Guest (anonymous) callers are denied with the "login_required" error code. The Ikon client runtime intercepts this and triggers the deferred-login flow.
  sealed class RequireLoginAttribute : PolicyAttribute
    ctor()
  // Internal callers (PolicyCallContext.IsInternal) bypass the check — same as LoggedInPolicy — because in-process callers are already trusted.
  sealed class RequireRoleAttribute : PolicyAttribute
    ctor(params string[] roles)
    bool RequireAll { get; set; }
    string[] RequiredRoles { get; }
