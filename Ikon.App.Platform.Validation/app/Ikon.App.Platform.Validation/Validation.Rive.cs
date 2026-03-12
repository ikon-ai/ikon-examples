public partial class Validation
{
    private void RenderRiveSection(UIView view)
    {
        view.Column([Layout.Column.Lg], content: view =>
        {
            // Section 1: Layout Fit & Alignment
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Layout Fit & Alignment");
                view.Text([Text.Caption, "mb-4"], "Control how Rive animations fit within their containers");

                view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                {
                    view.Column(["flex-1 min-w-48"], content: view =>
                    {
                        view.Text([Text.Label, "mb-2"], "Fit");
                        view.Select(
                            triggerStyle: ["w-full"],
                            value: _riveFit.Value.ToString(),
                            placeholder: "Select fit",
                            options: System.Enum.GetValues<RiveFit>().Select(f => new SelectOption(f.ToString(), f.ToString())).ToArray(),
                            onValueChange: async value =>
                            {
                                if (System.Enum.TryParse<RiveFit>(value, out var fit))
                                {
                                    _riveFit.Value = fit;
                                }
                            });
                    });

                    view.Column(["flex-1 min-w-48"], content: view =>
                    {
                        view.Text([Text.Label, "mb-2"], "Alignment");
                        view.Select(
                            triggerStyle: ["w-full"],
                            value: _riveAlignment.Value.ToString(),
                            placeholder: "Select alignment",
                            options: System.Enum.GetValues<RiveAlignment>().Select(a => new SelectOption(a.ToString(), a.ToString())).ToArray(),
                            onValueChange: async value =>
                            {
                                if (System.Enum.TryParse<RiveAlignment>(value, out var alignment))
                                {
                                    _riveAlignment.Value = alignment;
                                }
                            });
                    });
                });
            });

            // Section 2: Event Testing with rating.riv
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Rive Events - Rating Animation");
                view.Text([Text.Caption, "mb-4"], "Interactive rating animation that emits events with structured data");

                view.Row([Layout.Row.Lg], content: view =>
                {
                    view.Box(["flex-1 h-64 rounded-lg overflow-hidden bg-slate-900"], content: view =>
                    {
                        view.RiveCanvas(
                            ["w-full h-full"],
                            source: "/rating.riv",
                            stateMachines: ["State Machine 1"],
                            onEvent: async eventData =>
                            {
                                _riveLastEventName.Value = eventData.Name;
                                _riveLastEventRating.Value = eventData.Props.GetInt("rating").ToString();
                                _riveLastEventMessage.Value = eventData.Props.GetString("message");
                            },
                            layoutFit: _riveFit.Value,
                            layoutAlignment: _riveAlignment.Value,
                            autoplay: true,
                            key: "rating-test");
                    });

                    view.Column(["flex-1"], content: view =>
                    {
                        view.Text([Text.H3, "mb-2"], "Event Output");
                        view.Box(["p-4 bg-quaternary rounded-lg"], content: view =>
                        {
                            view.Text([Text.Label], "Last Event Name:");
                            view.Text([Text.Body, "mb-2 font-mono"], _riveLastEventName.Value);

                            view.Text([Text.Label], "Rating:");
                            view.Text([Text.Body, "mb-2 font-mono"], string.IsNullOrEmpty(_riveLastEventRating.Value) ? "(none)" : _riveLastEventRating.Value);

                            view.Text([Text.Label], "Message:");
                            view.Text([Text.Body, "font-mono"], string.IsNullOrEmpty(_riveLastEventMessage.Value) ? "(none)" : _riveLastEventMessage.Value);
                        });

                        view.Text([Text.Caption, "mt-4"], "Click on the stars in the animation to emit events");
                    });
                });
            });

            // Section 3: Person Databinding - Known I/O Testing
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Rive Data Binding - Person Form");
                view.Text([Text.Caption, "mb-4"], "Test string, number, boolean, color, and enum inputs with documented property names");

                view.Row([Layout.Row.Lg], content: view =>
                {
                    view.Box(["flex-1 h-96 rounded-lg overflow-hidden bg-slate-900"], content: view =>
                    {
                        view.RiveCanvas(
                            ["w-full h-full"],
                            source: "/person_databinding_test.riv",
                            stateMachines: ["State Machine 1"],
                            viewModel: new RiveViewModel()
                                .Boolean("agreedToTerms", _rivePersonAgreedToTerms.Value)
                                .Number("age", _rivePersonAge.Value)
                                .String("name", _rivePersonName.Value)
                                .Color("favColor", _riveFavColorR.Value, _riveFavColorG.Value, _riveFavColorB.Value)
                                .Enum("country", _riveCountry.Value),
                            triggers: [_riveSubmitTrigger, _riveResetTrigger],
                            layoutFit: _riveFit.Value,
                            layoutAlignment: _riveAlignment.Value,
                            autoplay: true,
                            key: "person-test");
                    });

                    view.Column(["flex-1 overflow-y-auto max-h-96 p-4"], content: view =>
                    {
                        view.Text([Text.H3, "mb-4"], "Input Controls");

                        view.Column([Layout.Column.Md], content: view =>
                        {
                            view.Text([Text.Label, "mb-2"], "Name (string input)");
                            view.TextField(
                                [Input.Default],
                                value: _rivePersonName.Value,
                                placeholder: "Enter name",
                                onValueChange: value =>
                                {
                                    _rivePersonName.Value = value;
                                    return Task.CompletedTask;
                                });

                            view.Text([Text.Label, "mb-2 mt-4"], $"Age (number input): {_rivePersonAge.Value:F0}");
                            view.Slider(
                                [Slider.Default],
                                value: [_rivePersonAge.Value],
                                min: 0,
                                max: 100,
                                step: 1,
                                onValueChange: values =>
                                {
                                    if (values.Count > 0)
                                    {
                                        _rivePersonAge.Value = values[0];
                                    }

                                    return Task.CompletedTask;
                                },
                                content: view =>
                                {
                                    view.SliderTrack([Slider.Track], content: view =>
                                    {
                                        view.SliderRange([Slider.Range]);
                                    });
                                    view.SliderThumb([Slider.Thumb]);
                                });

                            view.Row([Layout.Row.InlineCenter, "mt-4"], content: view =>
                            {
                                view.Switch(
                                    [Switch.Default],
                                    @checked: _rivePersonAgreedToTerms.Value,
                                    onCheckedChange: value =>
                                    {
                                        _rivePersonAgreedToTerms.Value = value;
                                        return Task.CompletedTask;
                                    },
                                    content: view => view.SwitchThumb([Switch.Thumb]));
                                view.Text([Text.Caption, "ml-2"], "Agreed to Terms (boolean input)");
                            });

                            view.Text([Text.Label, "mb-2 mt-4"], "Country (enum input)");
                            view.Select(
                                triggerStyle: ["w-full"],
                                value: _riveCountry.Value.ToString(),
                                placeholder: "Select country",
                                options:
                                [
                                    new SelectOption("0", "USA"),
                                    new SelectOption("1", "UK"),
                                    new SelectOption("2", "Canada"),
                                    new SelectOption("3", "Australia")
                                ],
                                onValueChange: async value =>
                                {
                                    if (int.TryParse(value, out var index))
                                    {
                                        _riveCountry.Value = index;
                                    }
                                });

                            view.Text([Text.Label, "mb-2 mt-4"], "Favorite Color (color input)");
                            view.Row([Layout.Row.Sm, "flex-wrap"], content: view =>
                            {
                                view.Column(["flex-1 min-w-20"], content: view =>
                                {
                                    view.Text([Text.Caption], $"R: {_riveFavColorR.Value}");
                                    view.Slider(
                                        [Slider.Default],
                                        value: [_riveFavColorR.Value],
                                        min: 0,
                                        max: 255,
                                        step: 1,
                                        onValueChange: values =>
                                        {
                                            if (values.Count > 0)
                                            {
                                                _riveFavColorR.Value = (int)values[0];
                                            }

                                            return Task.CompletedTask;
                                        },
                                        content: view =>
                                        {
                                            view.SliderTrack([Slider.Track], content: view =>
                                            {
                                                view.SliderRange([Slider.Range]);
                                            });
                                            view.SliderThumb([Slider.Thumb]);
                                        });
                                });

                                view.Column(["flex-1 min-w-20"], content: view =>
                                {
                                    view.Text([Text.Caption], $"G: {_riveFavColorG.Value}");
                                    view.Slider(
                                        [Slider.Default],
                                        value: [_riveFavColorG.Value],
                                        min: 0,
                                        max: 255,
                                        step: 1,
                                        onValueChange: values =>
                                        {
                                            if (values.Count > 0)
                                            {
                                                _riveFavColorG.Value = (int)values[0];
                                            }

                                            return Task.CompletedTask;
                                        },
                                        content: view =>
                                        {
                                            view.SliderTrack([Slider.Track], content: view =>
                                            {
                                                view.SliderRange([Slider.Range]);
                                            });
                                            view.SliderThumb([Slider.Thumb]);
                                        });
                                });

                                view.Column(["flex-1 min-w-20"], content: view =>
                                {
                                    view.Text([Text.Caption], $"B: {_riveFavColorB.Value}");
                                    view.Slider(
                                        [Slider.Default],
                                        value: [_riveFavColorB.Value],
                                        min: 0,
                                        max: 255,
                                        step: 1,
                                        onValueChange: values =>
                                        {
                                            if (values.Count > 0)
                                            {
                                                _riveFavColorB.Value = (int)values[0];
                                            }

                                            return Task.CompletedTask;
                                        },
                                        content: view =>
                                        {
                                            view.SliderTrack([Slider.Track], content: view =>
                                            {
                                                view.SliderRange([Slider.Range]);
                                            });
                                            view.SliderThumb([Slider.Thumb]);
                                        });
                                });
                            });

                            view.Box(
                                [$"w-full h-6 rounded mt-2", $"bg-[rgb({_riveFavColorR.Value},{_riveFavColorG.Value},{_riveFavColorB.Value})]"],
                                content: _ => { });
                        });

                        view.Text([Text.H3, "mb-2 mt-6"], "Triggers");
                        view.Row([Layout.Row.Md, "flex-wrap"], content: view =>
                        {
                            view.Button(
                                [Button.PrimaryMd],
                                label: "Submit Form",
                                onClick: async () => _riveSubmitTrigger.Fire());

                            view.Button(
                                [Button.SecondaryMd],
                                label: "Reset Form",
                                onClick: async () => _riveResetTrigger.Fire());
                        });
                    });
                });
            });

            // Section 4: Keyboard Bindings Test
            view.Box([Card.Default, "p-6"], content: view =>
            {
                view.Text([Text.H2, "mb-4"], "Keyboard Bindings");
                view.Text([Text.Caption, "mb-4"], "Focus the animation area and use arrow keys to control triggers");

                view.Row([Layout.Row.Lg], content: view =>
                {
                    view.Box(["flex-1 h-64 rounded-lg overflow-hidden bg-slate-900 focus:ring-2 focus:ring-blue-500"], content: view =>
                    {
                        view.RiveCanvas(
                            ["w-full h-full"],
                            source: "/person_databinding_test.riv",
                            stateMachines: ["State Machine 1"],
                            keyboardBindings:
                            [
                                RiveKeyboard.Trigger(RiveKeyboardKey.ArrowUp, "onFormSubmit"),
                                RiveKeyboard.Trigger(RiveKeyboardKey.ArrowDown, "onFormReset")
                            ],
                            layoutFit: _riveFit.Value,
                            layoutAlignment: _riveAlignment.Value,
                            autoplay: true,
                            key: "keyboard-test");
                    });

                    view.Column(["flex-1"], content: view =>
                    {
                        view.Text([Text.H3, "mb-4"], "Keyboard Controls");
                        view.Box(["p-4 bg-quaternary rounded-lg"], content: view =>
                        {
                            view.Row([Layout.Row.InlineCenter, "mb-2"], content: view =>
                            {
                                view.Text([Text.Code], "Arrow Up");
                                view.Text([Text.Body, "ml-2"], "Fire onFormSubmit trigger");
                            });

                            view.Row([Layout.Row.InlineCenter], content: view =>
                            {
                                view.Text([Text.Code], "Arrow Down");
                                view.Text([Text.Body, "ml-2"], "Fire onFormReset trigger");
                            });
                        });

                        view.Text([Text.Caption, "mt-4"], "Click on the animation first to focus it, then use arrow keys");
                    });
                });
            });

        });
    }
}
