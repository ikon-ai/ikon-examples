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
                        view.TextField([Input.Success], placeholder: "Valid input", value: "valid@email.com");
                        view.Text([FormField.SuccessText], "Email format is valid");
                    });

                    view.Box([FormField.Root], content: view =>
                    {
                        view.Text([FormField.Label], "Warning state");
                        view.TextField([Input.Warning], placeholder: "Potentially problematic", value: "user123");
                        view.Text([FormField.WarningText], "This username may be taken");
                    });

                    view.Box([FormField.Root], content: view =>
                    {
                        view.Text([FormField.Label], "Error state");
                        view.TextField([Input.Error], placeholder: "Invalid input", value: "invalid");
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

            // TextArea autoResize playground
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "TextArea - Auto-resize");
                view.Text([Text.Caption, "mb-4"], "Toggle autoResize and tweak rows / maxRows live.");

                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Row(["items-center gap-3 flex-wrap"], content: row =>
                    {
                        row.Switch([Switch.Root],
                            value: _autoResizePlaygroundEnabled.Value,
                            onValueChange: async v => _autoResizePlaygroundEnabled.Value = v,
                            content: s => s.SwitchThumb([Switch.Thumb]));
                        row.Text([Text.Body], $"autoResize: {(_autoResizePlaygroundEnabled.Value ? "true" : "false")}");
                    });

                    view.Row(["items-center gap-3 flex-wrap"], content: row =>
                    {
                        row.Text([Text.Body, "w-28"], $"rows: {_autoResizePlaygroundRows.Value}");
                        row.Slider([Slider.Root, "w-64"],
                            value: [_autoResizePlaygroundRows.Value],
                            min: 1, max: 10, step: 1,
                            onValueChange: async v => { if (v.Count > 0) _autoResizePlaygroundRows.Value = (int)v[0]; },
                            content: s =>
                            {
                                s.SliderTrack([Slider.Track], content: t => t.SliderRange([Slider.Range]));
                                s.SliderThumb([Slider.Thumb]);
                            });
                    });

                    view.Row(["items-center gap-3 flex-wrap"], content: row =>
                    {
                        row.Switch([Switch.Root],
                            value: _autoResizePlaygroundMaxRowsDefined.Value,
                            onValueChange: async v => _autoResizePlaygroundMaxRowsDefined.Value = v,
                            content: s => s.SwitchThumb([Switch.Thumb]));
                        row.Text([Text.Body], $"maxRows defined: {(_autoResizePlaygroundMaxRowsDefined.Value ? "true" : "false")}");
                    });

                    if (_autoResizePlaygroundMaxRowsDefined.Value)
                    {
                        view.Row(["items-center gap-3 flex-wrap"], content: row =>
                        {
                            row.Text([Text.Body, "w-28"], $"maxRows: {_autoResizePlaygroundMaxRows.Value}");
                            row.Slider([Slider.Root, "w-64"],
                                value: [_autoResizePlaygroundMaxRows.Value],
                                min: 1, max: 20, step: 1,
                                onValueChange: async v => { if (v.Count > 0) _autoResizePlaygroundMaxRows.Value = (int)v[0]; },
                                content: s =>
                                {
                                    s.SliderTrack([Slider.Track], content: t => t.SliderRange([Slider.Range]));
                                    s.SliderThumb([Slider.Thumb]);
                                });
                        });
                    }

                    var maxRowsLabel = _autoResizePlaygroundMaxRowsDefined.Value
                        ? _autoResizePlaygroundMaxRows.Value.ToString()
                        : "undefined (uncapped)";

                    view.Text([Text.Caption],
                        $"Live config — autoResize: {(_autoResizePlaygroundEnabled.Value ? "true" : "false")}, rows: {_autoResizePlaygroundRows.Value}, maxRows: {maxRowsLabel}");

                    view.TextArea([Textarea.Default, "!min-h-0"],
                        placeholder: "Type multiple lines and watch the height respond to the controls above.",
                        value: _textAreaValue.Value,
                        rows: _autoResizePlaygroundRows.Value,
                        autoResize: _autoResizePlaygroundEnabled.Value,
                        maxRows: _autoResizePlaygroundMaxRowsDefined.Value ? _autoResizePlaygroundMaxRows.Value : (int?)null,
                        submitOnEnter: false,
                        onValueChange: async v => _textAreaValue.Value = v ?? "");
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
                    view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
                    {
                        view.Checkbox([Checkbox.Root],
                            value: _checkboxChecked.Value,
                            onValueChange: async state => _checkboxChecked.Value = state,
                            content: view => view.CheckboxIndicator([Checkbox.Indicator], content: v => v.Icon(name: "check")));
                        view.Label([Label.Base], content: v => v.Text(text: "Unchecked/Checked"));
                    });

                    view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
                    {
                        view.TriStateCheckbox([Checkbox.Root],
                            value: CheckedState.Indeterminate,
                            content: view => view.CheckboxIndicator([Checkbox.Indicator], content: v => v.Icon(name: "minus")));
                        view.Label([Label.Base], content: v => v.Text(text: "Indeterminate"));
                    });

                    view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
                    {
                        view.Checkbox([Checkbox.Root],
                            value: true, disabled: true,
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
                    view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
                    {
                        view.Switch([Switch.Root],
                            value: _switchChecked.Value,
                            onValueChange: async v => _switchChecked.Value = v, content: view => view.SwitchThumb([Switch.Thumb]));
                        view.Label([Label.Base], content: v => v.Text(text: _switchChecked.Value ? "On" : "Off"));
                    });

                    view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
                    {
                        view.Switch([Switch.Root],
                            value: true, disabled: true, content: view => view.SwitchThumb([Switch.Thumb]));
                        view.Label([Label.Base, "opacity-50"], content: v => v.Text(text: "Disabled on"));
                    });

                    view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
                    {
                        view.Switch([Switch.Root],
                            value: false, disabled: true, content: view => view.SwitchThumb([Switch.Thumb]));
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
                        view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
                        {
                            view.RadioGroupItem([RadioGroup.Item],
                                value: "option1", content: v => v.RadioGroupIndicator([RadioGroup.Indicator]));
                            view.Label([Label.Base], content: v => v.Text(text: "Option 1"));
                        });
                        view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
                        {
                            view.RadioGroupItem([RadioGroup.Item],
                                value: "option2", content: v => v.RadioGroupIndicator([RadioGroup.Indicator]));
                            view.Label([Label.Base], content: v => v.Text(text: "Option 2"));
                        });
                        view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
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
                        view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
                        {
                            view.RadioGroupItem([RadioGroup.Item],
                                value: "h-opt1", content: v => v.RadioGroupIndicator([RadioGroup.Indicator]));
                            view.Label([Label.Base], content: v => v.Text(text: "Small"));
                        });
                        view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
                        {
                            view.RadioGroupItem([RadioGroup.Item],
                                value: "h-opt2", content: v => v.RadioGroupIndicator([RadioGroup.Indicator]));
                            view.Label([Label.Base], content: v => v.Text(text: "Medium"));
                        });
                        view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
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
                view.Row([Layout.Row.Lg, "h-48 flex-wrap"], content: view =>
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
                        onSubmit: async _ =>
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
                    view.TextArea([Textarea.Default, "mb-2"],
                        placeholder: "Type and press Ctrl+Enter...",
                        rows: 3,
                        onSubmit: async _ =>
                        {
                            _textAreaSubmitStatus.Value = $"Submitted at {DateTime.Now:HH:mm:ss}";
                        });
                    view.Text([Text.Caption], $"Status: {_textAreaSubmitStatus.Value}");
                });
            });
        });
    }

    private void RenderAdvancedInputsSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Calendar
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Calendar");
                view.Text([Text.Caption, "mb-4"], "Inline month-grid selector");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Calendar(
                        style: [Calendar.Root],
                        headerStyle: [Calendar.Header],
                        weekdayStyle: [Calendar.Weekday],
                        dayStyle: [Calendar.Day],
                        daySelectedStyle: [Calendar.DaySelected],
                        dayTodayStyle: [Calendar.DayToday],
                        dayOutsideStyle: [Calendar.DayOutside],
                        dayDisabledStyle: [Calendar.DayDisabled],
                        navButtonStyle: [Calendar.NavButton],
                        titleStyle: [Calendar.HeaderTitle],
                        gridStyle: [Calendar.Grid],
                        rowStyle: [Calendar.Row],
                        value: _calendarValue.Value,
                        onValueChange: async v => _calendarValue.Value = v ?? "");
                    view.Text([Text.Caption], $"Selected: {_calendarValue.Value}");
                });
            });

            // DatePicker
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "DatePicker");
                view.Text([Text.Caption, "mb-4"], "Trigger + popover containing a calendar");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.DatePicker(
                        triggerStyle: [DatePicker.Trigger],
                        contentStyle: [DatePicker.Content, "p-2"],
                        calendarStyle: [Calendar.Root],
                        headerStyle: [Calendar.Header],
                        weekdayStyle: [Calendar.Weekday],
                        dayStyle: [Calendar.Day],
                        daySelectedStyle: [Calendar.DaySelected],
                        dayTodayStyle: [Calendar.DayToday],
                        dayOutsideStyle: [Calendar.DayOutside],
                        dayDisabledStyle: [Calendar.DayDisabled],
                        navButtonStyle: [Calendar.NavButton],
                        titleStyle: [Calendar.HeaderTitle],
                        gridStyle: [Calendar.Grid],
                        rowStyle: [Calendar.Row],
                        placeholder: "Select a date",
                        value: _datePickerValue.Value,
                        minDate: "2025-01-01",
                        maxDate: "2030-12-31",
                        onValueChange: async v => _datePickerValue.Value = v ?? "");
                    view.Text([Text.Caption], $"Selected: {(_datePickerValue.Value == "" ? "(none)" : _datePickerValue.Value)}");
                });
            });

            // ColorPicker
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "ColorPicker");
                view.Text([Text.Caption, "mb-4"], "Saturation square + hue slider + hex input");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.ColorPicker(
                        triggerStyle: [ColorPicker.Trigger],
                        contentStyle: [ColorPicker.Content],
                        value: _colorPickerValue.Value,
                        format: ColorFormat.Hex,
                        presets: ["#ef4444", "#f97316", "#eab308", "#22c55e", "#14b8a6", "#3b82f6", "#6366f1", "#9d76ed"],
                        onValueChange: async v => _colorPickerValue.Value = v ?? "#000000");
                    view.Row([Layout.Row.Sm, "flex-wrap"], content: v =>
                    {
                        v.Text([Text.Caption], $"Hex: {_colorPickerValue.Value}");
                    });
                });
            });

            // TimePicker
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "TimePicker");
                view.Text([Text.Caption, "mb-4"], "Hour / minute columns with optional 12-hour format");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.TimePicker(
                        triggerStyle: [TimePicker.Trigger],
                        contentStyle: [TimePicker.Content],
                        columnStyle: [TimePicker.Column],
                        itemStyle: [TimePicker.Item],
                        itemSelectedStyle: [TimePicker.ItemSelected],
                        value: _timePickerValue.Value,
                        hourFormat: HourFormat.Hour24,
                        granularity: TimeGranularity.Minute,
                        minuteStep: 5,
                        onValueChange: async v => _timePickerValue.Value = v ?? "");
                    view.Text([Text.Caption], $"Selected: {_timePickerValue.Value}");
                });
            });

            // RichTextEditor
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "RichTextEditor");
                view.Text([Text.Caption, "mb-4"], "Inline HTML editor with formatting toolbar");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.RichTextEditor(
                        style: [RichTextEditor.Root],
                        toolbarStyle: [RichTextEditor.Toolbar],
                        toolbarButtonStyle: [RichTextEditor.ToolbarButton],
                        contentStyle: [RichTextEditor.Content],
                        placeholder: "Write something…",
                        value: _richTextValue.Value,
                        onValueChange: async v => _richTextValue.Value = v ?? "");
                    view.Text([Text.Caption], $"HTML length: {_richTextValue.Value.Length}");
                });
            });

            // CodeEditor
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "CodeEditor");
                view.Text([Text.Caption, "mb-4"], "Monospace editor with line-number gutter");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.CodeEditor(
                        style: [CodeEditor.Root],
                        gutterStyle: [CodeEditor.Gutter],
                        contentStyle: [CodeEditor.Content],
                        languageBadgeStyle: [CodeEditor.LanguageBadge],
                        language: "csharp",
                        value: _codeEditorValue.Value,
                        tabSize: 4,
                        onValueChange: async v => _codeEditorValue.Value = v ?? "");
                    view.Text([Text.Caption], $"Lines: {_codeEditorValue.Value.Count(c => c == '\n') + 1}");
                });
            });

            // Carousel
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Carousel");
                view.Text([Text.Caption, "mb-4"], "Horizontal slider with arrows + indicator dots");
                view.Column([Layout.Column.Md], content: view =>
                {
                    view.Row(["items-center gap-3 flex-wrap"], content: row =>
                    {
                        row.Switch([Switch.Root],
                            value: _carouselLoop.Value,
                            onValueChange: async v => _carouselLoop.Value = v,
                            content: s => s.SwitchThumb([Switch.Thumb]));
                        row.Text([Text.Body], $"loop: {(_carouselLoop.Value ? "true" : "false")}");
                    });

                    view.Carousel(
                        rootStyle: [Carousel.Root, "h-56 rounded-lg"],
                        viewportStyle: [Carousel.Viewport],
                        previousStyle: [Carousel.Previous],
                        nextStyle: [Carousel.Next],
                        indicatorsStyle: [Carousel.Indicators],
                        indicatorStyle: [Carousel.Indicator],
                        indicatorActiveStyle: [Carousel.IndicatorActive],
                        loop: _carouselLoop.Value,
                        index: _carouselIndex.Value,
                        onIndexChange: async i => _carouselIndex.Value = (int)i,
                        slides:
                        [
                            new CarouselSlideItem(v => v.Box(["h-full w-full flex items-center justify-center bg-brand-secondary text-brand-primary text-3xl font-semibold"], content: vv => vv.Text(text: "Slide 1"))),
                            new CarouselSlideItem(v => v.Box(["h-full w-full flex items-center justify-center bg-success-primary text-success-primary text-3xl font-semibold"], content: vv => vv.Text(text: "Slide 2"))),
                            new CarouselSlideItem(v => v.Box(["h-full w-full flex items-center justify-center bg-warning-primary text-warning-primary text-3xl font-semibold"], content: vv => vv.Text(text: "Slide 3"))),
                            new CarouselSlideItem(v => v.Box(["h-full w-full flex items-center justify-center bg-info-solid/10 text-info-primary text-3xl font-semibold"], content: vv => vv.Text(text: "Slide 4"))),
                        ]);
                    view.Text([Text.Caption], $"Active slide: {_carouselIndex.Value + 1}");
                });
            });

            // Carousel - multi-item per page
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Carousel - Multi-item");
                view.Text([Text.Caption, "mb-4"], "Responsive: 1 item < 480px · 2 items < 768px · 3 items < 1024px · 4 items ≥ 1024px. Resize the window to see breakpoints kick in.");
                view.Column([Layout.Column.Md], content: view =>
                {
                    var tileColors = new[]
                    {
                        "bg-brand-secondary text-brand-primary",
                        "bg-success-primary text-success-primary",
                        "bg-warning-primary text-warning-primary",
                        "bg-info-solid/10 text-info-primary",
                        "bg-error-primary text-error-primary",
                        "bg-quaternary text-primary",
                        "bg-brand-secondary text-brand-primary",
                        "bg-success-primary text-success-primary",
                    };

                    view.Row(["items-center gap-3 flex-wrap"], content: row =>
                    {
                        row.Switch([Switch.Root],
                            value: _multiCarouselLoop.Value,
                            onValueChange: async v => _multiCarouselLoop.Value = v,
                            content: s => s.SwitchThumb([Switch.Thumb]));
                        row.Text([Text.Body], $"loop: {(_multiCarouselLoop.Value ? "true" : "false")}");
                    });

                    view.Carousel(
                        rootStyle: [Carousel.Root, "h-40 rounded-lg"],
                        viewportStyle: [Carousel.Viewport],
                        previousStyle: [Carousel.Previous],
                        nextStyle: [Carousel.Next],
                        indicatorsStyle: [Carousel.Indicators],
                        indicatorStyle: [Carousel.Indicator],
                        indicatorActiveStyle: [Carousel.IndicatorActive],
                        loop: _multiCarouselLoop.Value,
                        slidesPerView: 1,
                        slidesPerGroup: 1,
                        slideGapPx: 12,
                        breakpoints:
                        [
                            new CarouselBreakpoint(MinWidth: 480, SlidesPerView: 2),
                            new CarouselBreakpoint(MinWidth: 768, SlidesPerView: 3),
                            new CarouselBreakpoint(MinWidth: 1024, SlidesPerView: 4),
                        ],
                        index: _multiCarouselIndex.Value,
                        onIndexChange: async i => _multiCarouselIndex.Value = (int)i,
                        slides: tileColors.Select((cls, i) => new CarouselSlideItem(v =>
                            v.Box([$"h-full w-full flex items-center justify-center rounded-md text-2xl font-semibold {cls}"],
                                content: vv => vv.Text(text: $"Item {i + 1}")))).ToList());
                    view.Text([Text.Caption], $"Leading slide index: {_multiCarouselIndex.Value + 1} of 8");
                });
            });

            // FeedScroller (TikTok-style vertical feed)
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "FeedScroller");
                view.Text([Text.Caption, "mb-4"], "Full-viewport vertical feed · scroll-snap · IntersectionObserver · preload window · near-end paging");

                view.Box(["h-[560px] w-full max-w-[360px] mx-auto rounded-2xl overflow-hidden bg-black"], content: view =>
                {
                    var loaded = _feedPagesLoaded.Value;
                    var colors = new[] { "from-violet-600 to-fuchsia-500", "from-sky-500 to-emerald-500", "from-rose-500 to-orange-500", "from-amber-500 to-pink-500", "from-cyan-500 to-indigo-500", "from-green-500 to-lime-400" };

                    var slides = Enumerable.Range(0, loaded * 3).Select(i => new FeedSlide(
                        Content: v => v.Box([$"h-full w-full bg-gradient-to-br {colors[i % colors.Length]} flex items-center justify-center relative"], content: vv =>
                        {
                            vv.Box([FeedScroller.SlideOverlay], content: o =>
                            {
                                o.Text(["text-2xl font-semibold"], $"Slide #{i + 1}");
                                o.Text(["text-sm opacity-80"], "Vertical snap · preloaded · muted autoplay");
                            });
                        }),
                        Key: $"slide-{i}")).ToList();

                    view.FeedScroller(
                        slides: slides,
                        style: [FeedScroller.Root],
                        slideStyle: [FeedScroller.Slide],
                        activeIndex: _feedActiveIndex.Value,
                        preloadAhead: 2,
                        preloadBehind: 1,
                        scrollEndThreshold: 2,
                        muted: _feedMuted.Value,
                        onActiveChange: async i => _feedActiveIndex.Value = (int)i,
                        onScrollNearEnd: async _ => _feedPagesLoaded.Value = _feedPagesLoaded.Value + 1,
                        onMuteChange: async m => _feedMuted.Value = m);
                });

                view.Row([Layout.Row.Md, "mt-3 flex-wrap"], content: v =>
                {
                    v.Text([Text.Caption], $"Active: {_feedActiveIndex.Value + 1}");
                    v.Text([Text.Caption], $"Pages loaded: {_feedPagesLoaded.Value}");
                    v.Text([Text.Caption], _feedMuted.Value ? "🔇 Muted" : "🔊 On");
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

            // Themed defaults - every component below is written bare, with no slot styles
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Themed defaults");
                view.Text([Text.Caption, "mb-4"], "No explicit styles are passed; each part must still carry its theme");
                view.Column([Layout.Column.Lg], content: view =>
                {
                    view.Text([Text.Body], "DatePicker");
                    view.DatePicker(placeholder: "Select a date");

                    view.Text([Text.Body], "TimePicker");
                    view.TimePicker();

                    view.Text([Text.Body], "ColorPicker");
                    view.ColorPicker();

                    view.Text([Text.Body], "Calendar");
                    view.Calendar();

                    view.Text([Text.Body], "RichTextEditor");
                    view.RichTextEditor(placeholder: "Write something…");

                    view.Text([Text.Body], "CodeEditor");
                    view.CodeEditor(language: "csharp", defaultValue: "var x = 1;");

                    view.Text([Text.Body], "FileUpload");
                    view.FileUpload();
                });
            });
        });
    }
}
