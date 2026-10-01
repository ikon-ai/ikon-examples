using System.Text.Json;

public static class ValidationFunctions
{
    [Function(Name = "AllTypes", Description = "Takes all supported parameter types and returns them as JSON", Visibility = FunctionVisibility.External)]
    public static string AllTypes(string s, double d, bool b, int i, List<string> list, Dictionary<string, double> dict, Guid guid, DateTime dateTime)
    {
        return JsonSerializer.Serialize(new { s, d, b, i, list, dict, guid, dateTime });
    }

    [Function(Name = "EchoBytes", Description = "Returns the same byte array back", Visibility = FunctionVisibility.External)]
    public static byte[] EchoBytes(byte[] data) => data;
}
