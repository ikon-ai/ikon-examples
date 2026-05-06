<!-- mined-from: Ikon.App.Assembler -->
# Embedded Coding Agent — Read/Edit/Write/Build/Save Toolset

Register the canonical six-tool surface for an in-app coding agent: `ReadFile`, `WriteFile`, `EditFile` (exact-match find/replace), `ListFiles`, `SearchFiles` (glob), `SearchContent` (regex), `BuildApp` (`dotnet build`), `SaveToGit`. Same shape Claude/Cursor expose — the LLM already knows how to drive this.

## When to use

You want the LLM to edit files in a sandboxed workspace inside your app. The user is talking to it in a chat panel; the agent should be able to plan, edit, build, and commit without the user filing diffs by hand.

## Snippet

```csharp
private void RegisterTools(EmergePass<AgentResponse> pass)
{
    pass.AddTool("ReadFile",
        "Read a file. Returns line-numbered output. When editing, do not include the '<line>\\t' prefix.",
        (string filePath) => ToolReadFile(filePath));

    pass.AddTool("WriteFile",
        "Create or overwrite a file. You must read a file before overwriting it.",
        (string filePath, string content) => ToolWriteFile(filePath, content));

    pass.AddTool("EditFile",
        "Replace text in a file. oldString must match exactly. Set replaceAll=true for all occurrences.",
        (string filePath, string oldString, string newString, bool replaceAll) =>
            ToolEditFile(filePath, oldString, newString, replaceAll));

    pass.AddTool("ListFiles",
        "List files and directories. Use path parameter to list a subdirectory.",
        (string path) => ToolListFiles(path));

    pass.AddTool("SearchFiles",
        "Find files matching a glob pattern (e.g. '**/*.cs').",
        (string pattern) => ToolSearchFiles(pattern));

    pass.AddTool("SearchContent",
        "Search file contents for a regex. Returns matching paths.",
        (string pattern, string path) => ToolSearchContent(pattern, path));

    pass.AddTool("BuildApp",
        "Run 'dotnet build'. Always build before saving to ensure code compiles.",
        async () => await ToolBuildApp());

    pass.AddTool("SaveToGit",
        "Commit all changes with the given message and push. Save after each meaningful change.",
        async (string message) => await ToolSaveToGit(message));
}
```

## Notes

- Two safety rails matter: (1) WriteFile/EditFile must reject if the file wasn't read first this turn (track `_knownFileVersions[fullPath] = lastWriteUtc`); (2) every path is resolved with a workspace-root containment check that rejects symlinks and `..` escapes.
- ReadFile output should include line-number prefixes (`{n}\t{line}`) — exactly the shape Claude expects. Strip them defensively in EditFile when the model copies them back.
- BuildApp tail-truncates stdout/stderr to a few KB — full build logs blow the context.
- SaveToGit should commit + push in one atomic step. Trigger preview hot-reload on success.
- Tool descriptions are part of the API surface — write them like LLM-facing docs, not internal comments.

## See also

- `mind-thread-with-tools`
- `chat-with-tool-calls`
