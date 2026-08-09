<!-- mined-from: Examples.Emergence -->
# Multi-line Text Input — TextArea Bound to State

A multi-line text box the user types into: a description, notes, a prompt, a message body. Use `view.TextArea` (NOT `view.TextField`, which is single-line). It merges its own **`Textarea.Default`** base token automatically, so a bare `style:` array of your own utilities is all you need.

## When to use

Anywhere the input is more than one line: a prompt box, a notes/description field, a message composer, pasted content for an LLM to process. For a one-line field (name, search, title) use `view.TextField([Input.Default, …])` instead.

## Snippet

```csharp
private readonly Reactive<string> _notes = new("");

// Two-way bound (recommended for a field the user edits):
view.TextArea(
    [Textarea.Default, "w-full min-h-[120px] font-mono text-sm"],  // base token FIRST, then your sizing/overrides
    bind: _notes,
    label: "Notes",
    placeholder: "Type your notes…");

// Value + onValueChange form (when you don't have a Reactive to bind):
view.TextArea(
    [Textarea.Default, "w-full min-h-[120px]"],
    value: _notes.Value,
    placeholder: "Describe the task…",
    onValueChange: async v => _notes.Value = v ?? "");

// Or omit style: entirely to accept the themed default at its default height:
view.TextArea(bind: _notes, label: "Notes", placeholder: "Type your notes…");
```

## Notes

- **`view.TextArea` → `Textarea.Default`; `view.TextField` → `Input.Default`.** They are different tokens: `Input.Default` is single-line (`h-10`), `Textarea.Default` is multi-line (`min-h-[80px]`, `py-2`). Handing a TextArea an `Input.*` token is now harmless — it is ignored and the textarea base merges anyway — but name the right one.
- **A `style:` array MERGES on top of the component's default token.** Pass just your own utilities (`w-full`, `min-h-*`, `font-mono`); the themed base stays underneath and your classes win where they overlap. Omitting `style:` entirely is fine too.
- For editable fields prefer `bind:` (a `Reactive<string>`) over `value:` + `onValueChange:`; add a `label:` and `placeholder:` so the field is findable by screen readers and the app validator. A `value:` with no `bind:`/`onValueChange:` renders a read-only field on purpose — do not use that shape for input.

## See also

- `ai-prefill-form-from-description` — feed a bound textarea's prose into `Emerge.Run<T>`.
