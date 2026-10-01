using Ikon.App.Platform.Validation.Protocol;
using Ikon.Teleport;
using ConnectionState = Ikon.Sdk.ConnectionState;
using ConnectionStateExtensions = Ikon.Sdk.ConnectionStateExtensions;
using CoreJson = Ikon.Common.Core.Json;

public partial class Validation
{
    public sealed class SelfTestJsonDoc
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    public sealed class SelfTestTomlDoc
    {
        public string Name { get; set; } = "";
        public long Count { get; set; }
        public bool Enabled { get; set; }
    }

    private async Task<string> SelfTestReactiveListAsync()
    {
        var list = new ReactiveList<int>([5, 3, 9, 3]);
        var before = list.Version;

        list.Mutate(items =>
        {
            items.Sort();
            items.Add(12);
        });

        Expect(list.Version == before + 1, $"Mutate fired {list.Version - before} notifications instead of one");
        Expect(list.Peek.SequenceEqual([3, 3, 5, 9, 12]), $"Mutate left [{string.Join(",", list.Peek)}]");
        Expect(list.Find(item => item > 4) == 5, "Find did not return the first match");
        Expect(list.Exists(item => item == 12) && !list.Exists(item => item == 4), "Exists answered wrongly");
        Expect(list.FindAll(item => item == 3).Count == 2, "FindAll did not return both matches");
        Expect(list.FindIndex(item => item == 9) == 3 && list.FindIndex(item => item == 99) == -1, "FindIndex answered wrongly");
        Expect(list.IndexOf(5) == 2 && list.IndexOf(42) == -1, "IndexOf answered wrongly");

        IReadOnlyList<string> names = ["ada", "grace", "ada", "linus"];
        Expect(names.FindIndex(name => name == "ada") == 0, "IReadOnlyList.FindIndex did not find the first match");
        Expect(names.FindLastIndex(name => name == "ada") == 2, "IReadOnlyList.FindLastIndex did not find the last match");
        Expect(names.IndexOf("linus") == 3 && names.IndexOf("nobody") == -1, "IReadOnlyList.IndexOf answered wrongly");

        var removed = list.RemoveAll(item => item == 3);
        Expect(removed == 2 && list.Count == 3, $"RemoveAll removed {removed}, {list.Count} left");

        return "Mutate sorted+appended with one notification; Find/Exists/FindAll/FindIndex/IndexOf and the IReadOnlyList FindIndex/FindLastIndex/IndexOf extensions agree";
    }

    private async Task<string> SelfTestReactiveHashSetAsync()
    {
        var set = new ReactiveHashSet<string>(["Alpha"], StringComparer.OrdinalIgnoreCase);
        var before = set.Version;

        set.Mutate(items =>
        {
            items.Add("beta");
            items.Add("ALPHA");
            items.Remove("missing");
        });

        Expect(set.Version == before + 1, $"Mutate fired {set.Version - before} notifications instead of one");
        Expect(set.Count == 2, $"the case-insensitive comparer was lost: {set.Count} members after adding ALPHA to Alpha");
        Expect(set.Contains("BETA") && set.Contains("alpha"), "Contains ignored the comparer after a copy-on-write mutation");
        Expect(!set.Add("Beta") && set.Remove("beta") && set.Count == 1, "Add/Remove after Mutate answered wrongly");

        return "Mutate applied three edits with one notification and kept the OrdinalIgnoreCase comparer across copy-on-write";
    }

    private async Task<string> SelfTestReactiveScopeAsync()
    {
        var perUser = new UserReactive<string>("unset");
        perUser.SetFor("selftest-alice", "A");
        perUser.SetFor("selftest-bob", "B");

        using (ReactiveScope.Use(new UserScope("selftest-alice")))
        {
            Expect(perUser.Value == "A" && ReactiveScope.UserId == "selftest-alice", "Use(UserScope) did not resolve alice's value");

            using (ReactiveScope.UseNested(new UserScope("selftest-bob")))
            {
                Expect(perUser.Value == "B" && ReactiveScope.UserId == "selftest-bob", "UseNested did not switch to bob");
                perUser.Value = "B2";
            }

            Expect(ReactiveScope.UserIdOrNull == "selftest-alice", $"UseNested restored {ReactiveScope.UserIdOrNull ?? "nothing"} instead of alice");
            Expect(perUser.Value == "A", "alice's value changed while bob was nested");
        }

        Expect(perUser.ValueFor("selftest-bob") == "B2", "a write inside UseNested did not land on bob");
        Expect(perUser.ValueFor("selftest-carol") == "unset", "a user never written did not read the initial value");

        return "UseNested switched alice to bob and restored alice on dispose; the nested write landed on bob only";
    }

    private async Task<string> SelfTestReadOnlyReactiveAsync()
    {
        var source = new Reactive<int>(1);
        IReadOnlyReactive<int> view = source;
        var observed = new List<int>();
        Action<int> onChanged = value =>
        {
            lock (observed)
            {
                observed.Add(value);
            }
        };

        view.ValueChanged += onChanged;
        source.Value = 2;
        source.Update(value => value + 40);
        view.ValueChanged -= onChanged;
        source.Value = 7;

        Expect(view.Peek == 7 && view.Value == 7, $"the read-only view reads {view.Peek} after the owner wrote 7");
        Expect(await WaitUntilAsync(() => { lock (observed) { return observed.Count >= 2; } }, TimeSpan.FromSeconds(2)), "ValueChanged did not fire through the read-only view");

        lock (observed)
        {
            Expect(observed.SequenceEqual([2, 42]), $"ValueChanged saw [{string.Join(",", observed)}] instead of [2,42] (and nothing after unsubscribing)");
        }

        return "IReadOnlyReactive<int> saw Value/Peek follow the owner and ValueChanged fire 2 then 42, silent after unsubscribe";
    }

    private async Task<string> SelfTestReactiveEffectAsync()
    {
        var dep = new Reactive<int>(0);
        var runs = 0;
        var lastSeen = -1;

        var effect = new ReactiveEffect(() =>
        {
            Volatile.Write(ref lastSeen, dep.Peek);
            Interlocked.Increment(ref runs);
        }, dep);

        try
        {
            Expect(await WaitUntilAsync(() => Volatile.Read(ref runs) >= 1, TimeSpan.FromSeconds(2)), "the effect did not run at construction");

            dep.Value = 5;
            Expect(await WaitUntilAsync(() => Volatile.Read(ref lastSeen) == 5, TimeSpan.FromSeconds(2)), $"the effect did not re-run on a dep change (runs={runs})");
        }
        finally
        {
            effect.Dispose();
        }

        var runsAtDispose = Volatile.Read(ref runs);
        dep.Value = 9;
        await Task.Delay(100);
        Expect(Volatile.Read(ref runs) == runsAtDispose, "the effect ran after Dispose");

        return $"ReactiveEffect ran at construction, re-ran on change (saw 5), and stayed silent after Dispose ({runsAtDispose} runs)";
    }

    private async Task<string> SelfTestUserReactiveListSeedAsync()
    {
        var perUser = new UserReactiveList<string>(userId => [$"seed-{userId}"]);

        using (ReactiveScope.Use(new UserScope("selftest-dora")))
        {
            Expect(perUser.Value.SequenceEqual(["seed-selftest-dora"]), $"the in-scope seed was [{string.Join(",", perUser.Value)}]");
            perUser.Add("added");
        }

        Expect(perUser.ValueFor("selftest-dora").SequenceEqual(["seed-selftest-dora", "added"]), "ValueFor did not see the in-scope add");
        Expect(perUser.ValueFor("selftest-eli").SequenceEqual(["seed-selftest-eli"]), $"ValueFor seeded eli with [{string.Join(",", perUser.ValueFor("selftest-eli"))}] outside any user scope");

        perUser.AddFor("selftest-eli", "x");
        Expect(perUser.ValueFor("selftest-eli").Count == 2 && perUser.ValueFor("selftest-dora").Count == 2, "AddFor leaked across users");

        return "the factory seeded each user from their own id, inside a scope and through ValueFor; AddFor stayed per user";
    }

    private async Task<string> SelfTestRetrierAsync()
    {
        var attempts = 0;
        var retries = 0;
        var failures = 0;

        ExpectThrows<IOException>(() => Retrier.Run(
            () =>
            {
                attempts++;
                throw new IOException("transient");
            },
            retries: 3,
            onRetry: _ => retries++,
            onFailure: _ => failures++,
            useExponentialBackoff: false,
            description: SelfTestDeliberate), "Retrier.Run on an always-failing call");
        Expect(attempts == 4 && retries == 3 && failures == 1, $"retries=3 made {attempts} attempts, {retries} onRetry, {failures} onFailure (expected 4/3/1)");

        var nonTransient = 0;
        ExpectThrows<InvalidOperationException>(() => Retrier.Run(() =>
        {
            nonTransient++;
            throw new InvalidOperationException("bug");
        }, retries: 3, useExponentialBackoff: false, description: SelfTestDeliberate), "Retrier.Run on a non-transient failure");
        Expect(nonTransient == 1, $"a non-transient exception was retried ({nonTransient} attempts)");

        var capped = 0;
        ExpectThrows<TimeoutException>(() => Retrier.Run(() =>
        {
            capped++;
            throw new TimeoutException("slow");
        }, retries: 5, useExponentialBackoff: false, maxRetries: _ => 1, description: SelfTestDeliberate), "Retrier.Run with maxRetries 1");
        Expect(capped == 2, $"maxRetries=1 made {capped} attempts instead of 2");

        var asyncAttempts = 0;
        var value = await Retrier.RunAsync(async () =>
        {
            asyncAttempts++;
            await Task.Yield();

            if (asyncAttempts < 3)
            {
                throw new HttpRequestException("flaky");
            }

            return 42;
        }, retries: 5, useExponentialBackoff: false, description: SelfTestDeliberate);
        Expect(value == 42 && asyncAttempts == 3, $"RunAsync returned {value} after {asyncAttempts} attempts");

        var custom = 0;
        ExpectThrows<FormatException>(() => Retrier.Run(() =>
        {
            custom++;
            throw new FormatException("custom");
        }, retryableExceptions: [typeof(FormatException)], retries: 2, useExponentialBackoff: false, description: SelfTestDeliberate), "Retrier.Run with a custom filter");
        Expect(custom == 3, $"a custom retryable filter made {custom} attempts instead of 3");

        return "retries=3 → 4 attempts/3 onRetry/1 onFailure; non-transient not retried; maxRetries caps; RunAsync recovers on attempt 3; custom filter honoured";
    }

    private async Task<string> SelfTestMimeTypesAsync()
    {
        Expect(MimeTypes.ImageAvif == "image/avif", $"ImageAvif is {MimeTypes.ImageAvif}");
        Expect(MimeTypes.IsImage(MimeTypes.ImageAvif) && MimeTypes.Is(MimeTypes.ImageAvif, "IMAGE/AVIF") && MimeTypes.IsBinary(MimeTypes.ImageAvif), "ImageAvif is not classified as a binary image");
        Expect(MimeTypes.GetMimeTypeFromFilename("PHOTO.PNG") == MimeTypes.ImagePng, "an upper-case .PNG did not resolve to image/png");
        Expect(MimeTypes.GetMimeTypeFromExtension(".webp") == MimeTypes.ImageWebp, "a dotted .webp did not resolve");
        Expect(MimeTypes.GetMimeTypeFromExtension("validation-unknown") == MimeTypes.DefaultMimeType, "an unknown extension did not fall back to the default");

        MimeTypes.AddOrUpdate(".VSelfTest", "application/x-validation-selftest");
        Expect(MimeTypes.GetMimeTypeFromExtension("vselftest") == "application/x-validation-selftest", "AddOrUpdate did not normalize the extension");
        Expect(MimeTypes.GetExtensionFromMimeType("application/x-validation-selftest") == "vselftest", "GetExtensionFromMimeType did not find the registered extension");

        Expect(MimeTypes.GetMimeTypeFromFilename("photo.avif") == MimeTypes.ImageAvif, "a .avif file did not resolve to image/avif");

        return "ImageAvif classified as image and resolved from .avif; case/dot normalization, default fallback and AddOrUpdate round trip hold";
    }

    private async Task<string> SelfTestStringDistanceAsync()
    {
        Expect(StringDistance.Levenshtein("kitten", "sitting") == 3, "kitten→sitting is not 3");
        Expect(StringDistance.Levenshtein("flaw", "lawn") == 2, "flaw→lawn is not 2");
        Expect(StringDistance.Levenshtein("", "abc") == 3 && StringDistance.Levenshtein(null, "abcd") == 4 && StringDistance.Levenshtein("abc", null) == 3, "empty/null inputs did not return the other side's length");
        Expect(StringDistance.Levenshtein("same", "same") == 0, "identical strings are not 0 apart");

        return "kitten→sitting 3, flaw→lawn 2, empty/null → other length, identical → 0";
    }

    private async Task<string> SelfTestJson5Async()
    {
        const string json5 = """
            {
              // a comment strict JSON rejects
              Name: 'validation',
              Count: 3,
            }
            """;

        var parsed = CoreJson.From<SelfTestJsonDoc>(json5, new JsonOptions(useJson5: true));
        Expect(parsed.Name == "validation" && parsed.Count == 3, $"JSON5 parsed to Name={parsed.Name} Count={parsed.Count}");

        ExpectThrows<Exception>(() => CoreJson.From<SelfTestJsonDoc>(json5), "strict JSON on a JSON5 payload");
        ExpectThrows<Exception>(() => CoreJson.From<SelfTestJsonDoc>("   "), "an empty payload");

        var fenced = CoreJson.FromLLMResponse<SelfTestJsonDoc>("Here you go:\n```json\n{\"Name\":\"fenced\",\"Count\":7}\n```");
        Expect(fenced.Name == "fenced" && fenced.Count == 7, "FromLLMResponse did not unwrap a fenced payload");

        var camel = CoreJson.To(new SelfTestJsonDoc { Name = "x", Count = 1 }, new JsonOptions(camelCase: true, indentation: false));
        Expect(camel.Contains("\"name\"", StringComparison.Ordinal) && camel.Contains("\"count\"", StringComparison.Ordinal), $"camelCase output was {camel}");

        return "JSON5 (comments, unquoted keys, trailing comma) parsed; strict JSON and empty input refused; fenced LLM reply unwrapped; camelCase honoured";
    }

    private async Task<string> SelfTestTomlAsync()
    {
        var parsed = Toml.From<SelfTestTomlDoc>("Name = \"validation\"\nCount = 3\nEnabled = true\n");
        Expect(parsed is { Name: "validation", Count: 3, Enabled: true }, $"parsed Name={parsed.Name} Count={parsed.Count} Enabled={parsed.Enabled}");

        var text = Toml.To(new SelfTestTomlDoc { Name = "round", Count = 42, Enabled = false });
        var back = Toml.From<SelfTestTomlDoc>(text);
        Expect(back is { Name: "round", Count: 42, Enabled: false }, $"round trip gave Name={back.Name} Count={back.Count} Enabled={back.Enabled}");

        var empty = Toml.From<SelfTestTomlDoc>("");
        Expect(empty.Name == "" && empty.Count == 0, "an empty document did not give a default instance");

        return "parsed a hand-written document, round-tripped To/From, empty input gives defaults";
    }

    private async Task<string> SelfTestNameConversionsAsync()
    {
        Expect(NameConversions.ToSnakeCase("XMLHttpRequest") == "xml_http_request", $"ToSnakeCase gave {NameConversions.ToSnakeCase("XMLHttpRequest")}");
        Expect(NameConversions.ToKebabCase("SelfTestRunner") == "self-test-runner", $"ToKebabCase gave {NameConversions.ToKebabCase("SelfTestRunner")}");
        Expect(NameConversions.ToPascalCase("self-test_runner") == "SelfTestRunner", $"ToPascalCase gave {NameConversions.ToPascalCase("self-test_runner")}");
        Expect(NameConversions.ToCamelCase("XMLHttp") == "xmlHttp", $"ToCamelCase gave {NameConversions.ToCamelCase("XMLHttp")}");
        Expect(NameConversions.ToDisplayName("SelfTest2Run") == "Self Test 2 Run", $"ToDisplayName gave {NameConversions.ToDisplayName("SelfTest2Run")}");
        Expect(NameConversions.ToSlug("  Kävely Reitit!  ", 50) == "kävely-reitit", $"ToSlug gave {NameConversions.ToSlug("  Kävely Reitit!  ", 50)}");
        Expect(NameConversions.ToSlug("abc def ghi", 4) == "abc", $"a truncated slug gave {NameConversions.ToSlug("abc def ghi", 4)}");

        return "snake/kebab/pascal/camel/display conversions and a non-ASCII, truncated slug all match";
    }

    private async Task<string> SelfTestExtendedCastAsync()
    {
        Expect(ExtendedCast.Convert<int>(null) == 0 && ExtendedCast.Convert<bool>(null) == false, "null did not become the value type's default");
        Expect(ExtendedCast.Convert<int?>(null) == null && ExtendedCast.Convert<string>(null) == null, "null did not stay null for a nullable target");

        using var document = JsonDocument.Parse("[1,2,3]");
        var array = ExtendedCast.Convert<int[]>(document.RootElement);
        Expect(array is [1, 2, 3], $"a JSON array converted to [{string.Join(",", array ?? [])}]");

        var widened = ((object)42).ExtendedCast<long>();
        Expect(widened == 42L, $"int→long gave {widened}");

        return "null → default/null by target, JSON array → int[], int → long via the extension";
    }

    private async Task<string> SelfTestTeleportMessageAsync()
    {
        var ping = new ProbePing { Seq = 42, SentAtMs = 1_700_000_000_123, Origin = "selftest", Mode = "roundtrip", Note = "äö €" };

        foreach (var compress in new[] { false, true })
        {
            var message = ProtocolMessage.Create(7, ping, trackId: 3, sequenceId: 11, targetIds: [5, 9], compress: compress);
            var decoded = ProtocolMessage.CopyFrom(message.Data.Span);

            Expect(decoded.Opcode == message.Opcode && decoded.SenderId == 7 && decoded.TrackId == 3 && decoded.SequenceId == 11, $"the header did not round-trip (compress={compress})");
            Expect(decoded.TargetIds.SequenceEqual([5, 9]), $"target ids came back as [{string.Join(",", decoded.TargetIds)}] (compress={compress})");

            var payload = decoded.GetPayload<ProbePing>();
            Expect(payload.Seq == 42 && payload.SentAtMs == ping.SentAtMs && payload.Origin == "selftest" && payload.Mode == "roundtrip" && payload.Note == "äö €", $"the ProbePing payload did not round-trip (compress={compress})");
        }

        return "an app-local .tp message (ProbePing) round-tripped header, targets and UTF-8 payload, plain and compressed";
    }

    private async Task<string> SelfTestPipelineStatusAsync()
    {
        var status = new PipelineStatus
        {
            InputItemCount = 10,
            InputItemCacheHits = 4,
            ProcessedItemCount = 8,
            ProcessedItemCacheHits = 3,
            OutputItemCount = 6,
            OutputItemCacheHits = 1,
            InputFailureCount = 1,
            ProcessFailureCount = 2,
            OutputFailureCount = 3,
        };

        Expect(status.TotalFailureCount == 6, $"TotalFailureCount was {status.TotalFailureCount} for 1+2+3");
        Expect(status.InputItemCacheMiss == 6 && status.ProcessedItemCacheMiss == 5 && status.OutputItemCacheMiss == 5, $"cache misses were {status.InputItemCacheMiss}/{status.ProcessedItemCacheMiss}/{status.OutputItemCacheMiss}");

        return "TotalFailureCount sums input/process/output failures and the cache-miss counters derive from counts minus hits (a real pipeline run needs its own runtime, so it is not started here)";
    }

    private async Task<string> SelfTestConnectionStateAsync()
    {
        var faulted = Enum.GetValues<ConnectionState>().Where(state => ConnectionStateExtensions.IsFaulted(state)).ToList();
        var disconnected = Enum.GetValues<ConnectionState>().Where(state => ConnectionStateExtensions.IsDisconnected(state)).ToList();

        Expect(faulted.SequenceEqual([ConnectionState.Offline]), $"IsFaulted is true for [{string.Join(",", faulted)}]");
        Expect(disconnected.Count == 2 && disconnected.Contains(ConnectionState.Idle) && disconnected.Contains(ConnectionState.Offline), $"IsDisconnected is true for [{string.Join(",", disconnected)}]");
        Expect(ConnectionStateExtensions.IsConnected(ConnectionState.Connected) && !ConnectionStateExtensions.IsConnected(ConnectionState.Reconnecting), "IsConnected answered wrongly");
        Expect(ConnectionStateExtensions.IsConnecting(ConnectionState.Connecting), "IsConnecting is false for Connecting");

        return "IsFaulted only Offline; IsDisconnected Idle+Offline; IsConnected/IsConnecting match";
    }
}
