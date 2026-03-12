public partial class Validation
{
    private void RenderFormsSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // FormField styles
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "FormField Styles");
                view.Column([Layout.Column.Lg], content: view =>
                {
                    // Normal field
                    view.Box([FormField.Root], content: view =>
                    {
                        view.Text([FormField.Label], "Email");
                        view.TextField([Input.Default], placeholder: "Enter your email");
                        view.Text([FormField.HelpText], "We'll never share your email.");
                    });

                    // Required field
                    view.Box([FormField.Root], content: view =>
                    {
                        view.Text([FormField.LabelRequired], "Username");
                        view.TextField([Input.Default], placeholder: "Enter username");
                    });

                    // Field with error
                    view.Box([FormField.Root], content: view =>
                    {
                        view.Text([FormField.Label], "Password");
                        view.TextField([Input.Invalid], placeholder: "Enter password", type: "password");
                        view.Text([FormField.ErrorText], "Password must be at least 8 characters.");
                    });

                    // Field with success
                    view.Box([FormField.Root], content: view =>
                    {
                        view.Text([FormField.Label], "Verified Field");
                        view.TextField([Input.Default], placeholder: "Verified value", value: "valid@email.com");
                        view.Text([FormField.SuccessText], "Email verified successfully!");
                    });

                    // Field with warning
                    view.Box([FormField.Root], content: view =>
                    {
                        view.Text([FormField.Label], "Username");
                        view.TextField([Input.Default], placeholder: "Enter username", value: "user123");
                        view.Text([FormField.WarningText], "This username is already taken but available with a suffix.");
                    });
                });
            });

            // Label styles
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Label Styles");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Label.Base], "Label.Base - Standard label");
                    view.Text([Label.Required], "Label.Required - With asterisk");
                    view.Text([Label.Optional], "Label.Optional - With (optional) suffix");
                    view.Text([Label.Error], "Label.Error - Error state label");
                });
            });

            // OtpField
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "OTP Field");
                view.Text([Text.Caption, "mb-4"], "One-time password input field");

                view.Column([Layout.Column.Md], content: view =>
                {
                    view.OtpField( [OtpField.Root], maxLength: 6, content: view =>
                    {
                        view.OtpFieldInput( [OtpField.Input], index: 0);
                        view.OtpFieldInput( [OtpField.Input], index: 1);
                        view.OtpFieldInput( [OtpField.Input], index: 2);
                        view.Text([Text.Muted], "-");
                        view.OtpFieldInput( [OtpField.Input], index: 3);
                        view.OtpFieldInput( [OtpField.Input], index: 4);
                        view.OtpFieldInput( [OtpField.Input], index: 5);
                    });
                });
            });

            // PasswordToggleField
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Password Toggle Field");
                view.Text([Text.Caption, "mb-4"], "Password field with visibility toggle");

                view.Column([Layout.Column.Md, "max-w-sm"], content: view =>
                {
                    view.PasswordToggleField([Input.Password.Wrapper], content: view =>
                    {
                        view.PasswordToggleFieldInput([Input.Default, Input.Password.Input], placeholder: "Enter password");
                        view.PasswordToggleFieldToggle([Input.Password.Toggle], content: view =>
                        {
                            view.PasswordToggleFieldIcon(
                                visibleIcon: v => v.Icon([Icon.Size.Sm], name: "eye-open"),
                                hiddenIcon: v => v.Icon([Icon.Size.Sm], name: "eye-closed"));
                        });
                    });
                });
            });

            // DropdownMenu styles (for form context menus)
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "DropdownMenu Styles");
                view.Text([Text.Caption, "mb-4"], "Static preview of dropdown menu styles");

                view.Box([DropdownMenu.Content, "w-56"], content: view =>
                {
                    view.Box([DropdownMenu.Label], content: v => v.Text(text: "My Account"));
                    view.Box([DropdownMenu.Separator]);
                    view.Box([DropdownMenu.Group], content: view =>
                    {
                        view.Box([DropdownMenu.Item], content: view =>
                        {
                            view.Icon( ["mr-2 h-4 w-4"], name: "person");
                            view.Text(text: "Profile");
                            view.Text([DropdownMenu.Shortcut], "⇧⌘P");
                        });
                        view.Box([DropdownMenu.Item], content: view =>
                        {
                            view.Icon( ["mr-2 h-4 w-4"], name: "gear");
                            view.Text(text: "Settings");
                            view.Text([DropdownMenu.Shortcut], "⌘S");
                        });
                    });
                    view.Box([DropdownMenu.Separator]);
                    view.Box([DropdownMenu.CheckboxItem], content: view =>
                    {
                        view.Box(["w-4 h-4 mr-2 flex items-center justify-center"], content: v => v.Icon(name: "check"));
                        view.Text(text: "Show Statusbar");
                    });
                    view.Box([DropdownMenu.CheckboxItem], content: view =>
                    {
                        view.Box(["w-4 h-4 mr-2"]);
                        view.Text(text: "Show Full URLs");
                    });
                    view.Box([DropdownMenu.Separator]);
                    view.Box([DropdownMenu.Label], content: v => v.Text(text: "Theme"));
                    view.Box([DropdownMenu.RadioItem], content: view =>
                    {
                        view.Box(["w-4 h-4 mr-2 flex items-center justify-center"], content: v => v.Box(["w-2 h-2 rounded-full bg-current"]));
                        view.Text(text: "Light");
                    });
                    view.Box([DropdownMenu.RadioItem], content: view =>
                    {
                        view.Box(["w-4 h-4 mr-2"]);
                        view.Text(text: "Dark");
                    });
                    view.Box([DropdownMenu.RadioItem], content: view =>
                    {
                        view.Box(["w-4 h-4 mr-2"]);
                        view.Text(text: "System");
                    });
                    view.Box([DropdownMenu.Separator]);
                    view.Box([DropdownMenu.SubTrigger], content: view =>
                    {
                        view.Text(text: "More Options");
                        view.Icon( ["ml-auto h-4 w-4"], name: "chevron-right");
                    });
                });
            });

            // Command palette styles
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Command Palette Styles");
                view.Text([Text.Caption, "mb-4"], "Static preview of command palette styles (cmdk)");

                view.Box([Command.Root, "max-w-md"], content: view =>
                {
                    view.Box([Command.InputWrapper], content: view =>
                    {
                        view.Icon( ["mr-2 h-4 w-4 shrink-0 opacity-50"], name: "magnifying-glass");
                        view.TextField([Command.Input], placeholder: "Type a command or search...");
                    });
                    view.Box([Command.List], content: view =>
                    {
                        view.Box([Command.Empty], content: v => v.Text(text: "No results found."));
                        view.Box([Command.Group], content: view =>
                        {
                            view.Box([Command.GroupHeading], content: v => v.Text(text: "Suggestions"));
                            view.Box([Command.Item], content: view =>
                            {
                                view.Icon( ["mr-2 h-4 w-4"], name: "calendar");
                                view.Text(text: "Calendar");
                            });
                            view.Box([Command.Item], content: view =>
                            {
                                view.Icon( ["mr-2 h-4 w-4"], name: "face");
                                view.Text(text: "Search Emoji");
                            });
                            view.Box([Command.Item], content: view =>
                            {
                                view.Icon( ["mr-2 h-4 w-4"], name: "rocket");
                                view.Text(text: "Launch");
                                view.Text([Command.Shortcut], "⌘L");
                            });
                        });
                        view.Box([Command.Separator]);
                        view.Box([Command.Group], content: view =>
                        {
                            view.Box([Command.GroupHeading], content: v => v.Text(text: "Settings"));
                            view.Box([Command.Item], content: view =>
                            {
                                view.Icon( ["mr-2 h-4 w-4"], name: "person");
                                view.Text(text: "Profile");
                                view.Text([Command.Shortcut], "⌘P");
                            });
                            view.Box([Command.Item], content: view =>
                            {
                                view.Icon( ["mr-2 h-4 w-4"], name: "gear");
                                view.Text(text: "Settings");
                                view.Text([Command.Shortcut], "⌘S");
                            });
                        });
                    });
                });
            });

            // Form with validation messages
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Form Validation");
                view.Text([Text.Caption, "mb-4"], "Form with HTML5 validation - click Submit to see validation errors");

                view.Form([Layout.Column.Md, "max-w-md"], content: view =>
                {
                    view.FormField(["space-y-2"], name: "email", content: view =>
                    {
                        view.FormLabel([FormField.Label], content: v => v.Text(text: "Email (required)"));
                        view.FormControl([Input.Default], props: new Dictionary<string, object?>
                        {
                            ["placeholder"] = "Enter your email",
                            ["type"] = "email",
                            ["required"] = true
                        });
                        view.FormMessage([FormField.ErrorText], match: FormMessageMatch.ValueMissing, content: v =>
                            v.Text(text: "Email is required"));
                        view.FormMessage([FormField.ErrorText], match: FormMessageMatch.TypeMismatch, content: v =>
                            v.Text(text: "Please enter a valid email address"));
                    });

                    view.FormField(["space-y-2"], name: "username", content: view =>
                    {
                        view.FormLabel([FormField.Label], content: v => v.Text(text: "Username (3-20 chars)"));
                        view.FormControl([Input.Default], props: new Dictionary<string, object?>
                        {
                            ["placeholder"] = "Enter username",
                            ["required"] = true,
                            ["minLength"] = 3,
                            ["maxLength"] = 20
                        });
                        view.FormMessage([FormField.ErrorText], match: FormMessageMatch.ValueMissing, content: v =>
                            v.Text(text: "Username is required"));
                        view.FormMessage([FormField.ErrorText], match: FormMessageMatch.TooShort, content: v =>
                            v.Text(text: "Username must be at least 3 characters"));
                        view.FormMessage([FormField.ErrorText], match: FormMessageMatch.TooLong, content: v =>
                            v.Text(text: "Username must be at most 20 characters"));
                    });

                    view.FormField(["space-y-2"], name: "age", content: view =>
                    {
                        view.FormLabel([FormField.Label], content: v => v.Text(text: "Age (18-120)"));
                        view.FormControl([Input.Default], props: new Dictionary<string, object?>
                        {
                            ["placeholder"] = "18-120",
                            ["type"] = "number",
                            ["min"] = 18,
                            ["max"] = 120,
                            ["required"] = true
                        });
                        view.FormMessage([FormField.ErrorText], match: FormMessageMatch.ValueMissing, content: v =>
                            v.Text(text: "Age is required"));
                        view.FormMessage([FormField.ErrorText], match: FormMessageMatch.RangeUnderflow, content: v =>
                            v.Text(text: "You must be at least 18 years old"));
                        view.FormMessage([FormField.ErrorText], match: FormMessageMatch.RangeOverflow, content: v =>
                            v.Text(text: "Age must be 120 or less"));
                    });

                    view.FormSubmit([Button.PrimaryMd], content: v => v.Text(text: "Submit"));
                });
            });

            // Form with server-side validation
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Server-side Validation");
                view.Text([Text.Caption, "mb-4"], "Edit the fields to clear errors, click 'Simulate Error' to trigger server validation");

                view.Form([Layout.Column.Md, "max-w-md"], content: view =>
                {
                    view.FormField(["space-y-2"], name: "serverEmail", serverInvalid: _serverEmailInvalid.Value, content: view =>
                    {
                        view.FormLabel([FormField.Label], content: v => v.Text(text: "Email (Server Validated)"));
                        view.TextField(
                            [_serverEmailInvalid.Value ? Input.Invalid : Input.Default],
                            placeholder: "Enter your email",
                            value: _serverEmailValue.Value,
                            onValueChange: async value =>
                            {
                                _serverEmailValue.Value = value;
                                _serverEmailInvalid.Value = false;
                            });
                        if (_serverEmailInvalid.Value)
                        {
                            view.Text([FormField.ErrorText], "This email is already registered");
                        }
                    });

                    view.FormField(["space-y-2"], name: "serverUsername", serverInvalid: _serverUsernameInvalid.Value, content: view =>
                    {
                        view.FormLabel([FormField.Label], content: v => v.Text(text: "Username (Server Validated)"));
                        view.TextField(
                            [_serverUsernameInvalid.Value ? Input.Invalid : Input.Default],
                            placeholder: "Enter username",
                            value: _serverUsernameValue.Value,
                            onValueChange: async value =>
                            {
                                _serverUsernameValue.Value = value;
                                _serverUsernameInvalid.Value = false;
                            });
                        if (_serverUsernameInvalid.Value)
                        {
                            view.Text([FormField.ErrorText], "This username is reserved");
                        }
                    });

                    view.Row([Layout.Row.Md], content: view =>
                    {
                        view.Button([Button.PrimaryMd], onClick: async () =>
                        {
                            // Simulate server validation - these values are "taken"
                            if (_serverEmailValue.Value == "taken@example.com")
                            {
                                _serverEmailInvalid.Value = true;
                            }

                            if (_serverUsernameValue.Value == "admin")
                            {
                                _serverUsernameInvalid.Value = true;
                            }
                        }, content: v => v.Text(text: "Submit"));
                        view.Button([Button.OutlineMd], onClick: async () =>
                        {
                            _serverEmailInvalid.Value = true;
                            _serverUsernameInvalid.Value = true;
                        }, content: v => v.Text(text: "Simulate Error"));
                    });
                });
            });

            // Accessibility components
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Accessibility Components");
                view.Text([Text.Caption, "mb-4"], "AccessibleIcon and VisuallyHidden for screen reader support");

                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.BodyStrong], "AccessibleIcon");
                    view.Text([Text.Muted, "mb-2"], "Icons with accessible labels for screen readers");
                    view.Row([Layout.Row.Md, "items-center"], content: view =>
                    {
                        view.AccessibleIcon(label: "Close dialog", content: v =>
                            v.Icon([Icon.Default], name: "x"));
                        view.Text([Text.Caption], "Close icon with label 'Close dialog'");
                    });
                    view.Row([Layout.Row.Md, "items-center"], content: view =>
                    {
                        view.AccessibleIcon(label: "Settings", content: v =>
                            v.Icon([Icon.Default], name: "gear"));
                        view.Text([Text.Caption], "Settings icon with label 'Settings'");
                    });
                    view.Row([Layout.Row.Md, "items-center"], content: view =>
                    {
                        view.AccessibleIcon(label: "Delete item", content: v =>
                            v.Icon([Icon.Default], name: "trash-2"));
                        view.Text([Text.Caption], "Delete icon with label 'Delete item'");
                    });

                    view.Separator([Separator.Horizontal, "my-4"]);

                    view.Text([Text.BodyStrong], "VisuallyHidden");
                    view.Text([Text.Muted, "mb-2"], "Content hidden visually but accessible to screen readers");
                    view.Row([Layout.Row.Md, "items-center"], content: view =>
                    {
                        view.Button([Button.PrimaryMd], content: view =>
                        {
                            view.Icon([Icon.Default, "mr-2"], name: "plus");
                            view.Text(text: "Add");
                            view.VisuallyHidden(content: v => v.Text(text: " new item to the list"));
                        });
                        view.Text([Text.Caption], "Button reads 'Add new item to the list' to screen readers");
                    });
                    view.Row([Layout.Row.Md, "items-center"], content: view =>
                    {
                        view.VisuallyHidden(content: v => v.Text(text: "Navigation section starts here"));
                        view.Text([Text.Body], "Landmark text hidden from visual users");
                    });

                    view.Separator([Separator.Horizontal, "my-4"]);

                    view.Text([Text.BodyStrong], "Screen Reader Only Styles");
                    view.Text([Text.Muted, "mb-2"], "Using Accessibility styles from Default.cs");
                    view.Box(["p-2 border border-dashed border-quaternary rounded"], content: view =>
                    {
                        view.Text([Text.Caption], "This box contains hidden content:");
                        view.Text([Accessibility.ScreenReaderOnly], "This text is only visible to screen readers");
                        view.Text([Text.Caption], "(Check with a screen reader or inspect element)");
                    });
                });
            });
        });
    }
}
