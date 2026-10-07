using System.Diagnostics;

namespace Ikon.App.Patterns.Examples;

// The app container guide, as code that compiles.
//
// The holder is the one file the guide reads as — the shared names are its fields, and each
// `#region example:` is one fence that ExamplePinner regenerates into the guide.

file sealed class AppContainerExamples(IApp<SessionIdentity, ClientParameters> app)
{
    public async Task RunAsync()
    {
        #region example:app-container-run
        using var renode = Process.Start(new ProcessStartInfo("renode", ["--disable-gui", "--plain", "machine.resc"])
        {
            WorkingDirectory = Path.Combine(app.DataDirectory, "hardware"),
            RedirectStandardOutput = true,
            UseShellExecute = false,
        });
        #endregion

        await (renode?.WaitForExitAsync() ?? Task.CompletedTask);
    }
}
