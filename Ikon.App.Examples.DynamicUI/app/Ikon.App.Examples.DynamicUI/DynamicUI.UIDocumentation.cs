public partial class DynamicUI
{
    private static string GetUIDocumentation()
    {
        return """
            # Ikon Parallax UI Framework Documentation

            ## Overview
            The Ikon Parallax UI framework uses a fluent builder pattern. UI is built by calling extension methods on a `UIView` object. All components accept a `style` parameter for Crosswind/Tailwind CSS classes.

            ## Available Namespaces (already imported)
            - Ikon.Parallax
            - Ikon.Parallax.Components.Standard
            - Ikon.Parallax.Components.Charts
            - Ikon.Parallax.Themes.Default
            - Ikon.Common.Core.Reactive

            ## State Management
            Use `Reactive<T>` for reactive state that automatically updates the UI when changed.

            ```csharp
            // Get or create state from the shared state dictionary
            var counter = state.GetOrCreate<Reactive<int>>("counter", () => new Reactive<int>(0));
            var name = state.GetOrCreate<Reactive<string>>("name", () => new Reactive<string>(""));
            var items = state.GetOrCreate<Reactive<List<string>>>("items", () => new Reactive<List<string>>(new List<string>()));
            var isChecked = state.GetOrCreate<Reactive<bool>>("isChecked", () => new Reactive<bool>(false));

            // Read value
            int currentCount = counter.Value;

            // Update value (triggers UI refresh)
            counter.Value++;
            name.Value = "John";
            ```

            ## Layout Components

            ### Column (Vertical Layout)
            ```csharp
            view.Column([Layout.Column.Md, "p-4"], content: view =>
            {
                // Children stacked vertically
            });
            ```

            ### Row (Horizontal Layout)
            ```csharp
            view.Row([Layout.Row.Md, "items-center"], content: view =>
            {
                // Children arranged horizontally
            });
            ```

            ### Box (Generic Container)
            ```csharp
            view.Box([Card.Default, "p-4"], content: view =>
            {
                // Children
            });
            ```

            ## Text Components

            ```csharp
            view.Text([Text.Body], "Hello World");
            view.Text([Text.H1], "Main Title");
            view.Text([Text.H2], "Section Title");
            view.Text([Text.H3], "Subsection");
            view.Text([Text.Caption], "Small caption");
            view.Text([Text.Muted], "Muted text");
            view.Text([Text.BodyStrong], "Bold text");

            // Dynamic text
            view.Text([Text.Body], $"Count: {counter.Value}");
            ```

            ## Button Component
            IMPORTANT: Use the `label:` named parameter for button text.

            ```csharp
            // Primary button with click handler
            view.Button([Button.PrimaryMd], label: "Click Me", onClick: async () =>
            {
                counter.Value++;
            });

            // Other button variants
            view.Button([Button.SecondaryMd], label: "Secondary");
            view.Button([Button.OutlineMd], label: "Outline");
            view.Button([Button.GhostMd], label: "Ghost");
            view.Button([Button.DangerMd], label: "Delete");

            // Disabled button
            view.Button([Button.PrimaryMd], label: "Disabled", disabled: true);

            // Button with content (for icons)
            view.Button([Button.PrimaryMd], onClick: async () => { }, content: view =>
            {
                view.Icon([Icon.Default, "mr-2"], name: "plus");
                view.Text(text: "Add Item");
            });
            ```

            ## TextField (Text Input)
            ```csharp
            view.TextField([Input.Default],
                value: name.Value,
                placeholder: "Enter your name",
                onValueChange: async v => name.Value = v ?? "");

            // Number input
            view.TextField([Input.Default],
                type: "number",
                value: count.Value.ToString(),
                onValueChange: async v =>
                {
                    if (int.TryParse(v, out var n)) count.Value = n;
                });

            // With validation state
            view.TextField([Input.Invalid], placeholder: "Invalid input");
            ```

            ## TextArea (Multi-line Input)
            ```csharp
            view.TextArea([Textarea.Default],
                value: description.Value,
                placeholder: "Enter description...",
                onValueChange: async v => description.Value = v ?? "");
            ```

            ## Checkbox
            IMPORTANT: Use `@checked` (with @ prefix), `onCheckedChange`, and provide content with `CheckboxIndicator`.

            ```csharp
            var isChecked = state.GetOrCreate<Reactive<bool>>("isChecked", () => new Reactive<bool>(false));

            view.Row([Layout.Row.Sm, "items-center"], content: view =>
            {
                view.Checkbox([Checkbox.Root],
                    @checked: isChecked.Value,
                    onCheckedChange: async value => isChecked.Value = value,
                    content: view => view.CheckboxIndicator([Checkbox.Indicator],
                        content: v => v.Icon(name: "check")));
                view.Text([Text.Body], "Enable option");
            });
            ```

            ## Switch
            IMPORTANT: Use `@checked` (with @ prefix), `onCheckedChange`, and provide content with `SwitchThumb`.

            ```csharp
            var isEnabled = state.GetOrCreate<Reactive<bool>>("isEnabled", () => new Reactive<bool>(false));

            view.Row([Layout.Row.Sm, "items-center"], content: view =>
            {
                view.Switch([Switch.Root],
                    @checked: isEnabled.Value,
                    onCheckedChange: async v => isEnabled.Value = v,
                    content: view => view.SwitchThumb([Switch.Thumb]));
                view.Text([Text.Body], isEnabled.Value ? "On" : "Off");
            });
            ```

            ## Cards
            ```csharp
            view.Box([Card.Default, "p-4"], content: view =>
            {
                view.Text([Text.H3], "Card Title");
                view.Text([Text.Body], "Card content goes here.");
            });

            view.Box([Card.Elevated, "p-6"], content: view =>
            {
                view.Text([Text.H2], "Featured Card");
            });
            ```

            ## Alert
            ```csharp
            view.Box([Alert.Success], content: view =>
            {
                view.Text([Alert.Title], "Success!");
                view.Text([Alert.Description], "Your changes have been saved.");
            });

            view.Box([Alert.Danger], content: view =>
            {
                view.Text([Alert.Title], "Error");
                view.Text([Alert.Description], "Something went wrong.");
            });
            ```

            ## Form Field with Label
            ```csharp
            view.Box([FormField.Root], content: view =>
            {
                view.Text([FormField.Label], "Email");
                view.TextField([Input.Default],
                    value: email.Value,
                    placeholder: "you@example.com",
                    onValueChange: async v => email.Value = v ?? "");
                view.Text([FormField.HelpText], "We'll never share your email.");
            });
            ```

            ## Counter Example
            ```csharp
            var counter = state.GetOrCreate<Reactive<int>>("counter", () => new Reactive<int>(0));

            view.Column([Layout.Column.Md, "p-4", "items-center"], content: view =>
            {
                view.Text([Text.H2], "Counter");
                view.Text([Text.Display], counter.Value.ToString());
                view.Row([Layout.Row.Md], content: view =>
                {
                    view.Button([Button.OutlineMd], label: "-", onClick: async () =>
                    {
                        counter.Value--;
                    });
                    view.Button([Button.PrimaryMd], label: "+", onClick: async () =>
                    {
                        counter.Value++;
                    });
                });
            });
            ```

            ## Todo List Example
            ```csharp
            var todos = state.GetOrCreate<Reactive<List<string>>>("todos",
                () => new Reactive<List<string>>(new List<string>()));
            var newTodo = state.GetOrCreate<Reactive<string>>("newTodo",
                () => new Reactive<string>(""));

            view.Column([Layout.Column.Md, "p-4"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Todo List");

                // Add new todo
                view.Row([Layout.Row.Md, "mb-4"], content: view =>
                {
                    view.TextField([Input.Default, "flex-1"],
                        value: newTodo.Value,
                        placeholder: "Add a task...",
                        onValueChange: async v => newTodo.Value = v ?? "");
                    view.Button([Button.PrimaryMd], label: "Add", onClick: async () =>
                    {
                        if (!string.IsNullOrWhiteSpace(newTodo.Value))
                        {
                            todos.Value = new List<string>(todos.Value) { newTodo.Value };
                            newTodo.Value = "";
                        }
                    });
                });

                // Display todos
                foreach (var (todo, index) in todos.Value.Select((t, i) => (t, i)))
                {
                    view.Row([Card.Default, "p-3", "mb-2", "items-center", "justify-between"], key: $"todo-{index}", content: view =>
                    {
                        view.Text([Text.Body], todo);
                        view.Button([Button.DangerMd], label: "Remove", onClick: async () =>
                        {
                            var newList = todos.Value.ToList();
                            newList.RemoveAt(index);
                            todos.Value = newList;
                        });
                    });
                }
            });
            ```

            ## Icon Component
            ```csharp
            view.Icon([Icon.Default], name: "heart");
            view.Icon([Icon.Default, "mr-2"], name: "star");

            // Common icon names: heart, star, plus, minus, check, x, trash-2,
            // settings, search, chevron-down, chevron-up, menu, more-horizontal
            ```

            ## Style Classes Reference

            ### Layout Styles
            - `Layout.Column.Xs/Sm/Md/Lg/Xl` - Vertical flex with gaps
            - `Layout.Row.Xs/Sm/Md/Lg/Xl` - Horizontal flex with gaps

            ### Text Styles
            - `Text.H1/H2/H3` - Headings
            - `Text.Body/BodyStrong` - Body text
            - `Text.Caption/Muted` - Smaller/muted text
            - `Text.Display` - Large display text

            ### Button Styles
            - `Button.PrimarySm/Md/Lg` - Primary buttons
            - `Button.SecondaryMd` - Secondary
            - `Button.OutlineSm/Md` - Outline
            - `Button.GhostMd` - Ghost
            - `Button.DangerMd` - Danger/delete

            ### Input Styles
            - `Input.Default/DefaultSm/DefaultLg` - Text inputs
            - `Input.Invalid` - Invalid state
            - `Textarea.Default` - Textareas

            ### Card Styles
            - `Card.Default` - Standard card
            - `Card.Elevated` - Card with shadow

            ### Alert Styles
            - `Alert.Success/Danger/Warning/Info` - Alert variants
            - `Alert.Title/Description` - Alert text

            ### Form Styles
            - `FormField.Root` - Form field container
            - `FormField.Label` - Labels
            - `FormField.HelpText/ErrorText` - Help text

            ### Component Styles
            - `Checkbox.Root` - Checkbox container
            - `Checkbox.Indicator` - Check indicator
            - `Switch.Root` - Switch container
            - `Switch.Thumb` - Switch thumb

            ### Common Tailwind Utilities
            - Padding: `p-1`, `p-2`, `p-4`, `p-6`, `px-4`, `py-2`
            - Margin: `m-1`, `m-2`, `m-4`, `mt-2`, `mb-4`
            - Width: `w-full`, `w-1/2`, `max-w-md`, `flex-1`
            - Alignment: `items-center`, `justify-between`, `justify-center`
            - Gap: `gap-1`, `gap-2`, `gap-4`

            ## Important Rules
            1. Always use `state.GetOrCreate<Reactive<T>>()` for state
            2. Button text uses `label:` parameter: `view.Button([...], label: "Text")`
            3. Checkbox uses `@checked` (with @): `@checked: value`
            4. Switch uses `@checked` (with @): `@checked: value`
            5. onValueChange uses `async v =>`: `onValueChange: async v => x.Value = v ?? ""`
            6. Use `key:` parameter for list items
            7. All onClick/onValueChange handlers must be async
            """;
    }
}
