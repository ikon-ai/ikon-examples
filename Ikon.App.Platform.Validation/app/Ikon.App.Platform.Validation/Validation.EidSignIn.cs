public partial class Validation
{
    // Only an eID sign-in enters the "eid" surface, at /surfaces/eid. A visitor who has not signed in
    // enters "eid-login" at the same path, which asks for the shared password before it offers the eID:
    // the stock sign-in screen would offer it to everyone who opens the app, and each eID sign-in is billed.
    private const string EidSurface = "eid";
    private const string EidLoginSurface = "eid-login";

    private readonly ClientReactive<string> _eidSignInStatus = new("");

    private void RenderEidSignInPage(UIView view)
    {
        view.Column([Layout.Column.Lg, Container.Xl4, "py-8 px-4"], content: view =>
        {
            view.Text([Text.H1], "eID sign-in");

            switch (_enteredSurface.Value)
            {
                case EidSurface:
                    RenderEidSignedIn(view);
                    break;
                case EidLoginSurface:
                    RenderEidSignInStart(view);
                    break;
                // A deploy captures the page's snapshot before any client has entered a surface
                case "":
                    view.Text([Text.Body, "text-tertiary"], "Loading this page");
                    break;
                default:
                    view.Text([Text.Body, "text-error-primary"], $"FAIL the page at /surfaces/eid entered the surface {_enteredSurface.Value}",
                        props: TestId("eid-signin-status"));
                    break;
            }
        });
    }

    private void RenderEidSignInStart(UIView view)
    {
        if (RenderSectionLocked(view, "eID sign-in"))
        {
            return;
        }

        view.Box([Card.Default, "p-6"], content: view =>
        {
            view.Text([Text.Body, "mb-4"], "Signs in through Signicat. On the development platform, Signicat's sandbox offers a Norwegian BankID simulator instead of a real eID.");
            view.Button([Button.PrimaryMd], text: "Sign in with eID", onClick: StartEidSignInAsync, props: TestId("eid-signin-start"));

            if (!string.IsNullOrEmpty(_eidSignInStatus.Value))
            {
                view.Text([Text.Body, "mt-3 text-error-primary"], _eidSignInStatus.Value, props: TestId("eid-signin-status"));
            }
        });
    }

    private async Task StartEidSignInAsync()
    {
        _eidSignInStatus.Value = "";

        try
        {
            // False inside a frame, where the SDK never starts an eID sign-in
            if (!await ClientFunctions.LoginAsync("signicat"))
            {
                _eidSignInStatus.Value = "FAIL the browser did not start the eID sign-in";
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Warning(ex, $"eID sign-in start failed for client {ReactiveScope.ClientId}");
            _eidSignInStatus.Value = $"FAIL {ex.Message}";
        }
    }

    private void RenderEidSignedIn(UIView view)
    {
        view.Box([Card.Default, "p-6"], content: view =>
        {
            if (!_identityLoaded.Value)
            {
                view.Spinner();
                return;
            }

            // An eID gives the account its full name and nothing that fills a preferred or first name,
            // so the visible name stays empty
            bool passed = !_identityIsAnonymous.Value && _identityAuthProvider.Value == "signicat";
            string verdict = passed
                ? $"PASS signed in with signicat as {Displayed(_identityName.Value)}"
                : $"FAIL entered the eID surface signed in with {Displayed(_identityAuthProvider.Value)}";
            view.Text([Text.Body, "mb-4", passed ? "text-success-primary" : "text-error-primary"], verdict, props: TestId("eid-signin-status"));

            RenderFieldGrid(view,
                ("User ID", v => v.Text([Text.Body], _identityUserId.Value, props: TestId("eid-user-id"))),
                ("Name", v => v.Text([Text.Body], Displayed(_identityName.Value), props: TestId("eid-name"))));

            view.Button([Button.ErrorMd, "mt-4"], text: "Sign out", onClick: async () => await ClientFunctions.LogoutAsync(), props: TestId("eid-sign-out"));
        });
    }
}
