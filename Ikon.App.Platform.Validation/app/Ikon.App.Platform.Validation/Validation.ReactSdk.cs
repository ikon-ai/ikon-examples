public partial class Validation
{
    // The sibling probe opens a second connection with this Test parameter; counting the clients that
    // arrive with it is the server-side proof that useSiblingClient really connected.
    private const string SiblingClientTestParameter = "validation-sibling";
    private readonly Reactive<int> _siblingClientCount = new(0);
    private readonly Reactive<string> _lastSiblingClient = new("none");

    private void InitSiblingClientEvents()
    {
        app.OnClientJoined((clientContext, clientParams) =>
        {
            if (clientParams.Test == SiblingClientTestParameter)
            {
                _siblingClientCount.Value++;
                _lastSiblingClient.Value = $"session {clientContext.SessionId} joined at {DateTime.UtcNow:HH:mm:ss}";
            }

            return Task.CompletedTask;
        });

        app.OnClientLeft((clientContext, clientParams) =>
        {
            if (clientParams.Test == SiblingClientTestParameter)
            {
                _siblingClientCount.Value = Math.Max(0, _siblingClientCount.Value - 1);
                _lastSiblingClient.Value = $"session {clientContext.SessionId} left at {DateTime.UtcNow:HH:mm:ss}";
            }

            return Task.CompletedTask;
        });
    }

    // The client-side SDK surface a custom React frontend uses. Everything else in the app is rendered
    // from C#; these are the calls that only exist in the browser, so they are exercised from React.
    private void RenderReactSdkSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "React SDK");

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H3, "mb-4"], "Server functions");
                view.AddNode("function-tester", new Dictionary<string, object?>(), style: ["w-full"]);
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H3, "mb-4"], "Consent");
                view.AddNode("validation-consent-panel", new Dictionary<string, object?>
                {
                    ["purposes"] = string.Join(",", ConsentCardPurposes),
                    ["version"] = _consentVersion.Value,
                }, style: ["w-full"]);
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H3, "mb-4"], "Account removal");
                view.AddNode("validation-account-panel", new Dictionary<string, object?>(), style: ["w-full"]);
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H3, "mb-4"], "Feedback, sibling client and capture");

                RenderFieldGrid(view,
                    ("Sibling clients", v => v.Text([Text.Body], _siblingClientCount.Value.ToString(), props: TestId("identity-sibling-count"))),
                    ("Last sibling", v => v.Text([Text.Body], _lastSiblingClient.Value, props: TestId("identity-sibling-last"))));

                view.AddNode("validation-sdk-probe", new Dictionary<string, object?>
                {
                    ["siblingTest"] = SiblingClientTestParameter,
                    ["cameraEchoStreamId"] = _cameraEchoStreamId.Value,
                    ["screenEchoStreamId"] = _screenEchoStreamId.Value,
                }, style: ["w-full mt-4"]);
            });
        });
    }
}
