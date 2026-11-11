using Ikon.App.Sso;
using Ikon.App.Triggers;
using Ikon.Common.Core;
using Ikon.Common.Core.Protocol;

namespace Ikon.App.Platform.Patterns.Examples;

// The enterprise SSO guide, as code that compiles.

file sealed class SsoGuideExamples(IApp<SessionIdentity, ClientParameters> app)
{
    public async Task<SsoConnection> CreateEntraAsync(string tenantName, string directoryId)
    {
        #region example:sso-create-entra
        // The customer's admin consents to the platform's registration in their directory, so
        // there is no client secret to collect — only the directory id.
        var connection = await app.SsoConnections.CreateAsync(new SsoConnectionOptions
        {
            Name = tenantName,
            Preset = SsoPreset.MicrosoftEntra,
            Registration = SsoRegistration.Platform,
            EntraTenantId = directoryId,
            EmailDomains = ["lawfirm.example"],
            SessionMaxAge = TimeSpan.FromHours(10),
        });
        #endregion

        return connection;
    }

    public async Task<SsoConnection> CreateOidcAsync(string clientSecret)
    {
        #region example:sso-create-oidc
        var connection = await app.SsoConnections.CreateAsync(new SsoConnectionOptions
        {
            Name = "Law Firm",
            Preset = SsoPreset.Oidc,
            Issuer = "https://lawfirm.okta.com",
            ClientId = "0oa1b2c3d4",
            ClientSecret = clientSecret,
            EmailDomains = ["lawfirm.example"],
        });

        // A generic identity provider signs no one in until its domain is proven. Show the admin the record to publish.
        foreach (var domain in connection.EmailDomains)
        {
            Log.Instance.Info($"Publish TXT {domain.RecordName} = {domain.Value}");
        }
        #endregion

        return connection;
    }

    public async Task VerifyAsync(SsoConnection connection)
    {
        #region example:sso-verify-domain
        // Once the TXT record is published. The platform keeps looking for about two hours.
        connection = await app.SsoConnections.VerifyDomainAsync(connection.Id, "lawfirm.example");

        // Offer the identity provider up front on the tenant's own host.
        connection = await app.SsoConnections.SetHostsAsync(connection.Id, ["lawfirm.yourapp.com"]);
        #endregion
    }

    public async Task RequireAsync(SsoConnection connection)
    {
        #region example:sso-require
        // Admit everyone, but record who still signs in some other way…
        await app.SsoConnections.UpdateAsync(connection.Id, new SsoConnectionUpdate { RequiredMode = SsoRequiredMode.Warn });

        var stragglers = await app.SsoConnections.ListRequiredWarningsAsync(connection.Id);

        // …then, once nobody is left, send the domain's users to their identity provider.
        if (stragglers.Count == 0)
        {
            await app.SsoConnections.UpdateAsync(connection.Id, new SsoConnectionUpdate { RequiredMode = SsoRequiredMode.Enforce });
        }
        #endregion
    }

    public async Task GateAsync(Context context, string tenantConnectionId)
    {
        #region example:sso-read-context
        // A softer policy than `Enforce`, decided per tenant by the app itself.
        if (context.AuthProvider != "sso" || context.SsoConnectionId != tenantConnectionId)
        {
            await ClientFunctions.LoginAsync($"sso:{tenantConnectionId}", context.SessionId);
        }
        #endregion
    }

    public async Task<SsoProvisioningToken> EnableProvisioningAsync(SsoConnection connection)
    {
        #region example:sso-provisioning-enable
        await app.SsoConnections.UpdateProvisioningAsync(connection.Id, new SsoProvisioningUpdate { Enabled = true });

        // Shown once: the customer's admin pastes both into their directory's provisioning settings.
        var issued = await app.SsoConnections.RotateProvisioningTokenAsync(connection.Id);
        Log.Instance.Info($"Tenant URL {issued.ScimUrl}, secret token {issued.Token}");
        #endregion

        return issued;
    }
}

// Abstract so the bundle scan passes it over: this is an example of an app's listener, not a
// listener of this app.
file abstract class SsoProvisioningExamples
{
    private readonly Dictionary<string, int> _appliedVersions = new();

    #region example:sso-provisioning-trigger
    [Trigger(TriggerEventType.SsoProvisioning)]
    internal Task OnSsoProvisioningAsync(TriggerContext context)
    {
        var person = context.GetPayload<SsoProvisioningEvent>();

        // A retried event can arrive after a newer one; the newest version wins.
        if (_appliedVersions.TryGetValue(person.Id, out int applied) && applied >= person.Version)
        {
            return Task.CompletedTask;
        }

        // Create or update the member keyed on person.Id, never on the email, which a
        // directory may give to someone new. Deactivate them when !person.Active.
        _appliedVersions[person.Id] = person.Version;
        return Task.CompletedTask;
    }
    #endregion
}
