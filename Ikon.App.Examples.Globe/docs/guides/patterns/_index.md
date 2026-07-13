<!-- regenerated when pattern count crosses ten-mark thresholds -->
# Pattern Index

100 focused 30-60 LOC code patterns mined from production Ikon AI Apps. Coder agents call `guide("<name> pattern")` to retrieve any of them. Planner names them in the PATTERNS section of every plan; Coder retrieves and adapts as the structural skeleton. Together they replace prose criteria — the corpus is the criteria.

## Foundations (start here)

- `typical-app-structure` — the skeleton every Ikon AI App starts from.
- `busy-flag-loading` — any async work with visible feedback (Reactive bool + try/catch + label change).

## Conversational AI

- `chatbot-streaming` — single-LLM transcript with busy flag.
- `chat-with-tool-calls` — streaming chat handling LLM tool-call event loop.
- `multi-agent-parallel-discussion` — fan one prompt to N personas via Task.WhenAll.
- `agent-streaming-with-tool-status` — live status of tool-using agent.
- `streaming-agent-status` — live status block driven by an `AgentThread`'s own reactives (Status/Activity/ToolCallTimeline/Usage).
- `quick-reply-options-from-llm` — `<ask>...<option>` pills from LLM output.
- `clickable-reference-card-in-chat` — LLM tool emits clickable detail cards.
- `orchestrator-thread-with-tools` — Orchestrator + Persona + tool-providing Skill.
- `embedded-coding-agent-tools` — agent that writes/edits code as the user.
- `plan-then-code-iteration` — plan → revise → execute agentic loop.

## Voice & audio

- `voice-loop` — mic capture → STT → LLM → TTS playback.
- `push-to-talk-button` — CaptureButton in Hold mode with active styling.

## Image & video

- `image-gallery` — ImageGenerator + grid + remove.
- `before-after-pane-comparison` — symmetric input/output panes for image-to-image.
- `video-wall-grid` — grid of VideoStreamCanvas cells.
- `vrm-avatar-expression-motion` — VRM avatar driven by reactive expressions.
- `annotation-draw-overlay` — drawing layer over an image.
- `llm-vision-cache` — cache vision LLM responses keyed by image hash.

## Web & data

- `web-research` — WebSearcher + multi-stage AI with sources.
- `mapreduce-long-document-summary` — chunk → per-chunk summary → combine.
- `cloud-json-asset-store` — CloudJson asset CRUD pattern.

## Lists & collections

- `hover-reveal-actions` — secondary row actions revealed on hover with `hidden group-hover:flex` (layout-collapsing; `opacity-0` reserves dead height).
- `shared-list-ai-cleanup` — collection + AI transform button.
- `kanban-multi-column` — multi-column board with Move buttons.
- `dynamic-input-list` — variable-length input list with add/remove.
- `sortable-list-header` — clickable column headers driving sort.
- `inline-list-cell-edit` — type-to-save TextField inside a list row.
- `expandable-detail-card` — header-button + HashSet-toggle multi-open accordion.
- `tree-with-expand-collapse` — recursive tree with expanded-set state.
- `search-filter-grouped-results` — single search + sectioned grouped results.
- `filter-button-group` — pill filter group with ClientReactive.
- `heuristic-classify-and-tag` — static rules tag fetched records.

## Forms & input

- `multi-step-wizard` — wizard with sidebar + footer + step switch.
- `wizard-sidebar-stepper` — numbered-circle stepper visual.
- `ai-prefill-form-from-description` — LLM extracts structured fields from prose.
- `multiline-text-input` — multi-line `view.TextArea` bound to state, with the correct `Textarea.Default` token.
- `command-verb-input-with-hints` — verb parser + verb-prefill chip row.
- `slider-with-live-label` — slider value interpolated into label.
- `seat-grid-picker` — 2D button grid with state-color switch.
- `language-picker-i18n` — `_language` reactive + `T()` helper + Select.
- `quick-edit-find-replace` — fast find/replace via Haiku for cosmetic tweaks.

## Multi-user & games

- `multi-user-game` — shared + per-client state mix.
- `live-quiz-countdown-loop` — countdown round flow with reveal.
- `host-and-player-dual-roles` — host vs player flows in same app.
- `qr-join-with-host-flag` — QR join + ClientParameters host flag.
- `batched-turn-window` — wait-for-everyone countdown that pauses on typing.
- `client-user-binding` — bind ClientId to UserId for persistent identity.
- `leaderboard-row-with-colored-stats` — clickable row with threshold-tinted stats.

## Layout & navigation

- `three-pane-desktop-layout` — shrink-0 rails + min-w-0 center.
- `collapsible-sidebar-nav` — icon rail toggling between wide and narrow.
- `bottom-tab-bar-nav` — mobile-style flex-shrink-0 bottom tabs.
- `right-panel-tabs` — tabbed side panel with resize handle.
- `right-rail-tabs-with-attention-dots` — stable container, per-tab unread dots.
- `slide-in-side-panel` — backdrop + animated drawer.
- `dashboard-with-panels-crud` — panel CRUD on a dashboard.
- `cinematic-fullscreen-scene` — fullscreen image + 3-gradient stack + corner HUD.
- `role-based-screen-router` — AppScreen enum + per-role nav buttons.
- `url-path-route-state` — route state encoded in URL path.

## State machines

- `state-machine-cards-and-transitions` — states tab + transitions tab driven by status enum.
- `time-scrubber-replay` — scrub through time with snapshot decoding.
- `undo-redo-cursor-history` — Cursor + History list + DraftCode.
- `saved-runs-sidebar` — CloudJson index of past runs.

## Visualization

> For pie / bar / line charts use the first-class components — see the **charts** guide (`view.PieChart`, `view.BarChart`, `view.LineChart`). The list pattern below is only a tiny inline sparkline.

- `kpi-card-grid` — responsive 2/4-col grid of headline-number cards.
- `bar-chart-from-list` — tiny inline bar SPARKLINE from a list (no axes/legend); for a real bar chart use `view.BarChart` (charts guide).
- `themed-weekly-progress` — weekly/periodic progress chart in the app's BRAND accent with full period structure (empty days as muted track marks, never a lone bar in a void).
- `score-bar-meter` — 0-10 bar with red/amber/emerald threshold coloring.
- `server-side-svg-visual` — custom SVG ring / gauge / arc / dial / donut / radial-orbital built as a C# string and shown with `view.Image`; single-quoted attributes avoid the C# quote-escaping spiral.
- `dialog-stat-card-grid` — detail modal with conditional-color stat tiles.
- `tactical-map-markers` — TacticalMap with derived markers/paths.
- `map-layer-toggle-panel` — checkbox-driven map layer visibility.

## Status & feedback

- `toast-notifications` — tone-coded auto-dismissing pill, age-based.
- `connection-status-pill` — most-urgent-wins precedence over reactives.
- `status-badge-from-enum` — switch-expression mapping enum → badge style.
- `alert-banner-dismissable` — pulsing top-banner with age counter + buttons.
- `colorized-activity-log` — color-coded log lines with auto-scroll.
- `weighted-progress-banner` — two-phase fixed-weight progress bar.
- `retry-with-status-text` — exponential-backoff with progress label.
- `playful-loading-text-rotator` — rotating waiting messages.
- `relative-time-with-i18n-buckets` — bucketed "5 min ago" via T(key, args).

## Theming & polish

- `theme-commitment` — adaptive vs fixed scheme: the choice and its emission.
- `status-pill` — theme-safe chips/badges/tags (the classic dark-mode defect class).
- `depth-and-atmosphere` — card/panel depth recipes (tint step, soft shadow, brand-tinted shadow, accent gradient) instead of flat boxed-in borders.
- `signature-moment` — one hero header + one designed celebration state + display-type scale jumps; the antidote to "typographic flatness" and forgettable apps.
- `inline-icon-text` — Row-composed text + view.Icon for sentences that need a pictograph; the emoji-as-icon defect exists because an icon can't live inside a Text string.

## Modals & overlays

- `destructive-confirm-dialog` — id-driven confirm modal.
- `plan-approval-modal` — agent-pause modal with Refine / Approve / Restart.
- `ai-suggestion-confirm-dismiss` — non-modal AI banner with confirm/dismiss.

## Performance & async

- `single-processor-channel-queue` — Channel + single Reader for sequential async.
- `parallel-extract-and-reply` — Task.WhenAll over independent extractions.
- `background-processing-pipeline` — fire-and-forget Task.Run with captured locals.
- `idle-driven-llm-action-loop` — fire AI when client goes idle.
- `threshold-driven-regenerate` — bucketed signal triggers regen on bucket-cross.
- `screenshot-critique-loop` — screenshot → LLM critique → re-edit.

## Platform mechanics

- `custom-react-node-embed` — `view.AddNode("custom.foo", ...)` for real React components.
- `persistent-user-preferences` — when to use PersistentUserReactive vs alternatives.
- `mission-toml-paired-with-markdown` — agent mission spec format.
- `nl-to-csharp-script-execution` — LLM-generated C# scripts evaluated at runtime.
- `global-keyboard-shortcuts` — KeyboardListener at root + Esc-stack ordering.
- `live-profile-extraction-side-panel` — extract entity facts from chat into side panel.
- `llm-pairwise-matchmaker` — LLM picks the best pair from N candidates.
- `agent-roster-card-grid` — responsive grid of toggle-able persona cards.
- `lora-stack-with-weight-sliders` — toggleable items with conditional sliders.
- `onboarding-name-capture` — single-field full-screen first-run greeter.
- `avatar-with-initials-fallback` — image-or-pulsing-box async avatar slot.
- `copy-and-share-action-row` — floating ActionButton row with copy + share.
- `file-upload-with-progress` — drag-drop + per-file progress tracker.

## Adding patterns

Drop a `.md` file in `platform-dotnet/Ikon.Agent/docs/patterns/`. The csproj wildcard auto-picks it up; rebuilding regenerates resources, and `ikon app docs` re-distributes patterns into every scaffolded app's `docs/guides/patterns/`.

Each pattern file:

```markdown
<!-- mined-from: <SourceApp> -->     ← optional, for mined patterns
# Title — Short Hook

One-paragraph description.

## When to use
Trigger conditions in 1-3 sentences.

## Snippet
```csharp
[30-60 lines real code]
```

## Notes
- Subtle behaviour, common pitfalls, alternatives.

## See also
- `related-pattern` — one-line description.
```

Patterns are NOT exhaustive examples — they're the smallest snippet that captures a shape. The Coder agent composes multiple patterns into one app. Apps are unique mixtures; patterns are the shared atoms.
