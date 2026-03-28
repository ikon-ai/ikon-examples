using Ikon.App;
using Ikon.Common.Core.Reactive;
using Ikon.Common.Core.Scope;
using Ikon.Parallax;
using Ikon.Parallax.Components.Standard;
using Ikon.Parallax.Themes.Default;
using Ikon.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

return await IkonServerRunner.RunApp(args);

public record SessionIdentity(string UserId);
public record ClientParams();

public record ChatMessage(string Role, string Content, DateTime Timestamp);

[App]
public partial class DynamicUI(IApp<SessionIdentity, ClientParams> app)
{
    private UI UI { get; } = new(app, new Theme());

    // Theme state
    private const string ThemeLight = "light";
    private const string ThemeDark = "dark";
    private readonly ClientReactive<string> _currentTheme = ClientReactive.Create(_ => ThemeLight);

    // Chat state
    private string _userInputValue = "";
    private readonly Reactive<int> _inputResetKey = new(0);
    private readonly Reactive<List<ChatMessage>> _messages = new([]);
    private readonly Reactive<bool> _isProcessing = new(false);
    private readonly Reactive<string> _streamingResponse = new("");

    // Dynamic UI state
    private readonly Reactive<string> _lastGeneratedCode = new("");
    private readonly Reactive<string?> _lastError = new(null);
    private readonly Reactive<bool> _isGenerating = new(false);
    private readonly Reactive<int> _executionVersion = new(0);
    private readonly Reactive<bool> _showCode = new(false);

    // Shared state for generated UIs
    private readonly SharedState _sharedState = new();

    public async Task Main()
    {
        InitializeMindChat();

        UI.Root([Page.Default, "font-sans bg-gradient-to-br from-background via-background to-muted/30"],
            content: view =>
            {
                view.Row(["w-full h-screen"], content: view =>
                {
                    // Left panel - Chat
                    RenderChatPanel(view);

                    // Right panel - Dynamic UI display
                    RenderUIPanel(view);
                });
            });
    }

    private void RenderChatPanel(UIView view)
    {
        view.Column(["w-1/2 h-full flex flex-col bg-background/50"], content: view =>
        {
            // Header
            view.Row(["p-4 justify-between items-center"], content: view =>
            {
                view.Column([Layout.Column.Xs], content: view =>
                {
                    view.Text([Text.H2], "Dynamic UI Generator");
                    view.Text([Text.Muted], "Describe any UI and I'll create it for you");
                });

                // Theme toggle
                var isDark = _currentTheme.Value == ThemeDark;
                view.Button([Button.GhostMd, Button.Size.Icon],
                    onClick: ToggleThemeAsync,
                    content: v => v.Icon([Icon.Default], name: isDark ? "sun" : "moon"));
            });

            // Messages area
            view.ScrollArea(viewportStyle: ["p-4"], rootStyle: ["flex-1"], content: view =>
            {
                view.Column([Layout.Column.Md], content: view =>
                {
                    if (_messages.Value.Count == 0)
                    {
                        RenderWelcomeMessage(view);
                    }
                    else
                    {
                        foreach (var (message, index) in _messages.Value.Select((m, i) => (m, i)))
                        {
                            RenderChatMessage(view, message, index);
                        }
                    }

                    // Streaming response
                    if (!string.IsNullOrEmpty(_streamingResponse.Value))
                    {
                        view.Box([Card.Default, "p-3 max-w-[80%]"], content: view =>
                        {
                            view.Text([Text.Body], _streamingResponse.Value);
                        });
                    }

                    // Processing indicator
                    if (_isProcessing.Value && string.IsNullOrEmpty(_streamingResponse.Value))
                    {
                        view.Box([Card.Default, "p-3 max-w-[80%]"], content: view =>
                        {
                            view.Row([Layout.Row.Sm, "items-center"], content: view =>
                            {
                                view.Box([Icon.Spinner]);
                                view.Text([Text.Muted], "Thinking...");
                            });
                        });
                    }
                });
            });

            // Input area
            view.Box(["p-4"], content: view =>
            {
                view.Row([Layout.Row.Sm], content: view =>
                {
                    view.TextField(
                        [Input.Default, "flex-1"],
                        key: $"chat-input-{_inputResetKey.Value}",
                        defaultValue: "",
                        placeholder: "Describe a UI you want to create...",
                        disabled: _isProcessing.Value,
                        onValueChange: async value => { _userInputValue = value ?? ""; },
                        onSubmit: async _ => { await SendMessageAsync(); }
                    );

                    view.Button(
                        [Button.PrimaryMd],
                        _isProcessing.Value ? "..." : "Send",
                        disabled: _isProcessing.Value,
                        onClick: async () => { await SendMessageAsync(); }
                    );
                });
            });
        });
    }

    private void RenderUIPanel(UIView view)
    {
        view.Column(["w-1/2 h-full flex flex-col bg-muted/20"], content: view =>
        {
            // Header with controls
            view.Row(["p-4 justify-between items-center"], content: view =>
            {
                view.Text([Text.H3], "Preview");

                view.Row([Layout.Row.Sm], content: view =>
                {
                    view.Button(
                        [_showCode.Value ? Button.SecondaryMd : Button.OutlineMd],
                        _showCode.Value ? "Hide Code" : "Show Code",
                        onClick: async () => { _showCode.Value = !_showCode.Value; }
                    );

                    if (!string.IsNullOrEmpty(_lastError.Value))
                    {
                        view.Button(
                            [Button.SecondaryMd],
                            "Retry Fix",
                            disabled: _isGenerating.Value,
                            onClick: async () => { await RetryWithFixAsync(); }
                        );
                    }

                    view.Button(
                        [Button.OutlineMd],
                        "Clear State",
                        onClick: async () =>
                        {
                            _sharedState.Clear();
                            _executionVersion.Value++;
                        }
                    );
                });
            });

            // Code preview (collapsible)
            if (_showCode.Value && !string.IsNullOrEmpty(_lastGeneratedCode.Value))
            {
                view.Box(["p-4 bg-muted/50 rounded-lg mx-4 max-h-64 overflow-auto"], content: view =>
                {
                    view.Text(["text-xs font-mono text-foreground whitespace-pre-wrap"], _lastGeneratedCode.Value);
                });
            }

            // Error display
            if (!string.IsNullOrEmpty(_lastError.Value))
            {
                view.Box([Alert.Danger, "m-4"], content: view =>
                {
                    view.Text([Alert.Title], "Error");
                    view.Text([Alert.Description, "text-xs font-mono whitespace-pre-wrap"], _lastError.Value);
                });
            }

            // Generating indicator
            if (_isGenerating.Value)
            {
                view.Box(["flex-1 flex items-center justify-center"], content: view =>
                {
                    view.Column([Layout.Column.Md, "items-center"], content: view =>
                    {
                        view.Box([Icon.SpinnerLg]);
                        view.Text([Text.Muted], "Generating UI...");
                    });
                });
            }
            // Dynamic UI display area
            else if (!string.IsNullOrEmpty(_lastGeneratedCode.Value) && string.IsNullOrEmpty(_lastError.Value))
            {
                view.Box(["flex-1 p-4 overflow-auto"], content: uiView =>
                {
                    ExecuteUICode(uiView);
                });
            }
            // Empty state
            else if (string.IsNullOrEmpty(_lastGeneratedCode.Value))
            {
                view.Box(["flex-1 flex items-center justify-center"], content: view =>
                {
                    view.Column([Layout.Column.Md, "items-center text-center max-w-md"], content: view =>
                    {
                        view.Icon(["text-6xl text-muted-foreground/30"], name: "wand-2");
                        view.Text([Text.H3, "text-muted-foreground"], "No UI Generated Yet");
                        view.Text([Text.Muted], "Describe a UI in the chat and I'll generate it for you");
                    });
                });
            }
        });
    }

    private void ExecuteUICode(UIView uiView)
    {
        if (string.IsNullOrEmpty(_lastGeneratedCode.Value))
        {
            return;
        }

        // Read to establish reactive dependency so UI refreshes when execution version changes
        _ = _executionVersion.Value;

        // Execute the code synchronously during render - this is required for action callbacks to work
        try
        {
            var (success, error) = ExecuteCodeSync(_lastGeneratedCode.Value, uiView);

            if (!success && error != null)
            {
                _lastError.Value = error;
            }
        }
        catch (Exception ex)
        {
            _lastError.Value = $"Execution error: {ex.Message}";
        }
    }

    private void RenderWelcomeMessage(UIView view)
    {
        view.Column([Layout.Column.Lg, "text-center py-8"], content: view =>
        {
            view.Text(["text-4xl"], "👋");
            view.Text([Text.H3], "Welcome to Dynamic UI Generator!");
            view.Text([Text.Muted, "max-w-md mx-auto"],
                "I can create interactive UI components from natural language descriptions. Try asking me to create:");

            view.Column([Layout.Column.Sm, "mt-4"], content: view =>
            {
                var examples = new[]
                {
                    "A counter with increment and decrement buttons",
                    "A form with name and email fields",
                    "A card with a title, description, and action buttons",
                    "A todo list where I can add and remove items"
                };

                foreach (var example in examples)
                {
                    view.Button(
                        [Card.Interactive, "text-left p-3 w-full max-w-md"],
                        $"→ {example}",
                        onClick: async () =>
                        {
                            _userInputValue = example;
                            await SendMessageAsync();
                        }
                    );
                }
            });
        });
    }

    private void RenderChatMessage(UIView view, ChatMessage message, int index)
    {
        var isUser = message.Role == "user";

        view.Row([isUser ? "justify-end" : "justify-start", "w-full"], key: $"msg-{index}", content: view =>
        {
            var bgColor = isUser ? "bg-primary text-primary-foreground" : "bg-muted";
            view.Box([$"p-3 rounded-lg max-w-[85%] {bgColor}"], content: view =>
            {
                view.Text([Text.Body], message.Content);
                view.Text([Text.Caption, "mt-1 opacity-70"], message.Timestamp.ToString("HH:mm"));
            });
        });
    }

    private async Task SendMessageAsync()
    {
        var message = _userInputValue.Trim();

        if (string.IsNullOrEmpty(message) || _isProcessing.Value)
        {
            return;
        }

        _userInputValue = "";
        _inputResetKey.Value++;
        _isProcessing.Value = true;
        _streamingResponse.Value = "";

        // Add user message
        var userMessage = new ChatMessage("user", message, DateTime.Now);
        _messages.Value = [.. _messages.Value, userMessage];

        try
        {
            var response = await ProcessUserMessageAsync(message);

            // Add assistant message
            var assistantMessage = new ChatMessage("assistant", response, DateTime.Now);
            _messages.Value = [.. _messages.Value, assistantMessage];
        }
        catch (Exception ex)
        {
            var errorMessage = new ChatMessage("assistant", $"Sorry, I encountered an error: {ex.Message}", DateTime.Now);
            _messages.Value = [.. _messages.Value, errorMessage];
        }
        finally
        {
            _isProcessing.Value = false;
            _streamingResponse.Value = "";
        }
    }

    private async Task ToggleThemeAsync()
    {
        var currentTheme = _currentTheme.Value;
        var nextTheme = currentTheme == ThemeDark ? ThemeLight : ThemeDark;
        var updated = await ClientFunctions.SetThemeAsync(nextTheme);

        if (updated)
        {
            _currentTheme.Value = nextTheme;
        }
    }
}
