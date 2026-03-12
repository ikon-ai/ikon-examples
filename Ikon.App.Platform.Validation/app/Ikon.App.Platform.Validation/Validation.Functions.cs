using System.Text.Json;

public static class ValidationFunctions
{
    [Function(Name = "AllTypes", Description = "Takes all supported parameter types and returns them as JSON", Visibility = FunctionVisibility.Shared)]
    public static string AllTypes(string s, double d, bool b, int i, List<string> list, Dictionary<string, double> dict, Guid guid, DateTime dateTime)
    {
        return JsonSerializer.Serialize(new { s, d, b, i, list, dict, guid, dateTime });
    }

    [Function(Name = "EchoBytes", Description = "Returns the same byte array back", Visibility = FunctionVisibility.Shared)]
    public static byte[] EchoBytes(byte[] data) => data;
}

public partial class Validation
{
    private void RenderFunctionsSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Server Functions (RPC)");
                view.Text([Text.Caption, "mb-4"], "Tests client-to-server function calls via the Ikon SDK function registry");
            });

            view.AddNode("function-tester", new Dictionary<string, object?>(), style: ["w-full"]);
        });
    }
}
