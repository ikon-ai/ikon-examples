namespace Ikon.App.Patterns.Examples;

public static class RetrierExamples
{
    #region example:retrier-basic
    public static async Task<string> FetchAsync(HttpClient http, string url, CancellationToken ct)
    {
        return await Retrier.RunAsync(
            async token => await http.GetStringAsync(url, token),
            ct,
            retries: 3,
            onFailure: async ex =>
            {
                Log.Instance.Warning($"Fetch of {url} failed, giving up", ex);
                await Task.CompletedTask;
            },
            description: $"fetch {url}");
    }
    #endregion
}
