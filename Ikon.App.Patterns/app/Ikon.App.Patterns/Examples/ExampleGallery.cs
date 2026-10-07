using Ikon.App.Patterns.Examples;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Ikon.App.Patterns;

// Every example that draws something, as a gallery demo per example class, so the render
// smoke-test runs it and a person can look at it. Found by reflection rather than listed: an
// example added under Examples/ is covered without anyone remembering to register it.
//
// The examples are built against a stand-in app, never the running one. Several declare a UI of
// their own or bind services in field initializers, as a reader's app would, and those must not
// attach to the gallery's app.
internal static class ExampleGallery
{
    // Keys the label drawn above each example method, so the smoke test can judge each method's
    // output on its own.
    internal const string MethodLabelKeyPrefix = "example-method:";

    // Examples that cannot render standalone, by class or by `Class.Method`, with the reason. Each
    // still compiles, which is what keeps the guide quoting it honest; it is listed here so skipping
    // it is a decision on record. PatternRenderSmokeTests holds the list to a ceiling, so it cannot
    // grow without someone raising that on purpose.
    internal static readonly IReadOnlyDictionary<string, string> CompileOnly = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["CwThemeActivation"] = "reads the theme controller its app's Main takes from UI.UseTheme, and a view method alone runs no Main",
        ["PxLightDarkSwitchingWithUsetheme"] = "reads the theme controller its app's Main takes from UI.UseTheme, and a view method alone runs no Main",
        ["InboxUi"] = "builds a NotificationInbox, which binds the app's notification service",
        ["PaymentsGuide.Entitlement"] = "renders by the viewer's entitlement, which it asks app.Payments for",
        ["MediaUpload"] = "registers its upload callbacks with the app's file-upload handler, which only a running app's view carries",
        ["CwFullScreenLayoutsWithPadding"] = "declares the app's root with UI.Root, which a view inside the gallery cannot be",
        ["PxOpenAsGuestTheDefault.PxOpenAsGuestTheDefault"] = "declares the app's root with UI.Root, which a view inside the gallery cannot be",
        ["CustomComponent"] = "draws custom nodes (my-component, custom.lua-editor) that this gallery's frontend registers no component for, so each would paint an Unregistered node type placeholder",
        ["PxReactiveCollectionsReactivelistAndReactivedictio"] = "is handler code that mutates reactive collections, not a view: rendered, it would mutate them again on every render",
        ["PxBackgroundWorkTheForMethods"] = "is handler code that starts a background task writing client state, not a view: rendered, it would start another on every render",
        ["PxAppChromeAndSemantictone.PxAppChromeAndSemantictone3"] = "raises a success and an error toast in the same body that mounts the host, so a live render would raise two more on every render",
        ["PxHandBuiltSkeletons.PxHandBuiltSkeletons2"] = "is an empty snapshot-only branch, which draws nothing outside a boot-snapshot capture",
        ["PxPerRouteSnapshotsAndSeo"] = "registers the app's snapshot routes, startup code that draws nothing",
        ["PxHowToUseIt"] = "registers the app's snapshot routes, startup code that draws nothing",
    };

    // The label above each example method's output: what a person reads to know which example they
    // are looking at, and the mark the smoke test splits the demo on.
    internal static void RenderMethodLabel(IView view, string methodName) =>
        view.Text(["text-xs font-mono text-muted-foreground mt-4"], methodName, key: MethodLabelKeyPrefix + methodName);

    public static IEnumerable<IPatternDemo> Create(IApp<SessionIdentity, ClientParameters>? live = null)
    {
        var standIn = new PatternStandInApp(live);

        foreach (var type in ExampleTypes().Where(type => !CompileOnly.ContainsKey(ExampleName(type))))
        {
            var methods = type
                .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(IsViewExample)
                .Where(method => !CompileOnly.ContainsKey($"{ExampleName(type)}.{method.Name}"))
                .OrderBy(method => method.Name, StringComparer.Ordinal)
                .ToList();

            if (methods.Count == 0)
            {
                continue;
            }

            yield return new GuideExampleDemo(type, methods, standIn);
        }
    }

    // Every example class lives under Examples/, in the app's Examples namespace, in its Protocol
    // namespace for a Teleport type, or in the global one for a whole-file example; the agent guide
    // examples are a demo already.
    private static IEnumerable<Type> ExampleTypes() =>
        typeof(ExampleGallery).Assembly.GetTypes()
            .Where(type => type.Namespace is null or "Ikon.App.Patterns.Examples" or "Ikon.App.Patterns.Protocol")
            .Where(type => !type.IsNested && !type.IsGenericTypeDefinition && !type.IsInterface)
            .Where(type => type != typeof(AgentGuideExamples) && type != typeof(PatternsApp) && !typeof(IPatternDemo).IsAssignableFrom(type))
            .OrderBy(ExampleName, StringComparer.Ordinal);

    private static bool IsViewExample(MethodInfo method)
    {
        // A content lambda compiles to a UIView method of its own, and rendered alone it draws a
        // fragment outside the container that gives it meaning (a FormField outside its Form).
        var parameters = method.GetParameters();
        return !method.IsSpecialName
            && !method.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false)
            && !method.Name.StartsWith('<')
            && !method.IsGenericMethodDefinition
            && parameters.Length == 1
            && parameters[0].ParameterType == typeof(UIView);
    }

    // A file-scoped type's metadata name carries a compiler prefix ("<CrosswindStylingAndMotionGuide>F…__Name"),
    // and every holder class ends in "Examples", which says nothing in a list of examples.
    internal static string ExampleName(Type type)
    {
        var name = type.Name;
        var marker = name.LastIndexOf("__", StringComparison.Ordinal);
        name = name.StartsWith('<') && marker >= 0 ? name[(marker + 2)..] : name;
        return name.EndsWith("Examples", StringComparison.Ordinal) && name.Length > "Examples".Length ? name[..^"Examples".Length] : name;
    }

    private sealed class GuideExampleDemo(Type type, IReadOnlyList<MethodInfo> methods, PatternStandInApp standIn) : IPatternDemo
    {
        private object? _instance;

        public string Slug => "example-" + Regex.Replace(ExampleName(type), "(?<=[a-z0-9])([A-Z])", "-$1").ToLowerInvariant();
        public string Title => ExampleName(type);
        public string Category => "Examples";

        // Each example is called here, not inside a content lambda: the renderer contains a throw from
        // a lambda and logs it loudly once per source line, and every example would share this one,
        // so all but the first failure would pass the smoke-test unseen. Called directly, a throw
        // fails its own demo.
        public void RenderDemo(IView view)
        {
            var instance = methods.Any(method => !method.IsStatic) ? _instance ??= Construct() : null;
            BindPlaceholderViews(instance, view);

            foreach (var method in methods)
            {
                RenderMethodLabel(view, method.Name);

                try
                {
                    // An async example has run its rendering part by the time it returns, so a fault
                    // there is already on the task and would otherwise go unobserved.
                    if (method.Invoke(method.IsStatic ? null : instance, [view]) is Task { IsFaulted: true } faulted)
                    {
                        faulted.GetAwaiter().GetResult();
                    }
                }
                catch (TargetInvocationException ex) when (ex.InnerException is { } inner)
                {
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(inner).Throw();
                }
            }
        }

        // A fragment draws into a view the reader's own code has in scope (`row`, `container`), which
        // the example declares as a `null!` UIView field; pointing those at the demo's view renders
        // the fragment instead of throwing on the null.
        private void BindPlaceholderViews(object? instance, UIView view)
        {
            if (instance is null)
            {
                return;
            }

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(field => field.FieldType == typeof(UIView)))
            {
                field.SetValue(instance, view);
            }
        }

        private object Construct()
        {
            var constructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .OrderBy(candidate => candidate.GetParameters().Length)
                .First();

            var arguments = constructor.GetParameters()
                .Select(parameter => parameter.ParameterType.IsInstanceOfType(standIn)
                    ? standIn
                    : throw new InvalidOperationException($"{ExampleName(type)} takes a {parameter.ParameterType.Name}, which the gallery cannot supply; list its examples in ExampleGallery.CompileOnly with the reason"))
                .ToArray<object?>();

            return constructor.Invoke(arguments);
        }
    }
}
