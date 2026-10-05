namespace Ikon.App.Patterns.Examples;

#region example:functions-static-class
public class MathFunctions
{
    [Function(Name = "Add", Description = "Adds two numbers", Visibility = FunctionVisibility.External)]
    public static int Add(int a, int b) => a + b;
}
#endregion

#region example:functions-instance-class
[RegisterAll(Visibility = FunctionVisibility.External)]
public class GreetingFunctions(string greeting)
{
    [Function(Name = "Greet", Description = "Greets a person")]
    public string Greet(string name) => $"{greeting}, {name}!";
}
#endregion

public class VisibilityFunctions
{
    #region example:function-visibility
    [Function(Visibility = FunctionVisibility.External)]
    [RequireLogin]
    public string GetUserSecret() => "for logged-in users only";

    [Function(Visibility = FunctionVisibility.External)]
    [AllowAnonymous]
    public string GetPublicStatus() => "anyone can call this";
    #endregion
}

// The function-registry guide's policy and approval examples.

#region example:policy-custom
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

public static class PolicyExamples
{
    #region example:policy-require-approval
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

internal sealed partial class AgentGuideExamples
{

    private static async Task DocCallFunctionsAsync()
    {
        #region example:call-functions
        var sum = FunctionRegistry.Instance.Call<int>("Add", [2, 3]);
        var greeting = await FunctionRegistry.Instance.CallAsync<string>("Greet", args: ["World"]);
        #endregion

        _ = (sum, greeting);
    }

    private static void DocDirectRegistration()
    {
        #region example:direct-registration
        FunctionRegistry.Instance.AddFunction(
            Function.Register(MyMethod, "my_function",
                new FunctionAttribute { Description = "Description of what it does" }),
            FunctionVisibility.External);
        #endregion
    }

    private static void DocPipelineRegistration()
    {
        #region example:pipeline-registration
        FunctionRegistry.Instance.RegisterPipeline<MyPipeline>("run_my_pipeline", FunctionVisibility.External);
        #endregion
    }

    private static void DocRegisterFromType()
    {
        #region example:register-from-type
        FunctionRegistry.Instance.RegisterFromType(typeof(MathFunctions));
        #endregion
    }

    private static void DocRegisterFromInstance()
    {
        #region example:register-from-instance
        FunctionRegistry.Instance.RegisterFromInstance(new GreetingFunctions("Hello"));
        #endregion
    }
}
