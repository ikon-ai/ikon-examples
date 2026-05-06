<!-- mined-from: Ikon.App.Threads -->
# Playwright UI-Tree Extraction — Append the DOM Snapshot to Tool Results

After every agent action that interacts with a running app preview (build, run, click, screenshot), append a compact pretty-printed UI tree to the tool result. The agent gets ground truth about the actual rendered shape — class names, roles, placeholders — without you wiring custom telemetry.

## When to use

You have a coding agent driving a real running app via Playwright and want it to "see" what's on screen. Plain screenshots aren't enough — the agent needs queryable structure to plan the next click or assertion.

## Snippet

```csharp
private const string PlaywrightUITreeScript = """
    (() => {
        const maxDepth = 6;
        const skipTags = new Set(['SCRIPT', 'STYLE', 'LINK', 'META', 'HEAD', 'NOSCRIPT', 'SVG', 'PATH']);
        function walk(el, depth) {
            if (depth > maxDepth || skipTags.has(el.tagName)) return '';
            const tag = el.tagName.toLowerCase();
            const indent = '  '.repeat(depth);
            const text = Array.from(el.childNodes)
                .filter(n => n.nodeType === 3)
                .map(n => n.textContent.trim())
                .filter(t => t.length > 0).join(' ');
            const attrs = [];
            if (el.id) attrs.push('id=' + el.id);
            const cls = el.className;
            if (typeof cls === 'string' && cls.trim()) {
                const short = cls.trim().length > 60 ? cls.trim().substring(0, 57) + '...' : cls.trim();
                attrs.push('class="' + short + '"');
            }
            if (el.getAttribute('role')) attrs.push('role=' + el.getAttribute('role'));
            if (el.getAttribute('placeholder')) attrs.push('placeholder="' + el.getAttribute('placeholder') + '"');
            if (el.getAttribute('data-source')) attrs.push('source=' + el.getAttribute('data-source'));
            const attrStr = attrs.length > 0 ? ' ' + attrs.join(' ') : '';
            const textStr = text ? ' "' + (text.length > 50 ? text.substring(0, 47) + '...' : text) + '"' : '';
            const children = Array.from(el.children).map(c => walk(c, depth + 1)).filter(s => s.length > 0);
            if (children.length === 0 && !text && !attrStr) return '';
            let line = indent + '<' + tag + attrStr + '>' + textStr;
            if (depth === maxDepth && el.children.length > 0) {
                line += ' ... ' + el.children.length + ' children';
                return line;
            }
            return [line, ...children].join('\n');
        }
        const root = document.getElementById('root') || document.body;
        return walk(root, 0);
    })()
    """;

private async Task<string> GetUITreeSuffixAsync(string url)
{
    var (page, _) = await _appProcessManager.GetTestPageAsync(url);
    var tree = await page.EvaluateAsync<string>(PlaywrightUITreeScript);
    return string.IsNullOrWhiteSpace(tree) ? "" : "\n\nUI Tree:\n" + tree;
}

// Append after every relevant tool result:
return $"App running at {url}" + await GetUITreeSuffixAsync(url);
```

## Notes

- Cap depth (6) and class-name length (60) hard. The whole snapshot must stay under a few KB or it eats your context window.
- Skip noise tags (`SCRIPT`, `STYLE`, `SVG/PATH`). They never carry information the agent needs.
- Surface `data-source` (line number) attributes if your renderer emits them — that's the key the agent uses to find the right .cs line.
- Indent with 2 spaces; HTML-ish tag-and-attrs format reads naturally to LLMs.

## See also

- `chat-with-tool-calls`
- `embedded-coding-agent-tools`
