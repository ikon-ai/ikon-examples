public partial class Validation
{
    private void RenderInputsSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // TextField sizes
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "TextField - Sizes");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.TextField([Input.DefaultSm], placeholder: "Small input", value: _textFieldValue.Value,
                        onValueChange: async v => _textFieldValue.Value = v ?? "");
                    view.TextField([Input.Default], placeholder: "Medium input (default)", value: _textFieldValue.Value,
                        onValueChange: async v => _textFieldValue.Value = v ?? "");
                    view.TextField([Input.DefaultLg], placeholder: "Large input", value: _textFieldValue.Value,
                        onValueChange: async v => _textFieldValue.Value = v ?? "");
                });
            });

            // TextField states
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "TextField - States");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.TextField([Input.Default], placeholder: "Normal state", value: _textFieldValue.Value,
                        onValueChange: async v => _textFieldValue.Value = v ?? "");
                    view.TextField([Input.Invalid], placeholder: "Invalid state", value: _textFieldValue.Value,
                        onValueChange: async v => _textFieldValue.Value = v ?? "");
                    view.TextField([Input.InvalidSm], placeholder: "Invalid small", value: _textFieldValue.Value,
                        onValueChange: async v => _textFieldValue.Value = v ?? "");
                    view.TextField([Input.InvalidLg], placeholder: "Invalid large", value: _textFieldValue.Value,
                        onValueChange: async v => _textFieldValue.Value = v ?? "");
                    view.TextField([Input.Ghost], placeholder: "Ghost input", value: _textFieldValue.Value,
                        onValueChange: async v => _textFieldValue.Value = v ?? "");
                    view.TextField([Input.Default], placeholder: "Disabled", disabled: true);
                });
            });

            // TextField validation variants
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "TextField - Validation Variants");
                view.Text([Text.Caption, "mb-4"], "Different visual states for form validation feedback");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Box([FormField.Root], content: view =>
                    {
                        view.Text([FormField.Label], "Success state");
                        view.TextField([Input.Base, Input.Variant.Success, Input.Size.Md], placeholder: "Valid input", value: "valid@email.com");
                        view.Text([FormField.SuccessText], "Email format is valid");
                    });

                    view.Box([FormField.Root], content: view =>
                    {
                        view.Text([FormField.Label], "Warning state");
                        view.TextField([Input.Base, Input.Variant.Warning, Input.Size.Md], placeholder: "Potentially problematic", value: "user123");
                        view.Text([FormField.WarningText], "This username may be taken");
                    });

                    view.Box([FormField.Root], content: view =>
                    {
                        view.Text([FormField.Label], "Error state");
                        view.TextField([Input.Base, Input.Variant.Error, Input.Size.Md], placeholder: "Invalid input", value: "invalid");
                        view.Text([FormField.ErrorText], "Please enter a valid email address");
                    });

                    view.Box([FormField.Root], content: view =>
                    {
                        view.Text([FormField.Label], "ReadOnly state");
                        view.TextField([Input.Default, State.Readonly], placeholder: "Cannot edit", value: "Read-only value", disabled: true);
                        view.Text([FormField.HelpText], "This field cannot be modified (uses disabled with readonly styling)");
                    });
                });
            });

            // TextField types
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "TextField - Types");
                view.Text([Text.Caption, "mb-4"], "Different input types for various data formats");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.Caption], "Text (default)");
                    view.TextField([Input.Default], placeholder: "Enter text...", type: "text");

                    view.Text([Text.Caption], "Email");
                    view.TextField([Input.Default], placeholder: "email@example.com", type: "email");

                    view.Text([Text.Caption], "Password");
                    view.TextField([Input.Default], placeholder: "Enter password...", type: "password");

                    view.Text([Text.Caption], "Number");
                    view.TextField([Input.Default], placeholder: "0", type: "number", step: "1", min: "0", max: "100");

                    view.Text([Text.Caption], "Number with decimals");
                    view.TextField([Input.Default], placeholder: "0.00", type: "number", step: "0.01");

                    view.Text([Text.Caption], "Date");
                    view.TextField([Input.Default], type: "date");
                });
            });

            // TextArea sizes
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "TextArea - Sizes");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.TextArea([Textarea.DefaultSm], placeholder: "Small textarea", value: _textAreaValue.Value,
                        onValueChange: async v => _textAreaValue.Value = v ?? "");
                    view.TextArea([Textarea.Default], placeholder: "Medium textarea (default)", value: _textAreaValue.Value,
                        onValueChange: async v => _textAreaValue.Value = v ?? "");
                    view.TextArea([Textarea.DefaultLg], placeholder: "Large textarea", value: _textAreaValue.Value,
                        onValueChange: async v => _textAreaValue.Value = v ?? "");
                });
            });

            // TextArea states
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "TextArea - States");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.TextArea([Textarea.Default], placeholder: "Normal state", value: _textAreaValue.Value,
                        onValueChange: async v => _textAreaValue.Value = v ?? "");
                    view.TextArea([Textarea.Invalid], placeholder: "Invalid state", value: _textAreaValue.Value,
                        onValueChange: async v => _textAreaValue.Value = v ?? "");
                    view.TextArea([Textarea.Default], placeholder: "Disabled", disabled: true);
                });
            });

            // Select
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Select");
                view.Text([Text.Caption, "mb-4"], "Dropdown select with auto-rendered structure");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.Caption], "Flat options list");
                    view.Select(
                        value: _selectValue.Value,
                        placeholder: "Select a fruit...",
                        options:
                        [
                            new SelectOption("apple", "Apple"),
                            new SelectOption("banana", "Banana"),
                            new SelectOption("orange", "Orange"),
                            new SelectOption("grape", "Grape", Disabled: true)
                        ],
                        onValueChange: async v => _selectValue.Value = v);

                    view.Text([Text.Caption], "Grouped options");
                    view.Select(
                        value: _selectValue.Value,
                        placeholder: "Select food...",
                        groups:
                        [
                            new SelectOptionGroup("Fruits",
                            [
                                new SelectOption("apple", "Apple"),
                                new SelectOption("banana", "Banana"),
                                new SelectOption("orange", "Orange")
                            ]),
                            new SelectOptionGroup("Vegetables",
                            [
                                new SelectOption("carrot", "Carrot"),
                                new SelectOption("broccoli", "Broccoli"),
                                new SelectOption("spinach", "Spinach")
                            ])
                        ],
                        onValueChange: async v => _selectValue.Value = v);

                    view.Text([Text.Caption], "Disabled");
                    view.Select(
                        disabled: true,
                        placeholder: "Disabled select...",
                        options:
                        [
                            new SelectOption("a", "Option A"),
                            new SelectOption("b", "Option B")
                        ]);
                });
            });

            // Checkbox
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Checkbox");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Row([Layout.Row.Sm], content: view =>
                    {
                        view.Checkbox([Checkbox.Root],
                            @checked: _checkboxChecked.Value,
                            onCheckedChange: async state => _checkboxChecked.Value = state,
                            content: view => view.CheckboxIndicator([Checkbox.Indicator], content: v => v.Icon(name: "check")));
                        view.Label([Label.Base], content: v => v.Text(text: "Unchecked/Checked"));
                    });

                    view.Row([Layout.Row.Sm], content: view =>
                    {
                        view.Checkbox([Checkbox.Root],
                            checkedState: CheckedState.Indeterminate,
                            content: view => view.CheckboxIndicator([Checkbox.Indicator], content: v => v.Icon(name: "minus")));
                        view.Label([Label.Base], content: v => v.Text(text: "Indeterminate"));
                    });

                    view.Row([Layout.Row.Sm], content: view =>
                    {
                        view.Checkbox([Checkbox.Root],
                            @checked: true, disabled: true,
                            content: view => view.CheckboxIndicator([Checkbox.Indicator], content: v => v.Icon(name: "check")));
                        view.Label([Label.Base, "opacity-50"], content: v => v.Text(text: "Disabled checked"));
                    });
                });
            });

            // Switch
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Switch");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Row([Layout.Row.Sm], content: view =>
                    {
                        view.Switch([Switch.Root],
                            @checked: _switchChecked.Value,
                            onCheckedChange: async v => _switchChecked.Value = v, content: view => view.SwitchThumb([Switch.Thumb]));
                        view.Label([Label.Base], content: v => v.Text(text: _switchChecked.Value ? "On" : "Off"));
                    });

                    view.Row([Layout.Row.Sm], content: view =>
                    {
                        view.Switch([Switch.Root],
                            @checked: true, disabled: true, content: view => view.SwitchThumb([Switch.Thumb]));
                        view.Label([Label.Base, "opacity-50"], content: v => v.Text(text: "Disabled on"));
                    });

                    view.Row([Layout.Row.Sm], content: view =>
                    {
                        view.Switch([Switch.Root],
                            @checked: false, disabled: true, content: view => view.SwitchThumb([Switch.Thumb]));
                        view.Label([Label.Base, "opacity-50"], content: v => v.Text(text: "Disabled off"));
                    });
                });
            });

            // Radio Group
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Radio Group");
                view.RadioGroup([RadioGroup.Root],
                    value: _radioValue.Value,
                    onValueChange: async v => _radioValue.Value = v ?? "option1", content: view =>
                    {
                        view.Row([Layout.Row.Sm], content: view =>
                        {
                            view.RadioGroupItem([RadioGroup.Item],
                                value: "option1", content: v => v.RadioGroupIndicator([RadioGroup.Indicator]));
                            view.Label([Label.Base], content: v => v.Text(text: "Option 1"));
                        });
                        view.Row([Layout.Row.Sm], content: view =>
                        {
                            view.RadioGroupItem([RadioGroup.Item],
                                value: "option2", content: v => v.RadioGroupIndicator([RadioGroup.Indicator]));
                            view.Label([Label.Base], content: v => v.Text(text: "Option 2"));
                        });
                        view.Row([Layout.Row.Sm], content: view =>
                        {
                            view.RadioGroupItem([RadioGroup.Item],
                                value: "option3", content: v => v.RadioGroupIndicator([RadioGroup.Indicator]));
                            view.Label([Label.Base], content: v => v.Text(text: "Option 3"));
                        });
                    });
            });

            // Radio Group - Horizontal
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Radio Group - Horizontal");
                view.Text([Text.Caption, "mb-4"], "Horizontal orientation for inline radio options");
                view.RadioGroup([RadioGroup.RootHorizontal],
                    value: _radioHorizontalValue.Value,
                    orientation: Orientation.Horizontal,
                    onValueChange: async v => _radioHorizontalValue.Value = v ?? "h-opt1", content: view =>
                    {
                        view.Row([Layout.Row.Sm], content: view =>
                        {
                            view.RadioGroupItem([RadioGroup.Item],
                                value: "h-opt1", content: v => v.RadioGroupIndicator([RadioGroup.Indicator]));
                            view.Label([Label.Base], content: v => v.Text(text: "Small"));
                        });
                        view.Row([Layout.Row.Sm], content: view =>
                        {
                            view.RadioGroupItem([RadioGroup.Item],
                                value: "h-opt2", content: v => v.RadioGroupIndicator([RadioGroup.Indicator]));
                            view.Label([Label.Base], content: v => v.Text(text: "Medium"));
                        });
                        view.Row([Layout.Row.Sm], content: view =>
                        {
                            view.RadioGroupItem([RadioGroup.Item],
                                value: "h-opt3", content: v => v.RadioGroupIndicator([RadioGroup.Indicator]));
                            view.Label([Label.Base], content: v => v.Text(text: "Large"));
                        });
                    });
            });

            // Slider
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Slider");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.Caption], $"Value: {_sliderValue.Value:F0}");
                    view.Slider([Slider.Root],
                        value: [_sliderValue.Value],
                        min: 0, max: 100, step: 1, onValueChange: async v => { if (v.Count > 0) _sliderValue.Value = v[0]; }, content: view =>
                        {
                            view.SliderTrack([Slider.Track], content: view =>
                            {
                                view.SliderRange([Slider.Range]);
                            });
                            view.SliderThumb([Slider.Thumb]);
                        });

                    view.Text([Text.Caption], "Disabled");
                    view.Slider([Slider.Root],
                        value: [30], min: 0, max: 100, disabled: true, content: view =>
                        {
                            view.SliderTrack([Slider.Track], content: view =>
                            {
                                view.SliderRange([Slider.Range]);
                            });
                            view.SliderThumb([Slider.Thumb]);
                        });
                });
            });

            // Slider - Vertical Orientation
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Slider - Vertical");
                view.Text([Text.Caption, "mb-4"], "Vertical slider orientation");
                view.Row([Layout.Row.Lg, "h-48"], content: view =>
                {
                    view.Column(["items-center w-12"], content: view =>
                    {
                        view.Slider([Slider.RootVertical, "h-40"],
                            value: [_sliderVerticalValue.Value],
                            min: 0, max: 100,
                            orientation: Orientation.Vertical,
                            onValueChange: async v => { if (v.Count > 0) _sliderVerticalValue.Value = v[0]; },
                            content: view =>
                            {
                                view.SliderTrack([Slider.TrackVertical], content: view =>
                                {
                                    view.SliderRange([Slider.Range]);
                                });
                                view.SliderThumb([Slider.Thumb]);
                            });
                        view.Text([Text.Caption, "text-center w-full"], $"{_sliderVerticalValue.Value:F0}%");
                    });

                    view.Column(["items-center w-12"], content: view =>
                    {
                        view.Slider([Slider.RootVertical, "h-40 opacity-50"],
                            value: [60],
                            min: 0, max: 100,
                            orientation: Orientation.Vertical,
                            disabled: true,
                            content: view =>
                            {
                                view.SliderTrack([Slider.TrackVertical], content: view =>
                                {
                                    view.SliderRange([Slider.Range]);
                                });
                                view.SliderThumb([Slider.Thumb]);
                            });
                        view.Text([Text.Caption, "text-center w-full"], "Disabled");
                    });
                });
            });

            // Slider - Inverted
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Slider - Inverted");
                view.Text([Text.Caption, "mb-4"], "Inverted slider where the range fills from the end");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.Caption], $"Value: {_sliderInvertedValue.Value:F0}");
                    view.Slider([Slider.Root],
                        value: [_sliderInvertedValue.Value],
                        min: 0, max: 100,
                        inverted: true,
                        onValueChange: async v => { if (v.Count > 0) _sliderInvertedValue.Value = v[0]; },
                        content: view =>
                        {
                            view.SliderTrack([Slider.Track], content: view =>
                            {
                                view.SliderRange([Slider.Range]);
                            });
                            view.SliderThumb([Slider.Thumb]);
                        });

                    view.Text([Text.Caption], $"Vertical Inverted: {_sliderInvertedValue.Value:F0}");
                    view.Column(["items-center w-12 h-40"], content: view =>
                    {
                        view.Slider([Slider.RootVertical, "h-40"],
                            value: [_sliderInvertedValue.Value],
                            min: 0, max: 100,
                            orientation: Orientation.Vertical,
                            inverted: true,
                            onValueChange: async v => { if (v.Count > 0) _sliderInvertedValue.Value = v[0]; },
                            content: view =>
                            {
                                view.SliderTrack([Slider.TrackVertical], content: view =>
                                {
                                    view.SliderRange([Slider.Range]);
                                });
                                view.SliderThumb([Slider.Thumb]);
                            });
                    });
                });
            });

            // Slider - Range (Multiple Thumbs)
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Slider - Range");
                view.Text([Text.Caption, "mb-4"], "Slider with multiple thumbs for selecting a range");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.Caption], $"Range: {_sliderRangeValues.Value[0]:F0} - {_sliderRangeValues.Value[1]:F0}");
                    view.Slider([Slider.Root],
                        value: _sliderRangeValues.Value,
                        min: 0, max: 100,
                        onValueChange: async v => { if (v.Count >= 2) _sliderRangeValues.Value = [v[0], v[1]]; },
                        content: view =>
                        {
                            view.SliderTrack([Slider.Track], content: view =>
                            {
                                view.SliderRange([Slider.Range]);
                            });
                            view.SliderThumb([Slider.Thumb]);
                            view.SliderThumb([Slider.Thumb]);
                        });
                });
            });

            // TextField - Numeric
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "TextField - Numeric");
                view.Text([Text.Caption, "mb-4"], "Different configurations for number type inputs");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Text([Text.Caption], "Integer (0-100)");
                    view.TextField([Input.Default], placeholder: "0", type: "number", step: "1", min: "0", max: "100");

                    view.Text([Text.Caption], "Integer (negative allowed)");
                    view.TextField([Input.Default], placeholder: "0", type: "number", step: "1", min: "-50", max: "50");

                    view.Text([Text.Caption], "Decimal (step 0.01)");
                    view.TextField([Input.Default], placeholder: "0.00", type: "number", step: "0.01");

                    view.Text([Text.Caption], "Decimal (step 0.1, bounded)");
                    view.TextField([Input.Default], placeholder: "0.0", type: "number", step: "0.1", min: "0", max: "10");

                    view.Text([Text.Caption], "Large step (step 10)");
                    view.TextField([Input.Default], placeholder: "0", type: "number", step: "10", min: "0", max: "1000");

                    view.Text([Text.Caption], "No step (browser default)");
                    view.TextField([Input.Default], placeholder: "0", type: "number");

                    view.Text([Text.Caption], "Min only");
                    view.TextField([Input.Default], placeholder: "0", type: "number", step: "1", min: "0");

                    view.Text([Text.Caption], "Max only");
                    view.TextField([Input.Default], placeholder: "0", type: "number", step: "1", max: "100");

                    view.Text([Text.Caption], "Disabled");
                    view.TextField([Input.Default], placeholder: "0", type: "number", step: "1", min: "0", max: "100", disabled: true);

                    view.Text([Text.Caption], "With value binding");
                    view.TextField([Input.Default], value: _numericIntValue.Value, type: "number", step: "1", min: "0", max: "100",
                        onValueChange: async v => _numericIntValue.Value = v ?? "0");
                    view.Text([Text.Caption], $"Current value: {_numericIntValue.Value}");

                    view.Text([Text.Caption], "With decimal value binding");
                    view.TextField([Input.Default], value: _numericDecimalValue.Value, type: "number", step: "0.01",
                        onValueChange: async v => _numericDecimalValue.Value = v ?? "0");
                    view.Text([Text.Caption], $"Current value: {_numericDecimalValue.Value}");

                    view.Text([Text.Caption], "Different sizes");
                    view.TextField([Input.DefaultSm], placeholder: "Small", type: "number", step: "1", min: "0", max: "100");
                    view.TextField([Input.Default], placeholder: "Medium", type: "number", step: "1", min: "0", max: "100");
                    view.TextField([Input.DefaultLg], placeholder: "Large", type: "number", step: "1", min: "0", max: "100");
                });
            });

            // TextField onSubmit test
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "TextField onSubmit");
                view.Text([Text.Body, "mb-4"], "Press Enter to submit");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.TextField([Input.Default, "mb-2"],
                        placeholder: "Type and press Enter...",
                        onSubmit: async () =>
                        {
                            _textFieldSubmitStatus.Value = $"Submitted at {DateTime.Now:HH:mm:ss}";
                        });
                    view.Text([Text.Caption], $"Status: {_textFieldSubmitStatus.Value}");
                });
            });

            // TextArea onSubmit test
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "TextArea onSubmit");
                view.Text([Text.Body, "mb-4"], "Press Ctrl+Enter to submit");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.TextArea([Input.Default, "mb-2"],
                        placeholder: "Type and press Ctrl+Enter...",
                        rows: 3,
                        onSubmit: async () =>
                        {
                            _textAreaSubmitStatus.Value = $"Submitted at {DateTime.Now:HH:mm:ss}";
                        });
                    view.Text([Text.Caption], $"Status: {_textAreaSubmitStatus.Value}");
                });
            });

            // KeyboardListener - Global
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "KeyboardListener - Global");
                view.Text([Text.Caption, "mb-4"], "Listens for all key events on the document");
                view.KeyboardListener(
                    global: true,
                    onKeyDown: async args =>
                    {
                        var modifiers = string.Join("+",
                            new[] { args.CtrlKey ? "Ctrl" : null, args.AltKey ? "Alt" : null, args.ShiftKey ? "Shift" : null, args.MetaKey ? "Meta" : null }
                                .Where(m => m != null));
                        var prefix = modifiers.Length > 0 ? modifiers + "+" : "";
                        _globalKeyDownEvent.Value = $"{prefix}{args.Key} (code: {args.Code}, repeat: {args.Repeat})";
                    },
                    onKeyUp: async args =>
                    {
                        _globalKeyUpEvent.Value = $"{args.Key} (code: {args.Code})";
                    });
                view.Column([Layout.Column.Sm], content: view =>
                {
                    view.Text([Text.Caption], $"Last key down: {_globalKeyDownEvent.Value}");
                    view.Text([Text.Caption], $"Last key up: {_globalKeyUpEvent.Value}");
                });
            });

            // KeyboardListener - Scoped
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "KeyboardListener - Scoped");
                view.Text([Text.Caption, "mb-4"], "Click inside the box below, then use arrow keys or Enter");
                view.KeyboardListener(
                    global: false,
                    preventDefault: true,
                    keys: new[] { Key.ArrowUp, Key.ArrowDown, Key.ArrowLeft, Key.ArrowRight, Key.Enter },
                    onKeyDown: async args =>
                    {
                        _scopedKeyDownEvent.Value = $"{args.Key} (code: {args.Code})";
                    },
                    content: view =>
                    {
                        view.Column([Layout.Column.Sm, "p-4 border border-dashed border-gray-400 rounded-md"], content: view =>
                        {
                            view.Text([Text.Body], "Focus this area and press arrow keys or Enter");
                            view.Text([Text.Caption], $"Last scoped key: {_scopedKeyDownEvent.Value}");
                        });
                    });
            });
        });
    }
}
