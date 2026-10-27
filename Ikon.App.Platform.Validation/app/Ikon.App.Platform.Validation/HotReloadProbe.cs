/// <summary>
/// The values the hot reload battery (validation/validate_hot_reload.py) rewrites in the running app.
/// It edits this file in place and restores it afterwards, so it must stay this shape: one constant per line.
/// </summary>
public static class HotReloadProbe
{
    public const string Build = "original";

    // Read by ValidationTheme, so a reload that changes it replaces the theme every client holds
    public const string Radius = "0.25rem";

    // A class no other screen uses, so the reload has to send each connected client a style it never had
    public const string Accent = "bg-success-500";
}
