# Ikon.Teleport.CodeGen Public API

namespace Ikon.Teleport.CodeGen
  sealed class TeleportDocument
    IReadOnlyCollection<TeleportExternalReference> ExternalEnums { get; }
    IReadOnlyCollection<TeleportExternalReference> ExternalTypes { get; }
    bool HasMessage { get; }
    TeleportNamespaces Namespaces { get; }
    string RootName { get; }
    string RootNamespace { get; }
    uint RootOpcode { get; }
    uint RootVersion { get; }
  sealed class TeleportExternalReference : IEquatable<TeleportExternalReference>
    ctor(string Name, string Namespace)
    string Name { get;  init; }
    string Namespace { get;  init; }
  sealed class TeleportGenerationResult
    string Code { get; }
    TeleportDocument Document { get; }
  static class TeleportGenerator
    static TeleportGenerationResult GenerateCSharp(string source, string rootDirectory = null, TeleportParserOptions parserOptions = null)
    static string GenerateCSharp(TeleportDocument document)
    static TeleportGenerationResult GenerateCSharpFromFile(string path, TeleportParserOptions parserOptions = null)
    static TeleportGenerationResult GenerateCpp(string source, string rootDirectory = null, TeleportParserOptions parserOptions = null)
    static string GenerateCpp(TeleportDocument document)
    static TeleportGenerationResult GenerateCppFromFile(string path, TeleportParserOptions parserOptions = null)
    static TeleportGenerationResult GenerateJson(string source, string rootDirectory = null, TeleportParserOptions parserOptions = null)
    static string GenerateJson(TeleportDocument document)
    static TeleportGenerationResult GenerateJsonFromFile(string path, TeleportParserOptions parserOptions = null)
    static TeleportGenerationResult GenerateTypeScript(string source, string rootDirectory = null, TeleportTypeScriptGeneratorOptions options = null, TeleportParserOptions parserOptions = null)
    static string GenerateTypeScript(TeleportDocument document, TeleportTypeScriptGeneratorOptions options = null)
    static TeleportGenerationResult GenerateTypeScriptFromFile(string path, TeleportTypeScriptGeneratorOptions options = null, TeleportParserOptions parserOptions = null)
    static string GetTypeScriptHeader()
    static TeleportDocument Parse(string source, string rootDirectory = null, TeleportParserOptions parserOptions = null)
    static TeleportDocument ParseFile(string path, TeleportParserOptions parserOptions = null)
  sealed class TeleportNamespaces
    string Default { get; }
    static TeleportNamespaces Empty { get; }
    IReadOnlyDictionary<string, string> Values { get; }
    string GetNamespace(string target)
    string TryGet(string key)
    bool TryGetExplicit(string key, out string value)
  sealed class TeleportParserOptions
    ctor()
    static TeleportParserOptions Default { get; }
    bool StrictOpcodeParsing { get;  init; }
  sealed class TeleportTypeScriptGeneratorOptions
    ctor()
    static TeleportTypeScriptGeneratorOptions Default { get; }
    bool IncludeHeader { get;  init; }
