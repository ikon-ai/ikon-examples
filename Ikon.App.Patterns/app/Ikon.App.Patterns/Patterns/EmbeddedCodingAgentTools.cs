namespace Ikon.App.Patterns.Patterns;

// Pattern: embedded-coding-agent-tools — see docs/patterns/embedded-coding-agent-tools.md.
// The stubs outside the region stand in for the real sandboxed file/build/git implementations so the
// tool-registration surface the doc extracts compiles on its own.
internal sealed class EmbeddedCodingAgentTools : IPatternDemo
{
    public string Slug => "embedded-coding-agent-tools";
    public string Title => "Embedded coding agent tools";
    public string Category => "AI";
    public void RenderDemo(IView view) => PatternDemoNote.RenderInfo(view, Title,
        "Registers sandboxed file/build/git tools onto an EmergePass so an embedded coding agent can read, edit, build, and commit. See the source and docs/patterns/embedded-coding-agent-tools.md.");

    private sealed record AgentResponse(string Message);
    private string ToolReadFile(string filePath) => throw new NotImplementedException();
    private string ToolWriteFile(string filePath, string content) => throw new NotImplementedException();
    private string ToolEditFile(string filePath, string oldString, string newString, bool replaceAll) => throw new NotImplementedException();
    private string ToolListFiles(string path) => throw new NotImplementedException();
    private string ToolSearchFiles(string pattern) => throw new NotImplementedException();
    private string ToolSearchContent(string pattern, string path) => throw new NotImplementedException();
    private Task<string> ToolBuildApp() => throw new NotImplementedException();
    private Task<string> ToolSaveToGit(string message) => throw new NotImplementedException();

    #region docsnippet:pattern-embedded-coding-agent-tools
    private void RegisterTools(EmergePass<AgentResponse> pass)
    {
        pass.AddTool(Tool.Of("ReadFile",
            "Read a file. Returns line-numbered output. When editing, do not include the '<line>\\t' prefix.",
            (string filePath) => ToolReadFile(filePath)));

        pass.AddTool(Tool.Of("WriteFile",
            "Create or overwrite a file. You must read a file before overwriting it.",
            (string filePath, string content) => ToolWriteFile(filePath, content)));

        pass.AddTool(Tool.Of("EditFile",
            "Replace text in a file. oldString must match exactly. Set replaceAll=true for all occurrences.",
            (string filePath, string oldString, string newString, bool replaceAll) =>
                ToolEditFile(filePath, oldString, newString, replaceAll)));

        pass.AddTool(Tool.Of("ListFiles",
            "List files and directories. Use path parameter to list a subdirectory.",
            (string path) => ToolListFiles(path)));

        pass.AddTool(Tool.Of("SearchFiles",
            "Find files matching a glob pattern (e.g. '**/*.cs').",
            (string pattern) => ToolSearchFiles(pattern)));

        pass.AddTool(Tool.Of("SearchContent",
            "Search file contents for a regex. Returns matching paths.",
            (string pattern, string path) => ToolSearchContent(pattern, path)));

        pass.AddTool(Tool.Of("BuildApp",
            "Run 'dotnet build'. Always build before saving to ensure code compiles.",
            async () => await ToolBuildApp()));

        pass.AddTool(Tool.Of("SaveToGit",
            "Commit all changes with the given message and push. Save after each meaningful change.",
            async (string message) => await ToolSaveToGit(message)));
    }
    #endregion
}
