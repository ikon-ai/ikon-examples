// Validation tab with one card per reactive scope, so the validation script can check who shares each
// value: two tabs of one client session, a reload, and a second instance (?id=). The global values use
// fixed keys, so every validation run of the deployment reads and writes the same records.
public partial class Validation
{
    private readonly Reactive<int> _scopeInstance = new(0);
    private readonly ClientReactive<int> _scopeClient = new(0);
    private readonly UserReactive<int> _scopeUser = new(0);
    private readonly PersistentReactive<int> _scopePersistent = new(0, key: "validation-scope-persistent");
    private readonly PersistentUserReactive<int> _scopePersistentUser = new(0, key: "validation-scope-persistent-user");
    private readonly GlobalReactive<int> _scopeGlobal = new(0, key: "validation-scope-global");
    private readonly GlobalUserReactive<int> _scopeGlobalUser = new(0, key: "validation-scope-global-user");
    private readonly PersistentGlobalReactive<int> _scopePersistentGlobal = new(0, key: "validation-scope-persistent-global");
    private readonly PersistentGlobalUserReactive<int> _scopePersistentGlobalUser = new(0, key: "validation-scope-persistent-global-user");
    private readonly GlobalReactiveList<string> _scopeGlobalList = new(key: "validation-scope-global-list");
    private readonly GlobalReactive<int> _scopeBurst = new(0, key: "validation-scope-burst");

    private void RenderReactiveScopesSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Text([Text.H2], "Reactive Scopes");
            view.Text([Text.Body], $"Instance id: {app.SessionIdentity.Id}", props: TestId("scope-instance-id"));

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H3, "mb-4"], "Who shares each value");
                RenderScopeCounter(view, "instance", "Reactive (this instance)", _scopeInstance);
                RenderScopeCounter(view, "client", "ClientReactive (this tab)", _scopeClient);
                RenderScopeCounter(view, "user", "UserReactive (this user in this instance)", _scopeUser);
                RenderScopeCounter(view, "persistent", "PersistentReactive", _scopePersistent);
                RenderScopeCounter(view, "persistent-user", "PersistentUserReactive", _scopePersistentUser);
                RenderScopeCounter(view, "global", "GlobalReactive (every instance)", _scopeGlobal);
                RenderScopeCounter(view, "global-user", "GlobalUserReactive", _scopeGlobalUser);
                RenderScopeCounter(view, "persistent-global", "PersistentGlobalReactive", _scopePersistentGlobal);
                RenderScopeCounter(view, "persistent-global-user", "PersistentGlobalUserReactive", _scopePersistentGlobalUser);
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H3, "mb-4"], "Shared list");
                var items = _scopeGlobalList.Value;
                view.Text([Text.Body], items.Count == 0 ? "(empty)" : string.Join(", ", items), props: TestId("scope-global-list-items"));

                view.Row([Layout.Row.Md, "flex-wrap mt-3"], content: view =>
                {
                    view.Button([Button.PrimaryMd], text: "Add an item from this instance", props: TestId("scope-global-list-add"),
                        onClick: async () => _scopeGlobalList.Add($"{app.SessionIdentity.Id}-{Guid.NewGuid().ToString("N")[..6]}"));
                    view.Button([Button.OutlineErrorMd], text: "Clear the list", props: TestId("scope-global-list-clear"),
                        onClick: async () => _scopeGlobalList.Value = []);
                });
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H3, "mb-4"], "Writes in quick succession");
                view.Text([Text.Body], "Writes the value 30 times in a row. The other instances see the last one after the write delay.");
                view.Text([Text.Body], _scopeBurst.Value.ToString(), props: TestId("scope-burst-value"));
                view.Button([Button.PrimaryMd], text: "Write 30 times", props: TestId("scope-burst-run"),
                    onClick: async () =>
                    {
                        for (var i = 0; i < 30; i++)
                        {
                            _scopeBurst.Value = _scopeBurst.Value + 1;
                        }
                    });
            });
        });
    }

    private static void RenderScopeCounter(UIView view, string name, string label, Reactive<int> counter)
    {
        view.Row([Layout.Row.Md, "items-center flex-wrap py-2"], content: view =>
        {
            view.Text([Text.Body, "min-w-40 flex-1"], label);
            view.Text([Text.Body, "font-mono"], counter.Value.ToString(), props: TestId($"scope-{name}-value"));
            view.Button([Button.OutlineMd], text: $"Add one to {label}", props: TestId($"scope-{name}-inc"),
                onClick: async () => counter.Update(value => value + 1));
        });
    }
}
