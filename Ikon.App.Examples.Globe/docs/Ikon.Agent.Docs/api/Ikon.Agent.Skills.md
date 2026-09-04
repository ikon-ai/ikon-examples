namespace Ikon.Agent.Skills
  static class Built
    static readonly Skill Attachments
    static readonly Skill Messaging
    static readonly Skill Termination
    static readonly Skill UserDecision
    static readonly Skill WebSearch
  // One Tool per tool the MCP server advertises, built schema-first from the server's own JSON input schema. The client must have been connected via McpClient.ConnectAsync before the skill's tools are enumerated. Tool names are made LLM-safe by replacing dots with underscores; calls go to the server under the original name.
  sealed class McpSkill : Skill
    ctor(McpClient mcpClient, string name = "mcp", string instructions = "")
    override string Instructions { get; }
    override string Name { get; }
    override IEnumerable<Tool> Tools()
  // Kind discriminates the shape: "decision" (default) — Options is non-empty, the host renders buttons, and the user's pick posts back as a UserDecisionResponse; "clarification" — Options is empty, the host renders a free-text prompt, and the user's typed answer posts back as a plain user message.
  sealed record UserDecisionPrompt
    ctor(string Question, IReadOnlyList<string> Options, string Kind = "decision")
    string Kind { get; init; }
    IReadOnlyList<string> Options { get; init; }
    string Question { get; init; }
  static class UserDecisionProtocol
    static Message BuildResponse(string choice)
    static Task<UserDecisionPrompt?> TryReadPromptAsync(AgentThread thread)
    static UserDecisionResponse? TryReadResponse(Message message)
    const string ArtifactMimeType
    const string ArtifactName
    const string ResponsePayloadKind
  // The host posts a user Message with PayloadKind = UserDecisionProtocol.ResponsePayloadKind and Payload = JSON of this record; the agent reads it on its next pass.
  sealed record UserDecisionResponse
    ctor(string Choice)
    string Choice { get; init; }
