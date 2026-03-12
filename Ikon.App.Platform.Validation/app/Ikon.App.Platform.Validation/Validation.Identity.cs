public partial class Validation
{
    private void RenderIdentitySection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Session Identity");
                view.Text([Text.Body], $"UserId: {host.SessionIdentity.UserId}");
                view.Text([Text.Body], $"Id: {host.SessionIdentity.Id}");
                view.Text([Text.Link], host.GlobalState.SessionChannelUrl, href: host.GlobalState.SessionChannelUrl);
            });

            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Client Parameters");
                var clientParams = host.Clients[ReactiveScope.ClientId]?.Parameters;
                view.Text([Text.Body], $"Id: {clientParams?.Id}");
                view.Text([Text.Body], $"Test: {clientParams?.Test}");
            });
        });
    }
}
