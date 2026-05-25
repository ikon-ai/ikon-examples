# Ikon.Code Public API

namespace Ikon.Code
  sealed class ClassCard : IEquatable<ClassCard>
    ctor(string SymbolId, string FqName, int Loc, string Summary, string Source, ImmutableArray<ExternalCall> ExternalCalls)
    ImmutableArray<ExternalCall> ExternalCalls { get; init; }
    string FqName { get; init; }
    int Loc { get; init; }
    string Source { get; init; }
    string Summary { get; init; }
    string SymbolId { get; init; }
  static class ClassCardBuilder
    static Task<ImmutableArray<ClassCard>> BuildClassCardsAsync(Compilation comp, GraphIndex graph, CancellationToken ct)
  static class ClassCardRanker
    static ImmutableArray<ClassCard> Rank(ImmutableArray<ClassCard> cards, string query, ISymbol? caretSymbol)
  sealed class CompilationGraphPipeline
    ctor()
    Task Run(Pipeline<T>.Branch<Item> input, CancellationToken ct)
  static class CompilationLoader
    static Task<Compilation> AnalyzeProjectAsync(Item projectItem, CancellationToken ct)
  class EventStreamAnalyzer
    ctor(int batchSize = 100, int overlapSize = 20, TimeSpan? maxInterval = null)
    IAsyncEnumerable<EventStreamSummary> AnalyzeAsync(IAsyncEnumerable<ProtocolMessage> stream, CancellationToken cancellationToken = null)
  class EventStreamSummary
    ctor()
    string History { get; set; }
    string Important { get; set; }
    string Summary { get; set; }
  sealed class ExternalCall : IEquatable<ExternalCall>
    ctor(string Namespace, string Member)
    string Member { get; init; }
    string Namespace { get; init; }
  static class GraphBuilder
    static Task<GraphIndex> BuildCallGraphAsync(Compilation comp, CancellationToken ct)
  sealed class GraphIndex : IEquatable<GraphIndex>
    ctor(ImmutableDictionary<IMethodSymbol, ImmutableHashSet<IMethodSymbol>> ForwardEdges, ImmutableDictionary<INamedTypeSymbol, ImmutableHashSet<ISymbol>> TypeMembers, ImmutableDictionary<INamespaceSymbol, ImmutableHashSet<INamedTypeSymbol>> NamespaceTypes, ImmutableHashSet<ISymbol> PublicApiRoots)
    ImmutableDictionary<IMethodSymbol, ImmutableHashSet<IMethodSymbol>> ForwardEdges { get; init; }
    ImmutableDictionary<INamespaceSymbol, ImmutableHashSet<INamedTypeSymbol>> NamespaceTypes { get; init; }
    ImmutableHashSet<ISymbol> PublicApiRoots { get; init; }
    ImmutableDictionary<INamedTypeSymbol, ImmutableHashSet<ISymbol>> TypeMembers { get; init; }
  // Placeholder result object returned by all Orchestrator‑tool invocations. Replace with the actual implementation from your SDK.
  sealed class MindResult : IEquatable<MindResult>
    ctor()
    // Error message if Success is false.
    string? Error { get; init; }
    // Arbitrary payload returned by the tool.
    object? Payload { get; init; }
    // True if the tool execution succeeded.
    bool Success { get; init; }
  // Typed façade for every tool exposed by the Orchestrator runtime. Each method matches one JSON‑tool definition and returns a MindResult .
  static class MindTools
    // codebase_search Find snippets of code from the codebase most relevant to a search query.
    static Task<MindResult> CodebaseSearchAsync(string query, string? explanation = null, IEnumerable<string>? targetDirectories = null)
    // create_diagram Produce a Mermaid diagram.
    static Task<MindResult> CreateDiagramAsync(string content)
    // delete_file Delete a file relative to the workspace root.
    static Task<MindResult> DeleteFileAsync(string targetFile, string? explanation = null)
    // edit_file Propose edits or create a new file.
    static Task<MindResult> EditFileAsync(string targetFile, string instructions, string codeEdit)
    // edit_notebook Edit or create a Jupyter notebook cell.
    static Task<MindResult> EditNotebookAsync(string targetNotebook, int cellIdx, bool isNewCell, string cellLanguage, string oldString, string newString)
    // file-patch Apply a unified-diff patch to a file.
    static Task<MindResult> FilePatchAsync(IAstWorkspace workspace, string targetFile, string patch)
    // file-read Read the entire contents of a file.
    static Task<MindResult> FileReadAsync(IAstWorkspace workspace, string targetFile)
    // file_search Fuzzy filename search in the workspace.
    static Task<MindResult> FileSearchAsync(string query, string explanation)
    // file-write Write content to a file, replacing existing content.
    static Task<MindResult> FileWriteAsync(IAstWorkspace workspace, string targetFile, string contents)
    // grep_search Exact regex search over workspace files.
    static Task<MindResult> GrepSearchAsync(string query, bool caseSensitive = false, string? includePattern = null, string? excludePattern = null, string? explanation = null)
    // list_dir List the contents of a directory relative to the workspace root.
    static Task<MindResult> ListDirAsync(string relativeWorkspacePath, string? explanation = null)
    // read_file Read a slice (or the whole) of a file from the workspace.
    static Task<MindResult> ReadFileAsync(string targetFile, bool shouldReadEntireFile, int startLineOneIndexed, int endLineOneIndexedInclusive, string? explanation = null)
    // reapply Re‑apply the last edit to a target file using a smarter model.
    static Task<MindResult> ReapplyAsync(string targetFile)
    // run_terminal_cmd Propose a command to be executed in the user's shell.
    static Task<MindResult> RunTerminalCmdAsync(string command, bool isBackground, string? explanation = null)
    // search_replace Replace one unique string occurrence in a file.
    static Task<MindResult> SearchReplaceAsync(string filePath, string oldString, string newString)
    // web_search Real‑time web search.
    static Task<MindResult> WebSearchAsync(string searchTerm, string? explanation = null)
  static class PromptAssembler
    static string AssemblePrompt(IEnumerable<ClassCard> cards, string userQuery)
  class TaskFingerprint
    ctor()
    List<string> Keywords { get; set; }
    string TaskBlurb { get; set; }

namespace Ikon.Code.AST
  class AddMemberAction : AstAction, IEquatable<AddMemberAction>
    ctor(AstSymbolKey ContainerKey, string ContainerKind, string ContainerName, string MemberKind, string MemberName, string Snippet, int? Position = null)
    AstSymbolKey ContainerKey { get; init; }
    string ContainerKind { get; init; }
    string ContainerName { get; init; }
    string MemberKind { get; init; }
    string MemberName { get; init; }
    int? Position { get; init; }
    string Snippet { get; init; }
  abstract class AstAction : IEquatable<AstAction>
  class AstDiagnostic
    ctor()
    IReadOnlyList<AstDiagnosticLocation> AdditionalLocations { get; init; }
    string Category { get; init; }
    string Id { get; init; }
    bool IsSuppressed { get; init; }
    AstDiagnosticLocation? Location { get; init; }
    string Message { get; init; }
    IReadOnlyDictionary<string, string?> Properties { get; init; }
    AstDiagnosticSeverity Severity { get; init; }
    override string ToString()
  class AstDiagnosticLocation
    ctor()
    int Column { get; init; }
    string FilePath { get; init; }
    int Line { get; init; }
    override string ToString()
  enum AstDiagnosticSeverity
    Hidden
    Info
    Warning
    Error
  class AstDiagnostics : IEquatable<AstDiagnostics>
    ctor()
    IEnumerable<AstDiagnostic> Errors { get; }
    bool HasErrors { get; }
    bool HasWarnings { get; }
    IEnumerable<AstDiagnostic> Warnings { get; }
    IReadOnlyList<AstDiagnostic> All
  // Represents a symbol in the code, such as a class, method, property, etc.
  class AstSymbol : IEquatable<AstSymbol>
    // Represents a symbol in the code, such as a class, method, property, etc.
    ctor()
    // Fully qualified id of the containing symbol, if any.
    string? ContainerId { get; init; }
    // Full XML/markdown documentation, if available.
    string? Documentation { get; init; }
    // Determines if the symbol is in user source or in external libs. ///
    bool IsInSource { get; init; }
    // Fully qualified id (TYPE:namespace.type.member(params)
    AstSymbolKey Key { get; init; }
    // The kind of symbol (type, method, property, etc.)
    AstSymbolKind Kind { get; init; }
    // The primary location in the source code.
    AstDiagnosticLocation? Location { get; init; }
    // Short name of the symbol.
    string Name { get; init; }
    // Other related symbols (e.g., overrides, implementations, etc.)
    IReadOnlyList<string> RelatedSymbols { get; init; }
    // For search, a relevance score (optional).
    double RelevanceScore { get; init; }
    // The signature (parameters, return type, etc.)
    string Signature { get; init; }
    // Full source of the item and the surrounding context.
    string Source { get; }
    // Short summary documentation.
    string? Summary { get; init; }
  class AstSymbolKey : IEquatable<AstSymbolKey>
    ctor(string? Namespace = null, string? TypeName = null, string? MemberName = null, string? Kind = null, string[]? ParameterTypes = null)
    string? Kind { get; init; }
    string? MemberName { get; init; }
    string? Namespace { get; init; }
    string[]? ParameterTypes { get; init; }
    string? TypeName { get; init; }
    AstSymbolKey? GetParent()
    AstSymbolKey WithMember(string member, string? kind = null, string[]? parameterTypes = null)
    AstSymbolKey WithoutName()
    AstSymbolKey WithoutParameters()
  enum AstSymbolKind
    Unknown
    Namespace
    Class
    Struct
    Interface
    Enum
    EnumMember
    Method
    Property
    Field
    Event
    Delegate
    Parameter
    Local
  class AstSymbolSearchOptions
    ctor()
    // Case sensitivity in matching.
    bool CaseSensitive { get; set; }
    // Include external (referenced) symbols in search.
    bool IncludeExternalDocs { get; set; }
    // Optional filter: restrict to a particular kind.
    AstSymbolKind? Kind { get; set; }
    // How many results to return.
    int MaxResults { get; set; }
  class AstTestCaseResult
    ctor()
    TimeSpan Duration { get; init; }
    string? Message { get; init; }
    string Name { get; init; }
    AstTestOutcome Outcome { get; init; }
    string? StackTrace { get; init; }
  enum AstTestOutcome
    Passed
    Failed
    Skipped
    Unknown
  class AstTestResults
    ctor()
    string RawOutput { get; init; }
    bool Success { get; }
    IReadOnlyList<AstTestCaseResult> Tests { get; init; }
  static class AstWorkspaceLoader
    static Task<IAstWorkspace> LoadAsync(string projectPath, bool initSymbols = false, CancellationToken ct = null)
  class ChangeAccessibilityAction : AstAction, IEquatable<ChangeAccessibilityAction>
    ctor(AstSymbolKey TargetKey, string OldAccessibility, string NewAccessibility)
    string NewAccessibility { get; init; }
    string OldAccessibility { get; init; }
    AstSymbolKey TargetKey { get; init; }
  class ChangeAttributesAction : AstAction, IEquatable<ChangeAttributesAction>
    ctor(AstSymbolKey TargetKey, string[] OldAttributes, string[] NewAttributes)
    string[] NewAttributes { get; init; }
    string[] OldAttributes { get; init; }
    AstSymbolKey TargetKey { get; init; }
  sealed class ChangeReturnTypeAction : AstAction, IEquatable<ChangeReturnTypeAction>
    ctor(string TargetId, string NewReturnType)
    string NewReturnType { get; init; }
    string TargetId { get; init; }
  class ChangeSignatureAction : AstAction, IEquatable<ChangeSignatureAction>
    ctor(AstSymbolKey MemberKey, ImmutableArray<string> NewParameters)
    AstSymbolKey MemberKey { get; init; }
    ImmutableArray<string> NewParameters { get; init; }
  class ChangeSignatureAction2 : AstAction, IEquatable<ChangeSignatureAction2>
    ctor(AstSymbolKey MemberKey, string[] NewParameters, string[]? NewTypeParameters, string? NewReturnType)
    AstSymbolKey MemberKey { get; init; }
    string[] NewParameters { get; init; }
    string? NewReturnType { get; init; }
    string[]? NewTypeParameters { get; init; }
  class ChangeTypeAction : AstAction, IEquatable<ChangeTypeAction>
    ctor(AstSymbolKey MemberKey, string NewType)
    AstSymbolKey MemberKey { get; init; }
    string NewType { get; init; }
  // Symbols in a graph to enable following references and do call chain walking.
  interface IAstSymbolGraph
    abstract Task<IReadOnlyList<AstSymbol>> GetReferencedSymbols(AstSymbol symbol)
    abstract Task<IReadOnlyList<AstSymbol>> GetReferencingSymbols(AstSymbol symbol)
  // A navigable, queryable tree of symbols within a workspace.
  interface IAstSymbolTree
    virtual IEnumerable<AstSymbol> Descendants(AstSymbol root, int max = 1000)
    abstract AstSymbol? FindSymbolByKey(AstSymbolKey key)
    abstract AstSymbol? FindSymbolByName(string name)
    abstract IReadOnlyList<AstSymbol> FindSymbols(Func<AstSymbol, bool> predicate)
    abstract IReadOnlyList<AstSymbol> GetChildren(AstSymbol symbol)
    abstract IReadOnlyList<AstSymbol> GetRootSymbols()
    abstract IReadOnlyList<AstSymbol> SearchSymbols(string query, int maxResults = 10)
  // Runs tests in a workspace and returns structured results.
  interface IAstTestRunner
    // Runs all available tests in the given workspace/project/solution.
    abstract Task<AstTestResults> RunAllTestsAsync(string projectOrSolutionPath, CancellationToken ct = null)
    // Optionally run only selected tests by fully qualified name.
    abstract Task<AstTestResults> RunSelectedTestsAsync(string projectOrSolutionPath, IEnumerable<string> fullyQualifiedTestNames, CancellationToken ct = null)
  // Represents an analysis workspace, supporting symbol traversal, diffs, refactorings, and diagnostics.
  interface IAstWorkspace
    // Gets the symbol graph for traversal.
    IAstSymbolGraph SymbolGraph { get; }
    // Gets the symbol tree for browsing all available symbols.
    IAstSymbolTree Symbols { get; }
    // Applies the given refactor actions and returns a new, updated workspace.
    abstract Task<IAstWorkspace> ApplyActionsAsync(IEnumerable<AstAction> actions, CancellationToken ct = null)
    // Adds Summary and documentation to the symbol.
    abstract Task<AstSymbol> EnrichSymbolAsync(AstSymbol symbol, CancellationToken ct = null)
    // Looks up a symbol by fully qualified name (exact).
    abstract Task<AstSymbol?> FindSymbolAsync(AstSymbolKey key, CancellationToken ct = null)
    // Returns summary for the symbol.
    abstract Task<AstSymbol?> FindSymbolAsync(string name, CancellationToken ct = null)
    // Formats the given files or wildcard patterns using normalizer and denormalizer passes. Returns a new workspace instance containing the formatted solution.
    abstract Task<IAstWorkspace> Format(string[] listOfFilesOrWildcards, CancellationToken ct = null)
    // Formats an entire project using normalizer and denormalizer passes. Returns a new workspace instance containing the formatted solution.
    abstract Task<IAstWorkspace> FormatProject(string projectName, CancellationToken ct = null)
    // Calculates refactor actions needed to transform the symbol identified by key to the code in newCode.
    abstract Task<IReadOnlyList<AstAction>> GetDiffActionsAsync(string key, bool isContainerKey, string newCode, bool isAdditive, CancellationToken ct = null)
    // Returns documentation for the symbol.
    abstract Task<string> GetDocumentationAsync(AstSymbol symbol, CancellationToken ct = null)
    // Gets relevant symbols to start with.
    abstract Task<IReadOnlyList<AstSymbol>> GetRelevantUserSymbolsAsync()
    // Get source code for the symbol
    abstract Task<string> GetSourceCodeAsync(AstSymbol symbol, CancellationToken ct = null)
    // Returns summary for the symbol.
    abstract Task<string> GetSummaryAsync(AstSymbol symbol, CancellationToken ct = null)
    // Applies a unified diff patch to a document or additional document and returns a new workspace with the change.
    abstract Task<IAstWorkspace> PatchFileAsync(string filePath, string patch, CancellationToken ct = null)
    // Reads the text of a document or additional document.
    abstract Task<string> ReadFileAsync(string filePath, CancellationToken ct = null)
    // Saves the current workspace to disk, formatting the code and persisting changes.
    abstract Task SaveAsync(CancellationToken ct = null)
    // Performs a semantic/fuzzy symbol search by name, kind, or signature.
    abstract Task<IReadOnlyList<AstSymbol>> SearchSymbolsAsync(string query, AstSymbolSearchOptions? options = null, CancellationToken ct = null)
    // Runs code analyzers/compilation and returns diagnostic results.
    abstract Task<AstDiagnostics> ValidateAsync(CancellationToken ct = null)
    // Replaces the text of a document or additional document and returns a new workspace with the change.
    abstract Task<IAstWorkspace> WriteFileAsync(string filePath, string contents, CancellationToken ct = null)
  class MoveMemberAction : AstAction, IEquatable<MoveMemberAction>
    ctor(string MemberKind, string MemberName, AstSymbolKey OldContainerKey, string OldContainerKind, string OldContainerName, AstSymbolKey NewContainerKey, string NewContainerKind, string NewContainerName)
    string MemberKind { get; init; }
    string MemberName { get; init; }
    AstSymbolKey NewContainerKey { get; init; }
    string NewContainerKind { get; init; }
    string NewContainerName { get; init; }
    AstSymbolKey OldContainerKey { get; init; }
    string OldContainerKind { get; init; }
    string OldContainerName { get; init; }
  class PatchMemberAction : AstAction, IEquatable<PatchMemberAction>
    ctor(string TargetKey, string Patch)
    string Patch { get; init; }
    string TargetKey { get; init; }
  class RemoveMemberAction : AstAction, IEquatable<RemoveMemberAction>
    ctor(AstSymbolKey ContainerKey, string ContainerKind, string ContainerName, string MemberKind, string MemberName)
    AstSymbolKey ContainerKey { get; init; }
    string ContainerKind { get; init; }
    string ContainerName { get; init; }
    string MemberKind { get; init; }
    string MemberName { get; init; }
  class RenameAction : AstAction, IEquatable<RenameAction>
    ctor(AstSymbolKey OldKey, AstSymbolKey NewKey)
    AstSymbolKey NewKey { get; init; }
    AstSymbolKey OldKey { get; init; }
  class ReplaceBodyAction : AstAction, IEquatable<ReplaceBodyAction>
    ctor(AstSymbolKey HostKey, string HostKind, string NewBody)
    AstSymbolKey HostKey { get; init; }
    string HostKind { get; init; }
    string NewBody { get; init; }
  // Sets the search configuration for Symbols.
  class SearchConfig
    ctor()
    bool EnableFuzzyMatching { get; set; }
    int ExactFullMatchScore { get; set; }
    int FuzzySegmentMatchScore { get; set; }
    int MaxResults { get; set; }
    int OrderedSegmentMatchScore { get; set; }
    int SegmentPenaltyPerExtra { get; set; }
    int SubstringMatchScore { get; set; }
    int SuffixMatchScore { get; set; }
    double UsageBoostFactor { get; set; }
  enum SymbolInfoLevel
    Basic
    Summary
    FullDoc
  static class Test
    static Task DiffTest(string projectPath, CancellationToken ct = null)
    static Task DiffTest1(string projectPath, CancellationToken ct = null)
    static Task DiffTest2(string projectPath, CancellationToken ct = null)
    static Task DiffTestPrimaryConstructorFail(string projectPath, CancellationToken ct = null)
    static Task ExecuteCodeChange(string projectPath, string key, bool isContainerKey, string code, bool isAdditive, CancellationToken ct = null)
    static Task Format(string projectPath, string projectName)
    static Task PrintAllSymbolsReachedFromMainAsync(IAstWorkspace workspace, int maxDepth, CancellationToken ct = null)
    static void PrintAstSymbolTree(IAstWorkspace workspace)
  static class UnifiedDiff
    static string Apply(string original, string diff)
    static bool LooksLikePatch(string text)

namespace Ikon.Code.AST.Roslyn
  class DotNetCliAstTestRunner : IAstTestRunner
    ctor()
    Task<AstTestResults> RunAllTestsAsync(string projectOrSolutionPath, CancellationToken ct = null)
    Task<AstTestResults> RunSelectedTestsAsync(string projectOrSolutionPath, IEnumerable<string> fullyQualifiedTestNames, CancellationToken ct = null)
  // Provides graph-style relationships (references, referenced by) between AST symbols in a Roslyn solution.
  class RoslynAstSymbolGraph : IAstSymbolGraph
    ctor(Solution solution, Project project, Func<AstSymbolKey, Task<ISymbol?>> keyToRoslynSymbol, Func<ISymbol, Task<AstSymbol?>> roslynToAstSymbol)
    Task<IReadOnlySet<AstSymbol>> CollectTransitiveDependenciesAsync(IEnumerable<AstSymbol> roots)
    // Returns all AST symbols referenced by the specified symbol.
    Task<IReadOnlyList<AstSymbol>> GetReferencedSymbols(AstSymbol symbol)
    // Returns all AST symbols that reference the specified symbol.
    Task<IReadOnlyList<AstSymbol>> GetReferencingSymbols(AstSymbol symbol)
  // Extension methods for converting Roslyn ISymbols and SyntaxNodes to AstSymbolKey, handling all major and edge symbol kinds.
  static class RoslynAstSymbolKeyExtensions
    static string GetNodeName(SyntaxNode node)
    // Creates an AstSymbolKey from a Roslyn ISymbol. Prefers doc-comment ID, but falls back to manual construction if needed.
    static AstSymbolKey ToSymbolKey(ISymbol symbol)
    // Creates an AstSymbolKey by walking up the SyntaxNode tree. If a SemanticModel is available, will use it to obtain the symbol.
    static AstSymbolKey ToSymbolKey(SyntaxNode node, SemanticModel? semanticModel = null)
    // Creates an AstSymbolKey from a documentation comment ID (DocId). Handles all relevant symbol kinds.
    static AstSymbolKey ToSymbolKeyFromDocumentationCommentId(string documentationCommentId)
    static AstSymbolKey? TryToSymbolKey(SyntaxNode node, SemanticModel? semanticModel = null)
  // Represents a tree of AST symbols built from a Roslyn project.
  class RoslynAstSymbolTree : IAstSymbolTree
    ctor(Project project)
    // Finds a symbol by its key, or returns null if not found.
    AstSymbol? FindSymbolByKey(AstSymbolKey key)
    AstSymbol? FindSymbolByName(string name)
    // Returns all symbols that match the given predicate.
    IReadOnlyList<AstSymbol> FindSymbols(Func<AstSymbol, bool> predicate)
    // Returns the children of the specified AST symbol.
    IReadOnlyList<AstSymbol> GetChildren(AstSymbol symbol)
    List<AstSymbol> GetEntrypointSymbols()
    // Returns the root symbols in the tree.
    IReadOnlyList<AstSymbol> GetRootSymbols()
    // Searches for symbols matching the given query string (by key or name), ordered by relevance.
    IReadOnlyList<AstSymbol> SearchSymbols(string query, int maxResults = 10)
  sealed class RoslynAstWorkspace : IAstWorkspace
    IAstSymbolGraph SymbolGraph { get; }
    IAstSymbolTree Symbols { get; }
    Task<IAstWorkspace> ApplyActionsAsync(IEnumerable<AstAction> actions, CancellationToken cancellationToken = null)
    Task<AstSymbol> EnrichSymbolAsync(AstSymbol symbol, CancellationToken cancellationToken = null)
    Task<AstSymbol?> FindSymbolAsync(AstSymbolKey key, CancellationToken cancellationToken = null)
    Task<AstSymbol?> FindSymbolAsync(string name, CancellationToken ct = null)
    Task<IAstWorkspace> Format(string[] listOfFilesOrWildcards, CancellationToken cancellationToken = null)
    Task<IAstWorkspace> FormatProject(string projectName, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<AstAction>> GetDiffActionsAsync(string key, bool isContainerKey, string code, bool isAdditive, CancellationToken cancellationToken = null)
    Task<string> GetDocumentationAsync(AstSymbol symbol, CancellationToken cancellationToken = null)
    Task<IReadOnlyList<AstSymbol>> GetRelevantUserSymbolsAsync()
    Task<string> GetSourceCodeAsync(AstSymbol symbol, CancellationToken cancellationToken = null)
    Task<string> GetSummaryAsync(AstSymbol symbol, CancellationToken cancellationToken = null)
    static Task<RoslynAstWorkspace> LoadAsync(string projectPath, bool initSymbols, CancellationToken cancellationToken = null)
    Task<IAstWorkspace> PatchFileAsync(string filePath, string patch, CancellationToken cancellationToken = null)
    Task<string> ReadFileAsync(string filePath, CancellationToken cancellationToken = null)
    Task SaveAsync(CancellationToken cancellationToken = null)
    Task<IReadOnlyList<AstSymbol>> SearchSymbolsAsync(string query, AstSymbolSearchOptions? options = null, CancellationToken cancellationToken = null)
    Task<AstDiagnostics> ValidateAsync(CancellationToken cancellationToken = null)
    Task<IAstWorkspace> WriteFileAsync(string filePath, string contents, CancellationToken cancellationToken = null)
    IMethodSymbol? _entrypoint
  // Converts Roslyn diagnostics to the AST diagnostic format.
  static class RoslynDiagnosticAdapter
    // Converts a Roslyn Diagnostic to an AstDiagnostic.
    static AstDiagnostic ConvertFromRoslyn(Diagnostic roslynDiagnostic)
  // Adapter for converting Roslyn ISymbols to AstSymbol.
  static class RoslynSymbolAdapter
    // Converts a Roslyn ISymbol to an AstSymbol.
    static AstSymbol ToAstSymbol(ISymbol symbol, string? containerId = null)
    // Converts a Roslyn Location to an AstDiagnosticLocation, or null if not in source.
    static AstDiagnosticLocation? ToDiagnosticLocation(Location? location)
  // Provides summary and documentation generation for Roslyn symbols with caching.
  static class RoslynSymbolSummaryProvider
    // Returns a cached or newly generated full documentation for the specified symbol.
    static Task<string> GetDocumentationAsync(ISymbol symbol, CancellationToken cancellationToken)
    // Returns a cached or newly generated summary for the specified symbol.
    static Task<string> GetSummaryAsync(ISymbol symbol, CancellationToken cancellationToken)
  class SymbolSearcher
    ctor()
    static IReadOnlyList<AstSymbol> SearchSymbols(Dictionary<AstSymbolKey, AstSymbol> symbolByKey, string query, SearchConfig config)

namespace Ikon.Code.AST.Roslyn.Differencing
  static class DifferencingEngine
    static Task<Solution> ApplyActionsAsync(Solution solution, IReadOnlyList<AstAction> actions, CancellationToken ct = null)
    static Task<IReadOnlyList<AstAction>> DiffSnippetAgainstSolution(Solution solution, string targetKey, bool isContainerKey, string updatedSnippet, bool isAdditive, CancellationToken ct = null)
  interface DifferencingEngine.INodeFingerprint
    string Accessibility { get; }
    ImmutableArray<string> Attributes { get; }
    string BodyHash { get; }
    AstSymbolKey ContainerId { get; }
    string MemberKind { get; }
    string ReturnType { get; }
    string Signature { get; }
    AstSymbolKey StableId { get; }
  static class DifferencingEngine.RoslynSnippetHelper
    static Task<ValueTuple<Document, SyntaxNode, SemanticModel, List<MemberDeclarationSyntax>>> AddSnippetToProjectAndGetNodes(Project project, string snippet, CancellationToken ct)

namespace Ikon.Code.AST.Roslyn.Normalizing
  // Optional solution‑level hook for normalisers that need workspace mutations (add/remove documents). Implement both interfaces.
  interface ISolutionNormalizer : ISyntaxNormalizer
    abstract Task<Solution> NormalizeSolutionAsync(Solution solution, NormalizationOptions options, CancellationToken ct = null)
  // Pure node‑level normaliser.
  interface ISyntaxNormalizer
    abstract Task<SyntaxNode> Normalize(SyntaxNode root, SemanticModel? semanticModel = null, IReadOnlyDictionary<SyntaxNode, TypeSyntax>? typeMap = null)
  enum NormalizationOptions
    None
    PrimaryConstructorToExplicit
    CollectionInitializersToExplicit
    MemberOrdering
    LambdaArgumentExpansion
    CollectionObjectCreationToExpression
    VarToExplicit
    AllmanBracePlacement
    Partials
    RemoveTopLevelRegions
    UsingDirective
    RemoveComments
    SingleLineParameters
    FileScopedNamespaces
    Prettifier
    AllSafe
  static class Normalizer
    static Task<Document> DenormalizeDocumentAsync(Document document, CancellationToken ct = null)
    static Task<SyntaxNode> DenormalizeNode(SyntaxNode root, NormalizationOptions options, SemanticModel? semanticModel = null)
    static Task<Solution> DenormalizeSolutionAsync(Solution solution, NormalizationOptions options, CancellationToken ct = null)
    static Task<Solution> DenormalizeSolutionAsync(Solution solution, Project? project, CancellationToken ct = null)
    static Task<Solution> DenormalizeSolutionAsync(Solution solution, Project? project, NormalizationOptions options, CancellationToken ct = null)
    static Task<SyntaxNode> NormalizeNode(SyntaxNode root, NormalizationOptions options = AllSafe, SemanticModel? semanticModel = null, Document? document = null, CancellationToken ct = null)
    static Task<Solution> NormalizeSolutionAsync(Solution solution, NormalizationOptions options = AllSafe, CancellationToken ct = null)
    static Task<Solution> NormalizeSolutionAsync(Solution solution, Project? project, CancellationToken ct = null)
    static Task<Solution> NormalizeSolutionAsync(Solution solution, Project? project, NormalizationOptions options, CancellationToken ct = null)
    static IReadOnlyDictionary<SyntaxNode, TypeSyntax> PrepareTypeMap(SyntaxNode node, SemanticModel? sem, NormalizationOptions options)
    // Runs the pipeline. Solution‑level normalisers execute once; node‑level passes are applied to every document.
    static Task<Solution> RunNormalizersAsync(Solution solution, IEnumerable<ISyntaxNormalizer> pipeline, NormalizationOptions options, CancellationToken ct = null)

namespace Ikon.Code.AST.Roslyn.Normalizing.Core
  // Adds missing blocks and enforces Allman brace placement. Uses Roslyn formatter plus a regex pass to collapse any accidental multiple blank‑line runs ("\r\n\r\n" → "\r\n"). Always emits Windows CRLF new‑lines.
  sealed class AddBracesNormalizer : ISyntaxNormalizer
    ctor()
    Task<SyntaxNode> Normalize(SyntaxNode root, SemanticModel? _ = null, IReadOnlyDictionary<SyntaxNode, TypeSyntax>? __ = null)
  // Rewrites *array* initialisers to the C# 12 collection‑expression syntax. new[] { a, b } → ((T[])[a, b]) // with explicit cast new string[] { a, b } → [a, b] // target‑typed Rules * Only transforms array creations **without explicit size** (ie. the classic initialiser form). * For *implicit* array creations (new[]) we preserve the original element type by wrapping the new collection expression in a cast to the compiler‑inferred array type (e.g. ((string?[])[…])). This avoids overload‑resolution ambiguities (e.g. string.Join vs object.Join). * For *explicit* array creations (new T[]) we trust the surrounding context for target‑typing; the cast is unnecessary, so we emit a plain […] expression.
  sealed class CollectionNormalizer : ISyntaxNormalizer
    ctor()
    Task<SyntaxNode> Normalize(SyntaxNode root, SemanticModel? semanticModel = null, IReadOnlyDictionary<SyntaxNode, TypeSyntax>? __ = null)
  // Converts a single top‑level namespace Foo.Bar { … } into the C# 10 *file‑scoped* form: namespace Foo.Bar; // members … – Only triggers when the compilation unit contains exactly **one** top‑level NamespaceDeclarationSyntax and **no other** non‑trivia members (apart from using/extern directives). – Preserves all trivia and runs the Roslyn formatter afterwards so indentation is corrected automatically.
  sealed class NamespaceNormalizer : ISyntaxNormalizer
    ctor()
    Task<SyntaxNode> Normalize(SyntaxNode root, SemanticModel? _ = null, IReadOnlyDictionary<SyntaxNode, TypeSyntax>? __ = null)
  // Collapses ParameterListSyntax *and* ArgumentListSyntax onto a single physical line: // before void Foo( int a, string b) // after void Foo(int a, string b) The same happens for invocation/creation expressions: Bar( x, y); becomes → Bar(x, y); ⚠️ Comments (//, /* */) and directives inside the lists are preserved and left where they were – they will force the list to stay multi-line so nothing breaks.
  sealed class SingleLineParametersNormalizer : ISyntaxNormalizer
    ctor()
    Task<SyntaxNode> Normalize(SyntaxNode root, SemanticModel? _ = null, IReadOnlyDictionary<SyntaxNode, TypeSyntax>? __ = null)
  // Rewrites var declarations **only when the inferred type is a C# “language alias” primitive** (bool, byte, sbyte, short, int, long, ushort, uint, ulong, float, double, decimal, char, string). Complex / generic / user‑defined types keep var so the original intent and readability remain. A guard ensures we never query a semantic model with a node from another tree.
  sealed class VarNormalizer : ISyntaxNormalizer
    ctor()
    Task<SyntaxNode> Normalize(SyntaxNode root, SemanticModel? semanticModel = null, IReadOnlyDictionary<SyntaxNode, TypeSyntax>? _ = null)

namespace Ikon.Code.AST.Roslyn.Normalizing.Pretty
  class LinebreaksAndIndentNormalizer : ISyntaxNormalizer
    ctor()
    Task<SyntaxNode> Normalize(SyntaxNode root, SemanticModel? semanticModel = null, IReadOnlyDictionary<SyntaxNode, TypeSyntax>? typeMap = null)
