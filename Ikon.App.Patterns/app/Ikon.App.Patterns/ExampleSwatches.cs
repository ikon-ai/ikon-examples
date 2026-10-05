namespace Ikon.App.Patterns;

// For the examples whose region is a list of Crosswind classes rather than a view: each class
// string is drawn under its own label on a sample box, so the gallery shows what the classes do
// (a motion track animates, a colour paints) and a person can read which string did it. A class
// that hides its element at some breakpoint hides only the sample, never the label.
public static class ExampleSwatches
{
    public static void Render(IView view, IReadOnlyList<string> classes)
    {
        view.Column(["gap-3 mt-2"], content: list =>
        {
            foreach (var classString in classes)
            {
                list.Column(["gap-1"], key: classString, content: swatch =>
                {
                    swatch.Text(["text-xs font-mono text-muted-foreground break-all"], classString);
                    swatch.Box(["rounded-md border border-secondary p-3 text-sm", classString], content: sample =>
                        sample.Text(text: "The quick brown fox jumps over the lazy dog"));
                });
            }
        });
    }
}
