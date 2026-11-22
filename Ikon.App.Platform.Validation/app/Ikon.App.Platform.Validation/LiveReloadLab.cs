using System.ComponentModel;
using System.Text.RegularExpressions;

/// <summary>
/// The code the live reload rig (validation/hot_reload_rig.py) edits in the running app, one member for each
/// kind of edit. The rig replaces the texts and inserts at the "rig:" comments, and restores the files
/// afterwards, so each text and comment must stay as it is and occur once.
/// </summary>
public static partial class LiveReloadLab
{
    public const string Const = "const-1";
    public const string InFlightConst = "in-flight-const-1";

    // Changed by the rig to make the app restart before its cases, so they run on a reloaded build
    public const string Warmup = "warmup-1";

    // Changed by the rig's many-edits case to restart the app after its edits, and again after none
    public const string ManyRestart = "many-restart-1";

    public static readonly string StaticReadonly = "static-readonly-1";
    public static string AutoProperty { get; } = "auto-1";
    public static string ExpressionProperty => "prop-1";

    private static readonly int[] Numbers = [1, 2, 3, 4];

    // rig:members

    // rig:comment

    /// <summary>Every value the lab tab shows that needs no object of the app, by the name the rig reads it under.</summary>
    public static IEnumerable<(string Name, string Value)> Rows() =>
    [
        ("const", Const),
        ("in-flight-const", InFlightConst),
        ("static-readonly", StaticReadonly),
        ("auto-property", AutoProperty),
        ("expression-property", ExpressionProperty),
        ("helper", Helper()),
        ("pair-first", PairFirst()),
        ("pair-second", PairSecond()),
        ("pair-local", PairLocal()),
        ("pair-closure", PairClosure()),
        ("file-one", FileOne()),
        ("file-two", FileTwo()),
        ("partial", InOtherFile()),
        ("interpolated", Interpolated(7)),
        ("switch", Switch(2)),
        ("pattern", Pattern(2)),
        ("linq", Linq()),
        ("query", Query()),
        ("query-clause", QueryClause()),
        ("capturing", Capturing()),
        ("not-capturing", NotCapturing()),
        ("lambda-added", LambdaAdded()),
        ("lambda-added-static", LambdaAddedStatic()),
        ("lambda-first-static", LabWithoutLambdas.FirstStatic()),
        ("lambda-removed", LambdaRemoved()),
        ("method-group", MethodGroup()),
        ("local-function", LocalFunction()),
        ("iterator", Iterator()),
        ("generic", Generic(5)),
        ("record", Record()),
        ("shape", new LabShape("shape-1").ToString()),
        ("enum", EnumNames()),
        ("attribute", Attributed()),
        ("default", WithDefault()),
        ("parameter", WithParameter()),
        ("return-type", ReturnShown()),
        ("rename", Rename()),
        ("caller", Caller()),
        ("conditional", Conditional()),
        ("unicode", Unicode()),
        ("builder", UsesBuilder()),
        ("broken", Broken()),
        ("unbound", Unbound()),
        ("reverted", Reverted()),
        ("quick", Quick()),
        ("in-flight", InFlight()),
        ("during-live", DuringLive()),
        ("during-live-other", DuringLiveOther()),
        ("many", Many()),
        ("locked", Locked()),
        ("crash", Crash()),
        ("commented", Commented()),
        ("saved-by-rename", SavedByRename()),
        ("saved-aside", SavedAside()),
        ("saved-aside-kept", SavedAsideKept()),
        ("generated", Generated()),
        ("large", Large()),
        ("ctor-new", new LabThing().Text),
        ("field-new", new LabThing().Field),
        ("static-ctor", LabStatic.Text),
    ];

    public static string Helper() => "helper-1";

    public static string PairFirst() => "pair-first-1";

    public static string PairSecond() => "pair-second-1";

    public static string PairLocal()
    {
        string pair = "pair-local";
        return Inner();

        string Inner() => pair + "-1";
    }

    public static string PairClosure()
    {
        string pair = "pair-closure";
        Func<string> text = () => pair + "-1";
        return text();
    }

    public static string FileOne() => "file-one-1";

    public static string Interpolated(int n) => $"interp-1 {n}";

    public static string Switch(int n) => n switch
    {
        1 => "switch-one",
        // rig:switch-arm
        _ => "switch-other",
    };

    public static string Pattern(object value)
    {
        if (value is int and > 1)
        {
            return "pattern-1";
        }

        return "pattern-none";
    }

    public static string Linq() => string.Join(",", Numbers.Where(n => n > 1).Select(n => n * 10));

    public static string Query()
    {
        int limit = 1;
        var query = from n in Numbers where n > limit select n * 10;
        return string.Join(",", query);
    }

    public static string QueryClause()
    {
        int limit = 1;
        var query = from n in Numbers where n > limit /* rig:query-clause */ select n + 1;
        return string.Join(",", query);
    }

    public static string Capturing()
    {
        string prefix = "cap";
        Func<string> text = () => prefix + "-1";
        return text();
    }

    public static string NotCapturing()
    {
        Func<string> text = () => "nocap-1";
        return text();
    }

    public static string LambdaAdded()
    {
        string word = "added";
        Func<string, string> first = text => text + word.Length;
        // rig:lambda-added
        return first(word);
    }

    public static string LambdaAddedStatic()
    {
        string word = "static";
        Func<string, string> first = text => text + word.Length;
        // rig:lambda-added-static
        return first(word);
    }

    public static string LambdaRemoved()
    {
        string word = "removed";
        Func<string, string> first = text => text + word.Length;
        Func<string, string> second = text => text + "+" + word;
        word = second(word);
        return first(word);
    }

    public static string MethodGroup() => "group-1" /* rig:method-group */;

    public static string Shout(string text) => text.ToUpperInvariant();

    public static string LocalFunction()
    {
        string word = "local";
        return Inner() + Inner();

        string Inner() => word + "-1";
    }

    public static async Task<string> AsyncText()
    {
        string before = "async-before-1";
        await Task.Delay(1);
        // rig:async
        return before + "/async-after-1";
    }

    public static string Iterator() => string.Join(",", Items());

    private static IEnumerable<string> Items()
    {
        yield return "iter-1";
        // rig:iterator
        yield return "iter-end";
    }

    public static string Generic<T>(T value) => $"generic-1 {value}";

    public static string Record()
    {
        var record = new LabRecord("record-1", 1);
        return record.ToString();
    }

    public static string EnumNames() => string.Join(",", Enum.GetNames<LabEnum>());

    [DisplayName("attribute-1")]
    public static string Attributed() =>
        typeof(LiveReloadLab).GetMethod(nameof(Attributed))!.GetCustomAttributes(typeof(DisplayNameAttribute), false).Cast<DisplayNameAttribute>().Single().DisplayName;

    public static string WithDefault(string value = "default-1") => value;

    public static string WithParameter() => "parameter-1";

    public static string ReturnType() => "return-1";

    public static string ReturnShown() => ReturnType().ToString() ?? "";

    public static string OldName() => "rename-1";

    public static string Rename() => OldName();

    public static string Caller() => "caller-1";

    public static string Removable() => "removable";

    public static string Conditional()
    {
#if DEBUG
        return "debug-1";
#else
        return "release-1";
#endif
    }

    public static string Unicode() => "unicode-1";

    public static string UsesBuilder() => "builder-1";

    public static string Broken() => "broken-1";

    public static string Unbound() => "unbound-1";

    public static string Reverted() => "reverted-1";

    public static string Quick() => "quick-1";

    public static string InFlight() => "in-flight-1";

    public static string DuringLive() => "during-live-1";

    public static string Many() => "many-0";

    public static string Locked() => "locked-1";

    public static string Crash() => "crash-1";

    public static string Commented()
    {
        string commented = "commented";
        // rig:commented
        Func<string> first = () => commented + "-1";
        Func<string> second = () => "!";
        return first() + second();
    }

    public static string SavedByRename() => "saved-by-rename-1";

    public static string SavedAside() => "saved-aside-1";

    public static string SavedAsideKept() => "saved-aside-kept-1";

    // The pattern is the input of a source generator, which writes the method's body
    [GeneratedRegex("lab-[0-9]+")]
    private static partial Regex LabPattern();

    public static string Generated() => LabPattern().Match("see lab-42") + " generated-1";

    public static string Throwing() => "throwing-1";

    public static string Background() => "background-1";

    public static string Effect(int count) => $"effect-1 at {count}";

    public static string Large()
    {
        int total = 0;
        total += Step("large", 1);
        total += Step("large", 2);
        total += Step("large", 3);
        total += Step("large", 4);
        total += Step("large", 5);
        total += Step("large", 6);
        total += Step("large", 7);
        total += Step("large", 8);
        total += Step("large", 9);
        total += Step("large", 10);
        total += Step("large", 11);
        total += Step("large", 12);
        total += Step("large", 13);
        total += Step("large", 14);
        total += Step("large", 15);
        total += Step("large", 16);
        total += Step("large", 17);
        total += Step("large", 18);
        total += Step("large", 19);
        total += Step("large", 20);
        total += Step("large", 21);
        total += Step("large", 22);
        total += Step("large", 23);
        total += Step("large", 24);
        total += Step("large", 25);
        total += Step("large", 26);
        total += Step("large", 27);
        total += Step("large", 28);
        total += Step("large", 29);
        total += Step("large", 30);
        total += Step("large", 31);
        total += Step("large", 32);
        total += Step("large", 33);
        total += Step("large", 34);
        total += Step("large", 35);
        total += Step("large", 36);
        total += Step("large", 37);
        total += Step("large", 38);
        total += Step("large", 39);
        total += Step("large", 40);
        total += Step("large", 41);
        total += Step("large", 42);
        total += Step("large", 43);
        total += Step("large", 44);
        total += Step("large", 45);
        total += Step("large", 46);
        total += Step("large", 47);
        total += Step("large", 48);
        total += Step("large", 49);
        total += Step("large", 50);
        total += Step("large", 51);
        total += Step("large", 52);
        total += Step("large", 53);
        total += Step("large", 54);
        total += Step("large", 55);
        total += Step("large", 56);
        total += Step("large", 57);
        total += Step("large", 58);
        total += Step("large", 59);
        total += Step("large", 60);
        total += Step("large", 61);
        total += Step("large", 62);
        total += Step("large", 63);
        total += Step("large", 64);
        total += Step("large", 65);
        total += Step("large", 66);
        total += Step("large", 67);
        total += Step("large", 68);
        total += Step("large", 69);
        total += Step("large", 70);
        total += Step("large", 71);
        total += Step("large", 72);
        total += Step("large", 73);
        total += Step("large", 74);
        total += Step("large", 75);
        total += Step("large", 76);
        total += Step("large", 77);
        total += Step("large", 78);
        total += Step("large", 79);
        total += Step("large", 80);
        total += Step("large", 81);
        total += Step("large", 82);
        total += Step("large", 83);
        total += Step("large", 84);
        total += Step("large", 85);
        total += Step("large", 86);
        total += Step("large", 87);
        total += Step("large", 88);
        total += Step("large", 89);
        total += Step("large", 90);
        total += Step("large", 91);
        total += Step("large", 92);
        total += Step("large", 93);
        total += Step("large", 94);
        total += Step("large", 95);
        total += Step("large", 96);
        total += Step("large", 97);
        total += Step("large", 98);
        total += Step("large", 99);
        total += Step("large", 100);
        total += Step("large", 101);
        total += Step("large", 102);
        total += Step("large", 103);
        total += Step("large", 104);
        total += Step("large", 105);
        total += Step("large", 106);
        total += Step("large", 107);
        total += Step("large", 108);
        total += Step("large", 109);
        total += Step("large", 110);
        total += Step("large", 111);
        total += Step("large", 112);
        total += Step("large", 113);
        total += Step("large", 114);
        total += Step("large", 115);
        total += Step("large", 116);
        total += Step("large", 117);
        total += Step("large", 118);
        total += Step("large", 119);
        total += Step("large", 120);
        return $"large-1 {total}";
    }

    private static int Step(string name, int value) => name.Length + value;
}

public sealed class LabThing
{
    public string Field = "field-1";

    public string Text { get; }

    public LabThing()
    {
        Text = "ctor-1";
    }
}

// No member of this class has a lambda that captures nothing, so the compiler has made no class to keep
// such delegates in. The first one the rig adds comes with that class, and the second adds a field to it
public static class LabWithoutLambdas
{
    public static string FirstStatic()
    {
        string label = "first";
        // rig:lambda-first-static
        return label + "-1";
    }
}

public static class LabStatic
{
    public static readonly string Text;

    static LabStatic()
    {
        Text = "static-ctor-1";
    }
}

public sealed class LabBox<T>(T value)
{
    public string Describe() => $"box-1 {value}";
}

// No method reads the name, so a body that starts to read it has to keep it in a new field
#pragma warning disable CS9113
public sealed class LabPrimary(string name, int size)
#pragma warning restore CS9113
{
    public string Describe() => $"primary-1 {size}";
}

public sealed record LabRecord(string Name, int Size);

public sealed record LabShape(string Name);

public enum LabEnum
{
    First,
    Second,
}
