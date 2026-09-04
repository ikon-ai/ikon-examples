using Ikon.Common.Core.Functions.Policy;

namespace Ikon.App.Platform.Validation.Docs;

// The function-registry guide's policy and approval examples.

#region docsnippet:policy-custom
public sealed class RefundCeilingPolicy : IFunctionPolicy
{
    public string Name => "refund-ceiling";

    public int Priority => 50;

    public ValueTask<PolicyDecision> EvaluateAsync(object?[] args, PolicyCallContext context)
    {
        var amount = PolicyArgs.Required<decimal>(args, 0);

        if (amount > 500m)
        {
            return new ValueTask<PolicyDecision>(
                PolicyDecision.Denied($"Refunds above 500 are not automatic", "refund_too_large"));
        }

        return new ValueTask<PolicyDecision>(PolicyDecision.Allowed());
    }
}
#endregion

public static class PolicyDocs
{
    #region docsnippet:policy-require-approval
    [Function(Visibility = FunctionVisibility.External)]
    [RequireLogin]
    [PolicyType(typeof(RefundCeilingPolicy))]
    [RequireApproval(Reason = "Refunds are paid out immediately", ApproverType = ApproverType.SpecificUser,
        UserId = "finance-lead")]
    public static Task<string> RefundAsync(decimal amount)
    {
        return Task.FromResult($"Refunded {amount}");
    }
    #endregion
}
