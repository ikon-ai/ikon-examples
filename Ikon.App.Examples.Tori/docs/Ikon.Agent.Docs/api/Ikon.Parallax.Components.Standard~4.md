namespace Ikon.Parallax.Components.Standard
  static class FormExtensions
    // formValue: HTML form value submitted when checked.
    // label: Trailing text label wrapped with the checkbox in a <label> — clicking the text toggles the control and the text becomes its accessible name. Prefer this over placing your own Text beside a bare Checkbox, which associates nothing.
    // bind: Two-way binds the checkbox to a Reactive<T> — reads bind.Value and writes it back on every toggle. When set, value: is ignored and onValueChange still fires after the write-back.
    static void Checkbox(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<bool>? bind = null, string? ariaLabel = null)
    // forceMount: When true, forces the indicator to render even when the checkbox is unchecked.
    static void CheckboxIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // onClearServerErrors: Invoked when server-side validation errors should be cleared.
    static void Form(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onClearServerErrors = null, Action<UIView>? content = null)
    static void FormControl(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // name: The name of the form field, used for validation and form submission.
    // serverInvalid: When true, indicates the field has a server-side validation error.
    static void FormField(this UIView view, string[]? style = null, string? name = null, bool? serverInvalid = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void FormLabel(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // match: The validation condition that must be met for this message to display.
    // forceMatch: When true, forces the message to display regardless of the match condition.
    static void FormMessage(this UIView view, string[]? style = null, FormMessageMatch? match = null, bool? forceMatch = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    static void FormSubmit(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // htmlFor: The id of the element this label is associated with.
    static void Label(this UIView view, string[]? style = null, string? htmlFor = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // loop: When true, keyboard navigation loops from last item to first, and vice versa.
    // orientation: Orientation used for keyboard navigation.
    // label: Group-level label rendered above the radio group (same field ergonomics as TextField).
    // bind: Two-way binds the group to a Reactive<T> — reads bind.Value and writes it back on every selection. When set, value: is ignored and onValueChange still fires after the write-back.
    static void RadioGroup(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, bool? required = null, bool? disabled = null, bool loop = true, Orientation orientation = Vertical, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<string>? bind = null)
    // forceMount: When true, forces the indicator to render even when the radio is not selected.
    static void RadioGroupIndicator(this UIView view, string[]? style = null, bool? forceMount = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // value: The unique value for this radio item within the group.
    // required: When true, indicates this radio item must be selected before the form can be submitted.
    static void RadioGroupItem(this UIView view, string[]? style = null, string? value = null, bool? disabled = null, bool? required = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // onValueChange: Fires continuously while dragging.
    // onValueCommit: Fires once when dragging ends.
    // content: The default content's thumb carries aria-readonly for a read-only slider (controlled value: with no write-back); custom thumbs should set it too.
    // label: Also the accessible name of the thumbs, where role="slider" lives — a name on the root names nothing; multi-thumb thumbs are numbered from it.
    // bind: Two-way binds a single-thumb slider to a Reactive<T>, writing back as the user drags; value: is ignored and onValueChange still fires. Multi-thumb ranges use the value: list form.
    static void Slider(this UIView view, string[]? style = null, IReadOnlyList<double>? value = null, IReadOnlyList<double>? defaultValue = null, double? min = null, double? max = null, double? step = null, int? minStepsBetweenThumbs = null, Orientation orientation = Horizontal, bool? disabled = null, bool? inverted = null, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<IReadOnlyList<double>, Task>? onValueChange = null, Func<IReadOnlyList<double>, Task>? onValueCommit = null, Action<UIView>? content = null, string? label = null, Reactive<double>? bind = null, string? ariaLabel = null)
    static void Slider(this UIView view, double value, string[]? style = null, double? min = null, double? max = null, double? step = null, Orientation orientation = Horizontal, bool? disabled = null, bool? inverted = null, string? name = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<double, Task>? onValueChange = null, Func<double, Task>? onValueCommit = null, Action<UIView>? content = null, string? label = null)
    static void SliderRange(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void SliderThumb(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void SliderTrack(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // formValue: HTML form value submitted when checked.
    // label: Trailing text label wrapped with the switch in a <label> — clicking the text toggles it and the text becomes the switch's accessible name; without this or ariaLabel it is announced as an unlabelled control.
    // bind: Two-way binds the switch to a Reactive<T> — reads bind.Value and writes it back on every toggle. When set, value: is ignored and onValueChange still fires after the write-back.
    static void Switch(this UIView view, string[]? style = null, bool? value = null, bool? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onValueChange = null, Action<UIView>? content = null, string? label = null, Reactive<bool>? bind = null, string? ariaLabel = null)
    static void SwitchThumb(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // formValue: HTML form value submitted when checked.
    static void TriStateCheckbox(this UIView view, string[]? style = null, CheckedState? value = null, CheckedState? defaultValue = null, bool? required = null, bool? disabled = null, string? name = null, string? formValue = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<CheckedState, Task>? onValueChange = null, Action<UIView>? content = null, string? ariaLabel = null)
  sealed record FormFieldError
    // Field: The name of the field, as passed to FormStateExtensions.FormField<T>.
    // Message: Human-readable text rendered under the field.
    ctor(string Field, string Message)
    string Field { get; init; }
    string Message { get; init; }
  enum FormMessageMatch
    ValueMissing
    TypeMismatch
    TooShort
    TooLong
    PatternMismatch
    RangeUnderflow
    RangeOverflow
    StepMismatch
    BadInput
    CustomError
  // Declare one as an app field per form (new FormState<PresetDraft>(() => new())); the reactives take their identity from the declaring field, not from this class. All state is client-scoped, so each client edits its own draft, and members must be called where a client scope is active (UI render or event handlers). Show resets busy, dirty and errors; SubmitAsync runs validation, then the handler under the busy flag, and closes on success.
  sealed class FormState<T>
    // newDraft: Produces the draft used by Show when no draft is supplied.
    ctor(Func<T> newDraft)
    T Draft { get; }
    string? Error { get; }
    bool HasErrors { get; }
    bool IsBusy { get; }
    bool IsDirty { get; }
    bool IsOpen { get; }
    void ClearErrors()
    void Close()
    // The mutator runs under the reactive's per-client lock, so concurrent field handlers cannot lose each other's edits. With a record draft, form.Edit(d => d with { Name = value }).
    void Edit(Func<T, T> mutate)
    string? ErrorFor(string field)
    void Fail(string message)
    void Fail(string field, string message)
    void Show()
    void Show(T draft)
    // Returns false without calling the handler while a submit is already in flight or when the validator returns any error. A FormException from the handler becomes a field or form error; any other exception becomes a form-level error carrying its message and is logged at warning. The form closes only when the handler completes and closeOnSuccess is true.
    // validate: Returns the field errors for the draft; an empty sequence means valid.
    Task<bool> SubmitAsync(Func<T, Task> onSubmit, Func<T, IEnumerable<FormFieldError>>? validate = null, bool closeOnSuccess = true)
  static class FormStateExtensions
    // contentStyle: Merged on top of Theming.Dialog.Content.
    static void FormDialog<T>(this UIView view, FormState<T> form, Action<UIView> content, string? title = null, string? description = null, string[]? contentStyle = null, string[]? overlayStyle = null, string? key = null)
    static void FormError<T>(this UIView view, FormState<T> form, string[]? style = null)
    // name: Field name; matches FormFieldError.Field and FormException.Field.
    static void FormField<T>(this UIView view, FormState<T> form, string name, Action<UIView> content, string[]? style = null, string? key = null)
    // validate: Returns the field errors for the draft; an empty sequence means valid.
    static void FormSubmit<T>(this UIView view, FormState<T> form, string text, Func<T, Task> onSubmit, Func<T, IEnumerable<FormFieldError>>? validate = null, bool closeOnSuccess = true, string[]? style = null, string? key = null)
  enum HourFormat
    Hour24
    Hour12
  // The size: form of the Theming.Icon.Xs..Xl tokens. The style-array form (view.Icon([Icon.Lg], ...)) stays valid and, being a caller class, wins over size: when both are given.
  enum IconSize
    Xs
    Sm
    Md
    Lg
    Xl
    Xl2
    Xl3
  sealed record ImageCaptureActionEvent : ActionEvent
    ctor(bool Success, string? Mime, int Width, int Height, string? Data)
    string? Data { get; init; }
    int Height { get; init; }
    string? Mime { get; init; }
    int Width { get; init; }
  static class ImageExtensions
    static void Avatar(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // delayMs: Delay in milliseconds before showing the fallback.
    static void AvatarFallback(this UIView view, string[]? style = null, int? delayMs = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // src: URL of the image to display.
    // alt: Alternative text description for accessibility.
    // onLoadingStatusChange: Invoked when the image loading status changes.
    static void AvatarImage(this UIView view, string[]? style = null, string? src = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<Task>? onLoadingStatusChange = null)
    // src: URL or path to the image source.
    // assetUri: Asset URI to resolve the image source from. Takes precedence over src.
    // alt: Alternative text description for accessibility.
    // onClick: Invoked when the user clicks the image. Accepts sync (() => …) and async (async () => …) lambdas alike.
    static void Image(this UIView view, string[]? style = null, string? src = null, AssetUri? assetUri = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null)
    // data: Binary image data.
    // mimeType: MIME type of the image (e.g., "image/png", "image/jpeg").
    // alt: Alternative text description for accessibility.
    // onClick: Invoked when the user clicks the image. Accepts sync (() => …) and async (async () => …) lambdas alike.
    static void Image(this UIView view, string[]? style = null, byte[]? data = null, string? mimeType = null, string? alt = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Delegate? onClick = null)
  static class InputExtensions
    // value: A controlled value with no onValueChange renders the field read-only.
    // autoSubmit: When true, onAutoSubmit fires once all characters are entered.
    static void OtpField(this UIView view, string[]? style = null, string? value = null, int? maxLength = null, bool autoSubmit = false, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<Task>? onAutoSubmit = null, Action<UIView>? content = null, string? label = null)
    // index: Zero-based index of this slot in the OTP field.
    static void OtpFieldInput(this UIView view, string[]? style = null, int index = 0, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void PasswordToggleField(this UIView view, string[]? style = null, bool? visible = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<bool, Task>? onVisibilityChange = null, Action<UIView>? content = null, string? label = null)
    static void PasswordToggleFieldIcon(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? visibleIcon = null, Action<UIView>? hiddenIcon = null)
    // autoComplete: Browser autocomplete hint (e.g., "current-password", "new-password").
    static void PasswordToggleFieldInput(this UIView view, string[]? style = null, string? autoComplete = null, string? placeholder = null, bool? disabled = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null)
    static void PasswordToggleFieldToggle(this UIView view, string[]? style = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Action<UIView>? content = null)
    // autoResize: When true, grows to fit content up to maxRows (default 6), then scrolls.
    // submitOnEnter: When true, Enter submits and Shift+Enter inserts a newline; default false (Ctrl/Cmd+Enter submits).
    // onSubmit: Receives the submitted value; prefer it over re-reading the bound reactive, which may lag (onValueChange is a separate round-trip).
    // clearOnSubmit: Defaults to true when onSubmit/onSubmitWithContext is set.
    // debounceMs: Throttles onValueChange round-trips (ms).
    // bind: Two-way binds a Reactive<T>, writing back on every keystroke; value: is ignored and onValueChange still fires after the write-back.
    static void TextArea(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, int? rows = null, bool? autoResize = null, int? maxRows = null, bool? submitOnEnter = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, Func<Context, Task>? onSubmitWithContext = null, bool? clearOnSubmit = null, Action<UIView>? content = null, bool? autoFocus = null, string? label = null, int? debounceMs = null, Reactive<string>? bind = null, string? ariaLabel = null)
    // A controlled value: with no write-back handler (bind:, onValueChange:, or onSubmit:) is read-only — the rule every input component shares.
    // onSubmit: Receives the submitted value on Enter; prefer it over re-reading the bound reactive, which may lag (onValueChange is a separate round-trip).
    // clearOnSubmit: Defaults to true only when onSubmit is set; without one Enter does not empty a bound field.
    // debounceMs: Throttles onValueChange round-trips (ms).
    // bind: Two-way binds a Reactive<T>, writing back on every keystroke; value: is ignored and onValueChange still fires.
    // multiline: Delegates to TextArea (MUI-style spelling); rows: alone implies it.
    static void TextField(this UIView view, string[]? style = null, string? value = null, string? defaultValue = null, string? placeholder = null, bool? disabled = null, string? type = null, string? step = null, string? min = null, string? max = null, string? styleId = null, string? key = null, IReadOnlyDictionary<string, object>? props = null, Func<string, Task>? onValueChange = null, Func<string, Task>? onSubmit = null, bool? clearOnSubmit = null, Action<UIView>? content = null, bool? autoFocus = null, string? label = null, int? debounceMs = null, Reactive<string>? bind = null, bool? multiline = null, int? rows = null, string? ariaLabel = null)
  sealed record InteractOutsideArgs
    ctor(string? TargetId)
    string? TargetId { get; init; }
