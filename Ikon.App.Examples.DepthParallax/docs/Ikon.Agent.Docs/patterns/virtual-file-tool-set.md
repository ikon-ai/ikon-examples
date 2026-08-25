<!-- mined-from: Ikon.App.Examples.Emergence -->
# Virtual File Tool Set — Sandbox FS Tools For Coding Agents

A `Dictionary<string, string>` plays the role of a workspace, exposed to an LLM agent through five tools: `write_file`, `read_file`, `list_files`, `delete_file`, `search_in_files`. Each tool returns a small JSON-shaped object so the model can parse the outcome. Because everything is in-memory you can show the file tree in the UI live as the agent works.

## When to use

Demos and benchmarks of agentic coders, in-app code playgrounds, "build me a project from one prompt" tools — anywhere you want full agent file-tool semantics without giving the model real disk access. The sandbox makes it safe to run untrusted prompts and trivial to display file contents.

## Snippet

```csharp
private readonly Dictionary<string, string> _virtualFiles = new();

await foreach (var ev in Emerge.Run<CoderResponse>(LLMModel.Claude45Sonnet, ctx, pass =>
{
    pass.AddTool(Tool.Of("write_file", "Write content to a file. Creates the file if it doesn't exist, overwrites if it does.",
            (string path, string content) => WriteFile(state, path, content)))
        .AddTool(Tool.Of("read_file", "Read the contents of a file",
            (string path) => ReadFile(state, path)))
        .AddTool(Tool.Of("list_files", "List all files in the virtual file system",
            () => ListFiles(state)))
        .AddTool(Tool.Of("delete_file", "Delete a file from the virtual file system",
            (string path) => DeleteFile(state, path)))
        .AddTool(Tool.Of("search_in_files", "Search for a pattern in all files",
            (string pattern) => SearchInFiles(state, pattern)));

    var filesList = _virtualFiles.Count > 0
        ? $"\n\nCurrent files:\n{string.Join("\n", _virtualFiles.Keys.Select(f => $"- {f}"))}"
        : "\n\nNo files created yet.";

    pass.Command = $"""
        You are an expert software developer. Complete the following task:
        {task}
        Current workspace state:{filesList}
        Return a JSON summary when done:
        {pass.JsonSchema}
        """;

    pass.MaxIterations = maxIterations;
    pass.MaxToolCalls = 50;
}).WithCancellation(_cts!.Token)) { /* log events, update _selectedFile, etc. */ }

private object WriteFile(ExampleState state, string path, string content)
{
    var isNew = !_virtualFiles.ContainsKey(path);
    _virtualFiles[path] = content;
    var lines = content.Split('\n').Length;
    state.Log($"{(isNew ? "Created" : "Updated")} file: {path} ({lines} lines)", LogLevel.Event);
    if (string.IsNullOrEmpty(_selectedFile.Value)) _selectedFile.Value = path;
    return new { success = true, action = isNew ? "Created" : "Updated", path, lines };
}

private object ReadFile(ExampleState state, string path)
{
    if (_virtualFiles.TryGetValue(path, out var content))
        return new { success = true, path, content };
    return new { success = false, error = $"File not found: {path}" };
}
```

## Notes

- Keep the *current file list* inside `pass.Command` — without it the agent often re-creates files instead of editing them.
- Return `{success: false, error: "..."}` rather than throwing on missing-file — the model handles structured errors better than exceptions.
- The dictionary doubles as the UI source: render the files panel directly from `_virtualFiles.Keys.OrderBy(f => f)`.
- Cap `MaxIterations` and `MaxToolCalls` — coder loops can run away on ambiguous tasks.
- For a terminal tool — a `submit`/`done` whose side effect IS the answer — return `Emerge.EndRun(value)` from the tool body instead of a string. The run ends right after the current tool batch rather than looping the result back to the model for another turn, and `value` becomes the run's result when it is assignable to the run's `T`. That is the deliberate exit; `MaxIterations` is only the backstop.

## See also

- `chat-with-tool-calls`
- `emergence-event-feed`
- `plan-then-code-iteration`
