<!-- every pattern file must be listed here — PatternIndexTests enforces it -->
# Pattern Index

70 focused 30-60 LOC code patterns mined from production Ikon AI Apps. Coder agents call `guide("<name> pattern")` to retrieve any of them. Planner names them in the PATTERNS section of every plan; Coder retrieves and adapts as the structural skeleton. Together they replace prose criteria — the corpus is the criteria.

## Foundations (start here)

- `typical-app-structure` — the skeleton every Ikon AI App starts from.
- `busy-flag-loading` — any async work with visible feedback (Reactive bool + try/catch + label change).

## Conversational AI

- `chatbot-streaming` — single-LLM transcript with busy flag.
- `chat-with-tool-calls` — streaming chat handling LLM tool-call event loop.
- `streaming-agent-status` — live status block driven by an `AgentThread`'s own reactives (Status/Activity/ToolCallTimeline/Usage).
- `quick-reply-options-from-llm` — `<ask>...<option>` pills from LLM output.
- `clickable-reference-card-in-chat` — LLM tool emits clickable detail cards.
- `orchestrator-thread-with-tools` — Orchestrator + Persona + tool-providing Skill.
- `embedded-coding-agent-tools` — agent that writes/edits code as the user.
- `plan-then-code-iteration` — plan → revise → execute agentic loop.
- `role-tagged-transcript-feed` — per-speaker styling driven from one role enum.
- `message-action-row` — per-message reply/copy/edit/delete that stays reachable on touch.

## Voice & audio

- `voice-loop` — mic capture → STT → LLM → TTS playback.
- `push-to-talk-button` — CaptureButton in Hold mode with active styling.

## Image & video

- `annotation-draw-overlay` — drawing layer over an image.
- `llm-vision-cache` — cache vision LLM responses keyed by image hash.
- `inline-image-marker-extraction` — `[vehicle:ID]` tokens parsed out of LLM output.

## Web & data

- `web-research` — WebSearcher + multi-stage AI with sources.
- `mapreduce-long-document-summary` — chunk → per-chunk summary → combine.
- `cloud-json-asset-store` — CloudJson asset CRUD pattern.

## Lists & collections

- `board-move-without-drag` — the per-card Move menu that makes a board operable without a pointer.
- `record-list-toolbar` — search / sort / date-range / export strip over a collection, driven by a derived query.
- `shared-list-ai-cleanup` — collection + AI transform button.
- `inline-list-cell-edit` — type-to-save TextField inside a list row.
- `tree-with-expand-collapse` — recursive tree with expanded-set state.

## Forms & input

- `form-field-discipline` — persistent labels, errors that survive a failed submit, review before a consequential action.
- `ai-prefill-form-from-description` — LLM extracts structured fields from prose.

## Device & sensors

- `device-motion-stream` — accelerometer/gyroscope batches for gait, cadence and impact.
- `offline-recording-archive` — the device records its own fixes so a tunnel costs nothing.
- `lock-screen-live-activity` — the iOS banner that updates with the app closed.

## Multi-user & games

- `multi-user-game` — shared + per-client state mix.
- `live-quiz-countdown-loop` — countdown round flow with reveal.
- `host-and-player-dual-roles` — host vs player flows in same app.
- `batched-turn-window` — wait-for-everyone countdown that pauses on typing.
- `client-user-binding` — bind ClientId to UserId for persistent identity.

## Layout & navigation

- `nav-and-menu-rows` — page tabs, sidebar rows and menu rows: the token each takes, and why navigation is not a row of buttons.
- `collapsible-sidebar-nav` — icon rail toggling between wide and narrow.
- `right-rail-tabs-with-attention-dots` — stable container, per-tab unread dots.
- `slide-in-side-panel` — backdrop + animated drawer.
- `url-path-route-state` — route state encoded in URL path.
- `command-palette-jump` — Cmd+K jump and action menu.

## State machines

- `undo-redo-cursor-history` — Cursor + History list + DraftCode.
- `saved-runs-sidebar` — CloudJson index of past runs.

## Visualization

> For pie / bar / line charts use the first-class components — see the **charts** guide (`view.PieChart`, `view.BarChart`, `view.LineChart`).

- `chart-for-the-question` — matching line / bar / pie / table to what the surface is being asked, and what to do when the platform ships no such chart.
- `server-side-svg-visual` — custom SVG ring / gauge / arc / dial / donut / radial-orbital built as a C# string and shown with `view.Image`; single-quoted attributes avoid the C# quote-escaping spiral.
- `emergence-event-feed` — color-coded log of `Emerge.Run` events.

## Status & feedback

- `toast-notifications` — tone-coded auto-dismissing pill, age-based.
- `connection-status-pill` — most-urgent-wins precedence over reactives.
- `notify-across-channels` — inbox, push, email and SMS from one route, with quiet hours.
- `zero-results-state` — the three different empty screens (nothing yet / nothing matches / nothing loaded).

## Theming & polish

- `theme-commitment` — adaptive vs fixed scheme: the choice and its emission.
- `status-pill` — theme-safe chips/badges/tags (the classic dark-mode defect class).
- `depth-and-atmosphere` — card/panel depth recipes (tint step, soft shadow, brand-tinted shadow, accent gradient) instead of flat boxed-in borders.

## Modals & overlays

- `overlay-selection` — choosing modal vs drawer vs popover vs tooltip vs toast by task depth.
- `destructive-confirm-dialog` — id-driven confirm modal.

## Performance & async

- `single-processor-channel-queue` — Channel + single Reader for sequential async.
- `parallel-extract-and-reply` — Task.WhenAll over independent extractions.
- `background-processing-pipeline` — fire-and-forget Task.Run with captured locals.
- `threshold-driven-regenerate` — bucketed signal triggers regen on bucket-cross.
- `screenshot-critique-loop` — screenshot → LLM critique → re-edit.
- `client-autosave-loop` — per-client background save on an interval.
- `refine-with-validation-loop` — LLM self-fixes compile errors via Emerge.Refine.

## Platform mechanics

- `endpoint-and-mcp-tool` — HTTP endpoints, webhooks and agent tools, and the three authorizations that separate them.
- `custom-react-node-embed` — `view.AddNode("custom.foo", ...)` for real React components.
- `persistent-user-preferences` — when to use PersistentUserReactive vs alternatives.
- `global-keyboard-shortcuts` — KeyboardListener at root + Esc-stack ordering.
- `live-profile-extraction-side-panel` — extract entity facts from chat into side panel.
- `copy-and-share-action-row` — floating ActionButton row with copy + share.
- `file-upload-with-progress` — drag-drop + per-file progress tracker.
- `virtual-file-tool-set` — sandboxed FS tools for coding agents.

## Adding patterns

Drop a `.md` file in `platform-dotnet/Ikon.Agent.Docs/docs/patterns/`. The csproj wildcard auto-picks it up; rebuilding regenerates resources, and `ikon app docs` re-distributes patterns into every scaffolded app's `docs/Ikon.Agent.Docs/patterns/`.

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
