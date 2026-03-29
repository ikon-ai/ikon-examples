# Ikon.Code Public API

namespace Ikon.Code
  sealed class ClassCard : IEquatable<ClassCard>
    ctor(string SymbolId, string FqName, int Loc, string Summary, string Source, ImmutableArray<ExternalCall> ExternalCalls)
    ImmutableArray<ExternalCall> ExternalCalls { get;  init; }
    string FqName { get;  init; }
    int Loc { get;  init; }
    string Source { get;  init; }
    string Summary { get;  init; }
    string SymbolId { get;  init; }
  static class ClassCardBuilder
    static Task<ImmutableArray<ClassCard>> BuildClassCardsAsync(Compilation comp, GraphIndex graph, CancellationToken ct)
  static class ClassCardRanker
    static ImmutableArray<ClassCard> Rank(ImmutableArray<ClassCard> cards, string query, ISymbol caretSymbol)
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
    string History { get;  set; }
    string Important { get;  set; }
    string Summary { get;  set; }
  sealed class ExternalCall : IEquatable<ExternalCall>
    ctor(string Namespace, string Member)
    string Member { get;  init; }
    string Namespace { get;  init; }
  static class GraphBuilder
    static Task<GraphIndex> BuildCallGraphAsync(Compilation comp, CancellationToken ct)
  sealed class GraphIndex : IEquatable<GraphIndex>
    ctor(ImmutableDictionary<IMethodSymbol, ImmutableHashSet<IMethodSymbol>> ForwardEdges, ImmutableDictionary<INamedTypeSymbol, ImmutableHashSet<ISymbol>> TypeMembers, ImmutableDictionary<INamespaceSymbol, ImmutableHashSet<INamedTypeSymbol>> NamespaceTypes, ImmutableHashSet<ISymbol> PublicApiRoots)
    ImmutableDictionary<IMethodSymbol, ImmutableHashSet<IMethodSymbol>> ForwardEdges { get;  init; }
    ImmutableDictionary<INamespaceSymbol, ImmutableHashSet<INamedTypeSymbol>> NamespaceTypes { get;  init; }
    ImmutableHashSet<ISymbol> PublicApiRoots { get;  init; }
    ImmutableDictionary<INamedTypeSymbol, ImmutableHashSet<ISymbol>> TypeMembers { get;  init; }
  sealed class MindResult : IEquatable<MindResult>
    ctor()
    string Error { get;  init; }
    object Payload { get;  init; }
    bool Success { get;  init; }
  static class MindTools
    static Task<MindResult> CodebaseSearchAsync(string query, string explanation = null, IEnumerable<string> targetDirectories = null)
    static Task<MindResult> CreateDiagramAsync(string content)
    static Task<MindResult> DeleteFileAsync(string targetFile, string explanation = null)
    static Task<MindResult> EditFileAsync(string targetFile, string instructions, string codeEdit)
    static Task<MindResult> EditNotebookAsync(string targetNotebook, int cellIdx, bool isNewCell, string cellLanguage, string oldString, string newString)
    static Task<MindResult> FilePatchAsync(IAstWorkspace workspace, string targetFile, string patch)
    static Task<MindResult> FileReadAsync(IAstWorkspace workspace, string targetFile)
    static Task<MindResult> FileSearchAsync(string query, string explanation)
    static Task<MindResult> FileWriteAsync(IAstWorkspace workspace, string targetFile, string contents)
    static Task<MindResult> GrepSearchAsync(string query, bool caseSensitive = false, string includePattern = null, string excludePattern = null, string explanation = null)
    static Task<MindResult> ListDirAsync(string relativeWorkspacePath, string explanation = null)
    static Task<MindResult> ReadFileAsync(string targetFile, bool shouldReadEntireFile, int startLineOneIndexed, int endLineOneIndexedInclusive, string explanation = null)
    static Task<MindResult> ReapplyAsync(string targetFile)
    static Task<MindResult> RunTerminalCmdAsync(string command, bool isBackground, string explanation = null)
    static Task<MindResult> SearchReplaceAsync(string filePath, string oldString, string newString)
    static Task<MindResult> WebSearchAsync(string searchTerm, string explanation = null)
  static class PromptAssembler
    static string AssemblePrompt(IEnumerable<ClassCard> cards, string userQuery)
  class TaskFingerprint
    ctor()
    List<string> Keywords { get;  set; }
    string TaskBlurb { get;  set; }

namespace Ikon.Code.AST
  class AddMemberAction : AstAction, IEquatable<AddMemberAction>
    ctor(AstSymbolKey ContainerKey, string ContainerKind, string ContainerName, string MemberKind, string MemberName, string Snippet, int? Position = null)
    AstSymbolKey ContainerKey { get;  init; }
    string ContainerKind { get;  init; }
    string ContainerName { get;  init; }
    string MemberKind { get;  init; }
    string MemberName { get;  init; }
    int? Position { get;  init; }
    string Snippet { get;  init; }
  abstract class AstAction : IEquatable<AstAction>
  class AstDiagnostic
    ctor()
    IReadOnlyList<AstDiagnosticLocation> AdditionalLocations { get;  init; }
    string Category { get;  init; }
    string Id { get;  init; }
    bool IsSuppressed { get;  init; }
    AstDiagnosticLocation Location { get;  init; }
    string Message { get;  init; }
    IReadOnlyDictionary<string, string> Properties { get;  init; }
    AstDiagnosticSeverity Severity { get;  init; }
    override string ToString()
  class AstDiagnosticLocation
    ctor()
    int Column { get;  init; }
    string FilePath { get;  init; }
    int Line { get;  init; }
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
  class AstSymbol : IEquatable<AstSymbol>
    ctor()
    string ContainerId { get;  init; }
    string Documentation { get;  init; }
    bool IsInSource { get;  init; }
    AstSymbolKey Key { get;  init; }
    AstSymbolKind Kind { get;  init; }
    AstDiagnosticLocation Location { get;  init; }
    string Name { get;  init; }
    IReadOnlyList<string> RelatedSymbols { get;  init; }
    double RelevanceScore { get;  init; }
    string Signature { get;  init; }
    string Source { get; }
    string Summary { get;  init; }
  class AstSymbolKey : IEquatable<AstSymbolKey>
    ctor(string Namespace = null, string TypeName = null, string MemberName = null, string Kind = null, string[] ParameterTypes = null)
    string Kind { get;  init; }
    string MemberName { get;  init; }
    string Namespace { get;  init; }
    string[] ParameterTypes { get;  init; }
    string TypeName { get;  init; }
    AstSymbolKey GetParent()
    AstSymbolKey WithMember(string member, string kind = null, string[] parameterTypes = null)
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
    bool CaseSensitive { get;  set; }
    bool IncludeExternalDocs { get;  set; }
    AstSymbolKind? Kind { get;  set; }
    int MaxResults { get;  set; }
  class AstTestCaseResult
    ctor()
    TimeSpan Duration { get;  init; }
    string Message { get;  init; }
    string Name { get;  init; }
    AstTestOutcome Outcome { get;  init; }
    string StackTrace { get;  init; }
  enum AstTestOutcome
    Passed
    Failed
    Skipped
    Unknown
  class AstTestResults
    ctor()
    string RawOutput { get;  init; }
    bool Success { get; }
    IReadOnlyList<AstTestCaseResult> Tests { get;  init; }
  static class AstWorkspaceLoader
    static Task<IAstWorkspace> LoadAsync(string projectPath, bool initSymbols = false, CancellationToken ct = null)
  class ChangeAccessibilityAction : AstAction, IEquatable<ChangeAccessibilityAction>
    ctor(AstSymbolKey TargetKey, string OldAccessibility, string NewAccessibility)
    string NewAccessibility { get;  init; }
    string OldAccessibility { get;  init; }
    AstSymbolKey TargetKey { get;  init; }
  class ChangeAttributesAction : AstAction, IEquatable<ChangeAttributesAction>
    ctor(AstSymbolKey TargetKey, string[] OldAttributes, string[] NewAttributes)
    string[] NewAttributes { get;  init; }
    string[] OldAttributes { get;  init; }
    AstSymbolKey TargetKey { get;  init; }
  sealed class ChangeReturnTypeAction : AstAction, IEquatable<ChangeReturnTypeAction>
    ctor(string TargetId, string NewReturnType)
    string NewReturnType { get;  init; }
    string TargetId { get;  init; }
  class ChangeSignatureAction : AstAction, IEquatable<ChangeSignatureAction>
    ctor(AstSymbolKey MemberKey, ImmutableArray<string> NewParameters)
    AstSymbolKey MemberKey { get;  init; }
    ImmutableArray<string> NewParameters { get;  init; }
  class ChangeSignatureAction2 : AstAction, IEquatable<ChangeSignatureAction2>
    ctor(AstSymbolKey MemberKey, string[] NewParameters, string[] NewTypeParameters, string NewReturnType)
    AstSymbolKey MemberKey { get;  init; }
    string[] NewParameters { get;  init; }
    string NewReturnType { get;  init; }
    string[] NewTypeParameters { get;  init; }
  class ChangeTypeAction : AstAction, IEquatable<ChangeTypeAction>
    ctor(AstSymbolKey MemberKey, string NewType)
    AstSymbolKey MemberKey { get;  init; }
    string NewType { get;  init; }
  interface IAstSymbolGraph
    abstract Task<IReadOnlyList<AstSymbol>> GetReferencedSymbols(AstSymbol symbol)
    abstract Task<IReadOnlyList<AstSymbol>> GetReferencingSymbols(AstSymbol symbol)
  interface IAstSymbolTree
    virtual IEnumerable<AstSymbol> Descendants(AstSymbol root, int max = 1000)
    abstract AstSymbol FindSymbolByKey(AstSymbolKey key)
    abstract AstSymbol FindSymbolByName(string name)
    abstract IReadOnlyList<AstSymbol> FindSymbols(Func<AstSymbol, bool> predicate)
    abstract IReadOnlyList<AstSymbol> GetChildren(AstSymbol symbol)
    abstract IReadOnlyList<AstSymbol> GetRootSymbols()
    abstract IReadOnlyList<AstSymbol> SearchSymbols(string query, int maxResults = 10)
  interface IAstTestRunner
    abstract Task<AstTestResults> RunAllTestsAsync(string projectOrSolutionPath, CancellationToken ct = null)
    abstract Task<AstTestResults> RunSelectedTestsAsync(string projectOrSolutionPath, IEnumerable<string> fullyQualifiedTestNames, CancellationToken ct = null)
  interface IAstWorkspace
    IAstSymbolGraph SymbolGraph { get; }
    IAstSymbolTree Symbols { get; }
    abstract Task<IAstWorkspace> ApplyActionsAsync(IEnumerable<AstAction> actions, CancellationToken ct = null)
    abstract Task<AstSymbol> EnrichSymbolAsync(AstSymbol symbol, CancellationToken ct = null)
    abstract Task<AstSymbol> FindSymbolAsync(AstSymbolKey key, CancellationToken ct = null)
    abstract Task<AstSymbol> FindSymbolAsync(string name, CancellationToken ct = null)
    abstract Task<IAstWorkspace> Format(string[] listOfFilesOrWildcards, CancellationToken ct = null)
    abstract Task<IAstWorkspace> FormatProject(string projectName, CancellationToken ct = null)
    abstract Task<IReadOnlyList<AstAction>> GetDiffActionsAsync(string key, bool isContainerKey, string newCode, bool isAdditive, CancellationToken ct = null)
    abstract Task<string> GetDocumentationAsync(AstSymbol symbol, CancellationToken ct = null)
    abstract Task<IReadOnlyList<AstSymbol>> GetRelevantUserSymbolsAsync()
    abstract Task<string> GetSourceCodeAsync(AstSymbol symbol, CancellationToken ct = null)
    abstract Task<string> GetSummaryAsync(AstSymbol symbol, CancellationToken ct = null)
    abstract Task<IAstWorkspace> PatchFileAsync(string filePath, string patch, CancellationToken ct = null)
    abstract Task<string> ReadFileAsync(string filePath, CancellationToken ct = null)
    abstract Task SaveAsync(CancellationToken ct = null)
    abstract Task<IReadOnlyList<AstSymbol>> SearchSymbolsAsync(string query, AstSymbolSearchOptions options = null, CancellationToken ct = null)
    abstract Task<AstDiagnostics> ValidateAsync(CancellationToken ct = null)
    abstract Task<IAstWorkspace> WriteFileAsync(string filePath, string contents, CancellationToken ct = null)
  class MoveMemberAction : AstAction, IEquatable<MoveMemberAction>
    ctor(string MemberKind, string MemberName, AstSymbolKey OldContainerKey, string OldContainerKind, string OldContainerName, AstSymbolKey NewContainerKey, string NewContainerKind, string NewContainerName)
    string MemberKind { get;  init; }
    string MemberName { get;  init; }
    AstSymbolKey NewContainerKey { get;  init; }
    string NewContainerKind { get;  init; }
    string NewContainerName { get;  init; }
    AstSymbolKey OldContainerKey { get;  init; }
    string OldContainerKind { get;  init; }
    string OldContainerName { get;  init; }
  class PatchMemberAction : AstAction, IEquatable<PatchMemberAction>
    ctor(string TargetKey, string Patch)
    string Patch { get;  init; }
    string TargetKey { get;  init; }
  class RemoveMemberAction : AstAction, IEquatable<RemoveMemberAction>
    ctor(AstSymbolKey ContainerKey, string ContainerKind, string ContainerName, string MemberKind, string MemberName)
    AstSymbolKey ContainerKey { get;  init; }
    string ContainerKind { get;  init; }
    string ContainerName { get;  init; }
    string MemberKind { get;  init; }
    string MemberName { get;  init; }
  class RenameAction : AstAction, IEquatable<RenameAction>
    ctor(AstSymbolKey OldKey, AstSymbolKey NewKey)
    AstSymbolKey NewKey { get;  init; }
    AstSymbolKey OldKey { get;  init; }
  class ReplaceBodyAction : AstAction, IEquatable<ReplaceBodyAction>
    ctor(AstSymbolKey HostKey, string HostKind, string NewBody)
    AstSymbolKey HostKey { get;  init; }
    string HostKind { get;  init; }
    string NewBody { get;  init; }
  class SearchConfig
    ctor()
    bool EnableFuzzyMatching { get;  set; }
    int ExactFullMatchScore { get;  set; }
    int FuzzySegmentMatchScore { get;  set; }
    int MaxResults { get;  set; }
    int OrderedSegmentMatchScore { get;  set; }
    int SegmentPenaltyPerExtra { get;  set; }
    int SubstringMatchScore { get;  set; }
    int SuffixMatchScore { get;  set; }
    double UsageBoostFactor { get;  set; }
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
  class RoslynAstSymbolGraph : IAstSymbolGraph
    ctor(Solution solution, Project project, Func<AstSymbolKey, Task<ISymbol>> keyToRoslynSymbol, Func<ISymbol, Task<AstSymbol>> roslynToAstSymbol)
    Task<IReadOnlySet<AstSymbol>> CollectTransitiveDependenciesAsync(IEnumerable<AstSymbol> roots)
    Task<IReadOnlyList<AstSymbol>> GetReferencedSymbols(AstSymbol symbol)
    Task<IReadOnlyList<AstSymbol>> GetReferencingSymbols(AstSymbol symbol)
  static class RoslynAstSymbolKeyExtensions
    static string GetNodeName(SyntaxNode node)
    static AstSymbolKey ToSymbolKey(ISymbol symbol)
    static AstSymbolKey ToSymbolKey(SyntaxNode node, SemanticModel semanticModel = null)
    static AstSymbolKey ToSymbolKeyFromDocumentationCommentId(string documentationCommentId)
    static AstSymbolKey TryToSymbolKey(SyntaxNode node, SemanticModel semanticModel = null)
  class RoslynAstSymbolTree : IAstSymbolTree
    ctor(Project project)
    AstSymbol FindSymbolByKey(AstSymbolKey key)
    AstSymbol FindSymbolByName(string name)
    IReadOnlyList<AstSymbol> FindSymbols(Func<AstSymbol, bool> predicate)
    IReadOnlyList<AstSymbol> GetChildren(AstSymbol symbol)
    List<AstSymbol> GetEntrypointSymbols()
    IReadOnlyList<AstSymbol> GetRootSymbols()
    IReadOnlyList<AstSymbol> SearchSymbols(string query, int maxResults = 10)
  sealed class RoslynAstWorkspace : IAstWorkspace
    IAstSymbolGraph SymbolGraph { get; }
    IAstSymbolTree Symbols { get; }
    Task<IAstWorkspace> ApplyActionsAsync(IEnumerable<AstAction> actions, CancellationToken cancellationToken = null)
    Task<AstSymbol> EnrichSymbolAsync(AstSymbol symbol, CancellationToken cancellationToken = null)
    Task<AstSymbol> FindSymbolAsync(AstSymbolKey key, CancellationToken cancellationToken = null)
    Task<AstSymbol> FindSymbolAsync(string name, CancellationToken ct = null)
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
    Task<IReadOnlyList<AstSymbol>> SearchSymbolsAsync(string query, AstSymbolSearchOptions options = null, CancellationToken cancellationToken = null)
    Task<AstDiagnostics> ValidateAsync(CancellationToken cancellationToken = null)
    Task<IAstWorkspace> WriteFileAsync(string filePath, string contents, CancellationToken cancellationToken = null)
    IMethodSymbol _entrypoint
  static class RoslynDiagnosticAdapter
    static AstDiagnostic ConvertFromRoslyn(Diagnostic roslynDiagnostic)
  static class RoslynSymbolAdapter
    static AstSymbol ToAstSymbol(ISymbol symbol, string containerId = null)
    static AstDiagnosticLocation ToDiagnosticLocation(Location location)
  static class RoslynSymbolSummaryProvider
    static Task<string> GetDocumentationAsync(ISymbol symbol, CancellationToken cancellationToken)
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
  interface ISolutionNormalizer : ISyntaxNormalizer
    abstract Task<Solution> NormalizeSolutionAsync(Solution solution, NormalizationOptions options, CancellationToken ct = null)
  interface ISyntaxNormalizer
    abstract Task<SyntaxNode> Normalize(SyntaxNode root, SemanticModel semanticModel = null, IReadOnlyDictionary<SyntaxNode, TypeSyntax> typeMap = null)
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
    static Task<SyntaxNode> DenormalizeNode(SyntaxNode root, NormalizationOptions options, SemanticModel semanticModel = null)
    static Task<Solution> DenormalizeSolutionAsync(Solution solution, NormalizationOptions options, CancellationToken ct = null)
    static Task<Solution> DenormalizeSolutionAsync(Solution solution, Project project, CancellationToken ct = null)
    static Task<Solution> DenormalizeSolutionAsync(Solution solution, Project project, NormalizationOptions options, CancellationToken ct = null)
    static Task<SyntaxNode> NormalizeNode(SyntaxNode root, NormalizationOptions options = AllSafe, SemanticModel semanticModel = null, Document document = null, CancellationToken ct = null)
    static Task<Solution> NormalizeSolutionAsync(Solution solution, NormalizationOptions options = AllSafe, CancellationToken ct = null)
    static Task<Solution> NormalizeSolutionAsync(Solution solution, Project project, CancellationToken ct = null)
    static Task<Solution> NormalizeSolutionAsync(Solution solution, Project project, NormalizationOptions options, CancellationToken ct = null)
    static IReadOnlyDictionary<SyntaxNode, TypeSyntax> PrepareTypeMap(SyntaxNode node, SemanticModel sem, NormalizationOptions options)
    static Task<Solution> RunNormalizersAsync(Solution solution, IEnumerable<ISyntaxNormalizer> pipeline, NormalizationOptions options, CancellationToken ct = null)

namespace Ikon.Code.AST.Roslyn.Normalizing.Core
  sealed class AddBracesNormalizer : ISyntaxNormalizer
    ctor()
    Task<SyntaxNode> Normalize(SyntaxNode root, SemanticModel _ = null, IReadOnlyDictionary<SyntaxNode, TypeSyntax> __ = null)
  sealed class CollectionNormalizer : ISyntaxNormalizer
    ctor()
    Task<SyntaxNode> Normalize(SyntaxNode root, SemanticModel semanticModel = null, IReadOnlyDictionary<SyntaxNode, TypeSyntax> __ = null)
  sealed class NamespaceNormalizer : ISyntaxNormalizer
    ctor()
    Task<SyntaxNode> Normalize(SyntaxNode root, SemanticModel _ = null, IReadOnlyDictionary<SyntaxNode, TypeSyntax> __ = null)
  sealed class SingleLineParametersNormalizer : ISyntaxNormalizer
    ctor()
    Task<SyntaxNode> Normalize(SyntaxNode root, SemanticModel _ = null, IReadOnlyDictionary<SyntaxNode, TypeSyntax> __ = null)
  sealed class VarNormalizer : ISyntaxNormalizer
    ctor()
    Task<SyntaxNode> Normalize(SyntaxNode root, SemanticModel semanticModel = null, IReadOnlyDictionary<SyntaxNode, TypeSyntax> _ = null)

namespace Ikon.Code.AST.Roslyn.Normalizing.Pretty
  class LinebreaksAndIndentNormalizer : ISyntaxNormalizer
    ctor()
    Task<SyntaxNode> Normalize(SyntaxNode root, SemanticModel semanticModel = null, IReadOnlyDictionary<SyntaxNode, TypeSyntax> typeMap = null)
