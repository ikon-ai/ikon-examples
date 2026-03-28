using Ikon.AI.Emergence;
using Ikon.AI.Kernel;
using Ikon.AI.LLM;

public partial class DynamicUI
{
    private KernelContext _chatContext = new();
    private const int MaxAutoRetries = 2;

    private void InitializeMindChat()
    {
        _chatContext = new KernelContext()
            .Add(new Instruction(InstructionType.Context, $"""
                You are a friendly AI assistant that helps users create dynamic user interfaces.
                You can generate live, functional UI components based on natural language descriptions.

                Current date and time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}

                ## Your Capabilities
                - Create interactive UI components (buttons, forms, cards, lists, etc.)
                - Generate counters, timers, and other stateful components
                - Build forms with validation
                - Create data displays and dashboards

                ## How It Works
                When a user describes a UI they want:
                1. Set ShouldGenerateUI to true
                2. Provide a detailed UIDescription for what to generate
                3. Respond with a brief, friendly message about what you'll create

                If the user is just chatting or asking questions, set ShouldGenerateUI to false.

                ## Examples of User Requests
                - "Create a counter with increment and decrement buttons"
                - "Make a form with name and email fields"
                - "Show a card with a title, description, and action buttons"
                - "Build a todo list where I can add and remove items"

                Keep responses concise and friendly.
                """));
    }

    private async Task<string> ProcessUserMessageAsync(string userMessage)
    {
        var contextAdditions = new List<string>();

        if (!string.IsNullOrEmpty(_lastError.Value))
        {
            contextAdditions.Add($"## Recent Error\nThe last UI generation had an error: {_lastError.Value}\nYou should acknowledge this and offer to help fix it.");
        }

        if (!string.IsNullOrEmpty(_lastGeneratedCode.Value))
        {
            contextAdditions.Add("## Currently Displayed UI\nUI code has been generated and is displayed in the preview panel.");
        }

        if (contextAdditions.Count > 0)
        {
            _chatContext = _chatContext.Add(new Instruction(InstructionType.Context, string.Join("\n\n", contextAdditions)));
        }

        _chatContext = _chatContext.Add(new MessageBlock(MessageBlockRole.User, userMessage));

        var (chatResponse, newContext) = await Emerge.Run<ChatResponse>(LLMModel.Claude45Sonnet, _chatContext, pass =>
        {
            pass.Temperature = 0.7f;
            pass.MaxOutputTokens = 1000;
            pass.Command = "Respond to the user based on the conversation context. If they want UI, set ShouldGenerateUI=true and provide a description.";
        }).FinalAsync();

        _chatContext = newContext;

        if (chatResponse.ShouldGenerateUI && !string.IsNullOrEmpty(chatResponse.UIDescription))
        {
            await GenerateAndExecuteUIAsync(chatResponse.UIDescription);
        }

        _chatContext = _chatContext.Add(new MessageBlock(MessageBlockRole.Model, chatResponse.Message));

        return chatResponse.Message;
    }

    private async Task GenerateAndExecuteUIAsync(string description)
    {
        _isGenerating.Value = true;
        _lastError.Value = null;

        try
        {
            var (response, validationError) = await GenerateUIWithRefinementAsync(description, _lastGeneratedCode.Value);

            _lastGeneratedCode.Value = response.Code;

            if (validationError != null)
            {
                _lastError.Value = validationError;
            }
            else
            {
                _lastError.Value = null;
                _executionVersion.Value++;
            }
        }
        catch (Exception ex)
        {
            _lastError.Value = $"Generation failed: {ex.Message}";
        }
        finally
        {
            _isGenerating.Value = false;
        }
    }

    private async Task RetryWithFixAsync()
    {
        if (string.IsNullOrEmpty(_lastGeneratedCode.Value) || string.IsNullOrEmpty(_lastError.Value))
        {
            return;
        }

        _isGenerating.Value = true;

        try
        {
            var (response, validationError) = await GenerateUIWithRefinementAsync(
                "Fix the error in the previous code",
                _lastGeneratedCode.Value,
                _lastError.Value);

            _lastGeneratedCode.Value = response.Code;

            if (validationError != null)
            {
                _lastError.Value = validationError;
            }
            else
            {
                _lastError.Value = null;
                _executionVersion.Value++;
            }
        }
        catch (Exception ex)
        {
            _lastError.Value = $"Retry failed: {ex.Message}";
        }
        finally
        {
            _isGenerating.Value = false;
        }
    }

    private async Task<(UICodeResponse Response, string? ValidationError)> GenerateUIWithRefinementAsync(
        string description,
        string? previousCode = null,
        string? initialError = null)
    {
        string? validationError = initialError;

        var (response, _) = await Emerge.Refine<UICodeResponse>(LLMModel.Claude45Sonnet, new KernelContext(), opt =>
        {
            opt.MaxRefinements = MaxAutoRetries;
            opt.Temperature = 0.3f;
            opt.MaxOutputTokens = 4000;
            opt.SystemPrompt = $"""
                You are an expert UI code generator for the Ikon Parallax UI framework.
                Your task is to generate C# code that creates UI components based on user descriptions.

                ## UI Framework Documentation
                {GetUIDocumentation()}

                ## Code Requirements
                1. Generate ONLY the C# code that goes inside a UI building function
                2. The code will be executed in a context where `view` is the root UIView
                3. Use the fluent builder pattern: `view.Method([styles], ...)`
                4. Style classes are used directly (e.g., Layout.Column.Md, Text.H2, Button.PrimaryMd)
                5. Use Reactive<T> for state that needs to update the UI
                6. All reactive state is accessed via the `state` dictionary

                ## Critical Syntax Rules
                - Button: `view.Button([Button.PrimaryMd], label: "Click", onClick: async () => counter.Value++);`
                - TextField: `view.TextField([Input.Default], value: x.Value, onValueChange: async v => x.Value = v ?? "");`
                - Checkbox: `view.Checkbox([Checkbox.Root], @checked: x.Value, onCheckedChange: async v => x.Value = v, content: ...);`
                - Switch: `view.Switch([Switch.Root], @checked: x.Value, onCheckedChange: async v => x.Value = v, content: ...);`

                Generate UI that matches EXACTLY what the user asked for.
                """;

            opt.Initial(s =>
            {
                s.Command = !string.IsNullOrEmpty(initialError)
                    ? $"""
                        ## User Request
                        {description}

                        IMPORTANT: Your previous code had an error:
                        {initialError}

                        Previous code that failed:
                        ```csharp
                        {previousCode}
                        ```

                        Please fix the error and generate corrected code.
                        """
                    : !string.IsNullOrEmpty(previousCode)
                        ? $"""
                            ## User Request
                            {description}

                            Previous code (iterate on this):
                            ```csharp
                            {previousCode}
                            ```

                            Modify the above code according to the user's request.
                            """
                        : $"""
                            ## User Request
                            {description}

                            Generate the UI code.
                            """;
            });

            opt.Refinement(s =>
            {
                s.Command = $"""
                    ## User Request
                    {description}

                    IMPORTANT: Your previous code had a compilation error:
                    {validationError}

                    Please fix the error and generate corrected code.
                    """;
            });

            opt.ShouldContinue = async (result, _) =>
            {
                validationError = await ValidateSyntaxAsync(result.Code);
                return validationError != null;
            };
        }).FinalAsync();

        return (response, validationError);
    }
}
